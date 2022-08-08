using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using ImGuiNET;
using Toolbox.Core;
using OpenTK.Input;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MapStudio.UI
{
    public class Outliner : DockWindow
    {
        public override string Name => "OUTLINER";

        public override ImGuiWindowFlags Flags => ImGuiWindowFlags.MenuBar;

        public static IFileFormat ActiveFileFormat;

        public bool IsFocused = false;

        public List<MenuItemModel> ContextMenu = new List<MenuItemModel>();
        public static MenuItemModel NewItemContextMenu = new MenuItemModel("ADD_NEW");
        public List<MenuItemModel> FilterMenuItems = new List<MenuItemModel>();

        public List<NodeBase> Nodes = new List<NodeBase>();
        public List<NodeBase> SelectedNodes = new List<NodeBase>();

        public NodeBase SelectedNode => SelectedNodes.LastOrDefault();

        public void AddSelection(NodeBase node) {
            SelectedNodes.Add(node);
            SelectionChanged?.Invoke(node, EventArgs.Empty);
        }

        public void RemoveSelection(NodeBase node) {
            SelectedNodes.Remove(node);
            SelectionChanged?.Invoke(node, EventArgs.Empty);
        }

        static NodeBase dragDroppedNode;

        const float RENAME_DELAY_TIME = 0.5f;
        const bool RENAME_ENABLE = false;

        /// <summary>
        /// Gets the currently dragged/dropped node from the outliner.
        /// If a node is dropped onto a control, this is used to get the data.
        /// </summary>
        /// <returns></returns>
        public static NodeBase GetDragDropNode()
        {
            return dragDroppedNode;
        }

        public EventHandler SelectionChanged;
        public EventHandler BeforeDrawCallback;

        //Rename handling
        private NodeBase renameNode;
        private bool isNameEditing = false;
        private string renameText;
        private double renameClickTime;

        public bool ShowWorkspaceFileSetting = true;

        public static bool AddToActiveWorkspace = true;

        public bool ClipNodes = true;

        //Search handling
        public bool ShowSearchBar = true;
        private bool isSearch = false;
        private string _searchText = "";

        private float ItemHeight;

        //Scroll handling
        public float ScrollX;
        public float ScrollY;

        bool updateScroll = false;

        //Selection range
        private bool SelectRange;

        private int SelectedIndex;
        private int SelectedRangeIndex;

        public Outliner() {
            SelectionChanged += delegate
            {
                if (SelectedNode == null)
                    return;

                if (SelectedNode.Tag is STBone) {
                    Runtime.SelectedBoneIndex = ((STBone)SelectedNode.Tag).Index;
                }
                else
                    Runtime.SelectedBoneIndex = -1;
                GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
            };
        }

        public void UpdateScroll(float scrollX, float scrollY)
        {
            ScrollX = scrollX;
            ScrollY = scrollY;
            updateScroll = true;
        }

        public void ScrollToSelected(NodeBase target)
        {
            if (target == null) //todo check visiblity.
                return;

            //Expand parents if necessary
            target.ExpandParent();

            //Calculate position node is at.
            float pos = 0;
            foreach (var node in Nodes)
            {
                if (GetNodePosition(target, node, ref pos, ItemHeight))
                    break;
            }

            ScrollY = pos;
            updateScroll = true;
        }

        private bool GetNodePosition(NodeBase target, NodeBase parent, ref float pos, float itemHeight)
        {
            bool HasText = parent.Header != null &&
              parent.Header.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;

            //Search is active and node is found but is not in results so skip scrolling
            if (isSearch && parent == target && !HasText)
                return false;
            //Node is found so return
            if (parent == target) {
                return true;
            }
            //Only update results for visible nodes
            if (isSearch && HasText || !isSearch)
                pos += itemHeight;
            if (parent.IsExpanded)
            {
                foreach (var child in parent.Children) {
                    if (GetNodePosition(target, child, ref pos, itemHeight))
                        return true;
                }
            }

            return false;
        }

        public override void Render()
        {
            BeforeDrawCallback?.Invoke(this, EventArgs.Empty);

            //Check if node is within range
            if (SelectRange)
            {
                foreach (var node in this.Nodes)
                    SelectNodeRange(node);
                SelectRange = false;
            }

            //For loading files into the existing workspace
            if (ShowWorkspaceFileSetting)
                ImGui.Checkbox($"Load files to active outliner.", ref AddToActiveWorkspace);

            if (ImGui.BeginMenuBar())
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4());

                if (ImGui.Button($"{IconManager.ADD_ICON}", new System.Numerics.Vector2(23)))
                {
                    ImGui.OpenPopup("addnew1");
                }
                if (ImGui.BeginPopup("addnew1"))
                {
                    foreach (var menuItem in ContextMenu)
                    {
                        ImGuiHelper.LoadMenuItem(menuItem);
                    }
                    ImGuiHelper.LoadMenuItem(NewItemContextMenu);
                    ImGui.EndPopup();
                }


                if (ImGui.Button($"{IconManager.FILTER_ICON}", new System.Numerics.Vector2(23))) {
                    ImGui.OpenPopup("filter1");
                }
                if (ImGui.BeginPopup("filter1"))
                {
                    foreach (var item in FilterMenuItems)
                        ImGuiHelper.LoadMenuItem(item);

                    ImGui.EndPopup();
                }
                ImGui.PopStyleColor();

                ImGuiHelper.IncrementCursorPosX(11);

                if (ShowSearchBar)
                {
                    ImGui.Text(IconManager.SEARCH_ICON.ToString());
                    ImGuiHelper.IncrementCursorPosX(11);

                    var posX = ImGui.GetCursorPosX();
                    var width = ImGui.GetWindowWidth();

                    //Span across entire outliner width
                    ImGui.PushItemWidth(width - posX);
                    if (ImGui.InputText("##search_box", ref _searchText, 200))
                    {
                        isSearch = !string.IsNullOrWhiteSpace(_searchText);
                    }
                    ImGui.PopItemWidth();
                }
                ImGui.EndMenuBar();
            }
            //Set the same header colors as hovered and active. This makes nav scrolling more seamless looking
            var active = ImGui.GetStyle().Colors[(int)ImGuiCol.Header];
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, active);
            ImGui.PushStyleColor(ImGuiCol.NavHighlight, new Vector4(0));

            ItemHeight = ImGui.GetTextLineHeightWithSpacing() + 3;

            if (ClipNodes)
            {
                foreach (var node in this.Nodes)
                    SetupNodes(node);

                int count = 0;
                foreach (var node in this.Nodes)
                    CalculateCount(node, ref count);

                ImGuiNative.igSetNextWindowContentSize(new System.Numerics.Vector2(0.0f, count * ItemHeight));
                ImGui.BeginChild("##tree_view1", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);
            }
            else
                ImGui.BeginChild("##tree_view1", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);

            IsFocused = ImGui.IsWindowFocused();

            if (updateScroll)
            {
                ImGui.SetScrollX(ScrollX);
                ImGui.SetScrollY(ScrollY);
                updateScroll = false;
            }
            else
            {
                ScrollX = ImGui.GetScrollX();
                ScrollY = ImGui.GetScrollY();
            }

            foreach (var child in Nodes)
                DrawNode(child, ItemHeight);

            //if (isSearch && Nodes.Count > 0)
             //   ImGui.TreePop();

            ImGui.EndChild();
            ImGui.PopStyleColor(2);
            /* TODO - figure out how to make right-click for add menu only work on background of outliner.
            if (ImGui.BeginPopupContextItem("##OUTLINER_POPUP", ImGuiPopupFlags.MouseButtonRight))
            {
                foreach (var menuItem in ContextMenu)
                {
                    ImGuiHelper.LoadMenuItem(menuItem);
                }
                ImGuiHelper.LoadMenuItem(NewItemContextMenu);
                ImGui.EndPopup();
            }*/
        }

        private void SetupNodes(NodeBase node)
        {
            node.Visible = false;
            foreach (var c in node.Children)
                SetupNodes(c);
        }

        private void SelectNodeRange(NodeBase node)
        {
            if (SelectedIndex != -1 && SelectedRangeIndex != -1)
            {
                var pos = node.DisplayIndex;
                if (SelectedIndex >= pos && pos >= SelectedRangeIndex ||
                    SelectedIndex <= pos && pos <= SelectedRangeIndex)
                {
                    node.IsSelected = true;
                    AddSelection(node);
                }
            }

            if (!node.IsExpanded)
                return;

            foreach (var c in node.Children)
                SelectNodeRange(c);
        }

        private void CalculateCount(NodeBase node, ref int counter)
        {
            bool HasText = node.Header != null &&
             node.Header.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;

            if (isSearch && HasText || !isSearch)
            {
                node.DisplayIndex = counter;
                counter++;
            }

            if (!isSearch && !node.IsExpanded)
                return;

            foreach (var c in node.Children)
                CalculateCount(c, ref counter);
        }

        public void DeselectAll()
        {
            foreach (var node in SelectedNodes)
                node.IsSelected = false;
        }

        public void DrawNode(NodeBase node, float itemHeight, int level = 0)
        {
            bool HasText = node.Header != null &&
                 node.Header.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;

            char icon = IconManager.FOLDER_ICON;
            if (node.Tag is STGenericMesh)
                icon = IconManager.MESH_ICON;
            if (node.Tag is STGenericModel)
                icon = IconManager.MODEL_ICON;
            if (node.Tag is STBone)
                icon = IconManager.BONE_ICON;
            if (!string.IsNullOrEmpty(node.Icon) && node.Icon.Length == 1)
                icon = (char)node.Icon[0];

            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None;
            flags |= ImGuiTreeNodeFlags.SpanFullWidth;

            if (node.Children.Count == 0 || isSearch)
                flags |= ImGuiTreeNodeFlags.Leaf;
            else
            {
                flags |= ImGuiTreeNodeFlags.OpenOnDoubleClick;
                flags |= ImGuiTreeNodeFlags.OpenOnArrow;
            }

            if (node.IsExpanded && !isSearch) {
                //Flags for opening as default settings
                flags |= ImGuiTreeNodeFlags.DefaultOpen;
                //Make sure the "IsExpanded" can force the node to expand
                ImGui.SetNextItemOpen(true);
            }

            float currentPos = node.DisplayIndex;

            //Node was selected manually outside the outliner so update the list
            if (node.IsSelected && !SelectedNodes.Contains(node))
                AddSelection(node);

            //Node was deselected manually outside the outliner so update the list
            if (!node.IsSelected && SelectedNodes.Contains(node))
                RemoveSelection(node);

            if (SelectedNodes.Contains(node))
                flags |= ImGuiTreeNodeFlags.Selected;

            //if (clip)
                

            if ((isSearch && HasText || !isSearch))
            {
                //Add active file format styling. This determines what file to save.
                //For files inside archives, it gets the parent of the file format to save.
                bool isActiveFile = Workspace.ActiveWorkspace.ActiveEditor == node.Tag;

                bool isRenaming = node == renameNode && isNameEditing && node.Tag is IRenamableNode;

                //Improve tree node spacing.
                var spacing = ImGui.GetStyle().ItemSpacing;
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(spacing.X, 1));

                //Make the active file noticable
                if (isActiveFile)
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.634f, 0.841f, 1.000f, 1.000f));

                //Align the text to improve selection sizing. 
                ImGui.AlignTextToFramePadding();

                //Disable selection view in renaming handler to make text more clear
                if (isRenaming)
                {
                    flags &= ~ImGuiTreeNodeFlags.Selected;
                    flags &= ~ImGuiTreeNodeFlags.SpanFullWidth;
                }

                //Load the expander or leaf tree node
                if (isSearch) {
                    if (ImGui.TreeNodeEx(node.ID, flags, $"")) { ImGui.TreePop(); }
                }
                else
                    node.IsExpanded = ImGui.TreeNodeEx(node.ID, flags, $"");

                node.Visible = true;

                ImGui.SameLine(); ImGuiHelper.IncrementCursorPosX(3);

                bool leftDoubleClicked = ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
                bool leftClicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);
                bool rightClicked = ImGui.IsItemClicked(ImGuiMouseButton.Right);
                bool nodeFocused = ImGui.IsItemFocused();
                bool isToggleOpened = ImGui.IsItemToggledOpen();
                bool beginDragDropSource = !isRenaming && node.Tag is IDragDropNode && ImGui.BeginDragDropSource();

                if (beginDragDropSource)
                {
                    //Placeholder pointer data. Instead use drag/drop nodes from GetDragDropNode()
                    GCHandle handle1 = GCHandle.Alloc(node.ID);
                    ImGui.SetDragDropPayload("OUTLINER_ITEM", (IntPtr)handle1, sizeof(int), ImGuiCond.Once);
                    handle1.Free();

                    dragDroppedNode = node;

                    //Display icon for texture types
                    if (node.Tag is STGenericTexture)
                        LoadTextureIcon(node);

                    //Display text for item being dragged
                    ImGui.Button($"{node.Header}");
                    ImGui.EndDragDropSource();
                }

                bool hasContextMenu = node is IContextMenu || node is IExportReplaceNode || node.Tag is ICheckableNode ||
                    node.Tag is IContextMenu || node.Tag is IExportReplaceNode ||
                    node.Tag is STGenericTexture || node.ContextMenus.Count > 0;

                if (ContextMenu.Count > 0)
                    hasContextMenu = true;

                //Apply a pop up menu for context items. Only do this if the menu has possible items used
                if (hasContextMenu)
                {
                    ImGui.PushID(node.Header);
                    if (ImGui.BeginPopupContextItem("##OUTLINER_POPUP", ImGuiPopupFlags.MouseButtonRight))
                    {
                        SetupRightClickMenu(node);
                        ImGui.EndPopup();
                    }
                    ImGui.PopID();
                }


                if (node.HasCheckBox)
                {
                    ImGui.SetItemAllowOverlap();

                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));

                    bool check = node.IsChecked;

                    if (ImGui.Checkbox($"##check{node.ID}", ref check)) {
                        foreach (var n in SelectedNodes)
                            n.IsChecked = check;
                        GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
                    }
                    ImGui.PopStyleVar();

                    ImGui.SameLine();
                }

                //Load the icon
                if (node.Tag is STGenericTexture) {
                    LoadTextureIcon(node);
                }
                else
                {
                    if (IconManager.HasIcon(node.Icon))
                    {
                        int iconID = IconManager.GetTextureIcon(node.Icon);

                        ImGui.AlignTextToFramePadding();
                        ImGui.Image((IntPtr)iconID, new System.Numerics.Vector2(22, 22));
                        ImGui.SameLine();
                    }
                    else
                    {
                        if (node.HasCheckBox)
                            ImGuiHelper.IncrementCursorPosX(5);

                        IconManager.DrawIcon(icon);
                        ImGui.SameLine();
                        if (icon == IconManager.BONE_ICON)
                            ImGuiHelper.IncrementCursorPosX(5);
                        else
                            ImGuiHelper.IncrementCursorPosX(3);
                    }
                }

                ImGui.AlignTextToFramePadding();

                //if (node.Tag is ICheckableNode)
                  //  ImGuiHelper.IncrementCursorPosY(-2);

                if (!isRenaming)
                {
                    if (node.CustomHeaderDraw == null)
                        ImGui.Text(node.Header);
                    else
                        node.CustomHeaderDraw();
                    if (node.GetTooltip?.Invoke() != null && ImGui.IsItemHovered())
                        ImGui.SetTooltip(node.GetTooltip());
                }
                else
                {
                    var renamable = node.Tag as IRenamableNode;

                    var bg = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];

                    //Make the textbox frame background blend with the tree background
                    //This is so we don't see the highlight color and can see text clearly
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, bg);
                    ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 1, 1, 1));
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);

                    var length = ImGui.CalcTextSize(renameText).X + 20;
                    ImGui.PushItemWidth(length);

                    if (ImGui.InputText("##RENAME_NODE", ref renameText, 512,
                        ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackCompletion |
                        ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.NoHorizontalScroll))
                    {
                        renamable.Renamed(renameText);
                        node.Header = renameText;

                        isNameEditing = false;
                    }
                    if (!ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        isNameEditing = false;
                    }

                    ImGui.PopItemWidth();
                    ImGui.PopStyleVar();
                    ImGui.PopStyleColor(2);
                }
                ImGui.PopStyleVar();

                if (isActiveFile)
                    ImGui.PopStyleColor();

                if (!isRenaming)
                {
                    //Check for rename selection on selected renamable node
                    if (node.IsSelected && node.Tag is IRenamableNode && RENAME_ENABLE)
                    {
                        bool renameStarting = renameClickTime != 0;
                        bool wasCancelled = false;

                        //Mouse click before editing started cancels the event
                        if (renameStarting && leftClicked)
                        {
                            renameClickTime = 0;
                            renameStarting = false;
                            wasCancelled = true;
                        }

                        //Check for delay
                        if (renameStarting)
                        {
                            //Create a delay between actions. This can be cancelled out during a mouse click
                            var diff = ImGui.GetTime() - renameClickTime;
                            if (diff > RENAME_DELAY_TIME)
                            {
                                //Name edit executed. Setup data for renaming.
                                isNameEditing = true;
                                renameNode = node;
                                renameText = ((IRenamableNode)node.Tag).GetRenameText();
                                //Reset the time
                                renameClickTime = 0;
                            }
                        }

                        //User has started a rename click. Start a time check
                        if (leftClicked && renameClickTime == 0 && !wasCancelled)
                        {
                            //Do a small delay for the rename event
                            renameClickTime = ImGui.GetTime();
                        }
                    }

                    //Deselect node during ctrl held when already selected
                    if (leftClicked && ImGui.GetIO().KeyCtrl && node.IsSelected)
                    {
                        RemoveSelection(node);
                        node.IsSelected = false;
                    }
                    //Click event executed on item
                    else if ((leftClicked || rightClicked) && !isToggleOpened) //Prevent selection change on toggle
                    {
                        //Reset all selection unless shift/control held down
                        if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                        {
                            foreach (var n in SelectedNodes)
                                n.IsSelected = false;
                            SelectedNodes.Clear();
                        }

                        //Check selection range
                        if (ImGui.GetIO().KeyShift) {
                            SelectedRangeIndex = node.DisplayIndex;
                            SelectRange = true;
                        }
                        else
                            SelectedIndex = node.DisplayIndex;

                        //Add the clicked node to selection.
                        AddSelection(node);
                        node.IsSelected = true;
                    }  //Focused during a scroll using arrow keys
                    else if (nodeFocused && !isToggleOpened && !node.IsSelected)
                    {
                        if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                        {
                            foreach (var n in SelectedNodes)
                                n.IsSelected = false;
                            SelectedNodes.Clear();
                        }

                        //Add the clicked node to selection.
                        AddSelection(node);
                        node.IsSelected = true;
                    }
                    //Expandable hiearchy from an archive file.
                    if (leftClicked && node.IsSelected)
                    {
                        if (node is ArchiveHiearchy && node.Tag == null)
                        {
                            var archiveWrapper = (ArchiveHiearchy)node;
                            archiveWrapper.OpenFileFormat();
                            archiveWrapper.IsExpanded = true;
                        }
                    }
                    //Double click event
                    if (leftDoubleClicked && !isToggleOpened && node.IsSelected) {
                        node.OnDoubleClicked();
                    }

                    //Update the active file format when selected. (updates dockspace layout and file menus)
                    if (node.Tag is IFileFormat && node.IsSelected)
                    {
                        if (ActiveFileFormat != node.Tag)
                            ActiveFileFormat = (IFileFormat)node.Tag;
                    }
                    else if (node.IsSelected && node.Parent != null)
                    {
                    }
                }
            }

            level++;
            if (isSearch || node.IsExpanded)
            {
                //Todo find a better alternative to clip parents
                //Clip only the last level.
                if (ClipNodes && node.Children.Count > 0 && node.Children.All(x => x.Children.Count == 0))
                {
                    var children = node.Children.ToList();
                    if (isSearch)
                        children = GetSearchableNodes(children);

                    var clipper = new ImGuiListClipper2(children.Count, itemHeight);

                    for (int line_i = clipper.DisplayStart; line_i < clipper.DisplayEnd; line_i++)
                    {
                        DrawNode(children[line_i], itemHeight, level);
                    }
                }
                else
                {
                    foreach (var child in node.Children)
                        DrawNode(child, itemHeight, level);
                }

                if (!isSearch)
                    ImGui.TreePop();
            }
        }

        private List<NodeBase> GetSearchableNodes(List<NodeBase> nodes)
        {
            List<NodeBase> nodeList = new List<NodeBase>();
            foreach (var node in nodes)
            {
                bool hasText = node.Header != null && node.Header.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                if (hasText)
                    nodeList.Add(node);
            }
            return nodeList;
        }

        private void LoadTextureIcon(NodeBase node)
        {
            if (((STGenericTexture)node.Tag).RenderableTex == null)
                ((STGenericTexture)node.Tag).LoadRenderableTexture();

            //Render textures loaded in GL as an icon
            if (((STGenericTexture)node.Tag).RenderableTex != null)
            {
                var tex = ((STGenericTexture)node.Tag);
                //Turn the texture to a cached icon
                IconManager.DrawTexture(node.Header, tex);
                ImGui.SameLine();
            }
        }

        private void SetupRightClickMenu(NodeBase node)
        {
            foreach (var item in node.ContextMenus)
                ImGuiHelper.LoadMenuItem(item);

            if (node.Tag is ICheckableNode)
            {
                var checkable = (ICheckableNode)node.Tag;
                if (ImGui.Selectable("Enable"))
                {
                    checkable.OnChecked(true);
                }
                if (ImGui.Selectable("Disable"))
                {
                    checkable.OnChecked(false);
                }
            }
            if (node.Tag is IExportReplaceNode)
            {
                var exportReplaceable = (IExportReplaceNode)node.Tag;

                if (ImGui.Selectable("Export"))
                {
                    var dialog = new ImguiFileDialog();
                    dialog.FileName = node.Header;

                    dialog.SaveDialog = true;
                    foreach (var filter in exportReplaceable.ExportFilter)
                        dialog.AddFilter(filter);
                    if (dialog.ShowDialog($"{node.GetType()}export"))
                    {
                        exportReplaceable.Export(dialog.FilePath);
                    }
                }
                if (ImGui.Selectable("Replace"))
                {
                    var dialog = new ImguiFileDialog();
                    foreach (var filter in exportReplaceable.ReplaceFilter)
                        dialog.AddFilter(filter);
                    if (dialog.ShowDialog($"{node.GetType()}replace"))
                    {
                        exportReplaceable.Replace(dialog.FilePath);
                    }
                }
                ImGui.Separator();
            }
            if (node.Tag is IContextMenu)
            {
                var contextMenu = (IContextMenu)node.Tag;
                var menuItems = contextMenu.GetContextMenuItems();
                foreach (var item in menuItems)
                    ImGuiHelper.LoadMenuItem(item);

                ImGui.Separator();
            }
        }

        private void DeselectAll(NodeBase node)
        {
            node.IsSelected = false;
            foreach (var c in node.Children)
                DeselectAll(c);
        }
    }
}
