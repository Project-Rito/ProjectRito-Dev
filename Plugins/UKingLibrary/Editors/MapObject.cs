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
using CafeLibrary.Rendering;
using Toolbox.Core.Animations;

namespace UKingLibrary
{
    public class MapObject : ActorBase
    {
        public bool IsStatic { get; set; }

        /// <summary>
        /// Properties related to the object instance.
        /// </summary>
        public IDictionary<string, dynamic> Properties;

        /// <summary>
        /// Parameters related to the actor instance.
        /// </summary>
        public IDictionary<string, dynamic> ActorInfo;

        /// <summary>
        /// The render instance of the scene which can be transformed and altered.
        /// </summary>
        public EditableObject Render;

        /// <summary>
        /// The name of the object actor.
        /// </summary>
        public override string Name
        {
            get { return Properties["UnitConfigName"]; }
            set { Properties["UnitConfigName"] = value; }
        }

        /// <summary>
        /// The unique hash ID to identify the instance of the object.
        /// </summary>
        public uint HashId
        {
            get { return Properties["HashId"]; }
            set { Properties["HashId"] = value; }
        }

        /// <summary>
        /// Calculates the SRT hash.
        /// </summary>
        public void CalculateSRTHash()
        {
            //This can be set as 0.
            int hash = 0;
            Properties["SRTHash"] = hash;
        }

        public List<LinkInstance> SourceLinks = new List<LinkInstance>();
        public List<LinkInstance> DestLinks = new List<LinkInstance>();

        public MapObject()
        {

        }

        /// <summary>
        /// Creates a new object instance with default properties.
        /// </summary>
        public void CreateNew(uint HashID, string configName)
        {
            Properties = new Dictionary<string, dynamic>();
            Properties.Add("UnitConfigName", configName);
            Properties.Add("HashId", HashID);
            Properties.Add("Translate", new List<float> { 0, 0, 0 });
            Properties.Add("SRTHash", (int)0);

            if (GlobalData.Actors.ContainsKey(configName))
                ActorInfo = GlobalData.Actors[configName] as IDictionary<string, dynamic>;
        }

        /// <summary>
        /// Loads all the actor data into the scene.
        /// </summary>
        public void LoadActor(MapMuuntEditor editor, dynamic obj, dynamic actor, NodeBase parent)
        {
            Properties = obj;
            ActorInfo = actor;

            //Dispose any previous renderables if the object is being updated
            if (Render != null)
                Render?.Dispose();

            //Get the renderable object
            Render = LoadRenderObject(ActorInfo, Properties, parent);
            //Prepare the gui tree node on the outliner with property tag and header name
            Render.UINode.Tag = this;
            Render.UINode.Header = Name;
            //Property drawer for gui node
            Render.UINode.TagUI = new NodePropertyUI();
            Render.IsVisibleCallback += delegate
            {
                if (Render is BfresRender)
                    return MapMuuntEditor.ShowVisibleActors;
                else
                    return MapMuuntEditor.ShowInvisibleActors;
            };

            ((EditableObjectNode)Render.UINode).UIProperyDrawer += delegate
            {
                PropertyDrawer.Draw(this, Properties);
            };
            //Icon for gui node
            string icon = "Node";
            if (actor.ContainsKey("bfres"))
            {
                if (File.Exists($"{Runtime.ExecutableDir}\\Images\\UkingObj\\{actor["bfres"]}.sbfres.png"))
                    IconManager.LoadTextureFile($"{Runtime.ExecutableDir}\\Images\\UkingObj\\{actor["bfres"]}.sbfres.png", 32, 32);

                if (IconManager.HasIcon($"{Runtime.ExecutableDir}\\Images\\UkingObj\\{actor["bfres"]}.sbfres.png"))
                    icon = $"{Runtime.ExecutableDir}\\Images\\UkingObj\\{actor["bfres"]}.sbfres.png";
            }
            Render.UINode.Icon = icon;

            //Load the transform attached to the object
            LoadObjectTransform();
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

        /// <summary>
        /// Saves the actor in the scene.
        /// </summary>
        public void SaveActor()
        {
            SaveTransform();
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
            var idle = render.SkeletalAnimations.FirstOrDefault(x => x.Name == "Wait");
            if (idle != null)
                animations.Add(idle);
            else
                animations.Add(render.SkeletalAnimations.FirstOrDefault());
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
            GLContext.ActiveContext.Scene.AddRenderObject(Render);
            //Add the actor to the animation player
            Workspace.ActiveWorkspace.StudioSystem.AddActor(this);
        }

        /// <summary>
        /// Removes the object from the current scene.
        /// </summary>
        public void RemoveFromScene() {
            GLContext.ActiveContext.Scene.RemoveRenderObject(Render);
            //Remove the actor to the animation player
            Workspace.ActiveWorkspace.StudioSystem.RemoveActor(this);
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
            if (Properties.ContainsKey(key) && Properties[key] is float && isRotation)
                return new Vector3(0, (float)Properties[key], 0);
            else if(Properties.ContainsKey(key) && Properties[key] is float)
                    return new Vector3((float)Properties[key]);

            if (Properties.ContainsKey(key) && Properties[key] is IList<dynamic>)
            {
                var array = (IList<dynamic>)Properties[key];
                return new Vector3(
                        array[0],
                        array[1],
                        array[2]);
            }
            return defaultValue;
        }

        /// <summary>
        /// Checks for transform differences and saves it to the actor.
        /// If the values are uniform, they will save in a single floating point parameter.
        /// </summary>
        void SaveTransform()
        {
            if (Render.Transform.Position != Vector3.Zero)
                SaveVector("Translate", Render.Transform.Position / GLContext.PreviewScale);
            if (Render.Transform.RotationEuler != Vector3.Zero)
                SaveVector("Rotate", Render.Transform.RotationEuler, true);
            if (Render.Transform.Scale != Vector3.Zero)
                SaveVector("Scale", Render.Transform.Scale);
        }

        private void SaveVector(string key, Vector3 value, bool isRotation = false)
        {
            if (isRotation)
            {
                //Single rotation on the up axis
                if (value.X == 0 && value.Z == 0 && value.Y != 0)
                    BymlHelper.SetValue(Properties, key, value.Y);
            }
            else if (value.IsUniform())
                BymlHelper.SetValue(Properties, key, value.X);
            else
            {
                BymlHelper.SetValue(Properties, key, new List<float>()
                {
                    value.X,
                    value.Y,
                    value.Z,
                });
            }
        }

        public virtual EditableObject LoadRenderObject(IDictionary<string, dynamic> actor, dynamic obj, NodeBase parent)
        {
            string name = obj["UnitConfigName"];

            //Default transform cube
            EditableObject render = new TransformableObject(parent);
            //Bfres renderer
            if (actor.ContainsKey("bfres"))
            {
                string modelPath = PluginConfig.GetContentPath($"Model\\{actor["bfres"]}.sbfres");
                string animPath = PluginConfig.GetContentPath($"Model\\{actor["bfres"]}_Animation.sbfres");
                if (File.Exists(modelPath))
                {
                    var renderCandidate = getActorSpecificBfresRender(actor, new BfresRender(modelPath, parent));
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
                    for (int i = 0; i < 30; i++)
                    {
                        string modelPartPath = PluginConfig.GetContentPath($"Model\\{actor["bfres"]}-{i.ToString("D2")}.sbfres");
                        
                        if (File.Exists(modelPartPath))
                        {
                            var renderCandidate = getActorSpecificBfresRender(actor, new BfresRender(modelPartPath, parent));
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

        private BfresRender getActorSpecificBfresRender(IDictionary<string, dynamic> actor, BfresRender render)
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
                    StudioLogger.WriteWarning($"No mainModel specified for {actor["bfres"]}!");
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

            var bounding = ((BfresModelRender)render.Models[0]).BoundingNode;
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

            string titleBGPath = PluginConfig.GetContentPath($"Pack\\TitleBG.pack");

            if (render.Textures.Count == 0)
            {

                var candidate = BfresLoader.GetTextures(texpathNX);
                if (candidate != null)
                    render.Textures = candidate;

                candidate = BfresLoader.GetTextures(texpath1);
                if (candidate != null)
                    render.Textures = candidate;

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
