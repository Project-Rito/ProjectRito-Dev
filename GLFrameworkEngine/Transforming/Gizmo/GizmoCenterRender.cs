using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class GizmoCenterRender : Circle2DRenderer
    {
        static GizmoCenterRender Instance;

        static BillboardMaterial Material = new BillboardMaterial();

        public GizmoCenterRender()
            : base(60, PrimitiveType.LineLoop)
        {

        }

        public static void Draw(GLContext context, Vector3 position, float scale, Vector4 color)
        {
            if (Instance == null)
                Instance = new GizmoCenterRender();

            var mdlMtx = Matrix4.CreateTranslation(position);

            Material.Color = color;
            Material.ModelMatrix = mdlMtx;
            Material.ScaleByCameraDistance = true;
            Material.Render(context);

            GLL.DepthMask(false);
            GLL.Enable(EnableCap.CullFace);
            GLL.CullFace(CullFaceMode.Back);

            GLL.LineWidth(1.5f);

            Instance.Draw(context);

            GLL.DepthMask(true);

            GLL.LineWidth(1);
        }
    }
}
