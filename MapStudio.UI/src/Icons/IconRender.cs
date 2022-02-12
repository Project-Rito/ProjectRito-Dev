using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;
using GLFrameworkEngine;

namespace MapStudio.UI
{
    public class IconRender 
    {
        public static int CreateTextureRender(GLTexture texture, int width, int height, bool displayAlpha = true)
        {
            var shader = GlobalShaders.GetShader("TEXTURE_ICON");

            Framebuffer frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, width, height, PixelInternalFormat.Rgba, 1);
            frameBuffer.Bind();

            GLH.ClearColor(0, 0, 0, 0);
            GLH.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GLH.Viewport(0, 0, width, height);

            GLH.Disable(EnableCap.Blend);

            shader.Enable();
            shader.SetBoolToInt("isSRGB", false);
            texture.Bind();

            //Draw the texture onto the framebuffer
            ScreenQuadRender.Draw(shader, texture.ID);

            //Disable shader and textures
            GLH.UseProgram(0);
            GLH.BindTexture(TextureTarget.Texture2D, 0);

            var image = (GLTexture2D)frameBuffer.Attachments[0];
            return image.ID;
        }

        public static int CreateTextureRender(STGenericTexture texture, int width, int height, bool displayAlpha = true)
        {
            if (texture.RenderableTex == null) {
                texture.LoadRenderableTexture();
            }

            if (texture.RenderableTex == null)
                return -1;

            int ID = texture.RenderableTex.ID;
            if (texture.Platform.OutputFormat == TexFormat.BC5_SNORM)
            {
                var reloaded = GLTexture2D.FromGeneric(texture, new ImageParameters()
                {
                    UseSoftwareDecoder = (texture.Platform.OutputFormat == TexFormat.BC5_SNORM),
                });
                ID = reloaded.ID;
            }

            var shader = GlobalShaders.GetShader("TEXTURE_ICON");

            Framebuffer frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, width, height, PixelInternalFormat.Rgba, 1);
            frameBuffer.Bind();

            GLH.ClearColor(0, 0, 0, 0);
            GLH.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GLH.Viewport(0, 0, width, height);

            GLH.Disable(EnableCap.Blend);

            shader.Enable();
            shader.SetBoolToInt("isSRGB", texture.IsSRGB);

            int[] mask = new int[4]
            {
                    OpenGLHelper.GetSwizzle(texture.RedChannel),
                    OpenGLHelper.GetSwizzle(texture.GreenChannel),
                    OpenGLHelper.GetSwizzle(texture.BlueChannel),
                    OpenGLHelper.GetSwizzle(displayAlpha ? texture.AlphaChannel : STChannelType.One),
            };
            ((GLTexture)texture.RenderableTex).Bind();
            GLH.TexParameter(((GLTexture)texture.RenderableTex).Target, TextureParameterName.TextureSwizzleRgba, mask);

            //Draw the texture onto the framebuffer
            ScreenQuadRender.Draw(shader, ID);

            //Disable shader and textures
            GLH.UseProgram(0);
            GLH.BindTexture(TextureTarget.Texture2D, 0);

            var image = (GLTexture2D)frameBuffer.Attachments[0];
            return image.ID;

         /*   //Dispose frame buffer
            frameBuffer.Dispoe();
            frameBuffer.DisposeRenderBuffer();*/
        }
    }
}
