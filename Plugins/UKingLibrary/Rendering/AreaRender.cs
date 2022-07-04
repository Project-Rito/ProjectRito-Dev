using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;
using Toolbox.Core.ViewModels;

namespace UKingLibrary.Rendering
{
    public class AreaRender : EditableObject, IInstanceColorPickable, ICloneable, IFrustumCulling, IInstanceDrawable
    {
        public AreaShapes _areaShape = AreaShapes.Box;
        public AreaShapes AreaShape
        {
            get
            {
                return _areaShape;
            }
            set
            {
                if (_areaShape != value)
                    UpdateInstanceGroup = true;
                _areaShape = value;
            }
        }

        public static bool DrawFilled = true;

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
        private Vector4 _fillColor = new Vector4(0.4f, 0.7f, 1.0f, 0.3f);
        public Vector4 FillColor
        {
            get
            {
                return _fillColor;
            }
            set
            {
                if (_fillColor != value)
                    UpdateInstanceGroup = true;
                _fillColor = value;
            }
        }

        RenderMesh<VertexPositionNormal> OutlineRenderer = null;
        RenderMesh<VertexPositionNormal> FillRenderer = null;

        //Area boxes have an inital transform
        static Matrix4 InitalTransform => new Matrix4(
             GLContext.PreviewScale, 0, 0, 0,
             0, GLContext.PreviewScale, 0, 0,
             0, 0, GLContext.PreviewScale, 0,
             0, 0, 0, 1);

        public bool EnableFrustumCulling => true;
        private bool _inFrustum;
        public bool InFrustum
        {
            get
            {
                return _inFrustum;
            }
            set
            {
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

        private bool _updateInstanceGroup = true;
        public virtual bool UpdateInstanceGroup { get { return _updateInstanceGroup; } set { _updateInstanceGroup = value; } }

        public override BoundingNode BoundingNode { get; } = new BoundingNode()
        {
            Box = new BoundingBox(
                new OpenTK.Vector3(-1, -1, -1) * GLContext.PreviewScale,
                new OpenTK.Vector3(1, 1, 1) * GLContext.PreviewScale),
        };

        public bool IsInsideFrustum(GLContext context)
        {
            return context.Camera.InFustrum(BoundingNode);
        }

        public AreaRender(NodeBase parent, AreaShapes shape, Vector4 color) : base(parent)
        {
            UpdateInstanceGroup = true;
            VisibilityChanged += (object sender, EventArgs e) =>
            {
                UpdateInstanceGroup = true;
            };
            RemoveCallback += (object sender, EventArgs e) =>
            {
                UpdateInstanceGroup = true;
            };

            //Update boundings on transform changed
            this.Transform.TransformUpdated += delegate {
                BoundingNode.UpdateTransform(Transform.TransformMatrix);
            };
            AreaShape = shape;
            Color = color;
        }


        public object Clone()
        {
            if (this.UINode.Tag is ICloneable)
            {
                return ((ICloneable)this.UINode.Tag).Clone();
            }
            return null;
        }

        /// <summary>
        /// Determines if this area should be in the same instance group as another render
        /// </summary>
        public bool GroupsWith(IInstanceDrawable drawable)
        {
            if (!(drawable is AreaRender))
                return false;

            if (((AreaRender)drawable).InFrustum != InFrustum)
                return false;
            if (((AreaRender)drawable).Color != Color)
                return false;
            if (((AreaRender)drawable).AreaShape != AreaShape)
                return false;
            if (((AreaRender)drawable).IsSelected != IsSelected)
                return false;

            return true;
        }

        [Obsolete("Deprecated. Prefer the instanced version.")]
        public void DrawColorPicking(GLContext context) {
            DrawColorPicking(context, new List<GLTransform> { Transform });
        }

        public void DrawColorPicking(GLContext context, List<GLTransform> transforms)
        {
            // Until MapObject serves as an editable object and passes fn calls to its render, this will have to exist here.
            // But once that happens this can be applicable to any actor profile just by blocking the DrawColorPicking call.
            if (KeyInfo.EventInfo.IsKeyDown(UKingInputSettings.INPUT.Scene.PassthroughAreas))
                return;

            List<Matrix4> modelMatrices = new List<Matrix4>(transforms.Count);
            foreach (var transform in transforms)
                modelMatrices.Add(InitalTransform * transform.TransformMatrix);

            Prepare();

            //Thicker picking region
            GL.LineWidth(32);
            if (PluginConfig.AreasSelectByBorders)
                OutlineRenderer.DrawPicking(context, this, modelMatrices);
            else
                FillRenderer.DrawPicking(context, this, modelMatrices);
            GL.LineWidth(1);
        }

        public override void DrawModel(GLContext context, Pass pass)
        {

        }

        public void DrawModel(GLContext context, Pass pass, List<GLTransform> transforms)
        {
            if (pass != Pass.OPAQUE || !this.InFrustum)
                return;

            
            List<Matrix4> modelMatrices = new List<Matrix4>(transforms.Count);
            foreach (var transform in transforms)
                modelMatrices.Add(InitalTransform * transform.TransformMatrix);

            Prepare();

            GL.Disable(EnableCap.CullFace);
            
            //Draw a filled in region
            if (DrawFilled)
            {
                GLMaterialBlendState.TranslucentAlphaOne.RenderBlendState();
                GLMaterialBlendState.TranslucentAlphaOne.RenderDepthTest();
                FillRenderer.DrawSolid(context, modelMatrices, new Vector4(Color.Xyz, 0.1f));
                GLMaterialBlendState.Opaque.RenderBlendState();
                GLMaterialBlendState.Opaque.RenderDepthTest();
            }

            //Draw lines of the region
            GL.LineWidth(1);
            OutlineRenderer.DrawSolidWithSelection(context, modelMatrices, Color, IsSelected | IsHovered);
            /*
            switch (AreaShape)
            {
                case AreaShapes.Sphere:
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    SphereRenderer.DrawSolidWithSelection(context, InitalTransform * Transform.TransformMatrix, Color, IsSelected | IsHovered);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    break;
                default:
                    OutlineRenderer.DrawSolidWithSelection(context, InitalTransform * Transform.TransformMatrix, Color, IsSelected | IsHovered);
                    break;
            }
            */

            //Draw debug boundings
            if (Runtime.RenderBoundingBoxes)
                this.BoundingNode.Box.DrawSolid(context, Transform.TransformMatrix, new Vector4(1, 0, 0, 1));

            GL.Enable(EnableCap.CullFace);
        }

        private void Prepare()
        {
            if (OutlineRenderer == null)
                OutlineRenderer = new CubeCrossedRenderer(1, PrimitiveType.LineStrip);
            if (AreaShape == AreaShapes.Sphere)
            {
                if (FillRenderer == null)
                    FillRenderer = new SphereRender(1, 50, 50);
            }
            else
            {
                if (FillRenderer == null)
                    FillRenderer = new CubeRenderer(1);
            }
        }

        public override void Dispose()
        {
            OutlineRenderer?.Dispose();
            FillRenderer?.Dispose();
        }

        class CubeCrossedRenderer : RenderMesh<VertexPositionNormal>
        {
            public CubeCrossedRenderer(float size = 1.0f, PrimitiveType primitiveType = PrimitiveType.Triangles) :
                base(DrawingHelper.GetCubeVertices(size), Indices, primitiveType)
            {

            }

            public static int[] Indices = new int[]
            {
            // front face
            0, 1, 2, 2, 3, 0,
            // top face
            3, 2, 6, 6, 7, 3,
            // back face
            7, 6, 5, 5, 4, 7,
            // left face
            4, 0, 3, 3, 7, 4,
            // bottom face
            0, 5, 1, 4, 5, 0, //Here we swap some indices for a cross section at the bottom
            // right face
            1, 5, 6, 6, 2, 1,};
        }
    }
}
