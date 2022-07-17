using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;

namespace CafeLibrary.Rendering
{
    public class BfresRender : GenericRendererInstanced, IInstanceColorPickable, ITransformableObject
    {
        public const float LOD_LEVEL_1_DISTANCE = 1000;
        public const float LOD_LEVEL_2_DISTANCE = 10000;

        public bool UseGameShaders = false;

        public override bool UsePostEffects => true;

        static bool drawAreaID = false;
        public static bool DrawDebugAreaID
        {
            get
            {
                return drawAreaID;
            }
            set
            {
                if (drawAreaID != value)
                {
                    drawAreaID = value;
                    //Make sure the viewport updates changes
                    GLContext.ActiveContext.UpdateViewport = true;
                }
            }
        }

        private BoundingNode _boundingNode;
        public override BoundingNode BoundingNode => _boundingNode;

        public List<BfresSkeletalAnim> SkeletalAnimations = new List<BfresSkeletalAnim>();
        public List<BfresMaterialAnim> MaterialAnimations = new List<BfresMaterialAnim>();
        public List<BfresCameraAnim> CameraAnimations = new List<BfresCameraAnim>();

        public List<BfshaLibrary.BfshaFile> ShaderFiles = new List<BfshaLibrary.BfshaFile>();

        public static ShaderProgram DefaultShader => GlobalShaders.GetShader("BFRES", "BFRES/Bfres");

        public bool StayInFrustum = false;
        private bool _inFrustum;
        public override bool InFrustum 
        { 
            get { 
                return _inFrustum; 
            }
            set {
                if (value != _inFrustum)
                    UpdateInstanceGroup = true;
                _inFrustum = value;
            }
        }

        private bool _isSelected;
        public override bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                    UpdateInstanceGroup = true;
                _isSelected = value;
            }
        }

        public bool UseDrawDistance { get; set; }

        public bool CanDisplayInCubemap
        {
            get
            {
               foreach (BfresModelRender model in Models)
                {
                    if (model.Meshes.Any(x => x.IsCubeMap))
                        return true;
                }
                return false;
            }
        }

        public Func<bool> FrustumCullingCallback;

        //Render distance for models to cull from far away.
        protected float renderDistanceSquared = 20000000;
        protected float renderDistance = 2000000;

        public EventHandler OnRenderInitialized;

        public BfresRender(string filePath, NodeBase parent = null) : base(parent)
        {
            VisibilityChanged += (object sender, EventArgs e) =>
            {
                UpdateInstanceGroup = true;
            };

            if (YAZ0.IsCompressed(filePath))
                UpdateModelFromFile(new System.IO.MemoryStream(YAZ0.Decompress(filePath)), filePath);
            else
                UpdateModelFromFile(System.IO.File.OpenRead(filePath), filePath);
        }

        public BfresRender(System.IO.Stream stream, string filePath, NodeBase parent = null) : base(parent)
        {
            VisibilityChanged += (object sender, EventArgs e) =>
            {
                UpdateInstanceGroup = true;
            };

            UpdateModelFromFile(stream, filePath);
        }

        public bool UpdateModelFromFile(System.IO.Stream stream, string name)
        {
            UpdateInstanceGroup = true;

            Name = name;

            if (name.Contains("course"))
                UseGameShaders = true;

            if (DataCache.ModelCache.ContainsKey(name))
            {
                var cachedModel = DataCache.ModelCache[name] as BfresRender;
                BfresLoader.LoadBfresCached(this, cachedModel);
                UpdateBoundingBox();
                return false;
            }

            BfresLoader.OpenBfres(stream, this);
            UpdateBoundingBox();

            if (!DataCache.ModelCache.ContainsKey(name) && Models.Count > 0)
                DataCache.ModelCache.Add(name, this);

            return Models.Count > 0;
        }


        /// <summary>
        /// Toggles meshes inside the model.
        /// </summary>
        public virtual void ToggleMeshes(string name, bool toggle)
        {
            UpdateInstanceGroup = true;

            foreach (var model in Models) {
                foreach (BfresMeshRender mesh in model.MeshList) {
                    if (mesh.Name == name)
                        mesh.IsVisible = toggle;
                }
            }
        }

        public virtual void OnSkeletonUpdated()
        {
            foreach (BfresModelRender model in this.Models)
                model.UpdateSkeletonUniforms();
        }

        /// <summary>
        /// Resets all the animation states to defaults.
        /// Animation value lists are cleared, bones have reset transforms.
        /// </summary>
        public override void ResetAnimations()
        {
            foreach (BfresModelRender model in Models)
            {
                foreach (var mesh in model.Meshes)
                    ((BfresMaterialRender)mesh.MaterialAsset).ResetAnimations();

                model.ResetAnimations();
            }
        }

        bool drawnOnce = false;


        public override bool GroupsWith(IInstanceDrawable drawable)
        {
            if (drawable is not BfresRender)
                return false;

            if (((BfresRender)drawable).Name != Name)
                return false;
            if (((BfresRender)drawable).IsVisible != IsVisible)
                return false;
            if (((BfresRender)drawable).IsSelected != IsSelected)
                return false;
            for (int i = 0; i < Models.Count; i++)
            {
                if (Models[i].IsVisible != ((BfresRender)drawable).Models[i].IsVisible)
                    return false;
            };
            return true;
        }

        /// <summary>
        /// Draws the model using a normal material pass. Supports instancing.
        /// </summary>
        public override void DrawModel(GLContext control, Pass pass, List<GLTransform> transforms)
        {
            if (!IsVisible || !InFrustum)
                return;

            // Use default transform if none is supplied
            if (transforms == null)
                transforms = new List<GLTransform>() { Transform };

            base.DrawModel(control, pass);

            //Make sure cubemaps can look seamless in lower mip levels
            GL.Enable(EnableCap.TextureCubeMapSeamless);

            if (DebugShaderRender.DebugRendering != DebugShaderRender.DebugRender.Default || DrawDebugAreaID)
                control.CurrentShader = GlobalShaders.GetShader("DEBUG");
            else
                control.UseSRBFrameBuffer = true;

            if (!drawnOnce)
            {
                OnRenderInitialized?.Invoke(this, EventArgs.Empty);
                drawnOnce = true;
            }

            Transform.UpdateMatrix();
            foreach (BfresModelRender model in Models)
                if (model.IsVisible)
                    model.Draw(control, pass, this, transforms);

            //Draw debug boundings
            if (Runtime.RenderBoundingBoxes)
                this.BoundingNode.Box.DrawSolid(control, this.Transform.TransformMatrix, Vector4.One);

         //  if (Runtime.RenderBoundingBoxes)
             //   DrawBoundings(control);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.DepthMask(true);
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
        }

        /// <summary>
        /// Draws the projected shadow model in light space.
        /// </summary>
        /// <param name="control"></param>
        public override void DrawShadowModel(GLContext control, List<GLTransform> transforms)
        {
            if (!IsVisible)
                return;

            foreach (BfresModelRender model in Models)
                if (model.IsVisible)
                    model.DrawShadowModel(control, this, transforms);
        }

        public override void DrawCubeMapScene(GLContext control, List<GLTransform> transforms)
        {
            foreach (BfresModelRender model in Models)
                if (model.IsVisible)
                    model.DrawCubemapModel(control, this, transforms);
        }

        /// <summary>
        /// Draw gbuffer pass for storing normals and depth information
        /// </summary>
        /// <param name="control"></param>
        public override void DrawGBuffer(GLContext control, List<GLTransform> transforms)
        {
            if (!InFrustum || !IsVisible)
                return;

            foreach (BfresModelRender model in Models)
            {
                if (model.IsVisible)
                    model.DrawGBuffer(control, this, transforms);
            }
        }

        public void DrawBoundings(GLContext control)
        {
            foreach (BfresModelRender model in Models)
            {
                if (!model.IsVisible)
                    continue;

                //Go through each bounding in the current displayed mesh
                var bounding = model.BoundingNode;

                var shader = GlobalShaders.GetShader("PICKING");
                control.CurrentShader = shader;
                control.CurrentShader.SetVector4("color", new Vector4(1));

                Matrix4 transform = Transform.TransformMatrix;
                bounding.UpdateTransform(transform);

                GL.LineWidth(2);

                var bnd = bounding.Box;
                foreach (BfresMeshRender mesh in model.Meshes) {
                    int ind = 0;
                    foreach (var poly in mesh.LODMeshes[0].Boundings) {
                        if (!mesh.LODMeshes[0].InFustrum[ind++])
                        BoundingBoxRender.Draw(control, poly.Box.Min, poly.Box.Max);
                    }
                }
            }
        }

        [Obsolete("Deprecated. Prefer the instanced version.")]
        public void DrawColorPicking(GLContext context) {
            if (!InFrustum || !IsVisible)
                return;

            DrawColorPicking(context, new List<GLTransform> { Transform });
        }

        /// <summary>
        /// Draws the model in the color picking pass. Supports instancing.
        /// </summary>
        /// <param name="context"></param>
        public void DrawColorPicking(GLContext context, List<GLTransform> transforms)
        {
            if (!InFrustum || !IsVisible)
                return;

            Transform.UpdateMatrix();

            var shader = GlobalShaders.GetShader("PICKING");
            context.CurrentShader = shader;

            if (context.ColorPicker.PickingMode == ColorPicker.SelectionMode.Object)
                context.ColorPicker.SetPickingColor(this, shader);

            foreach (BfresModelRender model in Models)
            {
                if (model.IsVisible)
                    model.DrawColorPicking(context, this, transforms);
            }
        }

        /// <summary>
        /// Checks for when the current render is in the fustrum of the camera
        /// Returns true if in view.
        /// </summary>
        public override bool IsInsideFrustum(GLContext context)
        {
            if (StayInFrustum) return true;

            if (FrustumCullingCallback != null) {
                return FrustumCullingCallback.Invoke();
            }

            InFrustum = false;

            //Todo check for actor objects that handle box culling differently
            foreach (BfresModelRender model in Models) {
                model.UpdateFrustum(context, this);
                /*if (model.MeshInFrustum.Any(x => x))
                    InFrustum = true;*/
            }

            // Draw distance map objects
            //if (UseDrawDistance)
             //   return context.Camera.InRange(renderDistanceSquared, Transform.Position);
            return true;
        }

        public void UpdateBoundingBox()
        {
            _boundingNode = new BoundingNode(new Vector3(float.MaxValue), new Vector3(float.MinValue));
            foreach (var model in Models)
            {
                if (!model.IsVisible)
                    continue;

                foreach (var mesh in model.MeshList)
                    _boundingNode.Include(mesh.BoundingNode);
            }
            this.Transform.ModelBounding = BoundingNode.Box;
            this.Transform.TransformUpdated += delegate {
                _boundingNode.UpdateTransform(this.Transform.TransformMatrix);
            };
        }
    }
}
