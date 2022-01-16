using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using MapStudio.UI;
using ByamlExt.Byaml;

namespace UKingLibrary
{
    public class MapData
    {
        public Dictionary<uint, MapObject> Objs = new Dictionary<uint, MapObject>();
        public Dictionary<uint, RailPathData> Rails = new Dictionary<uint, RailPathData>();

        public List<NodeBase> Nodes = new List<NodeBase>();

        public NodeBase ObjectFolder = new NodeBase(TranslationSource.GetText("OBJECTS"));
        public NodeBase RailFolder = new NodeBase(TranslationSource.GetText("RAIL_PATHS"));

        BymlFileData Byaml;

        public MapData() { }

        public MapData(Stream stream, string fileName) {
            Load(stream, fileName);
        }

        public void Load(Stream stream, string fileName) {
            Load(ByamlFile.LoadN(stream), fileName);
        }

        public void Load(BymlFileData byaml, string fileName)
        {
            Byaml = byaml;

            Dictionary<string, NodeBase> nodeFolders = new Dictionary<string, NodeBase>();

            NodeBase folder = new NodeBase(fileName);
            folder.AddChild(ObjectFolder);
            folder.AddChild(RailFolder);

            Nodes.Add(folder);

            int numObjs = byaml.RootNode["Objs"].Count;

            ProcessLoading.Instance.Update(70, 100, "Loading map objs");

            foreach (var obj in byaml.RootNode["Objs"]) // Your spacebar broken
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
                        if (!nodeFolders.ContainsKey(profile))
                        {
                            nodeFolders.Add(profile, new NodeBase(profile));
                            nodeFolders[profile].HasCheckBox = true; //Allow checking
                        }
                        parent = nodeFolders[profile];
                    }

                    //Setup properties for editing
                    MapObject data = new MapObject();

                    data.LoadActor((MapMuuntEditor)Workspace.ActiveWorkspace.ActiveEditor, mapObj, actor, parent);
                    Objs.Add(data.HashId, data);
                    //Add the renderable to the viewport
                    data.AddToScene();
                }
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
            foreach (var node in nodeFolders.OrderBy(x => x.Key))
                ObjectFolder.AddChild(node.Value);

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

        public void Save(Stream stream)
        {
            SaveEditor();
            var byamlData = Byaml;
            byamlData.RootNode = PropertiesToValues(Byaml.RootNode);
            ByamlFile.SaveN(stream, byamlData);
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
