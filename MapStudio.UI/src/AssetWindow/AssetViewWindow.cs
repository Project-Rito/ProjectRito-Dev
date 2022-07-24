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

        public AssetConfig Config = new AssetConfig();

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
            get { return _selectedAsset; }
            set { _selectedAsset = value; }
        }

        /// <summary>
        /// All asset categories
        /// </summary>
        public IList<IAssetCategory> AssetCategories
        {
            get
            {
                return _assetCategories.Where(x => !(x is FavoritesCategory || x is AssetViewFileExplorer)).ToList();
            }
        }

        private AssetItem _selectedAsset = null;

        private string _searchText = "";
        private bool isSearch = false;
        private bool filterFavorites = false;

        private AssetFolder ActiveFolder = null;
        private AssetFolder ParentFolder = null;

        private List<AssetItem> Assets = new List<AssetItem>();
        private List<AssetItem> FilteredAssets = new List<AssetItem>();

        private List<IAssetCategory> _assetCategories = new List<IAssetCategory>();

        private IAssetCategory ActiveCategory = null;
        private IAssetViewFileTypeList FileTypeList = null;

        public AssetViewWindow()
        {
            //Defaults
            AddCategory(new FavoritesCategory(this));
            AddCategory(new AssetViewFileExplorer(this));
        }

        /// <summary>
        /// Adds a category representing a collection of assets.
        /// </summary>
        public void AddCategory(IAssetCategory category) {
            if (_assetCategories.Any(x => x.Name == category.Name))
                return;
            _assetCategories.Add(category);
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
            foreach (var asset in ActiveCategory.Reload())
            {
                //Setup config
                this.Config.ApplySettings(asset);
                Assets.Add(asset);
            }

            if (ActiveCategory.IsFilterMode || isSearch)
                FilteredAssets = UpdateSearch(Assets);
        }

        /// <summary>
        /// Renders the asset window.
        /// </summary>
        public override void Render()
        {
            if (ActiveCategory == null) {
                Reload(_assetCategories.FirstOrDefault());
            }

            var width = ImGui.GetWindowWidth();

            var startPosX = ImGui.GetCursorPosX();
            ImGui.SetCursorPosX(width - 225);

            ImGui.SameLine();

            ImGui.SetCursorPosX(startPosX);

            if (ActiveCategory == null)
                return;

            if (ImGui.Button("<-"))
            {
                if (ParentFolder != null)
                    ReloadFromFolder(ParentFolder);
            }
            ImGui.SameLine();
            if (ImGui.Button("->"))
            {
                if (_selectedAsset is AssetFolder)
                    ReloadFromFolder((AssetFolder)_selectedAsset);
            }
            ImGui.SameLine();

            DrawSearchBar(); ImGui.SameLine();
            DrawIconMenuBar();

            ImGui.Columns(2);
            DrawCategoryList(); ImGui.NextColumn();
            DrawListView();     ImGui.NextColumn();
        }

        private bool initial_spacing = true;

        private void DrawCategoryList()
        {
            //Setup the default spacing
            if (initial_spacing) {
                ImGui.SetColumnWidth(0, 200);
                initial_spacing = false;
            }

            if (ImGui.BeginChild("assetCategoryList1"))
            {
                //Category filter
                ImGui.PushItemWidth(200);
                foreach (var category in _assetCategories)
                {
                    bool isSelected = category == ActiveCategory;
                    if (ImGui.Selectable(category.Name, isSelected)) {
                        Reload(category);
                    }

                    //Scroll focus using arrow keys
                    if (ImGui.IsItemFocused() && ActiveCategory != category) {
                        ActiveCategory = category;
                        Reload(category);
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndChild();
            }

            ImGui.PopItemWidth();
        }

        private void DrawSearchBar()
        {
            var width = ImGui.GetWindowWidth();

            ImGui.AlignTextToFramePadding();
            ImGui.Text(TranslationSource.GetText("SEARCH"));
            ImGui.SameLine();

            var posX = ImGui.GetCursorPosX();

            //Span across entire outliner width
            ImGui.PushItemWidth(width - posX - 150);
            if (ImGui.InputText("##search_box", ref _searchText, 200))
            {
                isSearch = !string.IsNullOrWhiteSpace(_searchText);
                FilteredAssets = UpdateSearch(Assets);
            }
            ImGui.PopItemWidth();
        }

        private void DrawIconMenuBar()
        {
            float itemWidth = Config.IconSize;
            float itemHeight = Config.IconSize;

            if (_selectedAsset != null && _selectedAsset.Favorited) {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1)));
            }
            else {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
            }
            if (ImGui.Button($"  {IconManager.STAR_ICON}   "))
            {
                if (_selectedAsset != null)
                    _selectedAsset.Favorited = !_selectedAsset.Favorited;
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();

            //Icon menus
            if (IconPopup(IconManager.THUMB_RESIZE_ICON, "icon_edit")) {
                ImGui.PushItemWidth(100);
                if (ImGui.SliderFloat("##thumbSize", ref itemWidth, MIN_THUMB_SIZE, MAX_THUMB_SIZE))
                {
                    Config.IconSize = itemWidth;
                    //Make the display name recalculate based on size
                    foreach (var asset in Assets)
                        asset.DisplayName = "";
                }
                ImGui.PopItemWidth();
                ImGui.EndPopup();
            }
            ImGui.SameLine();
            if (IconPopup(IconManager.FILTER_ICON, "filter_edit")) {
                bool filter = ActiveCategory.UpdateFilterList();
                if (filter) {
                    FilteredAssets = UpdateSearch(Assets);
                }
                ImGui.EndPopup();
            }
            ImGui.SameLine();
            if (IconPopup(IconManager.SETTINGS_ICON, "asset_settings")) {
                //Setting for displaying objects when spawned
                if (ImGuiHelper.InputFromBoolean(TranslationSource.GetText("FACE_CAMERA_AT_SPAWN"), GlobalSettings.Current.Asset, "FaceCameraAtSpawn"))
                {
                }
                ImGui.EndPopup();
            }
        }

        private void DrawCategoryFilter()
        {
            //Category filter
            ImGui.PushItemWidth(200);
            if (ImGui.BeginCombo("##category", ActiveCategory.Name))
            {
                foreach (var category in _assetCategories)
                {
                    bool isSelected = category == ActiveCategory;
                    if (ImGui.Selectable(category.Name, isSelected))
                    {
                        Reload(category);
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
        }

        #region ListView

        private void DrawListView()
        {
            float itemWidth = Config.IconSize;
            float itemHeight = Config.IconSize;

            var width = ImGui.GetWindowWidth() - ImGui.GetCursorPosX();

            AssetItem doubleClickedAsset = null;
            var objects = (isSearch || ActiveCategory.IsFilterMode || filterFavorites) ? FilteredAssets : Assets;

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

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0));

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

                        if (columnCount > 1)
                            ImGui.SetColumnWidth(j, itemWidth);

                        var obj = objects[index];

                        float textWidth = ImGui.CalcTextSize(obj.Name).X;

                        if (string.IsNullOrWhiteSpace(obj.DisplayName))
                            obj.DisplayName = TextEplislon(obj.Name, textWidth, itemWidth);

                        string name = obj.DisplayName;
                        bool isSelected = _selectedAsset == obj;

                        //Get the icon
                        var icon = IconManager.GetTextureIcon("Node");
                        if (obj.Icon != -1)
                            icon = obj.Icon;

                        //Load the icon onto the list
                        ImGui.BeginGroup();

                        var pos = ImGui.GetCursorPos();

                        Vector2 itemSize = new Vector2(itemWidth, totalItemHeight);
                        if (columnCount == 1)
                            ImGui.AlignTextToFramePadding();

                        if (columnCount > 1)
                        {
                            var pos1 = ImGui.GetCursorPos();

                            ImGui.Image((IntPtr)icon, new Vector2(itemWidth, itemWidth));

                            var pos2 = ImGui.GetCursorPos();
                            if (obj.Favorited)
                            {
                                ImGui.SetCursorPos(new Vector2(pos1.X, pos1.Y ));
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1)));
                                ImGui.Text($"  {IconManager.STAR_ICON}   ");
                                ImGui.PopStyleColor();
                            }
                            ImGui.SetCursorPos(pos2);

                            var defaultFontScale = ImGui.GetIO().FontGlobalScale;
                            ImGui.SetWindowFontScale(0.8f);

                            var w = ImGui.GetCursorPosX();

                            //Clamp to 0 so the text stays within the current position
                            //Center the text on the x axis
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
                            _selectedAsset = obj;
                        }
                        if (isDoubleClicked)
                            doubleClickedAsset = obj;

                        ImGui.NextColumn();
                    }
                }
                ImGui.EndChild();
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();

            if (doubleClickedAsset != null)
                doubleClickedAsset.DoubleClicked();

            if (doubleClickedAsset != null && doubleClickedAsset is AssetFolder) {
                ReloadFromFolder((AssetFolder)doubleClickedAsset);
            }
        }

        #endregion

        #region UI Helpers

        private bool IconPopup(char icon, string popupName)
        {
            var pos = ImGui.GetCursorScreenPos();
            if (ImGui.Button($"   {icon}   ")) {
                ImGui.OpenPopup(popupName);
            }

            ImGui.SetNextWindowPos(new Vector2(pos.X, pos.Y + 23));
            bool popup = ImGui.BeginPopup(popupName, ImGuiWindowFlags.AlwaysAutoResize);
            return popup;
        }

        private void TextCentered(string text)
        {
            float win_width = ImGui.GetWindowSize().X;
            float text_width = ImGui.CalcTextSize(text).X;

            // calculate the indentation that centers the text on one line, relative
            // to window left, regardless of the `ImGuiStyleVar_WindowPadding` value
            float text_indentation = (win_width - text_width) * 0.5f;

            // if text is too long to be drawn on one line, `text_indentation` can
            // become too small or even negative, so we check a minimum indentation
            float min_indentation = 20.0f;
            if (text_indentation <= min_indentation)
            {
                text_indentation = min_indentation;
            }

            ImGui.SameLine(text_indentation);
            ImGui.PushTextWrapPos(win_width - text_indentation);
            ImGui.TextWrapped(text);
            ImGui.PopTextWrapPos();
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

        #endregion

        #region Asset Loading

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

        #endregion
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

        /// <summary>
        /// 
        /// </summary>
        public string[] Categories { get; set; }

        public bool Favorited = false;

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
        public override void DoubleClicked() {
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

    class FavoritesCategory : IAssetCategory
    {
        public string Name => $"Favorites";

        private AssetViewWindow Window;
        public FavoritesCategory(AssetViewWindow window) {
            Window = window;
        }

        /// <summary>
        /// Reloads the asset list.
        /// </summary>
        public List<AssetItem> Reload() {
            return new List<AssetItem>();
        }

        /// <summary>
        /// Gets a value to determine if filtering is in use.
        /// </summary>
        public bool IsFilterMode => false;

        /// <summary>
        /// Determines if the filter list needs to be updated or not.
        /// The UI elements for filtering should be drawn in here.
        /// </summary>
        public bool UpdateFilterList() { return false; }
    }
}
    