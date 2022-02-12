using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    /// <summary>
    /// Creates a screen buffer texture of the current color buffer.
    /// This is drawn before meshes with the UserColorPass setting used.
    /// Meshes with UserColorPass can obtain the buffer with GetColorBuffer()
    /// </summary>
    public class ScreenBufferTexture
    {
        private static Framebuffer Filter;

        public static GLTexture2D ScreenBuffer;

        public static void Init()
        {
            Filter = new Framebuffer(FramebufferTarget.Framebuffer,
                640, 720, PixelInternalFormat.Rgba16f, 1);
        }

        public static GLTexture2D GetColorBuffer(GLContext control) {
            return ScreenBuffer;
        }

        public static void FilterScreen(GLContext control)
        {
            if (Filter == null)
                Init();

            GLL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            Filter.Bind();
            GLL.Viewport(0, 0, control.Width, control.Height);
            GLL.BindTexture(TextureTarget.Texture2D, 0);

            var shader = GlobalShaders.GetShader("SCREEN");
            shader.Enable();
            shader.SetInt("flipVertical", 1);

            var texture = (GLTexture2D)control.ScreenBuffer.Attachments[0];

            if (Filter.Width != control.Width || Filter.Height != control.Height)
                Filter.Resize(control.Width, control.Height);

            GLL.ClearColor(0,0,0,0);
            GLL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ScreenQuadRender.Draw(shader, texture.ID);
            ScreenBuffer = (GLTexture2D)Filter.Attachments[0];

            GLL.Flush();

            Filter.Unbind();
            GLL.BindTexture(TextureTarget.Texture2D, 0);

            GLL.UseProgram(0);

            ScreenBuffer.Bind();
            GLL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            GLL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GLL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GLL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GLL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
            GLL.BindTexture(TextureTarget.Texture2D, 0);

            control.ScreenBuffer.Bind();
            GLL.Viewport(0, 0, control.Width, control.Height);
        }
    }
}
