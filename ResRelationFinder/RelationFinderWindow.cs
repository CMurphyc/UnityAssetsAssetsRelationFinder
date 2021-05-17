using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

public class RelationFinderWindow : EditorWindow
{
    // 查询模式
    public enum BaseFinderMode
    {
        ReferenceFinderMode = 0,
        DependenceFinderMode = 1,
        MissingRefFinderMode = 2,
        Other = 3,
    }
    private static ResRelationDataSet resData = new ResRelationDataSet();
    private static bool isDataLoaded = false;
    private BaseFinderMode FinderMode = BaseFinderMode.ReferenceFinderMode; 
    private List<string> selectedResourceGuid = new List<string>();    
    private HashSet<string> updatedResourceGuid = new HashSet<string>();
    private RelationTreeView treeView = null;
    private bool canTreeBuild = true; // 关系树是否可以构造

    [SerializeField]
    private TreeViewState treeViewState;
    
    //查找资源引用信息
    [MenuItem("Assets/查询资源依赖·引用关系", false, 25)]
    public static void OnOpenFinderWindow()
    {
        DataInit();
        OpenWindow();
        RelationFinderWindow window = GetWindow<RelationFinderWindow>();
        window.RefreshResourceInfo();
    }
    
    //打开窗口
    [MenuItem("Window/依赖&引用关系查询r", false, 1000)]
    public static void OpenWindow()
    {
        RelationFinderWindow window = GetWindow<RelationFinderWindow>();
        window.wantsMouseMove = false;
        window.titleContent = new GUIContent("依赖&引用关系查询页");
        window.Show();
        window.Focus();        
    }

    private static void DataInit()
    {
        if(!isDataLoaded)
        {
            if(!resData.LoadRelationData())
            {
                resData.RefreshData();
            }
            isDataLoaded = true;
        }
    }
    
    private void RefreshResourceInfo()
    {
        selectedResourceGuid.Clear();
        foreach(var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            string guid = AssetDatabase.AssetPathToGUID(path);
            selectedResourceGuid.Add(guid);
        }
    }

    private void BuildTree()
    {
        if(canTreeBuild && selectedResourceGuid.Count != 0)
        {
            var root = BuildRoot(selectedResourceGuid);
            if(treeView == null)
            {
                if(treeViewState == null) treeViewState = new TreeViewState();
                var headerState = RelationTreeView.CreateDefaultMultiColumnHeaderState(position.width);
                var multiColumnHeader = new MultiColumnHeader(headerState);
                treeView = new RelationTreeView(treeViewState, multiColumnHeader);
            }
            treeView.root = root;
            treeView.CollapseAll();
            treeView.Reload();
            // 构造完一棵树后就没必要再刷新了
            canTreeBuild = false;
        }
    }
    private void DrawToolBar()
    {
        var style = new GUIStyle("ToolbarButton"); 
        EditorGUILayout.BeginHorizontal(new GUIStyle("Toolbar"));
        if(GUILayout.Button("刷新数据", style))
        {
            resData.RefreshData();
            EditorGUIUtility.ExitGUI();
        }

        var oldMode = FinderMode;
        FinderMode = (BaseFinderMode)EditorGUILayout.EnumPopup("当前模式: ", FinderMode);
        // 切换模式后允许重新构造
        canTreeBuild = oldMode != FinderMode;
        
        EditorGUILayout.EndHorizontal();
    }

    private void RectTreeView()
    {
        if(treeView != null)
        {
            var style = new GUIStyle("Toolbar");
            treeView.OnGUI(new Rect(0, style.fixedHeight, position.width, position.height));
        }
    }
    private void OnGUI()
    {
        BuildTree();
        RectTreeView();
        DrawToolBar();
    }

    private RelationTreeViewItem BuildRoot(List<string> guids)
    {
        updatedResourceGuid.Clear();
        // 标记序列号
        int id = 0;
        var root = new RelationTreeViewItem{id = id, depth = -1, displayName = "item", data = null};
        var stack = new Stack<string>();
        foreach(var it in guids)
        {
            // 这里是构造关系树的入口，depth必然为0，作为根节点
            var child = CreateChild(it, ref id, 0, stack);
            if(child != null) root.AddChild(child);
        }
        return root;
    }
    private RelationTreeViewItem CreateChild(string guid, ref int id, int depth, Stack<string> stack)
    {
        if(stack.Contains(guid)) return null;

        stack.Push(guid);
        if(!updatedResourceGuid.Contains(guid))
        {
            resData.UpdateResourceState(guid);
            updatedResourceGuid.Add(guid);
        }

        var relationData = resData.GetRelationDataItemByGuid(guid);
        // dfs构造关系树，作为标记的唯一id依次递增生成
        var root = new RelationTreeViewItem{id = ++id, displayName = relationData.name, data = relationData, depth = depth};
        var guids = new List<string>();
        switch(FinderMode)
        {
            case BaseFinderMode.DependenceFinderMode : 
                guids = relationData.dependence;
                break;
            case BaseFinderMode.ReferenceFinderMode :
                guids = relationData.reference;
                break;
            case BaseFinderMode.MissingRefFinderMode :
                // 有空加
                break;
        }
        foreach(var childGuid in guids)
        {
            var child = CreateChild(childGuid, ref id, depth + 1, stack);
            if(child != null) root.AddChild(child);
        }
        stack.Pop();
        return root;
    }
}
