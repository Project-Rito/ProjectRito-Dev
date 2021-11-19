﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class TransformEngine
    {
        public bool Enabled = true;

        /// <summary>
        /// The active mode when an active action has been set.
        /// </summary>
        public TransformActions ActiveMode = TransformActions.Translate;

        /// <summary>
        /// The active transform action.
        /// </summary>
        public List<ITransformAction> ActiveActions = new List<ITransformAction>();

        /// <summary>
        /// The transform settings of the engine.
        /// </summary>
        public TransformSettings TransformSettings = new TransformSettings();

        /// <summary>
        /// An event for when the transform has been changed.
        /// </summary>
        public EventHandler TransformChanged;

        /// <summary>
        /// An event called before the undo operation is executed.
        /// Add any additional undo operations in this to be undone in a collection at once.
        /// </summary>
        public EventHandler UndoChanged;

        /// <summary>
        /// The inital gizmo size before being scaled by the camera distance.
        /// </summary>
        public float GizmoSize = 0.05f;

        /// <summary>
        /// Skips the selection action when the mouse has been clicked during a shortcut action.
        /// </summary>
        public bool ReleaseTransform = false;

        /// <summary>
        /// Determines if a transform is currently active.
        /// </summary>
        public bool IsActive { get; private set; } = false;

        /// <summary>
        /// Gets the text input from typing the transform value.
        /// </summary>
        public string GetTextInput() => textInput;

        //Gizmo rendering
        private TranslateScaleGizmo TranslateRenderer = new TranslateScaleGizmo();
        private TranslateScaleGizmo ScaleRenderer = new TranslateScaleGizmo() { IsScale = true };
        private RotateGizmo RotateRenderer = new RotateGizmo();
        private RectangleScaleGizmo RectangleRenderer = new RectangleScaleGizmo();

        //The target transforms to alter during transformation actions
        public List<GLTransform> ActiveTransforms = new List<GLTransform>();
        private List<GLTransform> PreviousTransforms = new List<GLTransform>();

        //The axis hovered over during mouse move
        private Axis HoveredAxis = Axis.None;
        //List of objects that were selected for the transform action to be selected again and transformed if active.
        private List<ITransformableObject> objects;

        //Line used to display transform changes
        private LineRender LineTransform = new LineRender();
        //Check if the transform has been changed to start an undo operation
        private bool _transformChanged = false;
        ///Check if the transform is being dragged (activated by a shortcut key)
        private bool _draggedTransform = false;

        //The bounding region of the selected objects
        public BoundingBox BoundingBox;

        /// <summary>
        /// Inits an action so one can transform selected objects.
        /// </summary>
        public void InitAction(List<ITransformableObject> objects)
        {
            this.objects = objects;

            ActiveTransforms.Clear();
            foreach (var ob in objects)
            {
                if (ob is IEditModeObject && ((IEditModeObject)ob).EditMode)
                    continue;

                ActiveTransforms.Add(ob.Transform);
            }

            UpdateBoundingBox();
            UpdateTransformMode(ActiveMode);
        }

        public void UpdateBoundingBox()
        {
            BoundingBox = CalculateBoundingBox();
        }

        public BoundingBox CalculateBoundingBox()
        {
            var origin = CalculateGizmoOrigin();

            BoundingBox bounding = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
            foreach (var ob in objects)
            {
                //Note it probably would be more ideal to store bounding boxes through specific types.
                //Though I'd like to rework the system so there isn't so many needed interfaces.
                if (ob is EditableObject)
                { 
                    var bnd = ((EditableObject)ob).BoundingNode;
                    if (bnd == null)
                        continue;

                    //Make sure the box is in local space
                    var newBox = BoundingBox.FromVertices(bnd.Box.GetLocalSpaceVertices());
                    //Only use scale for the transform
                    var mat = ob.Transform.TransformMatrix.ClearRotation();
                    mat *= Matrix4.CreateTranslation(origin).Inverted();
                    newBox.ApplyTransform(mat);
                    bounding.Include(newBox);
                }
                if (ob is RenderablePathPoint)
                {
                    var point = ob as RenderablePathPoint;
                    if (point.BoundingBox == null)
                        continue;

                    var newBox = BoundingBox.FromVertices(point.BoundingBox.GetLocalSpaceVertices());
                    var mat = ob.Transform.TransformMatrix.ClearRotation();
                    mat *= Matrix4.CreateTranslation(origin).Inverted();
                    newBox.ApplyTransform(mat);
                    bounding.Include(newBox);
                }
            }
            if (bounding.Min != new Vector3(float.MaxValue))
                return bounding;

            return new BoundingBox(new Vector3(-1), new Vector3(1));
        }

        /// <summary>
        /// Starts a transformation for dragging the action.
        /// This will revert back to the previous action after applied.
        /// </summary>
        public void DragTransformAction(GLContext context, TransformActions action)
        {
            ActionOnMouseUp = this.ActiveMode;
            _draggedTransform = true;

            UpdateTransformMode(action);
            StartAction(context, action);
        }

        /// <summary>
        /// Starts an active transform action using all axis direction. 
        /// </summary>
        public int StartAction(GLContext context, TransformActions action)
        {
            if (ActiveActions.Count == 0 || action != this.ActiveMode)
                return 0;

            //Start a transform directly. This one will use no axis restrictions
            //This can be executed through the use of shortcuts
            TransformSettings.ActiveAxis = Axis.All;
            _transformChanged = false;
            IsActive = true;

            //ReleaseTransform = true;
            //Set the active action back to defaults before starting
            foreach (var activeAction in ActiveActions)
                activeAction.ResetTransform(context, TransformSettings);
            return 1;
        }

        private TransformActions ActionOnMouseUp = TransformActions.Translate;

        public int OnMouseDown(GLContext context, bool onSelection = false)
        {
            //Don't apply during active transforming
            if (TransformSettings.ActiveAxis != Axis.None)
                return 0;

            //Ignore right clicking to move camera
            if (MouseEventInfo.RightButton == OpenTK.Input.ButtonState.Pressed) {
                return 0;
            }

            //Start transform on mouse down
            _transformChanged = false;
            textInput = "";
            TransformSettings.HasTextInput = false;

            //Middle mouse can create a scale action
            if (MouseEventInfo.MiddleButton == OpenTK.Input.ButtonState.Pressed)
            {
                DragTransformAction(context, TransformActions.Scale);
                return 1;
            }

            //During a direct selection of an object, make sure to move in all axis
            //This allows to drag objects around once they are selected..
            if (onSelection)
                TransformSettings.ActiveAxis = Axis.All;
            else
            {
                //Select the axis by left clicking.
                if (TransformSettings.DisplayGizmo && MouseEventInfo.LeftButton == OpenTK.Input.ButtonState.Pressed)
                    TransformSettings.ActiveAxis = GetSelectedAxis(context);

                //If no axis is selected, try picking the place again
                //If a gizmo is displaying, make sure this is not activated on selection down to keep the gizmo displaying on click
                if (TransformSettings.ActiveAxis == Axis.None)
                {
                    //Check if any of the active objects can be picked for movement
                    var renders = context.Scene.GetSelectableObjects();
                    var picked = context.Scene.FindPickableAtPosition(context, renders, new Vector2(MouseEventInfo.X, context.Height - MouseEventInfo.Y));
                    if (picked != null && ActiveTransforms.Contains(picked.Transform))
                    {
                        TransformSettings.ActiveAxis = Axis.All;
                    }
                }
            }

            //Force update the mouse positon incase the mouse was off screen
            context.CurrentMousePoint = new Vector2(MouseEventInfo.X, MouseEventInfo.Y);
            foreach (var action in ActiveActions)
                action.ResetTransform(context, TransformSettings);
            return TransformSettings.ActiveAxis != Axis.None ? 1 : 0;
        }

        private Axis GetSelectedAxis(GLContext context)
        {
            Quaternion rotation = Quaternion.Identity;
            //Rotate the gizmo from the last selected transform
            if (TransformSettings.TransformMode == TransformSettings.TransformSpace.Local) {
                rotation = this.ActiveTransforms.LastOrDefault().Rotation;
            }

            //Check for axis picking
            switch (ActiveMode)
            {
                case TransformActions.Translate:
                    return TranslateRenderer.UpdateAxisSelection(context,
                        TransformSettings.Origin, rotation,
                    new Vector2(MouseEventInfo.X, MouseEventInfo.Y),
                    TransformSettings);
                case TransformActions.Scale:
                    return ScaleRenderer.UpdateAxisSelection(context,
                        TransformSettings.Origin, rotation,
                    new Vector2(MouseEventInfo.X, MouseEventInfo.Y),
                    TransformSettings);
                case TransformActions.Rotate:
                    return RotateRenderer.UpdateAxisSelection(context,
                        TransformSettings.Origin, rotation,
                    new Vector2(MouseEventInfo.X, MouseEventInfo.Y),
                    TransformSettings);
                case TransformActions.RectangleScale:
                    return RectangleRenderer.UpdateAxisSelection(context,
                        TransformSettings.Origin, rotation,
                    new Vector2(MouseEventInfo.X, MouseEventInfo.Y),
                    TransformSettings);
            }

            return Axis.None;
        }

        public int OnMouseMove(GLContext context)
        {
            if (TransformSettings.ActiveAxis == Axis.None) {
                HoveredAxis = GetSelectedAxis(context);

                //No axis selected so return
                return 0;
            }

            //Action is occuring
            foreach (var action in ActiveActions)
            {
                int value = action.TransformChanged(context, MouseEventInfo.X, MouseEventInfo.Y, TransformSettings);
                if (value == 1 && !_transformChanged)
                {
                    GLContext.ActiveContext.Scene.BeginUndoCollection();

                    foreach (var transform in ActiveTransforms)
                        transform.TransformStarted?.Invoke(this, EventArgs.Empty);

                    UndoChanged?.Invoke(this, EventArgs.Empty);

                    //Configure the settings

                    //Only drop to collision when the transform is moving on all axis.
                    TransformSettings.CollisionDetect = false;
                    if (TransformSettings.ActiveAxis == Axis.All)
                        TransformSettings.CollisionDetect = context.EnableDropToCollision;

                    IsActive = true;
                    _transformChanged = true;
                    //Store the current previous values
                    ReloadPreviousTransforms();
                    //Transform changed once, create an undo operation
                    UpdateUndoHandler(context.Scene);

                    GLContext.ActiveContext.Scene.EndUndoCollection();
                }
                if (value == 1)
                {
                    //Apply the transformation for viewing
                    action.ApplyTransform(PreviousTransforms, ActiveTransforms);
                    UpdateBoundingBox();
                    TransformChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            return 1;
        }

        /// <summary>
        /// Applies the current transformation for the active action.
        /// </summary>
        public void ApplyTransform()
        {
            foreach (var action in ActiveActions)
            {
                action.ApplyTransform(PreviousTransforms, ActiveTransforms);
                UpdateBoundingBox();
                TransformChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        //Create a new undo in the scene
        private void UpdateUndoHandler(GLScene scene)
        {
            TransformInfo[] infos = new TransformInfo[ActiveTransforms.Count];
            for (int i = 0; i < ActiveTransforms.Count; i++)
            {
                infos[i] = new TransformInfo(ActiveTransforms[i]);
            }
            scene.AddToUndo(new TransformUndo(infos));
        }

        public int OnMouseUp(GLContext context)
        {
            if (ActiveActions.Count == 0)
                return 0;

            foreach (var transform in ActiveTransforms)
                transform.TransformActionApplied?.Invoke(this, EventArgs.Empty);

            TransformSettings.ActiveAxis = Axis.None;

            if (_draggedTransform)
            {
                UpdateTransformMode(ActionOnMouseUp);
                _draggedTransform = false;
            }
            TransformSettings.TextInput = 0.0f;
            TransformSettings.HasTextInput = false;
            textInput = "";

            TransformSettings.Origin = CalculateGizmoOrigin();

            _transformChanged = false;
            IsActive = false;

            bool finished = false;
            foreach (var action in ActiveActions)
                finished |= action.FinishTransform() == 1;

            return finished ? 1 : 0;
        }

        /// <summary>
        /// Updates the current axis to be used during a transformation.
        /// </summary>
        public void UpdateAxis(GLContext context, Axis axis)
        {
            //Make sure the action is actively being used (axis is set to none during no movements)
            if (ActiveActions.Count > 0 && TransformSettings.ActiveAxis != Axis.None) {
                TransformSettings.ActiveAxis = axis;

                //Reset the transform back before performing an axis change
                if (PreviousTransforms.Count > 0)
                {
                    for (int i = 0; i < PreviousTransforms.Count; i++)
                    {
                        ActiveTransforms[i].Origin = PreviousTransforms[i].Position;
                        ActiveTransforms[i].Position = PreviousTransforms[i].Position;
                        ActiveTransforms[i].Scale = PreviousTransforms[i].Scale;
                        ActiveTransforms[i].Rotation = PreviousTransforms[i].Rotation;
                        ActiveTransforms[i].UpdateMatrix(_transformChanged);
                    }
                }

                TransformSettings.Origin = CalculateGizmoOrigin();
                foreach (var action in ActiveActions)
                    action.ResetTransform(context, TransformSettings);
            }
        }

        /// <summary>
        /// Updates the current transformation mode for transforming selected objects.
        /// </summary>
        /// <param name="action"></param>
        public void UpdateTransformMode(TransformActions action)
        {
            ActiveMode = action;

            Vector3 center = CalculateGizmoOrigin();
            TransformSettings.Origin = center;

            ActiveActions.Clear();
            switch (action)
            {
                case TransformActions.Translate: ActiveActions.Add(new TranslateAction(TransformSettings)); break;
                case TransformActions.Scale: ActiveActions.Add(new ScaleAction(TransformSettings)); break;
                case TransformActions.Rotate: ActiveActions.Add(new RotateAction(TransformSettings)); break;
                case TransformActions.RectangleScale: ActiveActions.Add(new RectangleAction(TransformSettings)); break;
            }
            GLContext.ActiveContext.UpdateViewport = true;
        }

        /// <summary>
        /// Updates the current origin used for the gizmo tool.
        /// This affects scaling and rotation from origin too.
        /// </summary>
        public void UpdateOrigin() {
            TransformSettings.Origin = CalculateGizmoOrigin();
        }

        private Vector3 CalculateGizmoOrigin()
        {
            //Calculate center between object selection for origin
            //The origin will determine gizmo placement and rotation pivot
            List<Vector3> points = new List<Vector3>();
            foreach (var ob in ActiveTransforms)
                points.Add(ob.Origin);

            BoundingBox.CalculateMinMax(points.ToArray(), out Vector3 min, out Vector3 max);
            return (min + max) * 0.5f;
        }

        public void OnKeyDown(GLContext context)
        {
            bool multiAxis = KeyEventInfo.KeyCtrl;

            //Axis set
            if (multiAxis)
            {
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.AxisX)) UpdateAxis(context, Axis.YZ);
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.AxisY)) UpdateAxis(context, Axis.XZ);
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.AxisZ)) UpdateAxis(context, Axis.XY);
            }
            else
            {
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.AxisX)) UpdateAxis(context, Axis.X);
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.AxisY)) UpdateAxis(context, Axis.Y);
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.AxisZ)) UpdateAxis(context, Axis.Z);
            }

            if (!KeyEventInfo.KeyCtrl)
            {
                //SRT action set
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.TranslateGizmo)) UpdateTransformMode(TransformActions.Translate);
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.RotateGizmo)) UpdateTransformMode(TransformActions.Rotate);
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.ScaleGizmo)) UpdateTransformMode(TransformActions.Scale);

                //SRT action starting
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.Scale)) DragTransformAction(context, TransformActions.Scale);
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.Rotate)) DragTransformAction(context, TransformActions.Rotate);
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Transform.Translate)) DragTransformAction(context, TransformActions.Translate);
            }

            if (ActiveActions.Count > 0 && !KeyEventInfo.KeyCtrl)
            {
                TextInput();
                if (!string.IsNullOrEmpty(textInput)) {
                    bool valid = float.TryParse(textInput, out TransformSettings.TextInput);
                    TransformSettings.HasTextInput = valid;
                }
                else
                {
                    TransformSettings.HasTextInput = false;
                    TransformSettings.TextInput = 0.0f;
                }
            }
        }

        private string textInput = "";

        //Allow numbers to be directly input into the transform
        private void TextInput()
        {
            for (char i = '0'; i <= '9'; i++) {
                if (KeyEventInfo.IsKeyDown(i.ToString())) {
                    textInput += i.ToString();
                }
            }
            if (KeyEventInfo.IsKeyDown($"backspace") && textInput.Length > 0)
                textInput = textInput.Remove(textInput.Length - 1);
            if (KeyEventInfo.IsKeyDown($"period"))
                textInput += ".";
        }

        private LineRender LineNormal;

        public void DrawSelection(GLContext context)
        {
           /* if (LineNormal == null)
                LineNormal = new LineRender();

            var mat = new StandardMaterial();
            mat.ModelMatrix = Matrix4.CreateTranslation(this.TransformSettings.Origin);
            mat.Render(context);

            GL.LineWidth(10);
            LineNormal.Draw(new Vector3(), TransformSettings.PlaneNormal * 300, Vector4.One, true);
            GL.LineWidth(1);*/

            if (ActiveActions.Count == 0) {
                InitAction(context.Scene.GetSelected());
            }

            //Scale from camera position
            TransformSettings.GizmoScale = context.Camera.ScaleByCameraDistance(TransformSettings.Origin, GizmoSize);

            Quaternion rotation = Quaternion.Identity;
            //Rotate the gizmo from the last selected transform
            if (TransformSettings.TransformMode == TransformSettings.TransformSpace.Local) { 
                rotation = this.ActiveTransforms.LastOrDefault().Rotation;
            }
            TransformSettings.Rotation = rotation;

            if (TransformSettings.ActiveAxis != Axis.None)
                DrawLine(context);

            if (TransformSettings.ActiveAxis == Axis.All)
                return;

            //Draw either a gizmo object or axis lines depending on the current 
            bool isMoving = TransformSettings.ActiveAxis != Axis.None;
            if (!TransformSettings.DisplayGizmo)
                isMoving = true;

            //Selected axis when an axis has been picked
            bool[] selectedAxis = new bool[6];
            bool[] hoveredAxis = new bool[7];

            if (TransformSettings.ActiveAxis.HasFlag(Axis.X)) selectedAxis[0] = true;
            if (TransformSettings.ActiveAxis.HasFlag(Axis.Y)) selectedAxis[1] = true;
            if (TransformSettings.ActiveAxis.HasFlag(Axis.Z)) selectedAxis[2] = true;

            if (HoveredAxis.HasFlag(Axis.X)) hoveredAxis[0] = true;
            if (HoveredAxis.HasFlag(Axis.Y)) hoveredAxis[1] = true;
            if (HoveredAxis.HasFlag(Axis.Z)) hoveredAxis[2] = true;

            foreach (var action in ActiveActions)
            {
                if (action is RectangleAction)
                {
                    if (TransformSettings.ActiveAxis.HasFlag(Axis.XN)) selectedAxis[3] = true;
                    if (TransformSettings.ActiveAxis.HasFlag(Axis.YN)) selectedAxis[4] = true;
                    if (TransformSettings.ActiveAxis.HasFlag(Axis.ZN)) selectedAxis[5] = true;
                    if (HoveredAxis.HasFlag(Axis.XN)) hoveredAxis[3] = true;
                    if (HoveredAxis.HasFlag(Axis.YN)) hoveredAxis[4] = true;
                    if (HoveredAxis.HasFlag(Axis.ZN)) hoveredAxis[5] = true;
                }
                else
                {
                    if (HoveredAxis.HasFlag(Axis.XY)) hoveredAxis[3] = true;
                    if (HoveredAxis.HasFlag(Axis.YZ)) hoveredAxis[4] = true;
                    if (HoveredAxis.HasFlag(Axis.XZ)) hoveredAxis[5] = true;
                    if (HoveredAxis.HasFlag(Axis.All)) hoveredAxis[6] = true;
                }

                //Draw the gizmo depending on the current action
                if (action is TranslateAction)
                    TranslateRenderer.Render(context, TransformSettings.Origin, rotation, TransformSettings.GizmoScale, isMoving, selectedAxis, hoveredAxis);
                if (action is ScaleAction)
                    ScaleRenderer.Render(context, TransformSettings.Origin, rotation, TransformSettings.GizmoScale, isMoving, selectedAxis, hoveredAxis);
                if (action is RotateAction)
                    RotateRenderer.Render(context, TransformSettings.Origin, rotation, TransformSettings.GizmoScale, isMoving, selectedAxis, hoveredAxis);
                if (action is RectangleAction)
                    RectangleRenderer.Render(context, BoundingBox, TransformSettings.Origin, rotation, TransformSettings.GizmoScale, isMoving, selectedAxis, hoveredAxis);
            }
        }

        //Draws a line from the origin to the changed transformation point
        private void DrawLine(GLContext context)
        {
            if (this.ActiveMode != TransformActions.Scale || this.ActiveMode != TransformActions.Rotate)
                return;

            var shader = GlobalShaders.GetShader("GIZMO");
            context.CurrentShader = shader;
            context.CurrentShader.SetVector4("color", new Vector4(1, 1, 1, 1));

            var mdlMtx = Matrix4.Identity;
            context.CurrentShader.SetMatrix4x4(GLConstants.ViewProjMatrix, ref mdlMtx);

            GL.Disable(EnableCap.DepthTest);

            var start = context.ScreenCoordFor(TransformSettings.Origin);
            var end = new Vector2(context.CurrentMousePoint.X, context.CurrentMousePoint.Y);

            foreach (var action in ActiveActions)
            {
                if (action is TranslateAction)
                    start = context.ScreenCoordFor(((TranslateAction)action).OriginStart);
            }

            start = context.NormalizeMouseCoords(start);
            end = context.NormalizeMouseCoords(end);

            DrawDashedOutline(context, mdlMtx, start, end, LineTransform);

            GL.Enable(EnableCap.DepthTest);
        }

        static void DrawDashedOutline(GLContext context, Matrix4 mdlMtx, Vector2 start, Vector2 end, LineRender render)
        {
            var dashMaterial = new DashMaterial();
            dashMaterial.Color = new Vector4(1);
            dashMaterial.MatrixCamera = Matrix4.Identity;
            dashMaterial.ModelMatrix = mdlMtx;
            dashMaterial.Render(context);

            GL.Enable(EnableCap.AlphaTest);
            GL.AlphaFunc(AlphaFunction.Gequal, 0.5F);

            GL.LineWidth(1.0f);
            render.Draw(start, end, new Vector4(1), true);

            GL.Disable(EnableCap.AlphaTest);
        }

        private void ReloadPreviousTransforms()
        {
            //Store the previous transformation data to apply during transformation viewing
            PreviousTransforms.Clear();
            foreach (var ob in ActiveTransforms)
                PreviousTransforms.Add(new GLTransform()
                {
                    Origin = ob.Origin,
                    Position = ob.Position,
                    Scale = ob.Scale,
                    Rotation = ob.Rotation,
                });
        }

        [Flags]
        public enum TransformActions
        {
            Translate = 1,
            Scale = 2,
            Rotate = 4,
            RectangleScale = 8,
        }

        [Flags]
        public enum Axis
        {
            None = 0,
            X = 1,
            Y = 2,
            Z = 4,
            All = 8,
            //For rectangle tool, opposing faces
            XN = 0x0040,
            YN = 0x1000,
            ZN = 0x8000,
            //Multi selection axis
            XY = X | Y,
            YZ = Y | Z,
            XZ = X | Z,
        }
    }
}
