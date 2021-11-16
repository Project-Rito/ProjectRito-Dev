using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core.ViewModels;

namespace UKingLibrary.Rendering
{
    public class AreaRender : EditableObject, IColorPickable, ICloneable
    {
        public AreaShapes AreaShape = AreaShapes.Box;

        public static bool DrawFilled = true;

        public Vector4 Color = Vector4.One;
        public Vector4 FillColor = new Vector4(0.4f, 0.7f, 1.0f, 0.3f);

        CubeCrossedRenderer CubeOutlineRender = null;
        CubeRenderer CubeFilledRenderer = null;

        SphereRender SphereRenderer = null;

        //Area boxes have an inital transform
        static Matrix4 InitalTransform => new Matrix4(
             GLContext.PreviewScale, 0, 0, 0,
             0, GLContext.PreviewScale, 0, 0,
             0, 0, GLContext.PreviewScale, 0,
             0, 0, 0, 1);

        public AreaRender(NodeBase parent, AreaShapes shape, Vector4 color) : base(parent)
        {
            AreaShape = shape;
            Color = color;
        }

        public override BoundingNode BoundingNode { get; } = new BoundingNode()
        {
            Box = new BoundingBox(
                new OpenTK.Vector3(-50, -50, -50),
                new OpenTK.Vector3(50, 50, 50)),
        };

        public object Clone()
        {
            var area = new AreaRender(this.ParentUINode, AreaShape, this.Color);
            area.Transform = new GLTransform()
            {
                Position = this.Transform.Position,
                Rotation = this.Transform.Rotation,
                Scale = this.Transform.Scale * GLContext.PreviewScale,
            };
            area.Transform.UpdateMatrix(true);

            if (this.UINode.Tag is ICloneable)
                area.UINode.Tag = ((ICloneable)this.UINode.Tag).Clone();
            return area;
        }

        public void DrawColorPicking(GLContext context)
        {
            Prepare();

            //Thicker picking region
            GL.LineWidth(32);
            switch (AreaShape)
            {
                case AreaShapes.Sphere:
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    SphereRenderer.DrawPicking(context, this, InitalTransform * Transform.TransformMatrix);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    break;
                default:
                    CubeOutlineRender.DrawPicking(context, this, InitalTransform * Transform.TransformMatrix);
                    break;
            }
            GL.LineWidth(1);
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if (pass != Pass.OPAQUE)
                return;

            Prepare();

            GL.Disable(EnableCap.CullFace);

            //Draw a filled in region
            if (DrawFilled)
            {
                GLMaterialBlendState.TranslucentAlphaOne.RenderBlendState();
                GLMaterialBlendState.TranslucentAlphaOne.RenderDepthTest();
                switch (AreaShape)
                {
                    case AreaShapes.Sphere:
                        SphereRenderer.DrawSolid(context, InitalTransform * Transform.TransformMatrix, new Vector4(Color.Xyz, 0.1f));
                        break;
                    default:
                        CubeFilledRenderer.DrawSolid(context, InitalTransform * Transform.TransformMatrix, new Vector4(Color.Xyz, 0.1f));
                        break;
                }
                GLMaterialBlendState.Opaque.RenderBlendState();
                GLMaterialBlendState.Opaque.RenderDepthTest();
            }

            //Draw lines of the region
            GL.LineWidth(1);
            switch (AreaShape)
            {
                case AreaShapes.Sphere:
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    SphereRenderer.DrawSolidWithSelection(context, InitalTransform * Transform.TransformMatrix, Color, IsSelected | IsHovered);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    break;
                default:
                    CubeOutlineRender.DrawSolidWithSelection(context, InitalTransform * Transform.TransformMatrix, Color, IsSelected | IsHovered);
                    break;
            }
            GL.Enable(EnableCap.CullFace);
        }

        private void Prepare()
        {
            if (AreaShape == AreaShapes.Sphere)
            {
                if (SphereRenderer == null)
                    SphereRenderer = new SphereRender(1, 10, 10);
            }
            else
            {
                if (CubeOutlineRender == null)
                    CubeOutlineRender = new CubeCrossedRenderer(1, PrimitiveType.LineStrip);
                if (CubeFilledRenderer == null)
                    CubeFilledRenderer = new CubeRenderer(1);
            }
        }

        public override void Dispose()
        {
            CubeOutlineRender?.Dispose();
            CubeFilledRenderer?.Dispose();

            SphereRenderer?.Dispose();
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
