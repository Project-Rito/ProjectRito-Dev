using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using MapStudio.UI;
using Toolbox.Core;
using Toolbox.Core.IO;
using Nintendo.Byml;
using CafeLibrary.Rendering;

namespace UKingLibrary
{
    public class MapData
    {
        public static bool ShowVisibleActors = true;
        public static bool ShowInvisibleActors = true;
        public static bool ShowMapModel = true;
        public static bool ShowActorLinks = true;
        public static bool ShowCollisionShapes = false;

        public Dictionary<uint, MapObject> Objs = new Dictionary<uint, MapObject>();
        public Dictionary<uint, RailPathData> Rails = new Dictionary<uint, RailPathData>();

        public NodeBase RootNode = new NodeBase();

        public NodeFolder ObjectFolder = new NodeFolder(TranslationSource.GetText("OBJECTS"));
        public NodeFolder RailFolder = new NodeFolder(TranslationSource.GetText("RAIL_PATHS"));

        BymlFile Byaml;
        STFileLoader.Settings FileSettings;

        public MapData() { }

        public MapData(Stream stream, IMapLoader parentLoader, string fileName) {
            Load(stream, parentLoader, fileName);
        }

        public void Load(Stream stream, IMapLoader parentLoader, string fileName) {
            FileSettings = STFileLoader.TryDecompressFile(stream, fileName);

            Load(BymlFile.FromBinary(FileSettings.Stream), parentLoader, fileName);
        }

        public void Load(BymlFile byaml, IMapLoader parentLoader, string fileName)
        {
            Byaml = byaml;

            Dictionary<string, NodeBase> nodeFolders = new Dictionary<string, NodeBase>();

            RootNode = new NodeBase(fileName) { Tag = this};
            RootNode.AddChild(ObjectFolder);
            RootNode.AddChild(RailFolder);

            int numObjs = byaml.RootNode.Hash["Objs"].Array.Count;

            ProcessLoading.Instance.Update(70, 100, "Loading map objs");

            for (int i = 0; i < numObjs; i++)
            {
                BymlNode mapObj = byaml.RootNode.Hash["Objs"].Array[i];
                if (mapObj.Hash.ContainsKey("UnitConfigName"))
                {
                    string name = mapObj.Hash["UnitConfigName"].String;
                    //Get the actor in the database
                    var actorInfo = new Dictionary<string, dynamic>();
                    if (GlobalData.Actors.ContainsKey(name))
                        actorInfo = GlobalData.Actors[name] as Dictionary<string, dynamic>;
                    //Get the actor profile
                    string profile = actorInfo.ContainsKey("profile") ? (string)actorInfo["profile"] : null;

                    //Set nodebase parent as the profile assigned by actor
                    NodeBase parent = null;
                    if (profile != null)
                    {
                        if (!nodeFolders.ContainsKey(profile))
                        {
                            nodeFolders.Add(profile, new NodeBase(profile));
                            nodeFolders[profile].HasCheckBox = true; //Allow checking
                        }
                        parent = nodeFolders[profile];
                    }
                    else
                    {
                        if (!nodeFolders.ContainsKey("Unknown"))
                        {
                            nodeFolders.Add("Unknown", new NodeBase("Unknown"));
                            nodeFolders["Unknown"].HasCheckBox = true; //Allow checking
                        }
                        parent = nodeFolders["Unknown"];
                    }

                    //Setup properties for editing
                    MapObject obj = new MapObject(parentLoader);

                    obj.CreateNew(mapObj, actorInfo, parent, this);
                    //Add the renderable to the viewport
                    obj.AddToScene();
                }
                ProcessLoading.Instance.Update((((i+1) * 40)/numObjs) + 70, 100, "Loading map objs");
            }

            ProcessLoading.Instance.Update(90, 100, "Loading map rails");

            if (byaml.RootNode.Hash.ContainsKey("Rails"))
            {
                foreach (var obj in byaml.RootNode.Hash["Rails"].Array)
                {
                    RailPathData data = new RailPathData();
                    data.LoadRail(this, obj, RailFolder);

                    string name = obj.Hash["UnitConfigName"].String;
                    if (obj.Hash.ContainsKey("UniqueName"))
                        name = obj.Hash["UniqueName"].String;

                    data.PathRender.UINode.Header = name;
                    data.AddToScene();
                }
            }

            //Prepare object links
            foreach (var obj in Objs.Values)
            {
                if (obj.Properties.ContainsKey("LinksToObj"))
                {
                    foreach (IDictionary<string, dynamic> link in obj.Properties["LinksToObj"])
                    {
                        uint dest = link["DestUnitHashId"].Value;

                        if (!Objs.ContainsKey(dest)) // Make sure the dest object exists in this file
                        {
                            StudioLogger.WriteWarning($"Cannot resolve link! Actor HashId: {obj.HashId} Link HashId: {dest}"); 
                            continue;
                        }
                            
                        //Rendered links
                        obj.Render.DestObjectLinks.Add(Objs[dest].Render);
                        //Add a link instance aswell for keeping track of both source and dest links
                        obj.AddLink(new MapObject.LinkInstance()
                        {
                            Properties = link,
                            Object = Objs[dest],
                        });

                        //Set LODs
                        if (link["DefinitionName"].Value == "PlacementLOD")
                            Objs[dest].Render.IsVisible = false;
                    }
                }
            }

            //Load all the outliner nodes into the editor
            ObjectFolder.FolderChildren = nodeFolders;

            // Set the camera position to the map pos
            if (byaml.RootNode.Hash.ContainsKey("LocationPosX") && byaml.RootNode.Hash.ContainsKey("LocationPosZ"))
            {
                float posX = byaml.RootNode.Hash["LocationPosX"].Float;
                float posY = 150;
                float posZ = byaml.RootNode.Hash["LocationPosZ"].Float;
                GLContext.ActiveContext.Camera.TargetPosition = new OpenTK.Vector3(posX, posY, posZ) * GLContext.PreviewScale;
            }
            // Fallback
            else
            {
                try
                {
                    float posX = Objs.First().Value.Properties["Translate"][0].Value;
                    float posY = Objs.First().Value.Properties["Translate"][1].Value + 150;
                    float posZ = Objs.First().Value.Properties["Translate"][2].Value;
                    GLContext.ActiveContext.Camera.TargetPosition = new OpenTK.Vector3(posX, posY, posZ) * GLContext.PreviewScale;
                } catch { }
            }

            ProcessLoading.Instance.Update(100, 100, "Finished!");
        }

        public NodeBase AddObject(MapObject obj, IMapLoader parentLoader)
        {
            Dictionary<string, NodeBase> nodeFolders = ObjectFolder.FolderChildren;

            string profile = obj.ActorInfo.ContainsKey("profile") ? (string)obj.ActorInfo["profile"] : null;

            //Set nodebase parent as the profile assigned by actor
            NodeBase parent = null;
            if (profile != null)
            {
                if (!nodeFolders.ContainsKey(profile))
                {
                    nodeFolders.Add(profile, new NodeBase(profile));
                    nodeFolders[profile].HasCheckBox = true; //Allow checking
                }
                parent = nodeFolders[profile];
            }
            else
            {
                if (!nodeFolders.ContainsKey("Unknown"))
                {
                    nodeFolders.Add("Unknown", new NodeBase("Unknown"));
                    nodeFolders["Unknown"].HasCheckBox = true; //Allow checking
                }
                parent = nodeFolders["Unknown"];
            }
            parent.AddChild(obj.Render.UINode);
            ObjectFolder.FolderChildren = nodeFolders;

            parentLoader.ParentEditor.Workspace.ScrollToSelectedNode(obj.Render.UINode);
            return parent;
        }

        public void Save(Stream stream)
        {
            SaveEditor();
            var bymlData = Byaml;
            bymlData.RootNode = PropertiesToValues(Byaml.RootNode);
            stream.Write(bymlData.ToBinary());
            stream.Position = 0;

            
            Stream compressed = FileSettings.CompressionFormat.Compress(stream);
            stream.Position = 0;
            compressed.CopyTo(stream);
            stream.SetLength(stream.Position);
        }

        private void SaveEditor()
        {
            List<BymlNode> objs = new List<BymlNode>();
            List<BymlNode> rails = new List<BymlNode>();

            //Save actors in order by their hash ID
            foreach (var obj in Objs.OrderBy(x => x.Key))
            {
                obj.Value.SaveActor();
                //Add objs
                objs.Add(PropertiesToValues(obj.Value.Properties));
            }
            //Save rails in order by their hash ID
            foreach (var rail in Rails.OrderBy(x => x.Key))
            {
                rail.Value.SaveRail();
                //Add rails
                rails.Add(PropertiesToValues(rail.Value.Properties));
            }
            //Apply the object and rails to the file
            BymlHelper.SetValue(Byaml.RootNode, "Objs", new BymlNode(objs));
            BymlHelper.SetValue(Byaml.RootNode, "Rails", new BymlNode(rails));
        }

        public void Dispose()
        {
            foreach (var objData in Objs.Values)
                objData.Render?.Dispose();
            foreach (var rail in Rails.Values)
                rail.PathRender?.Dispose();
        }

        public static dynamic ValuesToProperties(BymlNode input)
        {
            if (input.Type == NodeType.Hash)
            {
                IDictionary<string, dynamic> output = new Dictionary<string, dynamic>();
                foreach (KeyValuePair<string, BymlNode> pair in input.Hash)
                {
                    if (pair.Value.Type == NodeType.Hash)
                        output.Add(pair.Key, ValuesToProperties(pair.Value));
                    else if (pair.Value.Type == NodeType.Array)
                        output.Add(pair.Key, ValuesToProperties(pair.Value));
                    else
                        output.Add(pair.Key, new MapData.Property<dynamic>(pair.Value.GetDynamic()));
                }
                return output;
            }
            else if (input.Type == NodeType.Array)
            {
                IList<dynamic> output = new List<dynamic>();
                foreach (BymlNode item in input.Array)
                {
                    if (item.Type == NodeType.Hash)
                        output.Add(ValuesToProperties(item));
                    else if (item.Type == NodeType.Array)
                        output.Add(ValuesToProperties(item));
                    else
                        output.Add(new MapData.Property<dynamic>(item.GetDynamic()));
                }
                return output;
            }
            else
            {
                return new MapData.Property<dynamic>(input);
            }
        }

        public static BymlNode PropertiesToValues(dynamic input)
        {
            if (input is IDictionary<string, dynamic>)
            {
                BymlNode output = new BymlNode(new Dictionary<string, BymlNode>(input.Count));
                foreach (KeyValuePair<string, dynamic> pair in input)
                {
                    if (pair.Value is IDictionary<string, dynamic>)
                        output.Hash.Add(pair.Key, PropertiesToValues(pair.Value));
                    else if (pair.Value is IList<dynamic>)
                        output.Hash.Add(pair.Key, PropertiesToValues(pair.Value));
                    else if (pair.Value is MapData.Property<dynamic>)
                        output.Hash.Add(pair.Key, new BymlNode(pair.Value.Value));
                    else
                        output.Hash.Add(pair.Key, new BymlNode(pair.Value)); // This would indicate that the structure isn't fully made up of Properties
                }
                return output;
            }
            else if (input is IList<dynamic>)
            {
                BymlNode output = new BymlNode(new List<BymlNode>(input.Count));
                foreach (dynamic item in input)
                {
                    if (item is IDictionary<string, dynamic>)
                        output.Array.Add(PropertiesToValues(item));
                    else if (item is IList<dynamic>)
                        output.Array.Add(PropertiesToValues(item));
                    else if (item is MapData.Property<dynamic>)
                        output.Array.Add(new BymlNode(item.Value));
                    else
                        output.Array.Add(new BymlNode(item)); // This would indicate that the structure isn't fully made up of Properties
                }
                return output;
            }
            else if (input is BymlNode)
            {
                return input;
            }
            else
            {
                return new BymlNode(input);
            }
        }

        public class Property<T> : ICloneable
        {
            public Property(T value)
            {
                Value = value;
            }
            public object Clone()
            {
                return new Property<T>(Value);
            }
            public T Value;
            public bool Invalid; // Todo - make this a delegate to save memory, IsInvalid() or something.
        }
    }
}
