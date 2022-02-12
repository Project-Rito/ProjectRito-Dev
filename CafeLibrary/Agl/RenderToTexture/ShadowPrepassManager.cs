using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    public class ShadowPrepassManager
    {
        public static Framebuffer Filter;

        public static void Init()
        {
            Filter = new Framebuffer(FramebufferTarget.Framebuffer,
                640, 720, PixelInternalFormat.Rgba16f, 1);
        }

        public static void CreateShadowPrepassTexture(GLContext control,
          GLTexture shadowMap, GLTexture normalsTexture, GLTexture depthTexture, GLTexture2D output)
        {
            if (Filter == null)
                Init();

            Filter.Bind();

            GLL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, output.ID, 0);

            GLL.Viewport(0, 0, control.Width, control.Height);
            GLL.BindTexture(TextureTarget.Texture2D, 0);

            var shader = GlobalShaders.GetShader("SHADOWPREPASS");
            shader.Enable();

            if (Filter.Width != control.Width || Filter.Height != control.Height)
                Filter.Resize(control.Width, control.Height);

            if (output.Width != control.Width || output.Height != control.Height)
            {
                output.Bind();
                GLL.TexImage2D(output.Target, 0, output.PixelInternalFormat,
                          control.Width, control.Height, 0, output.PixelFormat, output.PixelType, IntPtr.Zero);
                output.Unbind();
            }

            GLL.ClearColor(0, 0, 0, 0);
            GLL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var viewProjMatInv = control.Camera.ViewProjectionMatrix.Inverted();
            var lightVP = control.Scene.ShadowRenderer.GetLightSpaceViewProjMatrix();
            var lightDir = control.Scene.ShadowRenderer.GetLightDirection();
            var viewPos = control.Camera.GetViewPostion();

            shader.SetMatrix4x4("mtxViewProjInv", ref viewProjMatInv);
            shader.SetMatrix4x4("mtxLightVP", ref lightVP);
            shader.SetTexture(depthTexture, "depthTexture", 1);
            shader.SetTexture(shadowMap, "shadowMap", 2);
            shader.SetTexture(normalsTexture, "normalsTexture", 3);

            shader.SetVector3("viewPos", viewPos);
            shader.SetVector3("lightPos", lightDir);
            shader.SetFloat("shadowBias", 0.0025f);

            ScreenQuadRender.Draw();

            GLL.Flush();

            Filter.Unbind();
            GLL.BindTexture(TextureTarget.Texture2D, 0);

            GLL.UseProgram(0);
            GLL.Viewport(0, 0, control.Width, control.Height);
        }
    }
}
