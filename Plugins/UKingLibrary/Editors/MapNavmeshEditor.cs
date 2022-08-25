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
        public NavmeshEditFilter EditFilter { get; set; } = new NavmeshEditFilter
        {
            AngleMax = 0.95f
        };

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

            DrawingHelper.VerticesIndices<Vector3> geometry = GetGeometry();
            m = new RenderMesh<HavokMeshShapeRender.HavokMeshShapeVertex>(geometry.Vertices.Select(pos => new HavokMeshShapeRender.HavokMeshShapeVertex
            {
                Position = pos,
                Normal = Vector3.Zero,
                VertexColor = Vector4.One,
                VertexIndex = 0
            }).ToArray(), geometry.Indices.ToArray(), PrimitiveType.Triangles);
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
            //DrawPositions();

            var shader = GlobalShaders.GetShader("HAVOK_SHAPE");
            context.CurrentShader = shader;
            shader.SetTransform(GLConstants.ModelMatrix, new GLTransform());

            GL.PointSize(4);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(-4f, 1f);
            m?.Draw(context);
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }
        #endregion

        private RenderMesh<HavokMeshShapeRender.HavokMeshShapeVertex> m = null;

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

            GL.Enable(EnableCap.DepthTest);

            // Draw everything else and apply the stencil.
            GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            foreach (var bakedCollision in ParentLoader.BakedCollision)
            {
                for (int i = 0; i < bakedCollision.ShapeRenders.Count; i++)
                {
                    bakedCollision.ShapeRenders[i].DrawForNavmeshPaint(Pipeline._context, EditFilter, i, Pipeline.Height, Pipeline.Width);
                }
            }
            GL.Disable(EnableCap.StencilTest);
        }

        private DrawingHelper.VerticesIndices<Vector3> GetGeometry()
        {
            PositionBuffer.Bind();

            DrawPositions();

            Vector4[] pixels = new Vector4[Pipeline.Width * Pipeline.Height];
            GL.ReadPixels(0, 0, Pipeline.Width, Pipeline.Height, PixelFormat.Rgba, PixelType.Float, pixels);

            PositionBuffer.Unbind();

            Dictionary<Tuple<float, float>, Vector4> uniqueVertexPixels = new Dictionary<Tuple<float, float>, Vector4>();
            foreach (Vector4 pixel in pixels.Where(pixel => pixel.W != 0))
            {
                Tuple<float, float> key = new Tuple<float, float>(pixel.X, pixel.Y);
                if (!uniqueVertexPixels.TryAdd(key, pixel) && uniqueVertexPixels[key].Z > pixel.Z)
                    uniqueVertexPixels[key] = pixel;
            }

            DrawingHelper.VerticesIndices<Vector3> result = new DrawingHelper.VerticesIndices<Vector3>();
            foreach (Vector4 uniqueVertexPixel in uniqueVertexPixels.Values)
            {
                result.Indices.Add(result.Indices.Count);
                result.Indices.Add(result.Indices.Count);
                result.Indices.Add(result.Indices.Count);

                int vertexIndex = (int)uniqueVertexPixel.X;//BitConverter.ToInt32(BitConverter.GetBytes(uniqueVertexPixel.X));
                int indicesIndex = (int)(vertexIndex - vertexIndex % 3);

                HavokMeshShapeRender render = ParentLoader.BakedCollision[0].ShapeRenders[(int)uniqueVertexPixel.Y];
                Matrix4 transform = render.Transform.TransformMatrix;
                transform.Transpose();

                var test = (transform * new Vector4(render.Vertices[render.Indices[indicesIndex + 0]].Position, 1f)).Xyz;
                var test2 = render.Vertices[render.Indices[indicesIndex + 0]].Position;

                result.Vertices.Add((transform * new Vector4(render.Vertices[render.Indices[indicesIndex + 0]].Position, 1f)).Xyz);
                result.Vertices.Add((transform * new Vector4(render.Vertices[render.Indices[indicesIndex + 1]].Position, 1f)).Xyz);
                result.Vertices.Add((transform * new Vector4(render.Vertices[render.Indices[indicesIndex + 2]].Position, 1f)).Xyz);
            }

            return result;
        }
        #endregion

        public void Dispose()
        {
            ParentLoader.ParentEditor.Workspace.ViewportWindow.Pipeline._context.FinalDraws.Remove(this);
        }

        #region Types

        /// <summary>
        /// Info for filtering data before adding to navmesh.
        /// </summary>
        public struct NavmeshEditFilter
        {
            /// <summary>
            /// From 0-1, the maximum angle to allow in navmesh.
            /// Should be tested against the y-component of a normalized world normal.
            /// </summary>
            public float AngleMax;
        }
        #endregion
    }
}
