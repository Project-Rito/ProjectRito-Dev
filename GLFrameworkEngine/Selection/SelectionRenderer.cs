using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class SelectionRenderer
    {
        static GLMaterialBlendState TranslucentBlend = new GLMaterialBlendState()
        {
            AlphaSrc = BlendingFactorSrc.One,
            AlphaDst = BlendingFactorDest.One,
            BlendColor = true,
        };

        static GLMaterialBlendState MaskedAlpha = new GLMaterialBlendState()
        {
            AlphaTest = true,
            AlphaFunction = AlphaFunction.Gequal,
            AlphaValue = 0.5f,
        };

        static DashMaterial DashMaterial = new DashMaterial();
        static StandardMaterial StandardMaterial = new StandardMaterial();

        public static void DrawDashedOutline(GLContext context, Matrix4 mdlMtx, RenderMesh<Vector2> mesh, PrimitiveType primitiveType)
        {
            DashMaterial.ModelMatrix = mdlMtx;
            DashMaterial.Render(context);

            //Draw the dashed alpha material
            MaskedAlpha.RenderAlphaTest();

            GLL.LineWidth(1.0f);
            mesh.UpdatePrimitiveType(primitiveType);
            mesh.Draw(context);

            GLL.Disable(EnableCap.AlphaTest);
        }

        public static void DrawFilledMask(GLContext context, Matrix4 mdlMtx,
            RenderMesh<Vector2> mesh, PrimitiveType primitiveType)
        {
            StandardMaterial.Color = new Vector4(1, 1, 1, 0.1f);
            StandardMaterial.ModelMatrix = mdlMtx;
            StandardMaterial.CameraMatrix = Matrix4.Identity;
            StandardMaterial.Render(context);

            TranslucentBlend.RenderBlendState();

            GLL.DepthMask(false);
            GLL.Enable(EnableCap.CullFace);
            GLL.CullFace(CullFaceMode.Back);

            mesh.UpdatePrimitiveType(primitiveType);
            mesh.Draw(context);

            GLL.DepthMask(true);
        }
    }
}
