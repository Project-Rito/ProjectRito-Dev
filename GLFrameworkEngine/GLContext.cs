using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using OpenTK.Input;

namespace GLFrameworkEngine
{
    public class GLContext
    {
        //Todo maybe relocate. This is to quickly access the current context (which is only one atm)
        public static GLContext ActiveContext = null;

        /// <summary>
        /// Default rendering arguments when a rendered frame is being drawn.
        /// </summary>
        public RenderFrameArgs FrameArgs = new RenderFrameArgs();

        /// <summary>
        /// The screen buffer storing the current color/depth render texture of the drawn scene objects.
        /// </summary>
        public Framebuffer ScreenBuffer { get; set; }

        /// <summary>
        /// Selection tools used for selecting scene objects in different ways.
        /// </summary>
        public SelectionToolEngine SelectionTools = new SelectionToolEngine();

        /// <summary>
        /// Transform tools for transforming scene objects.
        /// </summary>
        public TransformEngine TransformTools = new TransformEngine();

        /// <summary>
        /// Picking toolsets for picking objects.
        /// </summary>
        public PickingTool PickingTools = new PickingTool();

        /// <summary>
        /// A toolset for linking IObjectLink objects to a transformable object.
        /// </summary>
        public LinkingTool LinkingTools = new LinkingTool();

        /// <summary>
        /// Color picking for picking scene objects using a color ID pass.
        /// </summary>
        public ColorPicker ColorPicker = new ColorPicker();

        /// <summary>
        /// Ray picking for picking scene objects using bounding radius/boxes.
        /// </summary>
        public RayPicking RayPicker = new RayPicking();

        /// <summary>
        /// Collision ray picking for dropping scene objects to collision.
        /// </summary>
        public CollisionRayCaster CollisionCaster = new CollisionRayCaster();

        /// <summary>
        /// The camera instance to use in the scene.
        /// </summary>
        public Camera Camera { get; set; }

        /// <summary>
        /// The scene information containing the list of drawables along with selection handling.
        /// </summary>
        public GLScene Scene = new GLScene();

        /// <summary>
        /// The width of the current context. Should be given the width height.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the current context. Should be given the viewport height.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// If the current context is in focus or not.
        /// </summary>
        public bool Focused { get; set; } = true;

        /// <summary>
        /// Gets or sets the mouse position after a mouse down event.
        /// </summary>
        public Vector2 MouseOrigin { get; set; }

        /// <summary>
        /// Gets or sets the current mouse postion.
        /// </summary>
        public Vector2 CurrentMousePoint = Vector2.Zero;

        /// <summary>
        /// Gets or sets the offset from the mouse origin.
        /// </summary>
        public Vector2 MouseOffset => CurrentMousePoint - MouseOrigin;

        /// <summary>
        /// Determines to enable SRGB or not for the current context.
        /// </summary>
        public bool UseSRBFrameBuffer;

        /// <summary>
        /// Toggles bloom usage.
        /// </summary>
        public bool EnableBloom;

        /// <summary>
        /// Toggles fog usage.
        /// </summary>
        public bool EnableFog = true;

        /// <summary>
        /// Toggles dropping objects to collision of the current CollisionCaster or not
        /// during a translation transform.
        /// </summary>
        public bool EnableDropToCollision = true;

        /// <summary>
        /// The preview scale to scale up model displaying.
        /// </summary>
        public static float PreviewScale
        {
            get { return ActiveContext._previewScale; }
            set { ActiveContext._previewScale = value; }
        }

        private float _previewScale = 1.0f;

        /// <summary>
        /// An offset used to avoid z-fighting in cases of overlapping.
        /// Used in editor features like gizmos.
        /// </summary>
        public float ZOffset = 0.01f;

        public bool UpdateViewport = false;

        public bool DisableCameraMovement = false;

        public GLContext() { }

        public void SetActive() {
            GLContext.ActiveContext = this;
        }

        /// <summary>
        /// Gets or sets the 3D Cursor position used for placing objects and custom origin points.
        /// </summary>
        public Vector3 Cursor3DPosition
        {
            get
            {
                if (Scene.Cursor3D == null)
                    return Vector3.Zero;

              return Scene.Cursor3D.Transform.Position;
            }
            set
            {
                if (Scene.Cursor3D != null) {
                    Scene.Cursor3D.Transform.Position = value;
                    Scene.Cursor3D.Transform.UpdateMatrix(true);
                }
            }
        }

        /// <summary>
        /// Gets the camera ray of the current mouse point on screen/
        /// <returns></returns>
        public CameraRay PointScreenRay() => CameraRay.PointScreenRay((int)CurrentMousePoint.X, (int)CurrentMousePoint.Y, Camera);

        /// <summary>
        /// Gets the camera ray of the given x y screen scoordinates.
        /// <returns></returns>
        public CameraRay PointScreenRay(int x, int y) => CameraRay.PointScreenRay(x, y, Camera);

        /// <summary>
        /// Gets the x y screen coordinates from a 3D position.
        /// </summary>
        public Vector2 WorldToScreen(Vector3 coord)
        {
            Vector3 vec = Vector3.Project(coord, 0, 0, Width, Height, -1, 1, Camera.ViewMatrix * Camera.ProjectionMatrix);
            return new Vector2((int)vec.X, Height - (int)(vec.Y));
        }

        /// <summary>
        /// Gets a 3D position of the current mouse coordinates given the depth.
        /// </summary>
        public Vector3 GetPointUnderMouse(float depth) {
            return ScreenToWorld(this.CurrentMousePoint.X, this.CurrentMousePoint.Y, depth);
        }

        /// <summary>
        /// Gets a 3D position given the screen x y coordinates and depth.
        /// </summary>
        public Vector3 ScreenToWorld(float x, float y, float depth)
        {
            var ray = PointScreenRay((int)x, (int)y);
            return ray.Origin.Xyz + (ray.Direction * depth);
        }

        /// <summary>
        /// Converts the given mouse coordinates to normalized coordinate space to use in the GL coordinate system -1 1.
        /// </summary>
        public Vector2 NormalizeMouseCoords(Vector2 mousePos)
        {
            return new Vector2(
                 2.0f * mousePos.X / Width - 1,
                 -(2.0f * mousePos.Y / Height - 1));
        }

        /// <summary>
        /// Checks if the given shader is active in the context.
        /// </summary>
        public bool IsShaderActive(ShaderProgram shader) {
            return shader != null && shader.program == CurrentShader.program;
        }

        private ShaderProgram shader;

        /// <summary>
        /// The current shader in the context to be drawn.
        /// If null, will disable drawing the shader.
        /// </summary>
        public ShaderProgram CurrentShader
        {
            get { return shader; }
            set
            {
                if(value == null)
                {
                    GL.UseProgram(0);
                    return;
                }

                //Toggle shader if not active
                if (value != shader) {
                    shader = value;
                }
                shader.Enable();

                //Update standard camera matrices
                var mtxMdl = Camera.ModelMatrix;
                var mtxCam = Camera.ViewProjectionMatrix;
                shader.SetMatrix4x4(GLConstants.ModelMatrix, ref mtxMdl);
                shader.SetMatrix4x4("mtxCam", ref mtxCam);
            }
        }

        private bool _firstClick = true;
        private Vector2 _refPos;

        public void OnMouseMove( bool mouseDown, float frameTime)
        {
            UpdateViewport = true;

            //Set a saved mouse point to use in the application
            CurrentMousePoint = new Vector2(MouseEventInfo.X, MouseEventInfo.Y);

            SelectionTools.OnMouseMove(this);
            Scene.OnMouseMove(this);
            LinkingTools.OnMouseMove(this);

            int transformState = 0;
            //Transforming can occur on shortcut keys rather than just mouse down
            if (TransformTools.ActiveActions.Count > 0)
                transformState = TransformTools.OnMouseMove(this);

            if (mouseDown && MouseEventInfo.RightButton == ButtonState.Pressed)
            {
                if (_firstClick)
                    _refPos = new OpenTK.Vector2(MouseEventInfo.FullPosition.X, MouseEventInfo.FullPosition.Y);
                _firstClick = false;

                MouseEventInfo.MouseCursor = MouseEventInfo.Cursor.None;

                if (transformState != 0 || SelectionTools.IsActive || LinkingTools.IsActive || DisableCameraMovement)
                    return;

                Camera.Controller.MouseMove(_refPos, frameTime);
                MouseEventInfo.FullPosition = new System.Drawing.Point((int)_refPos.X, (int)_refPos.Y);

                ApplyMouseState();
            }
            else
            {
                _firstClick = true;
                MouseEventInfo.MouseCursor = MouseEventInfo.Cursor.Default;
            }

            PickingTools.OnMouseMove(this);
        }

        private void ApplyMouseState()
        {
            Mouse.SetPosition(MouseEventInfo.FullPosition.X, MouseEventInfo.FullPosition.Y);
        }

        private float previousMouseWheel;

        public void ResetPrevious() {
            previousMouseWheel = 0;
        }

        public void OnMouseWheel(float frameTime)
        {
            UpdateViewport = true;

            if (previousMouseWheel == 0)
                previousMouseWheel = MouseEventInfo.WheelPrecise;

            MouseEventInfo.Delta = MouseEventInfo.WheelPrecise - previousMouseWheel;

            if (SelectionTools.IsActive) {
                SelectionTools.OnMouseWheel(this);
            }
            else
            {
                Camera.Controller.MouseWheel(frameTime);
            }
            previousMouseWheel = MouseEventInfo.WheelPrecise;
        }

        public void OnMouseUp()
        {
            UpdateViewport = true;

            TransformTools.OnMouseUp(this);

            SelectionTools.OnMouseUp(this);
            Scene.OnMouseUp(this);
            PickingTools.OnMouseUp(this);
            LinkingTools.OnMouseUp(this);
        }

        public void OnKeyDown(KeyEventInfo e, bool isRepeat)
        {
            SelectionTools.OnKeyDown(this);
            TransformTools.OnKeyDown(this);
            Scene.OnKeyDown(e, this);

            if (!isRepeat)
                Camera.KeyPress();
        }

        public void OnMouseDown(float frameTime)
        {
            UpdateViewport = true;

            MouseOrigin = new Vector2(MouseEventInfo.X, MouseEventInfo.Y);
            Scene.OnMouseDown(this);

            SelectionTools.OnMouseDown(this);
            LinkingTools.OnMouseDown(this);

            if (!TransformTools.Enabled || SelectionTools.IsActive || LinkingTools.IsActive) //Skip picking, transforming and camera events for selection tools
                return;

            if (TransformTools.ActiveActions.Count > 0 && Scene.GetSelected().Count > 0)
            {
                var state = TransformTools.OnMouseDown(this);
                if (state != 0) //Skip picking and camera events for transforming objects
                    return;
            }
            //Transform is in a moving state through shortcut keys
            //Don't apply deselection from picking during the mouse down when it applies
            if (TransformTools.ReleaseTransform) {
                TransformTools.ReleaseTransform = false;
                return;
            }

            PickingTools.OnMouseDown(this);

            Camera.Controller.MouseClick(frameTime);
        }
    }
}
