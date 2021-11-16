using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using OpenTK;

namespace UKingLibrary
{
    public class TagRender : EditableObject
    {
        public Vector4 Color = new Vector4(1, 1, 1, 1);

        static GLTexture TagAndTexture = null;
        static GLTexture TagOrTexture = null;

        UVCubeRenderer CubeRenderer = null;
        StandardMaterial Material = new StandardMaterial();

        public bool EnableFrustumCulling => true;
        public bool InFrustum { get; set; }

        public BoundingNode Boundings = new BoundingNode()
        {
            Center = new Vector3(0, 0, 0),
            Box = new BoundingBox(new Vector3(-10), new Vector3(10)),
        };

        public bool IsInsideFrustum(GLContext context) {
            return context.Camera.InFustrum(Boundings);
        }

        public TagRender(NodeBase parent) : base(parent)
        {
            //Update boundings on transform changed
            this.Transform.TransformUpdated += delegate {
                Boundings.UpdateTransform(this.Transform.TransformMatrix);
            };
        }

        public void DrawColorPicking(GLContext context)
        {
            Prepare();

            CubeRenderer.DrawPicking(context, this, Transform.TransformMatrix);
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if (pass != Pass.OPAQUE || !this.InFrustum)
                return;

            Prepare();

            Material.DiffuseTextureID = -1;
            Material.Color = this.Color;

            string type = UINode.Header;
            switch (type)
            {
                case "LinkTagAnd":
                    Material.DiffuseTextureID = TagAndTexture.ID;
                    break;
                case "LinkTagOr":
                    Material.DiffuseTextureID = TagAndTexture.ID;
                    break;
                case "LinkTagNone":
                    Material.DiffuseTextureID = TagAndTexture.ID;
                    break;
            }

            Material.DisplaySelection = IsSelected | IsHovered;
            Material.HalfLambertShading = false;
            Material.ModelMatrix = Transform.TransformMatrix;
            Material.Render(context);

            CubeRenderer.DrawWithSelection(context, IsSelected || IsHovered);
        }

        private void UpdateMaterial(GLTexture texture)
        {
            Material.DiffuseTextureID = TagAndTexture.ID;
        }

        private void Prepare()
        {
            if (CubeRenderer == null || CubeRenderer.IsDisposed)
                CubeRenderer = new UVCubeRenderer(10);

            if (TagAndTexture == null)
                TagAndTexture = GLTexture2D.FromBitmap(Properties.Resources.TagAnd);
            if (TagOrTexture == null)
                TagOrTexture = GLTexture2D.FromBitmap(Properties.Resources.TagOr);
        }

        public override void Dispose()
        {
            CubeRenderer?.Dispose();
        }
    }
}
