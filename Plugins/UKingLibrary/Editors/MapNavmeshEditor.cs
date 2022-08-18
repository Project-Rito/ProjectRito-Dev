using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using ImGuiNET;
using GLFrameworkEngine;
using MapStudio.UI;

namespace UKingLibrary
{
    public class MapNavmeshEditor : IDrawable
    {
        public IMapLoader ParentLoader { get; set; }
        private ViewportRenderer Pipeline
        {
            get
            {
                return ParentLoader.ParentEditor.Workspace.ViewportWindow.Pipeline;
            }
        } 

        private SelectionBox SelectionBox = null;
        private SelectionCircle SelectionCircle = null;

        private bool IsEditing = false;

        public bool Enabled
        {
            get
            {
                if (ParentLoader.ParentEditor.SubEditor != TranslationSource.GetText("NAVMESH"))
                    return false;
                if (ParentLoader.ParentEditor.ActiveMapLoader != ParentLoader)
                    return false;
                if (ParentLoader.ParentEditor.Workspace.ActiveEditor != ParentLoader.ParentEditor)
                    return false;
                if (Workspace.ActiveWorkspace != ParentLoader.ParentEditor.Workspace)
                    return false;

                return true;
            }
        }

        public MapNavmeshEditor(IMapLoader parentLoader)
        {
            ParentLoader = parentLoader;

            Pipeline._context.FinalDraws.Add(this);
            Init();
        }

        public void OnKeyDown(KeyEventInfo e)
        {

        }

        public void OnKeyUp(KeyEventInfo e)
        {

        }

        public void OnMouseDown()
        {
            if (!Enabled)
                return;

            if (OpenTK.Input.Mouse.GetState().LeftButton == OpenTK.Input.ButtonState.Pressed)
                IsEditing = true;
        }

        public void OnMouseUp()
        {
            if (OpenTK.Input.Mouse.GetState().LeftButton == OpenTK.Input.ButtonState.Released)
                IsEditing = false;
        }

        private long _lastAppliedFrame = -1;
        public void OnMouseMove()
        {
            if (!IsEditing)
                return;

            if (_lastAppliedFrame == Pipeline._context.CurrentFrame) // Don't run this stuff multiple times per frame.
                return;
            _lastAppliedFrame = Pipeline._context.CurrentFrame;

            Vector3[] positions = GetPositions();
        }

        public void OnMouseWheel()
        {
            SelectionCircle?.Resize(MouseEventInfo.Delta);
        }

        private void Init()
        {
            InitSelectionRender();
            InitOffscreenBuffers();
        }

        #region General Rendering
        public bool IsVisible
        {
            get
            {
                return Enabled;
            }
            set { }
        }

        private void InitSelectionRender()
        {
            if (SelectionCircle == null)
            {
                SelectionCircle = new SelectionCircle();
                SelectionCircle.Start(Pipeline._context, Pipeline._context.CurrentMousePoint.X, Pipeline._context.CurrentMousePoint.Y);
            }
        }

        public void DrawModel(GLContext context, Pass pass)
        {
            if (pass != Pass.TRANSPARENT)
                return;
            SelectionCircle.Render(context, context.CurrentMousePoint.X, context.CurrentMousePoint.Y);
        }
        #endregion

        #region Offscreen Rendering
        /// <summary>
        /// A buffer with pixels representing worldspace positions to add to the navmesh.
        /// </summary>
        private Framebuffer PositionBuffer = null;

        private void InitOffscreenBuffers()
        {
            if (PositionBuffer == null)
                PositionBuffer = new Framebuffer(FramebufferTarget.Framebuffer, Pipeline.Width, Pipeline.Height, PixelInternalFormat.Rgba32f, 1);
        }

        private void DrawPositions()
        {
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            // Draw selection area to the stencil buffer.
            // Also draws color data but we'll clear that next.
            GL.Enable(EnableCap.StencilTest);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
            SelectionCircle.Render(Pipeline._context, Pipeline._context.CurrentMousePoint.X, Pipeline._context.CurrentMousePoint.Y);

            // Get rid of any actual color drawn; we just want the stencil.
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Draw everything else and apply the stencil.
            GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            foreach (var bakedCollision in ParentLoader.BakedCollision)
            {
                foreach (HavokMeshShapeRender shapeRender in bakedCollision.ShapeRenders)
                {
                    shapeRender.DrawPositionColor(Pipeline._context);
                }
            }
            GL.Disable(EnableCap.StencilTest);
        }

        private Vector3[] GetPositions()
        {
            PositionBuffer.Bind();

            DrawPositions();

            Vector4[] pixels = new Vector4[Pipeline.Width * Pipeline.Height];
            GL.ReadPixels(0, 0, Pipeline.Width, Pipeline.Height, PixelFormat.Rgba, PixelType.Float, pixels);

            PositionBuffer.Unbind();

            return pixels.Where(pixel => pixel.W != 0).Select(pixel => pixel.Xyz).ToArray();
        }
        #endregion

        public void Dispose()
        {
            ParentLoader.ParentEditor.Workspace.ViewportWindow.Pipeline._context.FinalDraws.Remove(this);
        }
    }
}
