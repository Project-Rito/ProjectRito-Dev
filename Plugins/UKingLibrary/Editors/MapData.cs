using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using MapStudio.UI;
using Toolbox.Core;
using Toolbox.Core.IO;
using ByamlExt.Byaml;
using CafeLibrary.Rendering;

namespace UKingLibrary
{
    public class MapData
    {
        public static bool ShowVisibleActors = true;
        public static bool ShowInvisibleActors = true;
        public static bool ShowMapModel = true;
        public static bool ShowActorLinks = true;

        public Dictionary<uint, MapObject> Objs = new Dictionary<uint, MapObject>();
        public Dictionary<uint, RailPathData> Rails = new Dictionary<uint, RailPathData>();

        public NodeBase RootNode = new NodeBase();

        public NodeFolder ObjectFolder = new NodeFolder(TranslationSource.GetText("OBJECTS"));
        public NodeFolder RailFolder = new NodeFolder(TranslationSource.GetText("RAIL_PATHS"));

        BymlFileData Byaml;
        STFileLoader.Settings FileSettings;

        public MapData() { }

        public MapData(Stream stream, IMapLoader parentLoader, string fileName) {
            Load(stream, parentLoader, fileName);
        }

        public void Load(Stream stream, IMapLoader parentLoader, string fileName) {
            FileSettings = STFileLoader.TryDecompressFile(stream, fileName);

            Load(ByamlFile.LoadN(FileSettings.Stream), parentLoader, fileName);
        }

        public void Load(BymlFileData byaml, IMapLoader parentLoader, string fileName)
        {
            Byaml = byaml;

            Dictionary<string, NodeBase> nodeFolders = new Dictionary<string, NodeBase>();

            RootNode = new NodeBase(fileName) { Tag = this};
            RootNode.AddChild(ObjectFolder);
            RootNode.AddChild(RailFolder);

            int numObjs = byaml.RootNode["Objs"].Count;

            ProcessLoading.Instance.Update(70, 100, "Loading map objs");

            for (int i = 0; i < numObjs; i++)
            {
                var mapObj = (IDictionary<string, dynamic>)byaml.RootNode["Objs"][i];
                if (mapObj.ContainsKey("UnitConfigName"))
                {
                    string name = mapObj["UnitConfigName"];
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

            if (byaml.RootNode.ContainsKey("Rails"))
            {
                foreach (var obj in byaml.RootNode["Rails"])
                {
                    RailPathData data = new RailPathData();
                    data.LoadRail(this, obj, RailFolder);

                    string name = obj["UnitConfigName"];
                    if (obj.ContainsKey("UniqueName"))
                        name = obj["UniqueName"];

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
            if (byaml.RootNode.ContainsKey("LocationPosX") && byaml.RootNode.ContainsKey("LocationPosZ"))
            {
                float posX = byaml.RootNode["LocationPosX"];
                float posY = 150;
                float posZ = byaml.RootNode["LocationPosZ"];
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
            var byamlData = Byaml;
            byamlData.RootNode = PropertiesToValues(Byaml.RootNode);
            ByamlFile.SaveN(stream, byamlData);
            stream.Position = 0;

            
            Stream compressed = FileSettings.CompressionFormat.Compress(stream);
            stream.Position = 0;
            compressed.CopyTo(stream);
            stream.SetLength(stream.Position);
        }

        private void SaveEditor()
        {
            List<dynamic> objs = new List<dynamic>();
            List<dynamic> rails = new List<dynamic>();

            //Save actors in order by their hash ID
            foreach (var obj in Objs.OrderBy(x => x.Key))
            {
                obj.Value.SaveActor();
                //Add objs
                objs.Add(obj.Value.Properties);
            }
            //Save rails in order by their hash ID
            foreach (var rail in Rails.OrderBy(x => x.Key))
            {
                rail.Value.SaveRail();
                //Add rails
                rails.Add(rail.Value.Properties);
            }
            //Apply the object and rails to the file
            BymlHelper.SetValue(Byaml.RootNode, "Objs", objs);
            BymlHelper.SetValue(Byaml.RootNode, "Rails", rails);
        }

        public void Dispose()
        {
            foreach (var objData in Objs.Values)
                objData.Render?.Dispose();
            foreach (var rail in Rails.Values)
                rail.PathRender?.Dispose();
        }

        public static dynamic ValuesToProperties(dynamic input)
        {
            if (input is IDictionary<string, dynamic>)
            {
                IDictionary<string, dynamic> output = new Dictionary<string, dynamic>();
                foreach (KeyValuePair<string, dynamic> pair in input)
                {
                    if (pair.Value is IDictionary<string, dynamic>)
                        output.Add(pair.Key, ValuesToProperties(pair.Value));
                    else if (pair.Value is IList<dynamic>)
                        output.Add(pair.Key, ValuesToProperties(pair.Value));
                    else
                        output.Add(pair.Key, new MapData.Property<dynamic>(pair.Value));
                }
                return output;
            }
            else if (input is IList<dynamic>)
            {
                IList<dynamic> output = new List<dynamic>();
                foreach (dynamic item in input)
                {
                    if (item is IDictionary<string, dynamic>)
                        output.Add(ValuesToProperties(item));
                    else if (item is IList<dynamic>)
                        output.Add(ValuesToProperties(item));
                    else
                        output.Add(new MapData.Property<dynamic>(item));
                }
                return output;
            }
            else
            {
                return new MapData.Property<dynamic>(input);
            }
        }

        public static dynamic PropertiesToValues(dynamic input)
        {
            if (input is IDictionary<string, dynamic>)
            {
                IDictionary<string, dynamic> output = new Dictionary<string, dynamic>();
                foreach (KeyValuePair<string, dynamic> pair in input)
                {
                    if (pair.Value is IDictionary<string, dynamic>)
                        output.Add(pair.Key, PropertiesToValues(pair.Value));
                    else if (pair.Value is IList<dynamic>)
                        output.Add(pair.Key, PropertiesToValues(pair.Value));
                    else if (pair.Value is MapData.Property<dynamic>)
                        output.Add(pair.Key, pair.Value.Value);
                    else
                        output.Add(pair.Key, pair.Value);
                }
                return output;
            }
            else if (input is IList<dynamic>)
            {
                IList<dynamic> output = new List<dynamic>();
                foreach (dynamic item in input)
                {
                    if (item is IDictionary<string, dynamic>)
                        output.Add(PropertiesToValues(item));
                    else if (item is IList<dynamic>)
                        output.Add(PropertiesToValues(item));
                    else if (item is MapData.Property<dynamic>)
                        output.Add(item.Value);
                    else
                        output.Add(item);
                }
                return output;
            }
            else
            {
                return new MapData.Property<dynamic>(input);
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
