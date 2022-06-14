﻿using System;
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
    // Todo - have this interact with water heightmap
    public class AscendingCurrentRender : EditableObject, IColorPickable, ICloneable, IFrustumCulling
    {
        public static bool DrawFilled = true;

        public Vector4 Color = Vector4.One;
        public Vector4 FillColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        RenderMesh<VertexPositionNormal> OutlineRenderer = null;
        RenderMesh<VertexPositionNormal> FillRenderer = null;

        //Area boxes have an inital transform
        static Matrix4 InitalTransform => new Matrix4(
             GLContext.PreviewScale, 0, 0, 0,
             0, GLContext.PreviewScale, 0, 0,
             0, 0, GLContext.PreviewScale, 0,
             0, GLContext.PreviewScale, 0, 1); // AscendingCurrent origins are at the bottom, not the middle.

        public bool EnableFrustumCulling => true;
        public bool InFrustum { get; set; }


        public override BoundingNode BoundingNode { get; } = new BoundingNode()
        {
            Box = new BoundingBox(
                new OpenTK.Vector3(-1, 0, -1) * GLContext.PreviewScale,
                new OpenTK.Vector3(1, 2, 1) * GLContext.PreviewScale),
        };

        public bool IsInsideFrustum(GLContext context)
        {
            return context.Camera.InFustrum(BoundingNode);
        }

        public AscendingCurrentRender(NodeBase parent, Vector4 color) : base(parent)
        {
            //Update boundings on transform changed
            this.Transform.TransformUpdated += delegate {
                BoundingNode.UpdateTransform(Transform.TransformMatrix);
            };
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

        public void DrawColorPicking(GLContext context)
        {
            Prepare();

            //Thicker picking region
            GL.LineWidth(32);
            OutlineRenderer.DrawPicking(context, this, InitalTransform * Transform.TransformMatrix);
            GL.LineWidth(1);
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if (pass != Pass.OPAQUE || !this.InFrustum)
                return;

            Prepare();

            GL.Disable(EnableCap.CullFace);

            //Draw a filled in region
            if (DrawFilled)
            {
                GLMaterialBlendState.TranslucentAlphaOne.RenderBlendState();
                GLMaterialBlendState.TranslucentAlphaOne.RenderDepthTest();
                FillRenderer.DrawSolid(context, new List<Matrix4> { InitalTransform * Transform.TransformMatrix }, new Vector4(Color.Xyz, 0.1f));
                GLMaterialBlendState.Opaque.RenderBlendState();
                GLMaterialBlendState.Opaque.RenderDepthTest();
            }

            //Draw lines of the region
            GL.LineWidth(1);
            OutlineRenderer.DrawSolidWithSelection(context, new List<Matrix4> { InitalTransform * Transform.TransformMatrix }, Color, IsSelected | IsHovered);

            //Draw debug boundings
            if (Runtime.RenderBoundingBoxes)
                this.BoundingNode.Box.DrawSolid(context, Transform.TransformMatrix, new Vector4(1, 0, 0, 1));

            GL.Enable(EnableCap.CullFace);
        }

        private void Prepare()
        {
            if (OutlineRenderer == null)
                OutlineRenderer = new CubeCrossedRenderer(1f, PrimitiveType.LineStrip);
            FillRenderer = new CubeRenderer(1f);
        }

        public override void Dispose()
        {
            OutlineRenderer?.Dispose();
            FillRenderer?.Dispose();
        }

        class CubeCrossedRenderer : RenderMesh<VertexPositionNormal>
        {
            public CubeCrossedRenderer(float size = 0.5f, PrimitiveType primitiveType = PrimitiveType.Triangles) :
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