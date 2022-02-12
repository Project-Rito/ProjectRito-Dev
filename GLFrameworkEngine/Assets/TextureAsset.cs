using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    [Serializable]
    public class TextureAsset : AssetBase
    {
        /// <summary>
        /// The rendered texture instance used to display the texture in OpenGL.
        /// </summary>
        public GLTexture RenderableTex { get; set; }

        public static System.Drawing.Bitmap CreateTextureRender(GLContext control, GLTexture renderableTex, int width = 50, int height = 50)
        {
            TextureAsset texture = new TextureAsset();
            texture.RenderableTex = renderableTex;
            texture.CreateTextureRender(control, null, width, height);
            return texture.Thumbnail;
        }

        public void CreateTextureRender(GLContext control, EventHandler thumbnailUpdate, int width = 50, int height = 50)
        {
            var shader = GlobalShaders.GetShader("SCREEN");

            Framebuffer frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, width, height);
            frameBuffer.Bind();

            GLH.ClearColor(0, 0, 0, 0);
            GLH.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GLH.Viewport(0, 0, width, height);

            GLH.Disable(EnableCap.Blend);

            shader.Enable();

            //Draw the texture onto the framebuffer
            ScreenQuadRender.Draw(shader, RenderableTex.ID);

            //Disable shader and textures
            GLH.UseProgram(0);
            GLH.BindTexture(TextureTarget.Texture2D, 0);

            var thumbnail = frameBuffer.ReadImagePixels(true);
            thumbnail.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);

            //Dispose frame buffer
            frameBuffer.Dispoe();
            frameBuffer.DisposeRenderBuffer();

            this.Thumbnail = thumbnail;
            thumbnailUpdate?.Invoke(this, EventArgs.Empty);
        }
    }
}
