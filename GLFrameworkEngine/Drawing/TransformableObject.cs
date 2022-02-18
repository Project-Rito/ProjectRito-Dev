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
    public class TransformableObject : EditableObject, IColorPickable, IFrustumCulling
    {
        public Vector4 Color = new Vector4(1, 1, 1, 1);

        LineRender AxisObject = null;
        UVCubeRenderer CubeRenderer = null;
        StandardMaterial Material = new StandardMaterial();

        public bool EnableFrustumCulling => true;
        public bool InFrustum { get; set; }

        public bool DrawCube = true;

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
            //Update boundings on transform changed
            this.Transform.TransformUpdated += delegate {
                Boundings.UpdateTransform(this.Transform.TransformMatrix);
            };
            UINode.Tag = this;
        }

        public void DrawColorPicking(GLContext context)
        {
            Prepare();

            CubeRenderer.DrawPicking(context, this, Transform.TransformMatrix);
        }

        public override void DrawModel(GLContext context, Pass pass, List<GLTransform> transforms = null)
        {
            if (pass != Pass.OPAQUE || !this.InFrustum)
                return;

            base.DrawModel(context, pass);

            Prepare();
            if (DrawCube)
            {
                Material.DiffuseTextureID = RenderTools.TexturedCubeTex.ID;
                Material.DisplaySelection = IsSelected | IsHovered;
                Material.Color = this.Color;
                Material.HalfLambertShading = false;
                Material.ModelMatrix = Transform.TransformMatrix;
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

                var solid = new StandardMaterial();
                solid.hasVertexColors = true;
                solid.ModelMatrix = Transform.TransformMatrix;
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
