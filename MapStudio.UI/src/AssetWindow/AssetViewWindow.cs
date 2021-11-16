using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Numerics;
using MapStudio.UI;
using ImGuiNET;
using GLFrameworkEngine;
using OpenTK.Graphics;

namespace MapStudio.UI
{
    public class AssetViewWindow : DockWindow
    {
        public override string Name => "ASSETS";
        //Only use child window scrolling
        public override ImGuiWindowFlags Flags => ImGuiWindowFlags.NoScrollbar;

        float itemWidth = 50;
        float itemHeight = 50;

        const float MAX_THUMB_SIZE = 150;
        const float MIN_THUMB_SIZE = 30;

        public bool IsFocused = false;

        /// <summary>
        /// The currently dragged asset item from the window.
        /// </summary>
        public AssetItem DraggedAsset = null;

        /// <summary>
        /// The currently selected asset item
        /// </summary>
        public AssetItem SelectedAsset
        {
            get { return selectedAsset; }
            set { selectedAsset = value; }
        }

        private AssetItem selectedAsset = null;

        private string _searchText = "";
        private bool isSearch = false;

        private AssetFolder ActiveFolder = null;
        private AssetFolder ParentFolder = null;

        private List<AssetItem> Assets = new List<AssetItem>();
        private List<AssetItem> FilteredAssets = new List<AssetItem>();

        private List<IAssetCategory> AssetCategories = new List<IAssetCategory>();

        private IAssetCategory ActiveCategory = null;
        private IAssetViewFileTypeList FileTypeList = null;

        public AssetViewWindow()
        {
            //Defaults
            AddCategory(new AssetViewFileExplorer(this));
        }

        /// <summary>
        /// Adds a category representing a collection of assets.
        /// </summary>
        public void AddCategory(IAssetCategory category) {
            AssetCategories.Add(category);
        }

        /// <summary>
        /// Reloads the set of assets by category.
        /// </summary>
        public void Reload(IAssetCategory assetCategory)
        {
            ActiveCategory = assetCategory;

            foreach (var asset in Assets)
                asset.Dispose();

            FilteredAssets.Clear();
            Assets.Clear();
            Assets.AddRange(ActiveCategory.Reload());

            if (ActiveCategory.IsFilterMode)
                FilteredAssets = UpdateSearch(Assets);
        }

        /// <summary>
        /// Renders the asset window.
        /// </summary>
        public override void Render()
        {
            if (ActiveCategory == null) {
                Reload(AssetCategories.FirstOrDefault());
            }

            AssetItem doubleClickedAsset = null;
            var width = ImGui.GetWindowWidth();

            var startPosX = ImGui.GetCursorPosX();
            ImGui.SetCursorPosX(width - 225);
            ImGui.PushItemWidth(200);
            if (ImGui.BeginCombo("##category", ActiveCategory.Name))
            {
                foreach (var category in AssetCategories)
                {
                    bool isSelected = category == ActiveCategory;
                    if (ImGui.Selectable(category.Name, isSelected)) {
                        Reload(category);
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();

            ImGui.SetCursorPosX(startPosX);

            if (ActiveCategory == null)
                return;

            var objects = (isSearch || ActiveCategory.IsFilterMode) ? FilteredAssets : Assets;

            if (ImGui.Button("<-"))
            {
                if (ParentFolder != null)
                    ReloadFromFolder(ParentFolder);
            }
            ImGui.SameLine();
            if (ImGui.Button("->"))
            {
                if (selectedAsset is AssetFolder)
                    ReloadFromFolder((AssetFolder)selectedAsset);
            }
            ImGui.SameLine();

            ImGui.PushItemWidth(100);
            if (ImGui.SliderFloat("##thumbSize", ref itemWidth, MIN_THUMB_SIZE, MAX_THUMB_SIZE)) {
                itemHeight = itemWidth;
                //Make the display name recalculate based on size
                foreach (var asset in Assets)
                    asset.DisplayName = "";
            }
            ImGui.PopItemWidth();

            ImGui.SameLine();

            //Setting for displaying objects when spawned
            if (ImGuiHelper.InputFromBoolean(TranslationSource.GetText("FACE_CAMERA_AT_SPAWN"), GlobalSettings.Current.Asset, "FaceCameraAtSpawn"))
            {
            }

            //Search bar
            {
                //Categories can have their own set of filters to use
                //The return for this determines if the filter list needs refreshing
                bool filter = ActiveCategory.UpdateFilterList();

                if (filter) {
                    FilteredAssets = UpdateSearch(Assets);
                }

                ImGui.AlignTextToFramePadding();
                ImGui.Text(TranslationSource.GetText("SEARCH"));
                ImGui.SameLine();

                var posX = ImGui.GetCursorPosX();

                //Span across entire outliner width
                ImGui.PushItemWidth(width - posX);
                if (ImGui.InputText("##search_box", ref _searchText, 200)) {
                    isSearch = !string.IsNullOrWhiteSpace(_searchText);
                    FilteredAssets = UpdateSearch(Assets);
                }
                ImGui.PopItemWidth();
            }

            //Column count based on the view size and item width for each column
            var columnCount = (int)Math.Max((width / (itemWidth)), 1);
            //Use listview at smallest width
            if (itemWidth == MIN_THUMB_SIZE)
                columnCount = 1;

            var rowCount = Math.Max(objects.Count / columnCount, 1);
            var totalItemHeight = itemHeight + 22;
            if (columnCount == 1)
                totalItemHeight = itemHeight;

            var color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
            ImGui.PushStyleColor(ImGuiCol.ChildBg, color);

            //Slightly increate the height to account for text wrapping
            ImGuiNative.igSetNextWindowContentSize(new System.Numerics.Vector2(0.0f, rowCount * (totalItemHeight)));
            if (ImGui.BeginChild("##assetList"))
            {
                IsFocused = ImGui.IsWindowFocused();

                //Clip each row
                ImGuiListClipper2 clipper = new ImGuiListClipper2(rowCount, (totalItemHeight));

                ImGui.Columns(columnCount, "assetCL", false);
                for (int line_i = clipper.DisplayStart; line_i < clipper.DisplayEnd; line_i++) // display only visible items
                {
                    //Columns
                    for (int j = 0; j < columnCount; j++) {
                        int index = (line_i * columnCount) + j;
                        if (index >= objects.Count)
                            break;

                        var obj = objects[index];

                        float textWidth = ImGui.CalcTextSize(obj.Name).X;

                        if (string.IsNullOrWhiteSpace(obj.DisplayName))
                            obj.DisplayName = TextEplislon(obj.Name, textWidth, itemWidth);

                        string name = obj.DisplayName;
                        bool isSelected = selectedAsset == obj;

                        //Get the icon
                        var icon = IconManager.GetTextureIcon("Node");
                        if (obj.Icon != -1)
                            icon = obj.Icon;

                        //Load the icon onto the list
                        ImGui.BeginGroup();

                        var pos = ImGui.GetCursorPos();

                        Vector2 itemSize = new Vector2(ImGui.GetColumnWidth(), totalItemHeight);
                        if (columnCount == 1)
                            ImGui.AlignTextToFramePadding();

                        //ImGuiHelper.IncrementCursorPosX(-4);

                        if (columnCount > 1)
                        {
                            ImGui.Image((IntPtr)icon, new Vector2(itemWidth, itemWidth));

                            var defaultFontScale = ImGui.GetIO().FontGlobalScale;
                            ImGui.SetWindowFontScale(0.7f);

                            var w = ImGui.GetCursorPosX();

                            //Clamp to 0 so the text stays within the current position
                            ImGui.SetCursorPosX(w + MathF.Max((itemWidth - textWidth) * 0.5f, 0));
                            ImGui.Text(name);

                            ImGui.GetIO().FontGlobalScale = defaultFontScale;
                        }
                        else
                        {
                            ImGui.Image((IntPtr)icon, new Vector2(22, 22));

                            ImGui.SameLine();

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text(name);
                        }

                        ImGui.EndGroup();

                        ImGui.SetCursorPos(pos);

                        bool select = ImGui.Selectable($"##{obj.Name}", isSelected, ImGuiSelectableFlags.AllowItemOverlap, itemSize);
                        bool beginDragDropSource = ImGui.BeginDragDropSource();
                        bool isDoubleClicked = ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0);

                        if (ImGui.IsItemFocused())
                            select = true;

                        if (beginDragDropSource)
                        {
                            //Placeholder pointer data. Instead use drag/drop nodes from GetDragDropNode()
                            GCHandle handle1 = GCHandle.Alloc(obj.Name);
                            ImGui.SetDragDropPayload("ASSET_ITEM", (IntPtr)handle1, sizeof(int), ImGuiCond.Once);
                            handle1.Free();

                            DraggedAsset = obj;
                            //Display icon
                            ImGui.Image((IntPtr)icon, new Vector2(itemWidth, itemWidth));

                            var w = ImGui.GetCursorPosX();

                            //Clamp to 0 so the text stays within the current position
                            ImGui.SetCursorPosX(w + MathF.Max((itemWidth - textWidth) * 0.5f, 0));
                            ImGui.TextWrapped(obj.Name);

                            ImGui.EndDragDropSource();
                        }

                        if (select) {
                            selectedAsset = obj;
                        }
                        if (isDoubleClicked)
                            doubleClickedAsset = obj;

                        ImGui.NextColumn();
                    }
                }
                ImGui.EndChild();
            }

            if (doubleClickedAsset != null)
                doubleClickedAsset.DoubleClicked();

            if (doubleClickedAsset != null && doubleClickedAsset is AssetFolder) {
                ReloadFromFolder((AssetFolder)doubleClickedAsset);
            }

            ImGui.PopStyleColor();
        }

        private string TextEplislon(string text, float textWidth, float itemWidth)
        {
            var diff = textWidth - itemWidth;
            var pos = MathF.Max((itemWidth - textWidth) * 0.5f, 0);
            //Diff
            if (diff > 0)
            {
                string clippedText = "";
                for (int i = 0; i < text.Length; i++)
                {
                    clippedText = clippedText.Insert(i, text[i].ToString());
                    if (ImGui.CalcTextSize(clippedText + "....").X >= pos + itemWidth)
                        return clippedText + "..";
                }
            }
            return text;
        }

        private List<AssetItem> UpdateSearch(List<AssetItem> assets)
        {
            List<AssetItem> filtered = new List<AssetItem>();
            for (int i = 0; i < assets.Count; i++)
            {
                bool HasText = assets[i].Name != null &&
                     assets[i].Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!assets[i].Visible)
                    continue;

                if (isSearch && HasText || !isSearch)
                    filtered.Add(assets[i]);
            }
            return filtered;
        }

        private void ReloadFromFolder(AssetFolder folder)
        {
            if (ActiveFolder == folder)
                return;

            foreach (var asset in Assets)
                asset?.Dispose();

            Assets.Clear();
            Assets.AddRange(LoadFromFolder(folder));
        }

        public List<AssetItem> LoadFromFolder(AssetFolder folder)
        {
            List<AssetItem> assets = new List<AssetItem>();

            //Assign the parent folder for going back
            var dir = new DirectoryInfo(folder.DirectoryPath).Parent;
            if (dir != null && folder.DirectoryPath != GlobalSettings.Current.Program.ProjectDirectory)
                ParentFolder = new AssetFolder(dir.FullName);
            else
                ParentFolder = null;

            //Assign the current folder
            ActiveFolder = folder;

            assets.Clear();
            assets.AddRange(LoadAssetsFromDirectory(folder.DirectoryPath));
            return assets;
        }

        private List<AssetItem> LoadAssetsFromDirectory(string directory)
        {
            List<AssetItem> assets = new List<AssetItem>();
            foreach (var file in Directory.GetDirectories(directory))
                assets.Add(new AssetFolder(file));
            foreach (var file in Directory.GetFiles(directory))
                assets.Add(new AssetFile(file, this.FileTypeList));

            var Thread3 = new Thread((ThreadStart)(() =>
            {
                LoadThumbnails(assets);
            }));
            Thread3.Start();

            return assets;
        }

        private void LoadThumbnails(List<AssetItem> assetItems)
        {
            foreach (var asset in assetItems)
                asset.Background_LoadThumbnail();
        }
    }

    public interface IAssetCategory
    {
        string Name { get; }

        /// <summary>
        /// Reloads the asset list.
        /// </summary>
        List<AssetItem> Reload();

        /// <summary>
        /// Gets a value to determine if filtering is in use.
        /// </summary>
        bool IsFilterMode { get; }

        /// <summary>
        /// Determines if the filter list needs to be updated or not.
        /// The UI elements for filtering should be drawn in here.
        /// </summary>
        bool UpdateFilterList();
    }

    /// <summary>
    /// Represents an asset to use in the AssetViewWindow.
    /// Assets are purposed to import data into a scene such as 
    /// map objects, models, materials, textures, etc.
    /// </summary>
    public class AssetItem
    {
        /// <summary>
        /// The display name of the asset.
        /// </summary>
        public string Name { get; set; }

        public object Tag { get; set; }

        public string DisplayName { get; internal set; }

        /// <summary>
        /// The image ID of the icon. 
        /// </summary>
        public virtual int Icon { get; set; } = -1;

        /// <summary>
        /// Determines if the asset is visible or not in the viewer.
        /// </summary>
        public virtual bool Visible { get; } = true;

        public virtual void DoubleClicked()
        {

        }

        public virtual void Background_LoadThumbnail()
        {
        }

        /// <summary>
        /// Disposes the asset when no longer in use.
        /// </summary>
        public virtual void Dispose()
        {

        }
    }

    /// <summary>
    /// Represents a file in the asset list.
    /// </summary>
    public class AssetFile : AssetItem
    {
        public string FilePath;

        private System.Drawing.Bitmap thumbnail;
        private int id = -1;

        private byte[] FileDataHash;
        private IAssetViewFileTypeList FileTypeLoader;

        public override int Icon
        {
            get
            {
                //Load the icon ID into opengl when visible if a thumbnail exists
                if (id == -1 && thumbnail != null)
                {
                    id = GLTexture2D.FromBitmap(thumbnail).ID;
                    //Dispose the thumnail once finished
                    thumbnail.Dispose();
                    thumbnail = null;
                }
                return id;
            }
        }

        public AssetFile(string filePath, IAssetViewFileTypeList typeList)
        {
            FilePath = filePath;
            FileTypeLoader = typeList;
            Name = Path.GetFileName(FilePath);
            FileDataHash = CalculateHash(FilePath);
        }

        public void LoadFileData()
        {
            string extension = Path.GetExtension(FilePath);
            if (FileTypeLoader.FileTypes.ContainsKey(extension)) {
                var type = FileTypeLoader.FileTypes[extension];
                
            }
        }

        /// <summary>
        /// Checks if the file has been altered on disk.
        /// </summary>
        public bool FileChanged() {
            return FileDataHash.SequenceEqual(CalculateHash(FilePath));
        }

        private byte[] CalculateHash(string fileName)
        {
            var sha1 = System.Security.Cryptography.SHA1.Create();
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                return sha1.ComputeHash(stream);
        }

        /// <summary>
        /// Attempts to load a thumbnail from the file on disk.
        /// This method should be ran on a background thread.
        /// </summary>
        public override void Background_LoadThumbnail()
        {
            //Try to load image data on the background thread.
            //Do not load the actual render data as it requires multi threading contexts
            if (FilePath.EndsWith(".png"))
            {
                var image = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromStream(new System.IO.MemoryStream(File.ReadAllBytes(FilePath)));
                thumbnail = Toolbox.Core.Imaging.BitmapExtension.CreateImageThumbnail(image, 64, 64, true);

                image.Dispose();
            }
        }

        /// <summary>
        /// Opens the file on a double click.
        /// </summary>
        public override void DoubleClicked()
        {
            FileUtility.OpenWithDefaultProgram(FilePath);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (id != -1)
            {
                OpenTK.Graphics.OpenGL.GL.DeleteTexture(id);
                id = -1;
            }
        }
    }

    /// <summary>
    /// Represents a folder in the asset list.
    /// When double clicked, this will reload the explorer with the folder contents.
    /// </summary>
    public class AssetFolder : AssetItem
    {
        public string DirectoryPath;

        public AssetFolder(string directoryPath)
        {
            DirectoryPath = directoryPath;
            Name = new DirectoryInfo(directoryPath).Name;
            Icon = MapStudio.UI.IconManager.GetTextureIcon("FOLDER");
        }
    }
}
