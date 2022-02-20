using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core.ViewModels;

namespace GLFrameworkEngine
{
    /// <summary>
    /// Represents a default transformable object with a standard cube drawn.
    /// </summary>
    public class TransformableObject : EditableObject, IInstanceColorPickable, IFrustumCulling, IInstanceDrawable
    {
        private Vector4 _color = Vector4.One;
        public Vector4 Color
        {
            get
            {
                return _color;
            }
            set
            {
                if (_color != value)
                    UpdateInstanceGroup = true;
                _color = value;
            }
        }

        LineRender AxisObject = null;
        UVCubeRenderer CubeRenderer = null;
        StandardInstancedMaterial Material = new StandardInstancedMaterial();

        public bool EnableFrustumCulling => true;
        private bool _inFrustum = true;
        public bool InFrustum
        {
            get
            {
                return _inFrustum;
            }
            set
            {
                if (_inFrustum != value)
                    UpdateInstanceGroup = true;
                _inFrustum = value;
            }
        }

        private bool _drawCube = true;
        public bool DrawCube
        {
            get
            {
                return _drawCube;
            }
            set
            {
                if (_drawCube != value)
                    UpdateInstanceGroup = true;
                _drawCube = value;
            }
        }

        public bool UpdateInstanceGroup { get; set; }

        public BoundingNode Boundings = new BoundingNode()
        {
            Center = new Vector3(0, 0, 0),
            Box = new BoundingBox(new Vector3(-10), new Vector3(10)),
        };

        public bool IsInsideFrustum(GLContext context) {
            return context.Camera.InFustrum(Boundings);
        }

        public TransformableObject(NodeBase parent) : base(parent)
        {
            UpdateInstanceGroup = true;

            //Update boundings on transform changed
            this.Transform.TransformUpdated += delegate {
                Boundings.UpdateTransform(this.Transform.TransformMatrix);
            };
            UINode.Tag = this;
        }

        public bool GroupsWith(IInstanceDrawable drawable)
        {
            if (!(drawable is TransformableObject))
                return false;

            if (((TransformableObject)drawable).InFrustum != InFrustum)
                return false;
            if (((TransformableObject)drawable).IsVisible != IsVisible)
                return false;
            if (((TransformableObject)drawable).Color != Color)
                return false;
            if (((TransformableObject)drawable).DrawCube != DrawCube)
                return false;

            return true;
        }

        [Obsolete("Deprecated. Prefer the instanced version.")]
        public void DrawColorPicking(GLContext context)
        {
            Prepare();

            CubeRenderer.DrawPicking(context, this, Transform.TransformMatrix);
        }

        public void DrawColorPicking(GLContext context, List<GLTransform> transforms)
        {
            List<Matrix4> modelMatrices = new List<Matrix4>(transforms.Count);
            foreach (var transform in transforms)
                modelMatrices.Add(transform.TransformMatrix);

            Prepare();

            CubeRenderer.DrawPicking(context, this, modelMatrices);
        }


        public void DrawModel(GLContext context, Pass pass, List<GLTransform> transforms)
        {
            if (pass != Pass.OPAQUE || !this.InFrustum)
                return;

            base.DrawModel(context, pass);

            List<Matrix4> modelMatrices = new List<Matrix4>(transforms.Count);
            foreach (var transform in transforms)
                modelMatrices.Add(transform.TransformMatrix);

            Prepare();
            if (DrawCube)
            {
                Material.DiffuseTextureID = RenderTools.TexturedCubeTex.ID;
                Material.DisplaySelection = IsSelected | IsHovered;
                Material.Color = this.Color;
                Material.HalfLambertShading = false;
                Material.ModelMatrices = modelMatrices;
                Material.Render(context);

                CubeRenderer.DrawWithSelection(context, IsSelected || IsHovered);
            }
            else //axis line drawer
            {
                float size = 30;
                bool sel = IsSelected || IsHovered;
                Vector4[] colors = new Vector4[3]
                {
                    new Vector4(1, 0, 0, 1),
                    new Vector4(0, 1, 0, 1),
                    new Vector4(0, 0, 1, 1),
                };

                Boundings.Box.DrawSolid(context, Matrix4.Identity, sel ? GLConstants.SelectColor : Vector4.One);

                var solid = new StandardInstancedMaterial();
                solid.hasVertexColors = true;
                solid.ModelMatrices = modelMatrices;
                solid.Render(context);

                AxisObject.Draw(new Vector3(0), new Vector3(size, 0, 0), !sel ? colors[0] : GLConstants.SelectColor, true);
                AxisObject.Draw(new Vector3(0), new Vector3(0, size, 0), !sel ? colors[1] : GLConstants.SelectColor, true);
                AxisObject.Draw(new Vector3(0), new Vector3(0, 0, size), !sel ? colors[2] : GLConstants.SelectColor, true);
            }
        }


        private void Prepare()
        {
            if (CubeRenderer == null || CubeRenderer.IsDisposed)
                CubeRenderer = new UVCubeRenderer(10);
            if (AxisObject == null && !DrawCube) {
                AxisObject = new LineRender(PrimitiveType.Lines);
            }
        }

        public override void Dispose()
        {
            CubeRenderer?.Dispose();
        }
    }
}
