using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using MapStudio.UI;
using GLFrameworkEngine;
using OpenTK;
using UKingLibrary.Rendering;
using UKingLibrary.UI;
using Toolbox.Core;
using Toolbox.Core.Hashes.Cryptography;

namespace UKingLibrary
{
    public class MapMuuntEditor : IEditor
    {
        public static bool ShowVisibleActors = true;
        public static bool ShowInvisibleActors = true;
        public static bool ShowMapModel = true;
        public static bool ShowActorLinks = true;

        /// <summary>
        /// The name of the editor instance.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The optional tool window for drawing the tool UI.
        /// </summary>
        public IToolWindowDrawer ToolWindowDrawer { get; set; }

        private string subEditor;

        /// <summary>
        /// The active sub editor to filter other editors.
        /// </summary>
        public string SubEditor
        {
            get { return subEditor; }
            set
            {
                if (subEditor != value)
                {
                    subEditor = value;
                }
            }
        }

        private List<string> ActorProfiles = new List<string>();

        /// <summary>
        /// Gets a list of possible sub editors.
        /// </summary>
        public List<string> SubEditors => ActorProfiles;

        /// <summary>
        /// Gets or sets the list of tree nodes used for the outlier.
        /// </summary>
        public List<NodeBase> Nodes { get; set; } = new List<NodeBase>();

        /// <summary>
        /// Gets a list of menu icons for the viewport window.
        /// </summary>
        public List<MenuItemModel> GetViewportMenuIcons()
        {
            return new List<MenuItemModel>();
        }

        /// <summary>
        /// Gets a list of menu icons for the outlier filter menu.
        /// </summary>
        public List<MenuItemModel> GetFilterMenuItems()
        {
            return new List<MenuItemModel>();
        }

        /// <summary>
        /// Gets a list of menu icons for the edit menu in the main window.
        /// </summary>
        public List<MenuItemModel> GetEditMenuItems()
        {
            return new List<MenuItemModel>();
        }

        private List<MapData> MapFiles;

        public void Load(List<MapData> mapFiles)
        {
            //Allow actor objects to be loaded in the asset list
            Workspace.ActiveWorkspace.AddAssetCategory(new AssetViewMapObject());

            ToolWindowDrawer = new MubinToolSettings();

            MapFiles = mapFiles;

            this.Nodes.Clear();
            foreach (var map in mapFiles)
                this.Nodes.AddRange(map.Nodes);
        }

        public void LoadProd(ProdInfo prodInfo, string fileName)
        {
            NodeBase folder = new NodeBase(fileName);
            Nodes.Add(folder);

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

                    GLContext.ActiveContext.Scene.AddRenderObject(render);
                }
            }
        }

        public void AssetViewportDrop(AssetItem item, Vector2 screenCoords) 
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
                if (folder == null) {
                    folder = new NodeBase(profile);
                    mapData.ObjectFolder.Children.Add(folder);
                }
                parent = folder;
            }
            //Determine what actor to add in via the name
            var name = actorInfo["name"];
            //Create the actor data and add it to the scene
            var obj = new MapObject();
            var index = mapData.Objs.Count;
            obj.CreateNew(Crc32.Compute($"ID{index}"), name);
            obj.LoadActor((MapMuuntEditor)Workspace.ActiveWorkspace.ActiveEditor, obj.Properties, actorInfo, parent);
            mapData.Objs.Add(obj.HashId, obj);
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

        public void OnMouseMove() { }
        public void OnMouseDown() { }
        public void OnMouseUp() { }
        public void OnSave(ProjectResources resources) { }

        public void OnKeyDown() {
            GLContext.ActiveContext.Scene.BeginUndoCollection();

            foreach (var mubin in MapFiles)
            {
                foreach (var rail in mubin.Rails.Values)
                    rail.OnKeyDown();
            }

            GLContext.ActiveContext.Scene.EndUndoCollection();
        }

        public void Dispose()
        {
            foreach (var mubin in MapFiles)
                mubin.Dispose();
        }
    }
}
