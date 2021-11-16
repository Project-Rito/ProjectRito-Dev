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
    /// Represents an orientation view cube for displaying the camera orientation.
    /// This cube is automatically drawn to the bottom left corner of the current viewport.
    /// </summary>
    public class OrientationGizmo
    {
        UVCubeRenderer CubeRenderer = null;
        StandardMaterial Material = new StandardMaterial();

        GLTexture DisplayCubeTexture = null;

        public void Draw(GLContext context)
        {
            Prepare();

            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);

            //Create a view rectangle to store the orientation cube in.
            var width = (int)(context.Width * 0.1f);
            var height = (int)(context.Height * 0.1f);
            //Display at the bottom left corner
            GL.Viewport(0, 0, width, height);

            float scale = 0.5f;
            //Use only the rotation in the view matrix.
            Matrix4 viewMatrix = context.Camera.ViewMatrix.ClearTranslation();
            //Use an ortho view.
            Matrix4 projMatrix = Matrix4.CreateOrthographic(width * scale, height * scale, -200f, 200f);

            Material.DiffuseTextureID = DisplayCubeTexture.ID;
            Material.CameraMatrix = viewMatrix * projMatrix;
            Material.Render(context);

            CubeRenderer.Draw(context);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Viewport(0, 0, context.Width, context.Height);
        }

        private void Prepare()
        {
            if (CubeRenderer == null || CubeRenderer.IsDisposed)
                CubeRenderer = new UVCubeRenderer(10);
            if (DisplayCubeTexture == null)
                DisplayCubeTexture = GLTexture2D.FromBitmap(Properties.Resources.TexturedCube);
        }

        public void Dispose()
        {
            CubeRenderer?.Dispose();
            DisplayCubeTexture?.Dispose();
        }
    }
}
