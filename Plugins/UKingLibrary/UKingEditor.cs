using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using MapStudio.UI;
using Toolbox.Core;
using GLFrameworkEngine;
using CafeLibrary;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;
using Toolbox.Core.Hashes.Cryptography;
using UKingLibrary.UI;
using OpenTK;

namespace UKingLibrary
{
    public class UKingEditor : FileEditor, IFileFormat, IDisposable
    {
        public string[] Description => new string[] { "Field Map Data for Breath of the Wild" };
        public string[] Extension => new string[] { "*.json" };

        /// <summary>
        /// Whether or not the file can be saved.
        /// </summary>
        public bool CanSave { get; set; } = true;

        /// <summary>
        /// Information of the loaded file.
        /// </summary>
        public File_Info FileInfo { get; set; } = new File_Info();

        UKingEditorConfig EditorConfig;

        NodeFolder MapFolder;
        NodeFolder DungeonFolder;

        public Terrain Terrain;
        public SARC TitleBG;

        //Editor loaders
        public List<FieldMapLoader> FieldLoaders = new List<FieldMapLoader>();
        public List<DungeonMapLoader> DungeonLoaders = new List<DungeonMapLoader>();

        public List<MapData> ActiveMapData;

        private List<GLScene> Scenes = new List<GLScene>();

        private Dictionary<string, Terrain> Terrains = new Dictionary<string, Terrain>();


        public bool Identify(File_Info fileInfo, Stream stream)
        {
            if (fileInfo.Extension == ".json")
            {
                string json = File.ReadAllText(fileInfo.FilePath);
                var ukingEditorFile = JsonConvert.DeserializeObject<UKingEditorConfig>(json);
                if (ukingEditorFile.IsValid)
                    return true;
            }
            return false;
        }

        public UKingEditor() 
        {
        }

        public void Load(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            string json = new StreamReader(stream).ReadToEnd();
            EditorConfig = JsonConvert.DeserializeObject<UKingEditorConfig>(json);

            ToolWindowDrawer = new MubinToolSettings();

            Root.Header = "UKingEditor";
            Root.OnSelected += delegate 
                {
                    MakeActiveEditor();
                };

            SetupContextMenus();
            

            Root.Children.Clear();

            MapFolder = new NodeFolder { Header = "Map" };
            DungeonFolder = new NodeFolder { Header = "Dungeon" };
            Root.AddChild(MapFolder);
            Root.AddChild(DungeonFolder);

            CacheBackgroundFiles();

            foreach (var fieldName in EditorConfig.OpenMapUnits.Keys)
            {
                foreach (var muPath in EditorConfig.OpenMapUnits[fieldName])
                {
                    string fullPath = Path.GetFullPath(muPath, Path.GetFullPath(Path.Join(EditorConfig.FolderName, $"aoc/0010/Map/{fieldName}/"), FileInfo.FolderPath));
                    string fileName = Path.GetFileName(muPath);
                    if (!File.Exists(fullPath))
                        continue;
                    LoadFieldMuunt(fieldName, fileName, STFileLoader.TryDecompressFile(File.Open(fullPath, FileMode.Open), fileName).Stream);
                }
            }
            foreach (var packName in EditorConfig.OpenDungeons)
            {
                string fullPath = Path.GetFullPath(Path.Join(EditorConfig.FolderName, $"content/Pack/{packName}"), FileInfo.FolderPath);

                if (!File.Exists(fullPath))
                    continue;
                LoadDungeon(packName, STFileLoader.TryDecompressFile(File.Open(fullPath, FileMode.Open), packName).Stream);
            }

            LoadAssetList();
        }

        private void SetupContextMenus()
        {
            MenuItemModel loadSectionMenuItem = new MenuItemModel(TranslationSource.GetText("LOAD_SECTION"));
            
            foreach (var fieldName in GlobalData.FieldNames)
            {
                loadSectionMenuItem.MenuItems.Add(new MenuItemModel(fieldName)
                {
                    MenuItems = GlobalData.SectionNames.Select(sectionName => new MenuItemModel(sectionName, () => { LoadSection(fieldName, sectionName); })).ToList()
                });
            }

            Root.ContextMenus.Add(loadSectionMenuItem);

            MenuItemModel loadDungeonMenuItem = new MenuItemModel(TranslationSource.GetText("LOAD_DUNGEON"));

            /*
            MenuItemModel loadDungeonMenuItem = new MenuItemModel(TranslationSource.GetText("LOAD_DUNGEON"), () => {
                ImguiFileDialog sfd = new ImguiFileDialog() { };

                sfd.AddFilter(new FileFilter("*.pack"));
                if (sfd.ShowDialog("SAVE_FILE"))
                    LoadDungeon(Path.GetFileName(sfd.FilePath), File.OpenRead(sfd.FilePath));
            });*/

            //File prompt for now
            List<string> packPaths = new List<string>();
            foreach (var dir in PluginConfig.GetContentPaths("Pack/"))
                foreach (var file in Directory.GetFiles(dir))
                    if (!packPaths.Any(p => Path.GetFileName(p) == Path.GetFileName(file)))
                        packPaths.Add(file);

            foreach (var packPath in packPaths)
                if (Path.GetFileName(packPath).StartsWith("Dungeon"))
                    loadDungeonMenuItem.MenuItems.Add(new MenuItemModel(Path.GetFileNameWithoutExtension(packPath), () => { LoadDungeon(Path.GetFileName(packPath), File.OpenRead(packPath)); EditorConfig.OpenDungeons.Add(Path.GetFileName(packPath)); }));
            

            Root.ContextMenus.Add(loadDungeonMenuItem);
        }

        private void LoadAssetList()
        {
            //Allow actor objects to be loaded in the asset list
            //Todo add more profiles and add a proper tree hiearchy to asset viewer
            var profiles = new string[] {
                "Dragon",
                "Enemy",
                "EnemySwarm",
                "GiantEnemy",
                "Guardian",
                "Item",
                "LastBoss",
                "Horse",
                "MapDynamicActive",
                "MapConstActive",
                "MergedDungeonParts",
                "NPC",
                "Prey",
                "Sandworm",
                "SiteBoss",
                "System",
                "Weapon",
                "WeaponBow",
                "WeaponShield",
                "WeaponLargeSword",
                "WeaponSmallSword" };

            foreach (var profile in profiles)
                this.Workspace.AddAssetCategory(new AssetViewMapObject(profile));
        }

        public override void DrawToolWindow()
        {
            ToolWindowDrawer?.Render();
        }

        private void LoadDungeon(string fileName, Stream stream)
        {
            // Set up new scene
            var scene = new GLScene();
            scene.Init();
            Scene = scene;
            Scenes.Add(Scene);
            Workspace.ViewportWindow.Pipeline._context.Scene = Scene;


            // Load file data
            DungeonMapLoader loader = new DungeonMapLoader();
            loader.Load(this, fileName, stream);

            ActiveMapData = loader.MapFiles;

            loader.RootNode.OnSelected = delegate
            {
                Scene = scene;
                Workspace.ViewportWindow.Pipeline._context.Scene = scene;
                ActiveMapData = loader.MapFiles;
            };

            DungeonFolder.AddChild(loader.RootNode);


            // Add rendered map model
            this.AddRender(loader.MapRender);

            DungeonLoaders.Add(loader);
        }

        private void LoadSection(string fieldName, string sectionName)
        {
            string[] endings = { "Static", "Dynamic" };

            foreach (var ending in endings)
            {
                string path = PluginConfig.GetContentPath($"Map/{fieldName}/{sectionName}/{sectionName}_{ending}.smubin");
                if (!File.Exists(path))
                    return;
                string fileName = Path.GetFileName(path);
                var stream = STFileLoader.TryDecompressFile(File.Open(path, FileMode.Open), fileName).Stream;

                EditorConfig.OpenMapUnits.TryAdd(fieldName, new List<string>());
                EditorConfig.OpenMapUnits[fieldName].Add($"{fileName.Substring(0, 3)}/{fileName}");

                LoadFieldMuunt(fieldName, fileName, stream);
            }
        }

        private void LoadFieldMuunt(string fieldName, string fileName, Stream stream)
        {
            if (!MapFolder.FolderChildren.ContainsKey(fieldName))
            {
                var scene = new GLScene();
                scene.Init();
                Scene = scene;
                Scenes.Add(Scene);
                Workspace.ViewportWindow.Pipeline._context.Scene = Scene;

                Terrain = new Terrain();
                Terrain.LoadTerrainTable(fieldName);
                Terrains.Add(fieldName, Terrain);

                var fieldMapData = new List<MapData>();
                ActiveMapData = fieldMapData;

                // Ensure field folder exists (MainField, AocField, etc)
                MapFolder.AddChild(new NodeFolder
                {
                    Header = fieldName,
                    OnSelected = delegate
                    {
                        Scene = scene;
                        Workspace.ViewportWindow.Pipeline._context.Scene = scene;
                        Terrain = Terrains[fieldName];
                        ActiveMapData = fieldMapData;
                    }
                });
            }

            // Load into mapdata
            FieldMapLoader loader = new FieldMapLoader();
            MapData mapData = loader.Load(this, fileName, stream);
            FieldLoaders.Add(loader);
            ActiveMapData.Add(mapData);

            mapData.RootNode.ContextMenus.Add(new MenuItemModel("Remove", () => { UnloadFieldMuunt(mapData); }));

            // Add muunt root to field folder
            NodeBase fieldFolder = MapFolder.FolderChildren[fieldName];
            fieldFolder.AddChild(mapData.RootNode);
            

            // Ensure terrain is loaded
            bool terrLoaded = false;
            foreach (var node in MapFolder.Children)
            {
                if (node.Header == fileName)
                    continue;
                if (node.Header.Substring(0, 3) == fileName.Substring(0, 3))
                {
                    terrLoaded = true;
                    break;
                }
            }

            if (!terrLoaded)
            {
                var terrSectionID = GetSectionIndex(fileName.Substring(0, 3));

                Terrain.LoadTerrainSection(fieldName, (int)terrSectionID.X, (int)terrSectionID.Y, this, PluginConfig.MaxTerrainLOD);
            }
        }

        private void UnloadFieldMuunt(MapData mapData)
        {
            NodeBase fieldFolder = mapData.RootNode.Parent;
            fieldFolder.Children.Remove(mapData.RootNode);
            mapData.Dispose();

        }

        //Todo prod should probably be a seperate FileEditor instance
        public void LoadProd(ProdInfo prodInfo, string fileName)
        {
            NodeBase folder = new NodeBase(fileName);
            Root.AddChild(folder);

            foreach (var actor in prodInfo.Actors)
            {
                foreach (var instance in actor.Instances)
                {
                    EditableObject render = new TransformableObject(folder);
                    render.UINode.Header = actor.Name;
                    render.UINode.Tag = instance;
                    render.Transform.Position = instance.Position * GLContext.PreviewScale;
                    render.Transform.RotationEuler = instance.Rotation;
                    render.Transform.Scale = new Vector3(instance.Scale);
                    render.Transform.UpdateMatrix(true);

                    AddRender(render);
                }
            }
        }

        private void MakeActiveEditor()
        {
            Workspace.ActiveEditor = this;
            if (!Scene.IsInit())
            {
                // Make a dummy scene
                Scene = new GLScene();
                Scene.Init();
            }

            Workspace.ViewportWindow.Pipeline._context.Scene = Scene;
        }

        public void Save(Stream stream)
        {
            //string json = File.ReadAllText(FileInfo.FilePath);
            string json = JsonConvert.SerializeObject(EditorConfig);
            stream.Position = 0;
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(json);
            }
            //Save the editor data
            foreach (var fieldLoader in FieldLoaders) {
                string fieldName = fieldLoader.MapFile.RootNode.Parent.Header;
                string fileName = fieldLoader.MapFile.RootNode.Header;
                string sectionName = fileName.Substring(0, 3);
                string outPath = Path.GetFullPath(Path.Join(Path.GetDirectoryName(FileInfo.FilePath), $"{EditorConfig.FolderName}/aoc/0010/Map/{fieldName}/{sectionName}/{fileName}"));

                var bymlStream = new MemoryStream();
                fieldLoader.Save(bymlStream);

                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                var outStream = File.Open(outPath, FileMode.OpenOrCreate, FileAccess.Write);
                outStream.Write(YAZ0.Compress(bymlStream.ReadAllBytes()));
                outStream.SetLength(outStream.Position);
                outStream.Flush();
                outStream.Close();
            }
            foreach (var dungeonLoader in DungeonLoaders)
            {
                string dungeonFileName = dungeonLoader.RootNode.Header;
                string outPath = Path.GetFullPath(Path.Join(Path.GetDirectoryName(FileInfo.FilePath), $"{EditorConfig.FolderName}/content/Pack/{dungeonFileName}"));

                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                var outStream = File.Open(outPath, FileMode.OpenOrCreate, FileAccess.Write);
                dungeonLoader.Save(outStream);
                outStream.Flush();
                outStream.Close();
            }
        }

        private Vector2 GetSectionIndex(string mapName)
        {
            string[] values = mapName.Split("-");
            int sectionID = values[0][0] - 'A' - 1;
            int sectionRegion = int.Parse(values[1]);
            return new Vector2(sectionID, sectionRegion);
        }

        private void CacheBackgroundFiles()
        {
            {
                // Where to cache to
                string cache = PluginConfig.GetCachePath($"Images\\Terrain");
                if (!Directory.Exists(cache))
                {

                    // Get the data from the bfres
                    string path = PluginConfig.GetContentPath("Model\\Terrain.Tex1.sbfres");
                    var texs = CafeLibrary.Rendering.BfresLoader.GetTextures(path);
                    if (texs == null)
                        return;

                    Directory.CreateDirectory(cache); // Ensure that our cache folder exists
                    foreach (var tex in texs)
                    {
                        string outputPath = $"{cache}\\{tex.Key}";
                        if (tex.Value.RenderTexture is GLTexture2D)
                            ((GLTexture2D)tex.Value.RenderTexture).Save(outputPath);
                        else
                            ((GLTexture2DArray)tex.Value.RenderTexture).Save(outputPath);
                    }
                }
            }

            var titleBgPath = PluginConfig.GetContentPath("Pack\\TitleBG.pack");
            TitleBG = new SARC();
            TitleBG.Load(File.OpenRead(titleBgPath), "TitleBG.pack");
        }

        public override List<MenuItemModel> GetViewportMenuIcons()
        {
            List<MenuItemModel> menus = new List<MenuItemModel>();
            menus.Add(new MenuItemModel(""));
            menus.Add(new MenuItemModel($"{IconManager.COPY_ICON}", Scene.CopySelected));
            menus.Add(new MenuItemModel($"{IconManager.PASTE_ICON}", () => Scene.PasteSelected(GLContext.ActiveContext)));
            menus.Add(new MenuItemModel($"{IconManager.ADD_ICON}", () => CreateAndSelect()));
            menus.Add(new MenuItemModel($"{IconManager.DELETE_ICON}", Scene.DeleteSelected));
            return menus;
        }

        public override List<MenuItemModel> GetEditMenuItems()
        {
            List<MenuItemModel> menus = new List<MenuItemModel>();
            menus.Add(new MenuItemModel(""));
            menus.Add(new MenuItemModel($"   {IconManager.COPY_ICON}    {TranslationSource.GetText("COPY")}", Scene.CopySelected));
            menus.Add(new MenuItemModel($"   {IconManager.PASTE_ICON}    {TranslationSource.GetText("PASTE")}", () => Scene.PasteSelected(GLContext.ActiveContext)));
            menus.Add(new MenuItemModel($"   {IconManager.ADD_ICON}    {TranslationSource.GetText("CREATE")}", () => CreateAndSelect()));
            menus.Add(new MenuItemModel($"   {IconManager.DELETE_ICON}    {TranslationSource.GetText("REMOVE")}", Scene.DeleteSelected));
            return menus;
        }

        public override void OnKeyDown(KeyEventInfo e)
        {
            base.OnKeyDown(e);

            GLContext.ActiveContext.Scene.BeginUndoCollection();

            if (e.IsKeyDown(InputSettings.INPUT.Scene.Create))
                CreateAndSelect();

            GLContext.ActiveContext.Scene.EndUndoCollection();
        }

        public override void AssetViewportDrop(AssetItem item, Vector2 screenCoords)
        {
            if (!(item.Tag is IDictionary<string, dynamic>))
                return;

            var mapData = ActiveMapData.FirstOrDefault();

            //Actor data attached to map object assets
            var actorInfo = item.Tag as IDictionary<string, dynamic>;
            //Get the actor profile
            string profile = actorInfo.ContainsKey("profile") ? (string)actorInfo["profile"] : null;
            //Set nodebase parent as the profile assigned by actor
            NodeBase parent = null;
            if (profile != null)
            {
                var folder = mapData.ObjectFolder.Children.FirstOrDefault(x => x.Header == profile);
                if (folder == null)
                {
                    folder = new NodeBase(profile);
                    mapData.ObjectFolder.Children.Add(folder);
                }
                parent = folder;
            }
            //Determine what actor to add in via the name
            var name = actorInfo["name"];
            //Create the actor data and add it to the scene
            var obj = new MapObject(this);
            var index = mapData.Objs.Count;
            obj.CreateNew(GetHashId(mapData), name, actorInfo, parent, mapData);
            //Add to the viewport scene
            obj.AddToScene();

            var context = GLContext.ActiveContext;
            context.Scene.DeselectAll(context);

            //Set our transform
            Quaternion rotation = Quaternion.Identity;
            //Spawn by drag/drop coordinates in 3d space.
            Vector3 position = context.ScreenToWorld(screenCoords.X, screenCoords.Y, 100);
            //Face the camera
            if (GlobalSettings.Current.Asset.FaceCameraAtSpawn)
                rotation = Quaternion.FromEulerAngles(0, -context.Camera.RotationY, 0);
            //Drop to collision if used.
            CollisionDetection.SetObjectToCollision(context, context.CollisionCaster, screenCoords, ref position, ref rotation);
            obj.Render.Transform.Position = position;
            obj.Render.Transform.Rotation = rotation;
            obj.Render.Transform.UpdateMatrix(true);
            obj.Render.UINode.IsSelected = true;

            Workspace.ActiveWorkspace.ReloadOutliner();
        }

        public void CreateAndSelect()
        {
            var context = GLContext.ActiveContext;

            var mapData = ActiveMapData.FirstOrDefault();

            var actorInfo = GlobalData.Actors["LinkTagAnd"] as IDictionary<string, dynamic>;
            string profile = actorInfo.ContainsKey("profile") ? (string)actorInfo["profile"] : null;

            NodeBase parent = null;
            if (profile != null)
            {
                var folder = mapData.ObjectFolder.Children.FirstOrDefault(x => x.Header == profile);
                if (folder == null)
                {
                    folder = new NodeBase(profile);
                    mapData.ObjectFolder.Children.Add(folder);
                }
                parent = folder;
            }

            var obj = new MapObject(this);
            obj.CreateNew(GetHashId(mapData), "LinkTagAnd", actorInfo, parent, mapData);
            obj.AddToScene();

            Scene.DeselectAll(context);

            obj.Render.Transform.Position = context.ScreenToWorld(context.Width / 2, context.Height / 2, 5 * GLContext.PreviewScale);
            obj.Render.Transform.UpdateMatrix(true);

            obj.Render.IsSelected = true;

            Workspace.ActiveWorkspace.ReloadOutliner();
        }

        public void Dispose()
        {
            foreach (var dungeonLoader in DungeonLoaders)
                dungeonLoader.Dispose();
            foreach (var terrain in Terrains.Values)
                terrain.Dispose();
            foreach (var fieldLoader in FieldLoaders)
                fieldLoader?.Dispose();
        }

        public static uint GetHashId(MapData mapData)
        {
            int index = mapData.Objs.Count;
            while (true)
            {
                uint id = Crc32.Compute($"ID{index}");
                if (!mapData.Objs.ContainsKey(id))
                    return id;
                index++;
            }
        }
    }
}
