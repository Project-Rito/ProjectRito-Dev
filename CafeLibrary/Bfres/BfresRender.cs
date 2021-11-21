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
    public class BfresRender : GenericRenderer, IColorPickable, ITransformableObject
    {
        public bool UseGameShaders = true;

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

        public override BoundingNode BoundingNode
        {
            get
            {
                var bounding = new BoundingNode(new Vector3(float.MaxValue), new Vector3(float.MinValue));
                foreach (var model in Models) {
                    foreach (var mesh in model.MeshList) {
                        bounding.Include(mesh.BoundingNode);
                    }
                }
                return bounding;
            }
        }

        public List<BfresSkeletalAnim> SkeletalAnimations = new List<BfresSkeletalAnim>();
        public List<BfresMaterialAnim> MaterialAnimations = new List<BfresMaterialAnim>();
        public List<BfresCameraAnim> CameraAnimations = new List<BfresCameraAnim>();

        public List<BfshaLibrary.BfshaFile> ShaderFiles = new List<BfshaLibrary.BfshaFile>();

        public static ShaderProgram DefaultShader => GlobalShaders.GetShader("BFRES", "BFRES/Bfres");

        public bool StayInFrustum = false;

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

        public BfresRender(string filePath, NodeBase parent = null) : base(parent) {

            if (YAZ0.IsCompressed(filePath))
                UpdateModelFromFile(new System.IO.MemoryStream(YAZ0.Decompress(filePath)), filePath);
            else
                UpdateModelFromFile(System.IO.File.OpenRead(filePath), filePath);
        }

        public BfresRender(System.IO.Stream stream, string filePath, NodeBase parent = null) : base(parent)
        {
            UpdateModelFromFile(stream, filePath);
        }

        public bool UpdateModelFromFile(System.IO.Stream stream, string name)
        {
            Name = name;

            if (name.Contains("course"))
                UseGameShaders = true;

            if (DataCache.ModelCache.ContainsKey(name))
            {
                var cachedModel = DataCache.ModelCache[name] as BfresRender;
                BfresLoader.LoadBfresCached(this, cachedModel);
                return false;
            }

            BfresLoader.OpenBfres(stream, this);

            if (Models.Count > 0)
            {
                var bounding = ((BfresModelRender)Models[0]).BoundingNode;
                Transform.ModelBounding = bounding.Box;
            }

            if (!DataCache.ModelCache.ContainsKey(name) && Models.Count > 0)
                DataCache.ModelCache.Add(name, this);

            return Models.Count > 0;
        }


        /// <summary>
        /// Toggles meshes inside the model.
        /// </summary>
        public virtual void ToggleMeshes(string name, bool toggle)
        {
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

        /// <summary>
        /// Draws the model using a normal material pass.
        /// </summary>
        public override void DrawModel(GLContext control, Pass pass)
        {
            if (!InFrustum || !IsVisible)
                return;

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
                    model.Draw(control, pass, this);

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
        public override void DrawShadowModel(GLContext control)
        {
            if (!IsVisible)
                return;

            foreach (BfresModelRender model in Models)
                if (model.IsVisible)
                    model.DrawShadowModel(control, this);
        }

        public override void DrawCubeMapScene(GLContext control)
        {
            foreach (BfresModelRender model in Models)
                if (model.IsVisible)
                    model.DrawCubemapModel(control, this);
        }

        /// <summary>
        /// Draw gbuffer pass for storing normals and depth information
        /// </summary>
        /// <param name="control"></param>
        public override void DrawGBuffer(GLContext control)
        {
            if (!InFrustum || !IsVisible)
                return;

            foreach (BfresModelRender model in Models)
            {
                if (model.IsVisible)
                    model.DrawGBuffer(control, this);
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

        /// <summary>
        /// Draws the model in the color picking pass.
        /// </summary>
        /// <param name="control"></param>
        public void DrawColorPicking(GLContext control)
        {
            if (!InFrustum || !IsVisible)
                return;

            Transform.UpdateMatrix();

            var shader = GlobalShaders.GetShader("PICKING");
            control.CurrentShader = shader;

            if (control.ColorPicker.PickingMode == ColorPicker.SelectionMode.Object)
                control.ColorPicker.SetPickingColor(this, shader);

            foreach (BfresModelRender model in Models)
            {
                if (model.IsVisible)
                    model.DrawColorPicking(control, this);
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
    }
}
