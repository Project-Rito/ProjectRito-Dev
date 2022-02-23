using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MapStudio.UI;
using Toolbox.Core;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using Toolbox.Core.Hashes.Cryptography;
using UKingLibrary.UI;
using OpenTK;

namespace UKingLibrary
{
    public class UKingEditor : FileEditor, IFileFormat, IDisposable
    {
        public string[] Description => new string[] { "Compressed Map Unit Binary" };
        public string[] Extension => new string[] { "*.smubin", ".pack" };

        /// <summary>
        /// Whether or not the file can be saved.
        /// </summary>
        public bool CanSave { get; set; } = true;

        /// <summary>
        /// Information of the loaded file.
        /// </summary>
        public File_Info FileInfo { get; set; }

        //Current map muunt files
        List<MapData> MapFiles;

        //Editor loaders
        FieldMapLoader FieldMapLoader;
        DungeonMapLoader DungeonLoader;

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            //Just load maps from checking the smubin extension atm.
            return fileInfo.Extension == ".smubin" || fileInfo.Extension == ".pack";
        }

        public UKingEditor() 
        {
        }

        public void Load(Stream stream)
        {
            ToolWindowDrawer = new MubinToolSettings();

            Root.Header = FileInfo.FileName;
            Root.Children.Clear();
            if (FileInfo.Extension == ".pack")
                LoadDungeon(FileInfo.FileName, stream);
            else
                LoadField(FileInfo.FileName, stream);

            LoadAssetList();
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
            //Load file data
            DungeonLoader = new DungeonMapLoader();
            DungeonLoader.Load(this, fileName, stream);
            MapFiles = DungeonLoader.MapFiles;

            //Add objects
            foreach (var map in DungeonLoader.MapFiles)
            {
                //Add tree from muunt files
                Root.AddChild(map.RootNode);
            }
            //Add rendered map model
            this.AddRender(DungeonLoader.MapRender);
        }

        private void LoadField(string fileName, Stream stream)
        {
            FieldMapLoader = new FieldMapLoader();
            FieldMapLoader.Load(this, fileName, stream);
            MapFiles = FieldMapLoader.MapFiles;
            foreach (var map in FieldMapLoader.MapFiles)
                Root.AddChild(map.RootNode);
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

        public void Save(Stream stream)
        {
            //Save the editor data
            if (DungeonLoader != null)
                DungeonLoader.Save(stream);
            else
                FieldMapLoader.Save(stream);
        }

        public override List<MenuItemModel> GetViewportMenuIcons()
        {
            List<MenuItemModel> menus = new List<MenuItemModel>();
            menus.Add(new MenuItemModel(""));
            menus.Add(new MenuItemModel($"{IconManager.COPY_ICON}", Scene.CopySelected));
            menus.Add(new MenuItemModel($"{IconManager.PASTE_ICON}", () => Scene.PasteSelected(GLContext.ActiveContext)));
            menus.Add(new MenuItemModel($"{IconManager.ADD_ICON}", () => CreateAndSelect(GLContext.ActiveContext)));
            menus.Add(new MenuItemModel($"{IconManager.DELETE_ICON}", Scene.DeleteSelected));
            return menus;
        }

        public override List<MenuItemModel> GetEditMenuItems()
        {
            List<MenuItemModel> menus = new List<MenuItemModel>();
            menus.Add(new MenuItemModel(""));
            menus.Add(new MenuItemModel($"   {IconManager.COPY_ICON}    {TranslationSource.GetText("COPY")}", Scene.CopySelected));
            menus.Add(new MenuItemModel($"   {IconManager.PASTE_ICON}    {TranslationSource.GetText("PASTE")}", () => Scene.PasteSelected(GLContext.ActiveContext)));
            menus.Add(new MenuItemModel($"   {IconManager.ADD_ICON}    {TranslationSource.GetText("CREATE")}", () => CreateAndSelect(GLContext.ActiveContext)));
            menus.Add(new MenuItemModel($"   {IconManager.DELETE_ICON}    {TranslationSource.GetText("REMOVE")}", Scene.DeleteSelected));
            return menus;
        }

        public override void OnKeyDown(KeyEventInfo e)
        {
            base.OnKeyDown(e);

            GLContext.ActiveContext.Scene.BeginUndoCollection();

            if (e.IsKeyDown(InputSettings.INPUT.Scene.Create))
                CreateAndSelect(GLContext.ActiveContext);

            GLContext.ActiveContext.Scene.EndUndoCollection();
        }

        public override void AssetViewportDrop(AssetItem item, Vector2 screenCoords)
        {
            if (!(item.Tag is IDictionary<string, dynamic>))
                return;

            var mapData = MapFiles.FirstOrDefault();

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

        public void CreateAndSelect(GLContext context)
        {
            var mapData = MapFiles.FirstOrDefault();

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

            context.Scene.DeselectAll(context);

            obj.Render.Transform.Position = context.ScreenToWorld(context.Width / 2, context.Height / 2, 5 * GLContext.PreviewScale);
            obj.Render.Transform.UpdateMatrix(true);

            obj.Render.IsSelected = true;

            Workspace.ActiveWorkspace.ReloadOutliner();
        }

        public void Dispose()
        {
            if (DungeonLoader != null)
                DungeonLoader.Dispose();
            if (FieldMapLoader != null)
                FieldMapLoader.Dispose();
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
