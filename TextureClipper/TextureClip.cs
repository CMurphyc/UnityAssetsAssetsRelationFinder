#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Reflection;
using System;
using System.Linq;
using System.Text.RegularExpressions;

public class TextureClip : MonoBehaviour
{
    public int leftTopX;
    public int leftTopY;
    public int clipWidth;
    public int clipHeight;
    public string clipFolder;
    public void ApplyClipInFolder()
    {
        string path = clipFolder.Substring(clipFolder.IndexOf("Assets"));
        if (!string.IsNullOrEmpty(path))
        {
            string directoryPath = "";
            if (!Path.HasExtension(path))
            {
                Debug.Log("选择到了文件夹：" + path);
                directoryPath = path;
            }
            else
            {
                Debug.Log("选择到了文件：" + path);
                string directoryName = Path.GetDirectoryName(path);
                Debug.Log("directoryName：" + directoryName);
                if (!string.IsNullOrEmpty(directoryName))
                {
                    directoryPath = directoryName;
                }
            }
            if (!string.IsNullOrEmpty(directoryPath))
            {
                if (EditorUtility.DisplayDialog("提示！", "是否确定执行文件夹下所有角色贴图裁剪操作？", "确认", "取消"))
                {
                    DirectoryInfo dictInfo = new DirectoryInfo(directoryPath);
                    FileInfo[] allFiles = dictInfo.GetFiles();
                    int count = allFiles.Length;
                    int curCount = 0;
                    try
                    {
                        foreach (var file in allFiles)
                        {
                            curCount++;
                            if (file.FullName.Contains(".png") || file.FullName.Contains(".tga"))
                            {
                                EditorUtility.DisplayProgressBar("裁剪贴图中...", file.Name, (float)curCount / count);
                                string assetPath = file.FullName.Substring(file.FullName.IndexOf("Assets"));
                                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                                if (texture != null)
                                {
                                    // TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                                    // bool reImport = false;
                                    // TextureImporterNPOTScale originNPOT = ti.npotScale;
                                    // if (originNPOT != TextureImporterNPOTScale.None)
                                    // {
                                    //     ti.npotScale = TextureImporterNPOTScale.None;
                                    //     reImport = true;
                                    // } 
                                    // bool originReadable = ti.isReadable;
                                    // if (!originReadable)
                                    // {
                                    //     ti.isReadable = true;
                                    //     reImport = true;
                                    // }
                                    // if (reImport) AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(texture));
                                    Clip(texture, assetPath, file.Name); 
                                    // if (ti.npotScale != originNPOT)
                                    // {
                                    //     ti.npotScale = originNPOT;
                                    // }
                                    // if (ti.isReadable != originReadable)
                                    // {
                                    //     ti.isReadable = originReadable;
                                    // }
                                    // if (reImport) AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(texture));
                                }
                            }
                        }
                        EditorUtility.ClearProgressBar();
                    } 
                    catch (Exception)
                    {
                        EditorUtility.ClearProgressBar();
                    } 
                }
            }
        }        
    }

    public void Clip(Texture2D pic, string path, string pngName)
    {
        // var sourceRegion = new RectInt(pic.width / 2 - clipWidth / 2, pic.height / 2 - clipHeight / 2, clipWidth, clipHeight);
        var sourceRegion = new RectInt(leftTopX, pic.height - leftTopY - clipHeight, clipWidth, clipHeight);
        var targetRegion = new RectInt(0, 0, clipWidth, clipHeight);

        Texture2D target = ApplyClip(pic, sourceRegion, targetRegion);
        byte[] data = target.EncodeToPNG();
        string suffix = @"(" + ".png" + ")" + "$";
        string newPicPath = Regex.Replace(path, suffix, "") + "_new.png";  
        System.IO.File.WriteAllBytes(newPicPath, data);
    }

    Texture2D ApplyClip(Texture2D pic, RectInt sourceRegion, RectInt targetRegion)
    {
        Color[] colors = pic.GetPixels(sourceRegion.x, sourceRegion.y, targetRegion.width, targetRegion.height);
        Texture2D target = new Texture2D(targetRegion.width, targetRegion.height, TextureFormat.ARGB32, false);
        target.SetPixels(0, 0, targetRegion.width, targetRegion.height, colors);
        return target;
    }
}

#endif