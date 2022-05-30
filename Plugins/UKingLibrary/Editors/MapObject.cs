using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GLFrameworkEngine;
using OpenTK;
using MapStudio.UI;
using Toolbox.Core;
using Toolbox.Core.ViewModels;
using Toolbox.Core.Hashes.Cryptography;
using UKingLibrary.UI;
using UKingLibrary.Rendering;
using CafeLibrary.Rendering;
using Toolbox.Core.Animations;
using ImGuiNET;
using HKX2;

namespace UKingLibrary
{
    public class MapObject : ActorBase, ICloneable
    {
        /// <summary>
        /// The file this is in
        /// </summary>
        MapData MapData;

        /// <summary>
        /// Properties related to the object instance.
        /// </summary>
        public IDictionary<string, dynamic> Properties;

        private bool _bakeCollision;
        public bool BakeCollision
        {
            get
            {
                return _bakeCollision;
            }
            set
            {

                if (value && !_bakeCollision)
                {
                    string actorCollisionPath = $"{PluginConfig.CollisionCachePath}/{Name}.hkrb";
                    if (File.Exists(actorCollisionPath))
                        _bakeCollision = true;
                }
                else if (value && _bakeCollision) { }
                else
                {
                    _bakeCollision = false;
                }
            }
        }

        /// <summary>
        /// Parameters related to the actor instance.
        /// </summary>
        public IDictionary<string, dynamic> ActorInfo;
        /// <summary>
        /// Whatever that parent thing is.
        /// </summary>
        public NodeBase Parent;

        /// <summary>
        /// The render instance of the scene which can be transformed and altered.
        /// </summary>
        public EditableObject Render;

        private IMapLoader ParentLoader;

        /// <summary>
        /// The name of the object actor.
        /// </summary>
        public override string Name
        {
            get { return Properties["UnitConfigName"].Value; }
            set { Properties["UnitConfigName"] = new MapData.Property<dynamic>(value); }
        }

        /// <summary>
        /// The unique hash ID to identify the instance of the object.
        /// </summary>
        public uint HashId
        {
            get { return Properties["HashId"].Value; }
            set { Properties["HashId"] = new MapData.Property<dynamic>(value); }
        }

        /// <summary>
        /// The parameters section (if it exists)
        /// </summary>
        public IDictionary<string, dynamic> Parameters
        {
            get
            {
                if (!Properties.ContainsKey("!Parameters"))
                    return new Dictionary<string, dynamic>();

                return Properties["!Parameters"];
            }
            set { Properties["!Parameters"] = value; }
        }

        /// <summary>
        /// The area shape (if this is an area)
        /// </summary>
        public AreaShapes AreaShape
        {
            get
            {

                if (!Parameters.ContainsKey("Shape"))
                    return AreaShapes.Box;

                return Enum.Parse(typeof(AreaShapes), Parameters["Shape"].Value);
            }
            set { BymlHelper.SetValue(Parameters, "Shape", value.ToString()); }
        }

        /// <summary>
        /// Calculates the SRT hash.
        /// </summary>
        public void CalculateSRTHash()
        {
            //This can be set as 0.
            int hash = 0;
            Properties["SRTHash"] = new MapData.Property<dynamic>(hash);
        }

        public List<LinkInstance> SourceLinks = new List<LinkInstance>();
        public List<LinkInstance> DestLinks = new List<LinkInstance>();

        public MapObject(IMapLoader parentLoader)
        {
            ParentLoader = parentLoader;
        }

        public void CreateNew(uint hashId, string unitConfigName, dynamic actorInfo, NodeBase parent, MapData mapData)
        {
            MapData = mapData;

            Properties = new Dictionary<string, dynamic>();
            Properties.Add("UnitConfigName", unitConfigName);
            Properties.Add("HashId", hashId);
            Properties.Add("Translate", new List<float> { 0, 0, 0 });
            Properties.Add("SRTHash", (int)0);
            Properties = MapData.ValuesToProperties(Properties);

            ActorInfo = actorInfo;

            Parent = parent;
            ReloadActor();
        }

        public void CreateNew(dynamic properties, dynamic actorInfo, NodeBase parent, MapData mapData)
        {
            MapData = mapData;

            Properties = UKingLibrary.MapData.ValuesToProperties(properties);
            BakeCollision = ParentLoader.BakedCollisionShapeExists(HashId);

            ActorInfo = actorInfo;
            string unitConfigName = Properties["UnitConfigName"].Value;

            Parent = parent;
            ReloadActor();
        }

        private void ReloadActor()
        {
            //Dispose any previous renderables if the object is being updated
            if (Render != null)
            {
                Render?.Dispose();
            }

            //Get the renderable object
            Render = LoadRenderObject(ActorInfo, Properties, Parent);
            //Prepare the gui tree node on the outliner with property tag and header name
            Render.UINode.Tag = this;

            // We're drawing a custom header, but we still want to set Header properly for searching and whatnot.
            Render.UINode.GetHeader = () =>
            {
                string header = Name;
                if (TranslationSource.HasKey($"ACTOR_NAME {Name}"))
                    header += " " + TranslationSource.GetText($"ACTOR_NAME {Name}");
                return header;
            };
            
            Render.UINode.CustomHeaderDraw = () =>
            {
                ImGui.BeginGroup(); // Set this to a single group so that the tooltip applies to the whole text section

                if (TranslationSource.HasKey($"ACTOR_NAME {Name}"))
                {
                    ImGui.SameLine();
                    ImGui.PushFont(ImGuiController.DefaultFontBold);
                    ImGui.Text(" " + TranslationSource.GetText($"ACTOR_NAME {Name}"));
                    ImGui.PopFont();
                }
                else
                    ImGui.Text(Name);
                
                ImGui.EndGroup();
            };

            
            Render.UINode.GetTooltip = () =>
            {
                if (TranslationSource.HasKey($"ACTOR_DOCS {Name}"))
                    return TranslationSource.GetText($"ACTOR_DOCS {Name}");
                else
                    return TranslationSource.GetText("NO_ACTOR_DOCUMENTATION_FOUND");
            };
            
            //Property drawer for gui node
            Render.UINode.TagUI = new NodePropertyUI();
            Render.IsVisibleCallback += delegate
            {
                if (Render is BfresRender)
                    return MapData.ShowVisibleActors;
                else
                    return MapData.ShowInvisibleActors;
            };

            ((EditableObjectNode)Render.UINode).UIProperyDrawer += delegate
            {
                if (ImGui.BeginCombo("##objFileSelect", MapData.RootNode.Header))
                {
                    for (int i = 0; i < ParentLoader.MapData.Count(); i++)
                    {
                        string fileName = ParentLoader.MapData[i].RootNode.Header;
                        bool isSelected = fileName == MapData.RootNode.Header;

                        if (ImGui.Selectable(fileName, isSelected))
                        {
                            MapData = ParentLoader.MapData[i];
                            MapData.Objs.Remove(HashId);
                            MapData.Objs.Add(HashId, this);

                            Parent.Children.Remove(Render.UINode);
                            Parent = MapData.AddObject(this, ParentLoader);
                        }

                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }

                bool bakeCollision = BakeCollision;
                if (ImGui.Checkbox(TranslationSource.GetText("BAKE_COLLISION"), ref bakeCollision))
                    BakeCollision = bakeCollision;
#if DEBUG
                if (ImGui.Button("debug: get shapes"))
                {
                    foreach (var loader in ParentLoader.BakedCollision)
                        loader.GetShapes(HashId);
                }
#endif

                PropertyDrawer.Draw(this, Properties, new PropertyDrawer.PropertyChangedCallback(OnPropertyUpdate));
            };
            //Icon for gui node
            string icon = "Node";
            if (ActorInfo.ContainsKey("bfres"))
            {
                if (!IconManager.HasIcon(PluginConfig.GetCachePath($"Images\\ActorImages\\{ActorInfo["bfres"]}.sbfres.png")))
                {
                    if (File.Exists(PluginConfig.GetCachePath($"Images\\ActorImages\\{ActorInfo["bfres"]}.sbfres.png")))
                        IconManager.LoadTextureFile(PluginConfig.GetCachePath($"Images\\ActorImages\\{ActorInfo["bfres"]}.sbfres.png"), 32, 32);
                }
                icon = PluginConfig.GetCachePath($"Images\\ActorImages\\{ActorInfo["bfres"]}.sbfres.png");
            }
            Render.UINode.Icon = icon;

            Render.AddCallback += delegate
            {
                AddToMap();
            };

            Render.RemoveCallback += delegate
            {
                RemoveFromMap();
            };

            Render.Clone += delegate
            {
                return ((MapObject)Clone()).Render;
            };

            foreach (var property in Properties.ToList())
                ValidateProperty(property.Key);

            //Load the transform attached to the object
            LoadObjectTransform();
        }

        public object Clone()
        {
            MapObject clone = new MapObject(ParentLoader);

            SaveTransform(); // We wanna make sure that the new actor is in the right location!

            clone.Properties = DeepCloneDictionary(Properties);
            clone.HashId = UKingEditor.GetHashId(MapData);
            clone.ActorInfo = ActorInfo;
            clone.Parent = Parent;
            clone.MapData = MapData;
            clone.BakeCollision = BakeCollision;
            clone.ReloadActor();

            return clone;
        }

        public IDictionary<string, dynamic> DeepCloneDictionary(IDictionary<string, dynamic> properties)
        {
            IDictionary<string, dynamic> clonedDictionary = new Dictionary<string, dynamic>();
            foreach (KeyValuePair<string, dynamic> entry in properties)
            {
                if (entry.Value is IDictionary<string, dynamic>)
                    clonedDictionary.Add(entry.Key, DeepCloneDictionary(entry.Value));
                else if (entry.Value is IList<dynamic>)
                    clonedDictionary.Add(entry.Key, DeepCloneList(entry.Value));
                else
                    clonedDictionary.Add(entry.Key, ((ICloneable)entry.Value).Clone()); // We're gonna assume that all values are ICloneable.
            }
            return clonedDictionary;
        }

        public IList<dynamic> DeepCloneList(IList<dynamic> properties)
        {
            IList<dynamic> clonedList = new List<dynamic>();
            foreach (dynamic value in properties)
            {
                if (value is IDictionary<string, dynamic>)
                    clonedList.Add(DeepCloneDictionary(value));
                else if (value is IList<dynamic>)
                    clonedList.Add(DeepCloneList(value));
                else
                    clonedList.Add(((ICloneable)value).Clone()); // We're gonna assume that all values are ICloneable.
            }
            return clonedList;
        }

        public void UpdateActorModel()
        {
            var context = GLContext.ActiveContext;
            var srcLinks = Render.SourceObjectLinks;
            var destLinks = Render.DestObjectLinks;

            bool selected = Render.IsSelected;
            ParentLoader.Scene.DeselectAll(context); // Not sure why we're passing this

            //Remove old from scene
            ParentLoader.Scene.RemoveRenderObject(Render);
            //Reload actor
            ReloadActor();
            //Add new render
            ParentLoader.Scene.AddRenderObject(Render);
            //Reapply everything needed
            Render.IsSelected = selected;
            Render.SourceObjectLinks = srcLinks;
            Render.DestObjectLinks = destLinks;
        }

        private void OnPropertyUpdate(string key) {
            if (!ValidateProperty(key))
                return;
            if (key == "UnitConfigName")
            {
                ActorInfo = GlobalData.Actors[Properties[key].Value];
                BakeCollision = true;
                SaveTransform();
                UpdateActorModel();
            }
        }

        /// <summary>
        /// Decides whether a property value is valid and updates the UI
        /// </summary>
        /// <param name="key">The property key</param>
        /// <returns>True if the property value is valid or the property is unknown</returns>
        private bool ValidateProperty(string key)
        {
            if (key == "UnitConfigName")
            {
                if (!GlobalData.Actors.ContainsKey(Properties[key].Value))
                {
                    // Oh no! We can't find this actor in ActorInfo...
                    Render.UINode.Icon = "Warning";
                    Render.UINode.Header = Properties[key].Value;
                    Properties[key].Invalid = true;
                    return false;
                }
                Properties[key].Invalid = false;
            }
            return true;
        }

        public void AddLink(LinkInstance link)
        {
            this.DestLinks.Add(link);
            link.Object.SourceLinks.Add(new LinkInstance()
            {
                Object = this,
                Properties = link.Properties,
            });
        }

        public void SaveBakedCollision()
        {
            if (BakeCollision)
            {
                // Reformat our transform.
                // (I really need to make a util to do this so the code isn't as messy)
                System.Numerics.Matrix4x4 transform = new System.Numerics.Matrix4x4();
                transform.M11 = Render.Transform.TransformMatrix.M11;
                transform.M12 = Render.Transform.TransformMatrix.M12;
                transform.M13 = Render.Transform.TransformMatrix.M13;
                transform.M14 = Render.Transform.TransformMatrix.M14;
                transform.M21 = Render.Transform.TransformMatrix.M21;
                transform.M22 = Render.Transform.TransformMatrix.M22;
                transform.M23 = Render.Transform.TransformMatrix.M23;
                transform.M24 = Render.Transform.TransformMatrix.M24;
                transform.M31 = Render.Transform.TransformMatrix.M31;
                transform.M32 = Render.Transform.TransformMatrix.M32;
                transform.M33 = Render.Transform.TransformMatrix.M33;
                transform.M34 = Render.Transform.TransformMatrix.M34;
                transform.M41 = Render.Transform.TransformMatrix.M41 / GLContext.PreviewScale;
                transform.M42 = Render.Transform.TransformMatrix.M42 / GLContext.PreviewScale;
                transform.M43 = Render.Transform.TransformMatrix.M43 / GLContext.PreviewScale;
                transform.M44 = Render.Transform.TransformMatrix.M44;

                // If the actor already has baked collision just update the transform
                if (ParentLoader.UpdateBakedCollisionShapeTransform(HashId, transform))
                    return;

                // If not add collision
                string actorCollisionPath = $"{PluginConfig.CollisionCachePath}/{Name}.hkrb";
                if (File.Exists(actorCollisionPath))
                {
                    ActorCollisionLoader actorCollisionLoader = new ActorCollisionLoader();
                    actorCollisionLoader.Load(File.OpenRead(actorCollisionPath), Path.GetFileName(actorCollisionPath));
                    hkpShape[] shapes = actorCollisionLoader.GetShapes();

                    

                    foreach (hkpShape shape in shapes)
                        ParentLoader.AddBakedCollisionShape(HashId, MapData.RootNode.Header, shape, transform);
                }
            }
            else
            {
                ParentLoader.RemoveBakedCollisionShape(HashId);
            }
        }

        /// <summary>
        /// Saves the actor in the scene.
        /// </summary>
        public void SaveActor()
        {
            SaveTransform();
            SaveBakedCollision();
        }

        public override void BeginFrame()
        {
            UpdateCalc = true;

            var render = Render as BfresRender;
            if (render == null)
                return;

            foreach (var model in render.Models)
                model.ModelData.Skeleton.Updated = false;
        }

        /// <summary>
        /// Calculates actor behavior in the scene during a timer loop.
        /// </summary>
        public override void Calc()
        {
            //Calculate any animation data
            if (!(Render is BfresRender))
                return;

            var render = Render as BfresRender;

            foreach (var anim in GetAnimations())
            {
                if (anim == null)
                    continue;

                anim.SetFrame(GetAnimationFrame(anim, anim.Frame));
                foreach (var model in render.Models)
                {
                    if (anim is BfresSkeletalAnim)
                    {
                        ((BfresSkeletalAnim)anim).NextFrame(model.ModelData.Skeleton);
                        AnimationStats.SkeletalAnims += 1;
                    }
                    else
                        anim.NextFrame();
                }
                anim.Frame++;
            }
        }

        /// <summary>
        /// Gets the animations used by the object.
        /// </summary>
        public virtual List<STAnimation> GetAnimations()
        {
            var render = Render as BfresRender;

            List<STAnimation> animations = new List<STAnimation>();
            //Get the wait animation by default
            var idle = render.SkeletalAnimations.FirstOrDefault(x => x.Name.StartsWith("Wait") && x.Loop == true); // Ideally: Find a wait animation that loops
            if (idle == null)
                idle = render.SkeletalAnimations.FirstOrDefault(x => x.Name.StartsWith("Wait")); // Fallback #1: Find a wait animation and force it to loop
            if (idle == null)
                idle = render.SkeletalAnimations.FirstOrDefault(x => x.Loop == true); // Fallback #2: Just get some random animation that loops

            if (idle != null)
            {
                idle.Loop = true; // We don't really want to worry about idle random states
                animations.Add(idle);
            }

            return animations;
        }

        private float GetAnimationFrame(STAnimation anim, float frame)
        {
            float animFrameNum = frame;

            if (anim.Loop)
            {
                //Loop current frame to 0 - frame count range
                var lastFrame = anim.FrameCount;
                while (animFrameNum > lastFrame)
                    animFrameNum -= lastFrame + 1;
            }

            return animFrameNum;
        }

        /// <summary>
        /// Adds the object to the current scene.
        /// </summary>
        public void AddToScene() {
            ParentLoader.Scene.AddRenderObject(Render);
            //Add the actor to the animation player
            ParentLoader.ParentEditor.Workspace.StudioSystem.AddActor(this);
        }

        /// <summary>
        /// Removes the object from the current scene.
        /// </summary>
        public void RemoveFromScene() {
            ParentLoader.Scene.RemoveRenderObject(Render);
            //Remove the actor from the animation player
            ParentLoader.ParentEditor.Workspace.StudioSystem.RemoveActor(this);
        }

        /// <summary>
        /// Adds the object to the map file
        /// </summary>
        public void AddToMap()
        {
            MapData.Objs.Add(HashId, this);
        }

        /// <summary>
        /// Removes the object from the map file
        /// </summary>
        public void RemoveFromMap()
        {
            MapData.Objs.Remove(HashId);
        }

        /// <summary>
        /// Loads the object transform into the scene.
        /// </summary>
        void LoadObjectTransform()
        {
            Render.Transform.Position = LoadVector("Translate", Vector3.Zero) * GLContext.PreviewScale;
            Render.Transform.RotationEuler = LoadVector("Rotate", Vector3.Zero, true);
            Render.Transform.Scale = LoadVector("Scale", Vector3.One);
            Render.Transform.UpdateMatrix(true);
            //Make updates to SRT hash on update
            Render.Transform.TransformUpdated += delegate {
                CalculateSRTHash();
            };
        }

        private Vector3 LoadVector(string key, Vector3 defaultValue, bool isRotation = false)
        {
            if (Properties.ContainsKey(key) && Properties[key] is MapData.Property<object>) // If this is a single value and not an array or something
            {
                if (Properties[key].Value is float && isRotation)
                    return new Vector3(0, (float)Properties[key].Value, 0);
                else if (Properties[key].Value is float)
                    return new Vector3((float)Properties[key].Value);
            }

            if (Properties.ContainsKey(key) && Properties[key] is IList<dynamic>)
            {
                var array = (IList<dynamic>)Properties[key];
                return new Vector3(
                        array[0].Value,
                        array[1].Value,
                        array[2].Value);
            }
            return defaultValue;
        }

        /// <summary>
        /// Checks for transform differences and saves it to the actor.
        /// If the values are uniform, they will save in a single floating point parameter.
        /// </summary>
        void SaveTransform()
        {
            Properties.Remove("Translate");
            Properties.Remove("Rotate");
            Properties.Remove("Scale");
            if (Render.Transform.Position != Vector3.Zero)
                SaveVector("Translate", Render.Transform.Position / GLContext.PreviewScale);
            if (Render.Transform.RotationEuler != Vector3.Zero)
                SaveVector("Rotate", Render.Transform.RotationEuler, true);
            if (Render.Transform.Scale != Vector3.One)
                SaveVector("Scale", Render.Transform.Scale);
        }

        private void SaveVector(string key, Vector3 value, bool isRotation = false)
        {
            if (isRotation && (value.X == 0 && value.Z == 0 && value.Y != 0))
            {
                //Single rotation on the up axis
                BymlHelper.SetValue(Properties, key, new MapData.Property<dynamic>(value.Y));
            }
            else if (value.IsUniform())
                BymlHelper.SetValue(Properties, key, new MapData.Property<dynamic>(value.X));
            else
            {
                BymlHelper.SetValue(Properties, key, new List<dynamic>()
                {
                    new MapData.Property<dynamic>(value.X),
                    new MapData.Property<dynamic>(value.Y),
                    new MapData.Property<dynamic>(value.Z),
                });
            }
        }

        public virtual EditableObject LoadRenderObject(IDictionary<string, dynamic> actor, IDictionary<string, dynamic> obj, NodeBase parent)
        {
            string name = obj["UnitConfigName"].Value;

            if (actor.ContainsKey("profile") && ((string)actor["profile"] == "Area" || (string)actor["profile"] == "SpotBgmTag"))
                return new AreaRender(parent, AreaShape, new Vector4(0, 0, 0, 1));

            if (name == "BoxWater")
                return new AreaWaterRender(parent, new Vector4(0, 0, 1, 1));

            if (TagRender.IsTag(name))
                return new TagRender(name, parent);

            //Default transform cube
            EditableObject render = new TransformableObject(parent);

            //Bfres render
            if (actor.ContainsKey("bfres"))
            {
                string modelPath = PluginConfig.GetContentPath($"Model\\{actor["bfres"]}.sbfres");
                string animPath = PluginConfig.GetContentPath($"Model\\{actor["bfres"]}_Animation.sbfres");
                if (File.Exists(modelPath))
                {
                    var renderCandidate = GetActorSpecificBfresRender(actor, new BfresRender(modelPath, parent));
                    if (renderCandidate != null)
                    {
                        render = renderCandidate;
                        LoadTextures((BfresRender)render, actor["bfres"]);

                        BfresLoader.LoadAnimations((BfresRender)render, modelPath);
                        if (File.Exists(animPath))
                            BfresLoader.LoadAnimations((BfresRender)render, animPath);
                    }
                }
                else
                {
                    for (int i = 0; ; i++)
                    {
                        string modelPartPath = PluginConfig.GetContentPath($"Model\\{actor["bfres"]}-{i.ToString("D2")}.sbfres");

                        if (File.Exists(modelPartPath))
                        {
                            var renderCandidate = GetActorSpecificBfresRender(actor, new BfresRender(modelPartPath, parent));
                            if (renderCandidate != null)
                            {
                                render = renderCandidate;
                                LoadTextures((BfresRender)render, actor["bfres"]);
                                BfresLoader.LoadAnimations((BfresRender)render, modelPartPath);
                                if (File.Exists(animPath))
                                    BfresLoader.LoadAnimations((BfresRender)render, animPath);

                                break;
                            }
                        }
                        else
                            break; // We couldn't find the model
                    }
                }
                if (!(render is BfresRender))
                    StudioLogger.WriteWarning($"missing bfres {actor["bfres"]} for actor {name}!");
            }

            if (render is BfresRender)
                ((BfresRender)render).FrustumCullingCallback = () => {
                    ((BfresRender)render).UseDrawDistance = true;
                    return FrustumCullObject((BfresRender)render);
                };

            return render;
        }

        private BfresRender GetActorSpecificBfresRender(IDictionary<string, dynamic> actor, BfresRender render)
        {
            bool containsActorMainModel = false;
            foreach (var model in render.Models)
            {
                if (actor.ContainsKey("mainModel"))
                {
                    if (model.Name != actor["mainModel"])
                        model.IsVisible = false;
                    else
                        containsActorMainModel = true;
                }
                else
                {
                    StudioLogger.WriteWarning($"No mainModel specified for {actor["bfres"]}! Using fallback.");
                    return render;
                }
            }
            if (!containsActorMainModel)
                return null;
            return render;
        }

        //Object specific frustum cull handling
        private bool FrustumCullObject(BfresRender render)
        {
            if (render.Models.Count == 0)
                return false;

            var transform = render.Transform;
            var context = GLContext.ActiveContext;

            var bounding = render.BoundingNode;
            bounding.UpdateTransform(transform.TransformMatrix);
            if (!context.Camera.InFustrum(bounding))
                return false;

            if (render.IsSelected)
                return true;

            float drawDist = 100000000 * GLContext.PreviewScale;

            if (render.UseDrawDistance)
                return context.Camera.InRange(transform.Position, drawDist);

            return true;
        }

        static void LoadTextures(BfresRender render, string bfres)
        {
            string texpathNX = PluginConfig.GetContentPath($"Model\\{bfres}.Tex.sbfres");
            string texpath1 = PluginConfig.GetContentPath($"Model\\{bfres}.Tex1.sbfres");

            string teratexpathNX = PluginConfig.GetContentPath($"Model\\Terrain.Tex.sbfres");
            string teratexpath1 = PluginConfig.GetContentPath($"Model\\Terrain.Tex1.sbfres");

            string titleBGPath = PluginConfig.GetContentPath($"Pack\\TitleBG.pack");

            if (render.Textures.Count == 0)
            {
                var candidate = BfresLoader.GetTextures(texpathNX);
                if (candidate != null)
                    render.Textures = candidate;

                candidate = BfresLoader.GetTextures(texpath1);
                if (candidate != null)
                    render.Textures = candidate;

                
                // Attach terrain textures in case we need them
                candidate = BfresLoader.GetTextures(teratexpathNX);
                if (candidate != null)
                    // Merge with terrain textures, so those are avaliable
                    foreach (KeyValuePair<string, GenericRenderer.TextureView> tex in candidate)
                        render.Textures.TryAdd(tex.Key, tex.Value);

                candidate = BfresLoader.GetTextures(teratexpath1);
                if (candidate != null)
                    // Merge with terrain textures, so those are avaliable
                    foreach (KeyValuePair<string, GenericRenderer.TextureView> tex in candidate)
                        render.Textures.TryAdd(tex.Key, tex.Value);


                // Try TitleBG - probably not gonna be there anyway, but whatever
                candidate = BfresLoader.GetTextures(titleBGPath + "\\" + texpathNX);
                if (candidate != null)
                    render.Textures = candidate;

                candidate = BfresLoader.GetTextures(titleBGPath + "\\" + texpath1);
                if (candidate != null)
                    render.Textures = candidate;

            }
        }

        public class LinkInstance
        {
            public IDictionary<string, dynamic> Properties;

            public MapObject Object;
        }
    }
}
