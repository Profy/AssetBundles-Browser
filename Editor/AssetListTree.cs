﻿using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.IMGUI.Controls;

using UnityEngine;
//using System;


namespace AssetBundleBrowser
{
    internal class AssetListTree : TreeView
    {
        private List<AssetBundleModel.BundleInfo> m_SourceBundles = new List<AssetBundleModel.BundleInfo>();
        private readonly AssetBundleManageTab m_Controller;
        private readonly List<Object> m_EmptyObjectList = new List<Object>();

        internal static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColumns());
        }
        private static MultiColumnHeaderState.Column[] GetColumns()
        {
            var retVal = new MultiColumnHeaderState.Column[] {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
            };
            retVal[0].headerContent = new GUIContent("Asset", "Short name of asset. For full name select asset and see message below");
            retVal[0].minWidth = 50;
            retVal[0].width = 100;
            retVal[0].maxWidth = 300;
            retVal[0].headerTextAlignment = TextAlignment.Left;
            retVal[0].canSort = true;
            retVal[0].autoResize = true;

            retVal[1].headerContent = new GUIContent("Bundle", "Bundle name. 'auto' means asset was pulled in due to dependency");
            retVal[1].minWidth = 50;
            retVal[1].width = 100;
            retVal[1].maxWidth = 300;
            retVal[1].headerTextAlignment = TextAlignment.Left;
            retVal[1].canSort = true;
            retVal[1].autoResize = true;

            retVal[2].headerContent = new GUIContent("Size", "Size on disk");
            retVal[2].minWidth = 30;
            retVal[2].width = 75;
            retVal[2].maxWidth = 100;
            retVal[2].headerTextAlignment = TextAlignment.Left;
            retVal[2].canSort = true;
            retVal[2].autoResize = true;

            retVal[3].headerContent = new GUIContent("!", "Errors, Warnings, or Info");
            retVal[3].minWidth = 16;
            retVal[3].width = 16;
            retVal[3].maxWidth = 16;
            retVal[3].headerTextAlignment = TextAlignment.Left;
            retVal[3].canSort = true;
            retVal[3].autoResize = false;

            return retVal;
        }

        private enum MyColumns
        {
            Asset,
            Bundle,
            Size,
            Message
        }
        internal enum SortOption
        {
            Asset,
            Bundle,
            Size,
            Message
        }

        private readonly SortOption[] m_SortOptions =
        {
            SortOption.Asset,
            SortOption.Bundle,
            SortOption.Size,
            SortOption.Message
        };

        internal AssetListTree(TreeViewState state, MultiColumnHeaderState mchs, AssetBundleManageTab ctrl) : base(state, new MultiColumnHeader(mchs))
        {
            m_Controller = ctrl;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }


        internal void Update()
        {
            bool dirty = false;
            foreach (var bundle in m_SourceBundles)
            {
                dirty |= bundle.Dirty;
            }
            if (dirty)
            {
                Reload();
            }
        }
        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }


        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            SortIfNeeded(root, rows);
            return rows;
        }

        internal void SetSelectedBundles(IEnumerable<AssetBundleModel.BundleInfo> bundles)
        {
            m_Controller.SetSelectedItems(null);
            m_SourceBundles = bundles.ToList();
            SetSelection(new List<int>());
            Reload();
        }
        protected override TreeViewItem BuildRoot()
        {
            var root = AssetBundleModel.Model.CreateAssetListTreeView(m_SourceBundles);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), args.item as AssetBundleModel.AssetTreeItem, args.GetColumn(i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, AssetBundleModel.AssetTreeItem item, int column, ref RowGUIArgs args)
        {
            Color oldColor = GUI.color;
            CenterRectUsingSingleLineHeight(ref cellRect);
            if (column != 3)
            {
                GUI.color = item.ItemColor;
            }

            switch (column)
            {
                case 0:
                    {
                        var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                        if (item.icon != null)
                        {
                            GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);
                        }

                        DefaultGUI.Label(
                            new Rect(cellRect.x + iconRect.xMax + 1, cellRect.y, cellRect.width - iconRect.width, cellRect.height),
                            item.displayName,
                            args.selected,
                            args.focused);
                    }
                    break;
                case 1:
                    DefaultGUI.Label(cellRect, item.Asset.BundleName, args.selected, args.focused);
                    break;
                case 2:
                    DefaultGUI.Label(cellRect, item.Asset.GetSizeString(), args.selected, args.focused);
                    break;
                case 3:
                    var icon = item.MessageIcon();
                    if (icon != null)
                    {
                        var iconRect = new Rect(cellRect.x, cellRect.y, cellRect.height, cellRect.height);
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                    }
                    break;
            }
            GUI.color = oldColor;
        }

        protected override void DoubleClickedItem(int id)
        {
            if (FindItem(id, rootItem) is AssetBundleModel.AssetTreeItem assetItem)
            {
                Object o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.Asset.FullAssetName);
                EditorGUIUtility.PingObject(o);
                Selection.activeObject = o;
            }
        }

        public void SetSelection(List<string> paths)
        {
            List<int> selected = new List<int>();
            AddIfInPaths(paths, selected, rootItem);
            SetSelection(selected);
        }

        private void AddIfInPaths(List<string> paths, List<int> selected, TreeViewItem me)
        {
            if (me is AssetBundleModel.AssetTreeItem assetItem && assetItem.Asset != null)
            {
                if (paths.Contains(assetItem.Asset.FullAssetName))
                {
                    if (selected.Contains(me.id) == false)
                    {
                        selected.Add(me.id);
                    }
                }
            }

            if (me.hasChildren)
            {
                foreach (TreeViewItem item in me.children)
                {
                    AddIfInPaths(paths, selected, item);
                }
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null)
            {
                return;
            }

            List<Object> selectedObjects = new List<Object>();
            List<AssetBundleModel.AssetInfo> selectedAssets = new List<AssetBundleModel.AssetInfo>();
            foreach (var id in selectedIds)
            {
                if (FindItem(id, rootItem) is AssetBundleModel.AssetTreeItem assetItem)
                {
                    Object o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.Asset.FullAssetName);
                    selectedObjects.Add(o);
                    Selection.activeObject = o;
                    selectedAssets.Add(assetItem.Asset);
                }
            }
            m_Controller.SetSelectedItems(selectedAssets);
            Selection.objects = selectedObjects.ToArray();
        }
        protected override bool CanBeParent(TreeViewItem item)
        {
            return false;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            args.draggedItemIDs = GetSelection();
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = m_EmptyObjectList.ToArray();
            List<AssetBundleModel.AssetTreeItem> items =
                new List<AssetBundleModel.AssetTreeItem>(args.draggedItemIDs.Select(id => FindItem(id, rootItem) as AssetBundleModel.AssetTreeItem));
            DragAndDrop.paths = items.Select(a => a.Asset.FullAssetName).ToArray();
            DragAndDrop.SetGenericData("AssetListTreeSource", this);
            DragAndDrop.StartDrag("AssetListTree");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (IsValidDragDrop())
            {
                if (args.performDrop)
                {
                    AssetBundleModel.Model.MoveAssetToBundle(DragAndDrop.paths, m_SourceBundles[0].m_Name.BundleName, m_SourceBundles[0].m_Name.Variant);
                    AssetBundleModel.Model.ExecuteAssetMove();
                    foreach (var bundle in m_SourceBundles)
                    {
                        bundle.RefreshAssetList();
                    }
                    m_Controller.UpdateSelectedBundles(m_SourceBundles);
                }
                return DragAndDropVisualMode.Copy;//Move;
            }

            return DragAndDropVisualMode.Rejected;
        }
        protected bool IsValidDragDrop()
        {
            //can't do drag & drop if data source is read only
            if (AssetBundleModel.Model.DataSource.IsReadOnly())
            {
                return false;
            }

            //can't drag onto none or >1 bundles
            if (m_SourceBundles.Count is 0 or > 1)
            {
                return false;
            }

            //can't drag nothing
            if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0)
            {
                return false;
            }

            //can't drag into a folder
            if (m_SourceBundles[0] is AssetBundleModel.BundleFolderInfo)
            {
                return false;
            }

            if (m_SourceBundles[0] is not AssetBundleModel.BundleDataInfo data)
            {
                return false; // this should never happen.
            }

            if (DragAndDrop.GetGenericData("AssetListTreeSource") is AssetListTree)
            {
                return false;
            }

            if (data.IsEmpty())
            {
                return true;
            }

            if (data.IsSceneBundle)
            {
                foreach (var assetPath in DragAndDrop.paths)
                {
                    if ((AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(SceneAsset)) &&
                        (!AssetDatabase.IsValidFolder(assetPath)))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (var assetPath in DragAndDrop.paths)
                {
                    if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(SceneAsset))
                    {
                        return false;
                    }
                }
            }

            return true;

        }

        protected override void ContextClickedItem(int id)
        {
            if (AssetBundleModel.Model.DataSource.IsReadOnly())
            {
                return;
            }

            List<AssetBundleModel.AssetTreeItem> selectedNodes = new List<AssetBundleModel.AssetTreeItem>();
            foreach (var nodeID in GetSelection())
            {
                selectedNodes.Add(FindItem(nodeID, rootItem) as AssetBundleModel.AssetTreeItem);
            }

            if (selectedNodes.Count > 0)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove asset(s) from bundle."), false, RemoveAssets, selectedNodes);
                menu.ShowAsContext();
            }

        }

        private void RemoveAssets(object obj)
        {
            var selectedNodes = obj as List<AssetBundleModel.AssetTreeItem>;
            var assets = new List<AssetBundleModel.AssetInfo>();
            //var bundles = new List<AssetBundleModel.BundleInfo>();
            foreach (var node in selectedNodes)
            {
                if (!System.String.IsNullOrEmpty(node.Asset.BundleName))
                {
                    assets.Add(node.Asset);
                }
            }
            AssetBundleModel.Model.MoveAssetToBundle(assets, string.Empty, string.Empty);
            AssetBundleModel.Model.ExecuteAssetMove();
            foreach (var bundle in m_SourceBundles)
            {
                bundle.RefreshAssetList();
            }
            m_Controller.UpdateSelectedBundles(m_SourceBundles);
            //ReloadAndSelect(new List<int>());
        }

        protected override void KeyEvent()
        {
            if (m_SourceBundles.Count > 0 && Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
                List<AssetBundleModel.AssetTreeItem> selectedNodes = new List<AssetBundleModel.AssetTreeItem>();
                foreach (var nodeID in GetSelection())
                {
                    selectedNodes.Add(FindItem(nodeID, rootItem) as AssetBundleModel.AssetTreeItem);
                }

                RemoveAssets(selectedNodes);
            }
        }

        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SortIfNeeded(rootItem, GetRows());
        }

        private void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return;
            }

            SortByColumn();

            rows.Clear();
            for (int i = 0; i < root.children.Count; i++)
            {
                rows.Add(root.children[i]);
            }

            Repaint();
        }

        private void SortByColumn()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
            {
                return;
            }

            List<AssetBundleModel.AssetTreeItem> assetList = new List<AssetBundleModel.AssetTreeItem>();
            foreach (var item in rootItem.children)
            {
                assetList.Add(item as AssetBundleModel.AssetTreeItem);
            }
            var orderedItems = InitialOrder(assetList, sortedColumns);

            rootItem.children = orderedItems.Cast<TreeViewItem>().ToList();
        }

        private IOrderedEnumerable<AssetBundleModel.AssetTreeItem> InitialOrder(IEnumerable<AssetBundleModel.AssetTreeItem> myTypes, int[] columnList)
        {
            SortOption sortOption = m_SortOptions[columnList[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(columnList[0]);
            return sortOption switch
            {
                SortOption.Asset => myTypes.Order(l => l.displayName, ascending),
                SortOption.Size => myTypes.Order(l => l.Asset.fileSize, ascending),
                SortOption.Message => myTypes.Order(l => l.HighestMessageLevel(), ascending),
                _ => myTypes.Order(l => l.Asset.BundleName, ascending),
            };
        }
    }

    internal static class MyExtensionMethods
    {
        internal static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, System.Func<T, TKey> selector, bool ascending)
        {
            return ascending ? source.OrderBy(selector) : source.OrderByDescending(selector);
        }

        internal static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, System.Func<T, TKey> selector, bool ascending)
        {
            return ascending ? source.ThenBy(selector) : source.ThenByDescending(selector);
        }
    }
}
