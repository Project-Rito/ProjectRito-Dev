using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace MapStudio.UI
{
    public class DeferredRenderQuad
    {
        static Plane2DRenderer PlaneRender;
 
        public static void Draw(GLContext control, GLTexture colorPass,
            GLTexture bloomPass, RenderFrameArgs frameArgs)
        {
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.CullFace);

            var shader = GlobalShaders.GetShader("FINALHDR");
            control.CurrentShader = shader;

            shader.SetInt("ENABLE_FBO_ALPHA", frameArgs.DisplayAlpha ? 1 : 0);
            shader.SetInt("ENABLE_BLOOM", 0);
            shader.SetInt("ENABLE_LUT", 0);
            shader.SetInt("ENABLE_BACKGROUND", 0);
            shader.SetBoolToInt("ENABLE_SRGB", control.UseSRBFrameBuffer);

            shader.SetVector3("backgroundTopColor", new Vector3(
                DrawableBackground.BackgroundTop.X,
                DrawableBackground.BackgroundTop.Y,
                DrawableBackground.BackgroundTop.Z));
            shader.SetVector3("backgroundBottomColor", new Vector3(
                DrawableBackground.BackgroundBottom.X,
                DrawableBackground.BackgroundBottom.Y,
                DrawableBackground.BackgroundBottom.Z));

            GL.ActiveTexture(TextureUnit.Texture1);
            colorPass.Bind();
            shader.SetInt("uColorTex", 1);

            if (frameArgs.DisplayBackground && DrawableBackground.Display)
            {
                shader.SetInt("ENABLE_BACKGROUND", 1);
             }
            if (bloomPass != null && control.EnableBloom)
            {
                shader.SetInt("ENABLE_BLOOM", 1);

                GL.ActiveTexture(TextureUnit.Texture24);
                bloomPass.Bind();
                shader.SetInt("uBloomTex", 24);
            }

            if (PlaneRender == null)
                PlaneRender = new Plane2DRenderer(1.0f, true);

            PlaneRender.Draw(control);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.UseProgram(0);
        }
    }
}
        