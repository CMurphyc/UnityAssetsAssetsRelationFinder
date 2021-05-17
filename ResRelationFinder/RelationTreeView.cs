using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;

//带数据的TreeViewItem
public class RelationTreeViewItem : TreeViewItem
{
    public ResRelationDataSet.ResRelationDataItem data;
}

public class RelationTreeView : TreeView
{
    public RelationTreeViewItem root;
    //列信息
    enum InfoType
    {
        Name,
        Path,
        State,
    }

    public RelationTreeView(TreeViewState state,MultiColumnHeader multicolumnHeader):base(state,multicolumnHeader)
    {
        extraSpaceBeforeIconAndLabel = 20;
        columnIndexForTreeFoldouts = 0;
        showAlternatingRowBackgrounds = true;
    }

    //响应双击事件
    protected override void DoubleClickedItem(int id)
    {
        var item = (RelationTreeViewItem)FindItem(id, rootItem);
        if (item != null)
        {
            // 高亮选中目标资源
            var assetObject = AssetDatabase.LoadAssetAtPath(item.data.resPath, typeof(UnityEngine.Object));
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = assetObject;
            EditorGUIUtility.PingObject(assetObject);
        }
    }
    
    public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
    {
        var columns = new[]
        {
            //图标+名称
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("资源名"),
                headerTextAlignment = TextAlignment.Center,
                sortedAscending = false,
                width = 350,
                minWidth = 60,
                canSort = false        
            },
            //路径
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("路径"),
                headerTextAlignment = TextAlignment.Center,
                sortedAscending = false,
                width = 450,
                minWidth = 60,
                canSort = false
    },
            //状态
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("文件状态"),
                headerTextAlignment = TextAlignment.Center,
                sortedAscending = false,
                width = 60,
                minWidth = 60,
                canSort = false          
            },
        };
        var state = new MultiColumnHeaderState(columns);
        return state;
    }

    protected override TreeViewItem BuildRoot()
    {
        return root;
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        var item = (RelationTreeViewItem)args.item;
        for(int i = 0; i < args.GetNumVisibleColumns(); ++i)
        {
            var type = (InfoType)args.GetColumn(i);
            DrawCell(type, item, args.GetCellRect(i), ref args);
        }
    }

    private void DrawCell(InfoType type, RelationTreeViewItem item, Rect rect, ref RowGUIArgs args)
    {
        CenterRectUsingSingleLineHeight(ref rect);
        if(type == InfoType.Name)
        {
            var typeIconRect = rect;
            // 缩进，目前先写死，应该没啥影响
            typeIconRect.x += GetContentIndent(item);
            typeIconRect.width = 20;
            if(typeIconRect.x < rect.xMax)
            {
                if(!string.IsNullOrEmpty(item?.data?.resPath))
                {
                    // 刷新资源类型缩略图
                    var icon = GetThumbNailIcon(item.data.resPath);
                    if(icon != null) GUI.DrawTexture(typeIconRect, icon, ScaleMode.ScaleToFit);
                }
            }                        
            args.rowRect = rect;
            base.RowGUI(args);
        }
        else if(type == InfoType.Path) GUI.Label(rect, item.data.resPath);
        else if(type == InfoType.State)
        {
            var style = new GUIStyle {richText = true, alignment = TextAnchor.MiddleCenter};
            GUI.Label(rect, ResRelationDataSet.GetInfoByState(item.data.state), style);
        }
    }

    //获取资源类型缩略图
    private Texture2D GetThumbNailIcon(string path)
    {
        // assets文件不是unity内置文件类型，没有缩略图
        var fileTypeSuffix = Path.GetExtension(path);
        if(fileTypeSuffix.Equals(".asset")) return null;

        Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
        if (obj != null)
        {
            Texture2D resTypeThumbNailIcon = AssetPreview.GetMiniTypeThumbnail(obj.GetType());
            return resTypeThumbNailIcon;
        }
        return null;
    }    
}
