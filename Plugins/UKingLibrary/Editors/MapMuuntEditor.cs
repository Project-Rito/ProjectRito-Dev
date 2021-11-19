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
                    FilterActorProfiles(value);
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

        public Dictionary<uint, MapObject> Objs = new Dictionary<uint, MapObject>();
        public Dictionary<uint, RailPathData> Rails = new Dictionary<uint, RailPathData>();

        public void LoadFile(dynamic mubinRoot, string fileName, bool isStatic)
        {
            Dictionary<string, NodeBase> nodeFolders = new Dictionary<string, NodeBase>();

            ActorProfiles.Clear();
            ActorProfiles.Add("Default");

            //Allow actor objects to be loaded in the asset list
            Workspace.ActiveWorkspace.AddAssetCategory(new AssetViewMapObject());

            NodeBase ObjectFolder = new NodeBase(TranslationSource.GetText("OBJECTS"));
            NodeBase RailFolder = new NodeBase(TranslationSource.GetText("RAIL_PATHS"));

            NodeBase folder = new NodeBase(fileName);
            folder.AddChild(ObjectFolder);
            folder.AddChild(RailFolder);

            Nodes.Add(folder);

            int numObjs = mubinRoot["Objs"].Count;

            ProcessLoading.Instance.Update(70, 100, "Loading map objs");

            foreach (var obj in     mubinRoot["Objs"]) // Your spacebar broken
            {
                var mapObj = (IDictionary<string, dynamic>)obj;
                if (mapObj.ContainsKey("UnitConfigName"))
                {
                    string name = mapObj["UnitConfigName"];
                    //Get the actor in the database
                    var actor = GlobalData.Actors[name] as IDictionary<string, dynamic>;
                    //Get the actor profile
                    string profile = actor.ContainsKey("profile") ? (string)actor["profile"] : null;

                    //Set nodebase parent as the profile assigned by actor
                    NodeBase parent = null;
                    if (profile != null)
                    {
                        if (!nodeFolders.ContainsKey(profile)) {
                            nodeFolders.Add(profile, new NodeBase(profile));
                            nodeFolders[profile].HasCheckBox = true; //Allow checking

                            ActorProfiles.Add(profile);
                        }
                        parent = nodeFolders[profile];
                    }

                    //Setup properties for editing
                    MapObject data = new MapObject();
                    if (profile == "Area")
                        data = new MapObjectArea();

                    data.IsStatic = isStatic;
                    data.LoadActor(this, mapObj, actor, parent);
                    Objs.Add(data.HashId, data);
                    //Add the renderable to the viewport
                    data.AddToScene();
                }
            }

            ProcessLoading.Instance.Update(90, 100, "Loading map rails");

            foreach (var obj in mubinRoot["Rails"])
            {
                RailPathData data = new RailPathData();
                data.IsStatic = isStatic;
                data.LoadRail(this, obj, RailFolder);

                string name = obj["UnitConfigName"];
                if (obj.ContainsKey("UniqueName"))
                    name = obj["UniqueName"];

                data.PathRender.UINode.Header = name;
                data.AddToScene();
            }

            ToolWindowDrawer = new MubinToolSettings();

            //Prepare object links
            foreach (var obj in Objs.Values)
            {
                 if (obj.Properties.ContainsKey("LinksToObj")) {
                    foreach (IDictionary<string, dynamic> link in obj.Properties["LinksToObj"]) {
                        uint dest = link["DestUnitHashId"];
                        //Rendered links
                        obj.Render.DestObjectLinks.Add(Objs[dest].Render);
                        //Add a link instance aswell for keeping track of both source and dest links
                        obj.AddLink(new MapObject.LinkInstance()
                        {
                            Properties = link,
                            Object = Objs[dest],
                        });

                        //Set LODs
                        if (link["DefinitionName"] == "PlacementLOD")
                            Objs[dest].Render.IsVisible = false;
                    }
                }
            }

            //Load all the outliner nodes into the editor
            foreach (var node in nodeFolders.OrderBy(x => x.Key))
                ObjectFolder.AddChild(node.Value);

            // Todo - set camera position to map pos

            ProcessLoading.Instance.Update(100, 100, "Finished!");
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

        public void SaveFile(dynamic mubinRoot, bool isStatic)
        {
            List<dynamic> objs = new List<dynamic>();
            List<dynamic> rails = new List<dynamic>();

            //Save actors in order by their hash ID
            foreach (var obj in Objs.OrderBy(x => x.Key)) {
                if (obj.Value.IsStatic != isStatic)
                    continue;

                obj.Value.SaveActor();
                //Add objs
                objs.Add(obj.Value.Properties);
            }
            //Save rails in order by their hash ID
            foreach (var rail in Rails.OrderBy(x => x.Key)) {
                if (rail.Value.IsStatic != isStatic)
                    continue;

                rail.Value.SaveRail();
                //Add rails
                rails.Add(rail.Value.Properties);
            }
            //Apply the object and rails to the file
            BymlHelper.SetValue(mubinRoot, "Objs", objs);
            BymlHelper.SetValue(mubinRoot, "Rails", rails);
        }

        void FilterActorProfiles(string text)
        {
            foreach (var objData in Objs.Values)
            {
                //Toggle visiblity based on the profile tag
                if (objData.ActorInfo["profile"] == text || text == "Default")
                    objData.Render.CanSelect = true;
                else
                    objData.Render.CanSelect = false;
            }
        }

        public void AssetViewportDrop(AssetItem item, Vector2 screenCoords) 
        {
            if (!(item.Tag is IDictionary<string, dynamic>))
                return;

            //Actor data attached to map object assets
            var actorInfo = item.Tag as IDictionary<string, dynamic>;
            //Get the actor profile
            string profile = actorInfo.ContainsKey("profile") ? (string)actorInfo["profile"] : null;
            //Set nodebase parent as the profile assigned by actor
            NodeBase parent = null;
            if (profile != null)
            {
                var folder = Nodes.FirstOrDefault(x => x.Header == profile);
                if (folder == null) {
                    folder = new NodeBase(profile);
                    Nodes.Add(folder);
                }
                parent = folder;
            }
            //Determine what actor to add in via the name
            var name = actorInfo["name"];
            //Create the actor data and add it to the scene
            var obj = new MapObject();
            var index = Objs.Count;
            obj.CreateNew(Crc32.Compute($"ID{index}"), name);
            obj.LoadActor(this, obj.Properties, actorInfo, parent);
            Objs.Add(obj.HashId, obj);
            //Add to the viewport scene
            obj.AddToScene();

            var context = GLContext.ActiveContext;
            context.Scene.DeselectAll(context);

            //Set our transform
            Quaternion rotation = Quaternion.Identity;
            //Spawn by drag/drop coordinates in 3d space.
            Vector3 position = context.CoordFor(screenCoords.X, screenCoords.Y, 100);
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

            foreach (var rail in Rails.Values)
                rail.OnKeyDown();

            GLContext.ActiveContext.Scene.EndUndoCollection();
        }

        public void Dispose()
        {
            foreach (var objData in Objs.Values)
                objData.Render?.Dispose();
            foreach (var rail in Rails.Values)
                rail.PathRender?.Dispose();
        }
    }
}
