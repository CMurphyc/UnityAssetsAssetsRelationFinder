using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

public class ResRelationDataSet
{
    // 引用依赖关系本地缓存路径
    private const string RES_RELATION_DATA_PATH = "Assets/Tools/ResRelationFinder/ResourceRelationData";
    // 上一次的更新时间
    private const string LAST_UPDATE_TIME = "ManyManyYearsAgo";
    // 加载到内存的引用关系数据缓存
    public Dictionary<string, ResRelationDataItem> relationDataSet = new Dictionary<string, ResRelationDataItem>();  

    public ResRelationDataItem GetRelationDataItemByGuid(string guid)
    {
        if(relationDataSet != null) return relationDataSet[guid];
        else return null;
    }  

    public void RefreshData()
    {
        try
        {          
            LoadRelationData();
            var paths = AssetDatabase.GetAllAssetPaths();
            int len = paths.Length;
            for (int i = 0; i < len; i++)
            {
                if(File.Exists(paths[i])) RefreshTargetRefInfo(paths[i]);
                var info = string.Format("已加载{0}%", 1.0f * i / len * 100);
                // 频繁调用绘制进度条开销比较大，验证影响效率，每1000个文件调用一次目标看来比较合理
                if(i % 1000 == 0 && EditorUtility.DisplayCancelableProgressBar("数据加载中", info, 1.0f * i / len))
                {
                    return;
                }
                if(i % 1000 == 0) GC.Collect();
            }

            EditorUtility.DisplayCancelableProgressBar("数据刷新中", "正在写入本地", 1f);
            WriteToLocal();

            EditorUtility.DisplayCancelableProgressBar("数据刷新中", "正在加载引用数据", 1f);
            UpdateReferenceInfo();   
        }
        catch(Exception e)
        {
            Debug.LogWarning(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    //通过依赖信息更新引用信息
    private void UpdateReferenceInfo()
    {
        foreach(var asset in relationDataSet)
        {
            foreach(var assetGuid in asset.Value.dependence)
            {
                if(!relationDataSet[assetGuid].reference.Contains(asset.Key)) relationDataSet[assetGuid].reference.Add(asset.Key);
            }
        }
    }

    // 刷新引用缓存
    private void RefreshTargetRefInfo(string path)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        Hash128 assetDependencyHash = AssetDatabase.GetAssetDependencyHash(path);
        if(!relationDataSet.ContainsKey(guid) || relationDataSet[guid].hash != assetDependencyHash.ToString())
        {
            var guids = AssetDatabase.GetDependencies(path, false).Select(p => AssetDatabase.AssetPathToGUID(p)).ToList();

            ResRelationDataItem data = new ResRelationDataItem();
            data.name = Path.GetFileNameWithoutExtension(path);
            data.resPath = path;
            data.hash = assetDependencyHash.ToString();
            data.dependence = guids;

            if(relationDataSet.ContainsKey(guid)) relationDataSet[guid] = data;
            else relationDataSet.Add(guid, data);
        }
    }

    // 将本地硬盘相关数据加载到内存中
    public bool LoadRelationData()
    {
        relationDataSet.Clear();
        if(!File.Exists(RES_RELATION_DATA_PATH))
        {
            return false;
        }

        var guids = new List<string>();
        var hashs = new List<string>();
        var dependence = new List<int[]>();

        using (FileStream fs = File.OpenRead(RES_RELATION_DATA_PATH))
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                string lastUpdateTime = (string)bf.Deserialize(fs);
                if(lastUpdateTime != LAST_UPDATE_TIME)
                {
                    return false;
                }

                EditorUtility.DisplayCancelableProgressBar("导入引用&依赖数据中", "数据读取中", 0);
                guids = (List<string>) bf.Deserialize(fs);
                hashs = (List<string>) bf.Deserialize(fs);
                dependence = (List<int[]>) bf.Deserialize(fs);
                EditorUtility.ClearProgressBar();
            }   
            catch(Exception e)
            {
                Debug.LogWarning(e);
                return false;
            }
            finally
            {
                fs.Close();
            }
        }

        for(int i = 0; i < guids.Count; ++i)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if(!string.IsNullOrEmpty(path))
            {
                var data = new ResRelationDataItem();
                data.hash = hashs[i];
                data.name = Path.GetFileNameWithoutExtension(path);
                data.resPath = path;
                relationDataSet.Add(guids[i], data);
            }
        }

        for(int i = 0; i < guids.Count; ++i)
        {
            string guid = guids[i];
            if(relationDataSet.ContainsKey(guid))
            {
                var depGuids = dependence[i].Select(index => guids[index]).Where(g => relationDataSet.ContainsKey(g)).ToList();
                relationDataSet[guid].dependence = depGuids;
            }
        }
        UpdateReferenceInfo();
        return true;
    }

    // 将更新后的相关缓存写入本地硬盘
    private void WriteToLocal()
    {
        if (File.Exists(RES_RELATION_DATA_PATH)) File.Delete(RES_RELATION_DATA_PATH);

        var guids = new List<string>();
        var hashs = new List<string>();
        var dependence = new List<int[]>();
        var guidIndex = new Dictionary<string, int>();
        using (FileStream fs = File.OpenWrite(RES_RELATION_DATA_PATH))
        {
            try
            {
                foreach (var pair in relationDataSet)
                {
                    guidIndex.Add(pair.Key, guidIndex.Count);
                    guids.Add(pair.Key);
                    hashs.Add(pair.Value.hash);
                }

                foreach(var guid in guids)
                {
                    int[] indexes = relationDataSet[guid].dependence.
                        Where(s => guidIndex.ContainsKey(s)).
                        Select(s => guidIndex[s]).ToArray();
                    dependence.Add(indexes);
                }

                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, LAST_UPDATE_TIME);
                bf.Serialize(fs, guids);
                bf.Serialize(fs, hashs);
                bf.Serialize(fs, dependence);
            }
            catch(Exception e)
            {
                Debug.LogWarning(e);
            }
            finally
            {
                fs.Close();
            }
        }
    }
    
    // 更新文件状态
    public void UpdateResourceState(string guid)
    {
        ResRelationDataItem data;
        if(relationDataSet.TryGetValue(guid,out data))
        {            
            if(File.Exists(data.resPath))
            {
                if (data.hash != AssetDatabase.GetAssetDependencyHash(data.resPath).ToString())
                {
                    data.state = ResState.Dirty;
                }
                else data.state = ResState.Clean;
            }
            else data.state = ResState.Missing;
        }
        else
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            data = new ResRelationDataItem();
            data.name = Path.GetFileNameWithoutExtension(path);
            data.resPath = path;
            data.state = ResState.ErrorOccur;
            relationDataSet.Add(guid, data);
        }
    }

    //获取资源当前的（修改）状态
    public static string GetInfoByState(ResState state)
    {
        return Enum.GetName(typeof(ResState), state);
    }

    public enum ResState
    {
        Clean = 0,
        Dirty = 1,
        Missing = 2,
        ErrorOccur = 3
    }

    /// <summary>
    /// 资源引用与依赖关系的单位数据
    /// </summary>
    public class ResRelationDataItem
    {
        // 该哈希是资源路径、资源、meta文件及目标平台和导入（修改）时间信息的集合
        // 该哈希的改变即预示着该资源的引用依赖关系有所改变
        public string hash;
        public string name = "";
        public string resPath = "";
        public ResState state = ResState.Clean;
        // 依赖
        public List<string> dependence = new List<string>();
        // 引用
        public List<string> reference = new List<string>();
    }

}
