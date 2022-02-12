using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Diagnostics;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    public class LightPrepassManager
    {
        public static Framebuffer Filter;

        public static void Init(int width, int height)
        {
            Filter = new Framebuffer(FramebufferTarget.Framebuffer, width, height, PixelInternalFormat.R11fG11fB10f, 0, false);
            Filter.SetDrawBuffers(DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1);

            if (GLH.GetError() == ErrorCode.InvalidOperation) Debugger.Break();
        }

        public static void CreateLightPrepassTexture(GLContext control, 
            GLTexture normalsTexture, GLTexture depthTexture, GLTexture output)
        {
            GLH.BindTexture(output.Target, 0);

            if (Filter == null)
                Init(control.Width, control.Height);

            Filter.Bind();

            if (Filter.Width != control.Width || Filter.Height != control.Height)
                Filter.Resize(control.Width, control.Height);

            if (output.Width != control.Width || output.Height != control.Height)
            {
                output.Bind();
                if (output is GLTexture2DArray)
                {
                    GLH.TexImage3D(output.Target, 0, output.PixelInternalFormat,
                          control.Width, control.Height, 1, 0, output.PixelFormat, output.PixelType, IntPtr.Zero);
                }
                else
                {
                    GLH.TexImage2D(output.Target, 0, output.PixelInternalFormat,
                          control.Width, control.Height, 0, output.PixelFormat, output.PixelType, IntPtr.Zero);
                }
                output.Unbind();
            }

            GLH.Viewport(0, 0, control.Width, control.Height);

            for (int i = 0; i < 1; i++)
            {
                if (output is GLTexture2DArray)
                {
                    GLH.FramebufferTextureLayer(FramebufferTarget.Framebuffer,
                            FramebufferAttachment.ColorAttachment0, output.ID, 0, i);
                }
                else
                {
                    GLH.FramebufferTexture(FramebufferTarget.Framebuffer,
                            FramebufferAttachment.ColorAttachment0, output.ID, 0);
                }
            }


            GLH.ClearColor(0, 0, 0, 0);
            GLH.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            SceneLightManager.DrawSceneLights(control.Camera, normalsTexture, depthTexture);
            CausticLightManager.DrawCaustics(control, normalsTexture, depthTexture);

            var errorcheck = GLH.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorcheck != FramebufferErrorCode.FramebufferComplete)
                throw new Exception(errorcheck.ToString());

            GLH.UseProgram(0);
            Filter.Unbind();
        }

    }
}
