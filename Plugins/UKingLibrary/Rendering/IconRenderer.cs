using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;
using CafeLibrary.Rendering;
using Toolbox.Core;

namespace UKingLibrary.UI
{
    public class IconModelRenderer
    {
        static Framebuffer Framebuffer;
        static GLContext Control;
        static GraphicsContext GContext;

        //Camera settings
        static float CameraRotationX = 0;
        static float CameraRotationY = 0;
        static float CameraDistance = 0;

        public static Bitmap[] CreateRender(IGraphicsContext context, IEnumerable<BfresRender> drawables, int width = 32, int height = 32)
        {
            List<Bitmap> textures = new List<Bitmap>();

            Framebuffer = new Framebuffer(FramebufferTarget.Framebuffer, width, height);

            //New context instance
            Control = new GLContext();
            Control.Camera = new Camera();

            //New GL instance for multi threading
            GraphicsMode mode = new GraphicsMode(new ColorFormat(32), 24, 8, 4, new ColorFormat(32), 2, false);
            var window = new GameWindow(width, height, mode);

            GContext = new GraphicsContext(mode, window.WindowInfo);
            GContext.MakeCurrent(window.WindowInfo);

            Control.Width = width;
            Control.Height = height;
            Control.Camera.Width = width;
            Control.Camera.Height = height;
            //Target screen buffer
            Control.ScreenBuffer = Framebuffer;

            GLH.Enable(EnableCap.FramebufferSrgb);

            //Setup the camera
            foreach (var drawable in drawables)
            {
                var boundingSphere = drawable.BoundingSphere;
                Control.Camera.FrameBoundingSphere(boundingSphere);
                Control.Camera.RotationX = CameraRotationX * STMath.Deg2Rad;
                Control.Camera.RotationY = -CameraRotationY * STMath.Deg2Rad;
                Control.Camera.TargetDistance += CameraDistance;
                Control.Camera.UpdateMatrices();

                //Render out the file to the pipeline FBO
                Framebuffer.Bind();

                GLH.Viewport(0, 0, Control.Width, Control.Height);
                GLH.ClearColor(1, 0, 0, 1);
                GLH.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                drawable.InFrustum = true;
                drawable.IsVisible = true;

                //Draw the models
                drawable.DrawModel(Control, Pass.OPAQUE);
                drawable.DrawModel(Control, Pass.TRANSPARENT);

                //End the frame
                GLH.Flush();
                GContext.SwapBuffers();

                //Get the fbo and make the icon
                var rt = Framebuffer.ReadImagePixels();
                textures.Add(rt);

              //  rt.Save($"{System.IO.Path.GetFileNameWithoutExtension(drawable.Name)}.png");
            }
            Cleanup();

            return textures.ToArray();
        }

        static void Cleanup()
        {
            Framebuffer?.Dispoe();
            GLH.Disable(EnableCap.FramebufferSrgb);
            GC.Collect();
        }
    }
}
