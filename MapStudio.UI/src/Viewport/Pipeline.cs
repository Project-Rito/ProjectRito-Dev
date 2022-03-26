using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using GLFrameworkEngine;

namespace MapStudio.UI
{
    public class ViewportRenderer
    {
        DrawableInfiniteFloor _floor;
        ObjectLinkDrawer _objectLinkDrawer;
        OrientationGizmo _orientationGizmo;
        DrawableBackground _background;

        public List<IRenderableFile> Files = new List<IRenderableFile>();
        public List<CameraAnimation> CameraAnimations = new List<CameraAnimation>();

        private bool isView2D;

        /// <summary>
        /// Determines to use a 2D or 3D camera view.
        /// </summary>
        public bool IsViewport2D
        {
            get { return isView2D; }
            set
            {
                isView2D = value;

                //Set the camera instance
                if (value)
                    _context.Camera = _camera2D;
                else
                    _context.Camera = _camera3D;
                //Update with changes
                _context.UpdateViewport = true;
                //Update camera context with window info
                OnResize();
            }
        }

        public int Width { get; set; }
        public int Height { get; set; }

        public GLContext _context;

        public Camera _camera3D;
        public Camera _camera2D;

        private OpenTK.Vector2 _previousPosition = OpenTK.Vector2.Zero;

        private DepthTexture DepthTexture;

        private Framebuffer PostEffects;
        private Framebuffer BloomEffects;
        private Framebuffer ScreenBuffer;
        private Framebuffer GBuffer;
        private Framebuffer FinalBuffer;

        public int GetViewportTexture() => ((GLTexture)FinalBuffer.Attachments[0]).ID;

        static bool USE_GBUFFER => ShadowMainRenderer.Display;

        public void InitScene()
        {
            _background = new DrawableBackground();
            _floor = new DrawableInfiniteFloor();
            _objectLinkDrawer = new ObjectLinkDrawer();
            _orientationGizmo = new OrientationGizmo();

            _context = new GLContext();
            _context.SetActive();
            _context.ScreenBuffer = ScreenBuffer;

            //For 2D controls/displaying

            //Top down, locked rotation, ortho projection
            _camera2D = new Camera();
            _camera2D.IsOrthographic = true;
            _camera2D.Mode = Camera.CameraMode.Inspect;
            _camera2D.ResetViewportTransform();
            _camera2D.Direction = Camera.FaceDirection.Top;
            _camera2D.UpdateMatrices();

            //3D camera
            _camera3D = new Camera();
            _context.Camera = _camera3D;
            _context.Camera.ResetViewportTransform();
            if (_context.Camera.Mode == Camera.CameraMode.Inspect)
            {
                _context.Camera.TargetPosition = new OpenTK.Vector3(0, 0, 0);
                _context.Camera.Distance = 50;
            }
            else
                _context.Camera.TargetPosition = new OpenTK.Vector3(0, 0, 50);

            _context.Scene.ShadowRenderer = new ShadowMainRenderer();
            _context.Scene.Cursor3D = new Cursor3D();
        }

        public void InitBuffers()
        {
            InitScene();

            ScreenBuffer = new Framebuffer(FramebufferTarget.Framebuffer,
                this.Width, this.Height, 16, PixelInternalFormat.Rgba16f, 1);
            ScreenBuffer.Resize(Width, Height);

            PostEffects = new Framebuffer(FramebufferTarget.Framebuffer,
                 Width, Height, PixelInternalFormat.Rgba16f, 1);
            PostEffects.Resize(Width, Height);

            BloomEffects = new Framebuffer(FramebufferTarget.Framebuffer,
                 Width, Height, PixelInternalFormat.Rgba16f, 1);
            BloomEffects.Resize(Width, Height);

            DepthTexture = new DepthTexture(Width, Height, PixelInternalFormat.DepthComponent24);

            //Set the GBuffer (Depth, Normals and another output)
            GBuffer = new Framebuffer(FramebufferTarget.Framebuffer);
            GBuffer.AddAttachment(FramebufferAttachment.ColorAttachment0,
                GLTexture2D.CreateUncompressedTexture(Width, Height, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgba, PixelType.Float));
            GBuffer.AddAttachment(FramebufferAttachment.ColorAttachment3,
                GLTexture2D.CreateUncompressedTexture(Width, Height, PixelInternalFormat.Rgb10A2, PixelFormat.Rgba, PixelType.Float));
            GBuffer.AddAttachment(FramebufferAttachment.ColorAttachment4,
                GLTexture2D.CreateUncompressedTexture(Width, Height, PixelInternalFormat.Rgb10A2, PixelFormat.Rgba, PixelType.Float));
            GBuffer.AddAttachment(FramebufferAttachment.DepthAttachment, DepthTexture);

            GBuffer.SetReadBuffer(ReadBufferMode.None);
            GBuffer.SetDrawBuffers(
                DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.None, DrawBuffersEnum.None,
                DrawBuffersEnum.ColorAttachment3, DrawBuffersEnum.ColorAttachment4);
            GBuffer.Unbind();

            FinalBuffer = new Framebuffer(FramebufferTarget.Framebuffer,
                this.Width, this.Height, PixelInternalFormat.Rgb16f, 1);
        }

        //Adds a camera to the scene for path viewing
        public void AddCameraAnimation(CameraAnimation animation)
        {
            CameraAnimations.Clear();
            CameraAnimations.Add(animation);
        }

        public void AddFile(IRenderableFile renderFile) {
            Files.Add(renderFile);
            _context.Scene.AddRenderObject(renderFile.Renderer);
        }

        public void AddFile(IDrawable renderFile) {
            _context.Scene.AddRenderObject(renderFile);
        }

        public Bitmap SaveAsScreenshot(int width, int height, bool useAlpha = false)
        {
            _context.UpdateViewport = true;

            //Resize the current viewport
            Width = width;
            Height = height;
            this.OnResize();

            RenderScene(new RenderFrameArgs()
            {
                DisplayAlpha = useAlpha,
                DisplayBackground = !useAlpha,
                DisplayOrientationGizmo = false,
                DisplayGizmo = false,
                DisplayCursor3D = false,
                DisplayFloor = false,
            });

            _context.UpdateViewport = true;

            return FinalBuffer.ReadImagePixels(useAlpha);
        }

        private OpenTK.Matrix4 viewProjection;

        public void RenderScene() {
            _context.Camera.UpdateMatrices();

            //Here we want the scene to only re draw when necessary for performance improvements
            if (viewProjection == _context.Camera.ViewProjectionMatrix && !_context.UpdateViewport)
                return;

            viewProjection = new OpenTK.Matrix4(
                _context.Camera.ViewProjectionMatrix.Row0,
                _context.Camera.ViewProjectionMatrix.Row1,
                _context.Camera.ViewProjectionMatrix.Row2,
                _context.Camera.ViewProjectionMatrix.Row3);

            _context.UpdateViewport = false;

            //Scene is drawn with frame arguments.
            //This is to customize what can be drawn during a single frame.
            //Backgrounds, alpha, and other data can be toggled for render purposes.
            RenderScene(_context.FrameArgs);
        }

        public void RenderScene(RenderFrameArgs frameArgs)
        {
            _context.Width = this.Width;
            _context.Height = this.Height;

            GL.Enable(EnableCap.DepthTest);

            var dir = _context.Scene.LightDirection;

            if (ShadowMainRenderer.Display)
                _context.Scene.ShadowRenderer.Render(_context, new OpenTK.Vector3(dir.X, dir.Y, dir.Z));

            ResourceTracker.ResetStats();

            DrawModels();

            //Transfer the screen buffer to the post effects buffer (screen buffer is multi sampled)
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, ScreenBuffer.ID);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, PostEffects.ID);
            GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            FinalBuffer.Bind();
            GL.Viewport(0, 0, Width, Height);

            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            
            //Draw post effects onto the final buffer
            DrawPostScreenBuffer(PostEffects, frameArgs);

            //Finally transfer the screen buffer depth onto the final buffer for non post processed objects
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, ScreenBuffer.ID);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, FinalBuffer.ID);
            GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);

            //Background
            if (frameArgs.DisplayBackground)
                _background.Draw(_context, Pass.OPAQUE);

            DrawSceneNoPostEffects();

            _context.CurrentShader = null;

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.Disable(EnableCap.Blend);

            if (ShadowMainRenderer.Display)
            {
                /*_context.Scene.ShadowRenderer.DrawShadowPrePass(_context,
                    AGraphicsLibrary.LightingEngine.LightSettings.ShadowPrepassTexture);
                */
                _context.Scene.ShadowRenderer.DrawDebugQuad(_context);
            }

            //Far cry editor like sprites attached to 3d objects
            if (frameArgs.Display2DSprites)
                DrawSprites();

            if (frameArgs.DisplayFloor)
                _floor.Draw(_context);
            if (frameArgs.DisplayOrientationGizmo)
                _orientationGizmo.Draw(_context);
            if (frameArgs.DisplayCursor3D)
                _context.Scene.Cursor3D.DrawModel(_context, Pass.OPAQUE);
            if (frameArgs.DisplayGizmo && _context.Scene.GetSelected().Count > 0)
                _context.TransformTools.DrawSelection(_context);

            _objectLinkDrawer.Draw(_context);

            _context.SelectionTools.Render(_context,
                _context.CurrentMousePoint.X,
               _context.CurrentMousePoint.Y);

            _context.LinkingTools.Render(_context,
                _context.CurrentMousePoint.X,
               _context.CurrentMousePoint.Y);

            GL.Enable(EnableCap.DepthTest);

            foreach (var anim in CameraAnimations)
                anim.DrawPath(_context);

            FinalBuffer.Unbind();
        }

        public void OnResize()
        {
            // Update the opengl viewport
            GL.Viewport(0, 0, Width, Height);

            //Resize all the screen buffers
            ScreenBuffer?.Resize(Width, Height);
            PostEffects?.Resize(Width, Height);
            GBuffer?.Resize(Width, Height);
            FinalBuffer?.Resize(Width, Height);
            BloomEffects?.Resize(Width, Height);

            //Store the screen buffer instance for color buffer effects
            _context.ScreenBuffer = ScreenBuffer;
            _context.Width = this.Width;
            _context.Height = this.Height;
            _context.Camera.Width = this.Width;
            _context.Camera.Height = this.Height;
            _context.Camera.UpdateMatrices();
        }

        public ITransformableObject GetPickedObject()
        {
            OpenTK.Vector2 position = new OpenTK.Vector2(MouseEventInfo.Position.X, _context.Height - MouseEventInfo.Position.Y);
            return _context.Scene.FindPickableAtPosition(_context, position);
        }

        private void DrawModels()
        {
            DrawGBuffer();

            GL.Viewport(0, 0, Width, Height);
            ScreenBuffer.Bind();

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            DrawSceneWithPostEffects();

            foreach (var actor in StudioSystem.Instance.Actors)
                actor.Draw(_context);

            ScreenBuffer.Unbind();
        }

        private void DrawSprites()
        {
            foreach (var file in _context.Scene.Objects)
            {
                if (!file.IsVisible)
                    continue;

                if (file is EditableObject)
                    ((EditableObject)file).DrawSprite(_context);
            }
        }

        private List<List<IInstanceDrawable>> instanceGroups = new List<List<IInstanceDrawable>>();
        private void DrawSceneWithPostEffects()
        {
            foreach (var obj in _context.Scene.Objects)
            {
                if (obj.IsVisible && obj is EditableObject && ((EditableObject)obj).UsePostEffects)
                {
                    obj.DrawModel(_context, Pass.OPAQUE);
                    obj.DrawModel(_context, Pass.TRANSPARENT);
                }
            }

            foreach (var group in instanceGroups)
                group.RemoveAll(file => file.UpdateInstanceGroup);
            instanceGroups.RemoveAll(group => group.Count == 0);
            foreach (var file in _context.Scene.Objects)
            {
                if (!(file is IInstanceDrawable))
                    continue;
                if (!((IInstanceDrawable)file).UpdateInstanceGroup)
                    continue;
                
                if (!file.IsVisible)
                    continue;

                if (file is IFrustumCulling)
                    if (!((IFrustumCulling)file).InFrustum)
                        continue;
                ((IInstanceDrawable)file).UpdateInstanceGroup = false;

                bool foundGroup = false;
                foreach (List<IInstanceDrawable> group in instanceGroups)
                {
                    if (group.Count == 32) // Put in a new group if this one is full.
                        continue;
                    if (((IInstanceDrawable)file).GroupsWith(group[0]))
                    {
                        group.Add((IInstanceDrawable)file);
                        foundGroup = true;
                        break;
                    }
                }
                if (!foundGroup)
                    instanceGroups.Add(new List<IInstanceDrawable>() { (IInstanceDrawable)file });
            }

            foreach (var group in instanceGroups)
            {
                if (group[0] is EditableObject && !((EditableObject)group[0]).UsePostEffects)
                    continue;
                List<GLTransform> transforms = new List<GLTransform>(group.Count);
                foreach (var file in group)
                    transforms.Add(file.Transform);

                group[0].DrawModel(_context, Pass.OPAQUE, transforms);
                group[0].DrawModel(_context, Pass.TRANSPARENT, transforms);
            }
        }

        private void DrawSceneNoPostEffects()
        {
            foreach (var file in _context.Scene.Objects)
            {
                if (!file.IsVisible || file is EditableObject && ((EditableObject)file).UsePostEffects)
                    continue;

                file.DrawModel(_context, Pass.OPAQUE);
                file.DrawModel(_context, Pass.TRANSPARENT);
            }


            foreach (var group in instanceGroups)
                group.RemoveAll(file => file.UpdateInstanceGroup);
            instanceGroups.RemoveAll(group => group.Count == 0);
            foreach (var file in _context.Scene.Objects)
            {
                if (!(file is IInstanceDrawable))
                    continue;
                if (!((IInstanceDrawable)file).UpdateInstanceGroup)
                    continue;
                ((IInstanceDrawable)file).UpdateInstanceGroup = false;

                if (!file.IsVisible)
                    continue;
                if (file is IFrustumCulling)
                    if (!((IFrustumCulling)file).InFrustum)
                        continue;
                bool foundGroup = false;
                foreach (List<IInstanceDrawable> group in instanceGroups)
                {
                    if (group.Count == 32) // Put in a new group if this one is full.
                        continue;

                    if (((IInstanceDrawable)file).GroupsWith(group[0]))
                    {
                        group.Add((IInstanceDrawable)file);
                        foundGroup = true;
                        break;
                    }
                }
                if (!foundGroup)
                    instanceGroups.Add(new List<IInstanceDrawable>() { (IInstanceDrawable)file });
            }

            foreach (var group in instanceGroups)
            {
                if (group[0] is EditableObject && ((EditableObject)group[0]).UsePostEffects)
                    continue;
                List<GLTransform> transforms = new List<GLTransform>(group.Count);
                foreach (var file in group)
                    transforms.Add(file.Transform);

                group[0].DrawModel(_context, Pass.OPAQUE, transforms);
                group[0].DrawModel(_context, Pass.TRANSPARENT, transforms);
            }
        }

        private bool draw_shadow_prepass = false;
        private void DrawGBuffer()
        {
            if (!USE_GBUFFER)
            {
                if (draw_shadow_prepass)
                {
                   // AGraphicsLibrary.LightingEngine.LightSettings.ResetShadowPrepass();
                    draw_shadow_prepass = false;
                }
                return;
            }

            GBuffer.Bind();
            GL.Viewport(0, 0, Width, Height);

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (var file in _context.Scene.Objects)
            {
                if (file is GenericRenderer)
                    ((GenericRenderer)file).DrawGBuffer(_context);
            }

            GBuffer.Unbind();

          /*  AGraphicsLibrary.LightingEngine.LightSettings.UpdateShadowPrepass(_context,
                 _context.Scene.ShadowRenderer.GetProjectedShadow(),
                 ((GLTexture2D)GBuffer.Attachments[1]),
                 DepthTexture);*/

            draw_shadow_prepass = true;
        }

        private GLTexture2D bloomPass;

        private void DrawPostScreenBuffer(Framebuffer screen, RenderFrameArgs frameArgs)
        {
            if (bloomPass == null)
            {
                bloomPass = GLTexture2D.CreateUncompressedTexture(1, 1);
            }

            var colorPass = (GLTexture2D)screen.Attachments[0];

            if (_context.EnableBloom)
            {
                var brightnessTex = BloomExtractionTexture.FilterScreen(_context, colorPass);
                BloomProcess.Draw(brightnessTex, BloomEffects, _context, Width, Height);
                bloomPass = (GLTexture2D)BloomEffects.Attachments[0];
            }

            FinalBuffer.Bind();
            DeferredRenderQuad.Draw(_context, colorPass, bloomPass, frameArgs);
        }
    }
}
