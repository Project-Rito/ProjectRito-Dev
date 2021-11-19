﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;

namespace GLFrameworkEngine
{
    public partial class RenderablePath : IDrawable, IEditModeObject, IColorPickable, ITransformableObject
    {
        private List<IRevertable> undoConnectionStack = null;
        private List<IRevertable> undoErasedStack = null;
        private List<IRevertable> undoDrawnStack = null;

        #region Input

        public virtual void OnMouseDown()
        {
            if (MouseEventInfo.LeftButton != OpenTK.Input.ButtonState.Pressed)
                return;

            if (EditToolMode == ToolMode.Connection)
                StartConnecion();
            if (EditToolMode == ToolMode.Drawing)
                StartDrawAction();
            if (EditToolMode == ToolMode.Erase)
                StartEraseAction();
        }

        public virtual void OnMouseUp() 
        {
            if (EditToolMode == ToolMode.Connection)
                EndConnecion(MouseEventInfo.X, MouseEventInfo.Y);
            else if (EditToolMode == ToolMode.Drawing)
                EndDrawAction();
            else if (EditToolMode == ToolMode.Erase)
                EndEraseAction();
        }

        public virtual void OnMouseMove()
        {
            if (!EditMode && !this.IsActive)
                return;

            if (EditToolMode == ToolMode.Connection)
                UpdateConnecion(MouseEventInfo.X, MouseEventInfo.Y);
            else if (EditToolMode == ToolMode.Drawing)
                UpdateDrawAction(MouseEventInfo.X, MouseEventInfo.Y);
            else if (EditToolMode == ToolMode.Erase)
                UpdateEraseAction();
            else if (GLContext.ActiveContext.TransformTools.ActiveMode == TransformEngine.TransformActions.Translate)
                this.ConnectToHoveredPoints();
        }

        public virtual void OnKeyDown()
        {
            if (!EditMode)
                return;

            bool hasSelection = this.GetSelectedPoints().Count > 0;
            bool isActive = this.IsActive;

            if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Scene.Extrude) && (hasSelection || isActive))
                this.ExtrudePointsFromSelected();
            if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Scene.Create) && isActive)
                this.ExtrudePointsFromSelected(true, true);

            if (hasSelection)
            {
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Scene.Fill))
                    this.FillSelectedPoints();
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Scene.Merge))
                    this.MergeSelectedPoints();
                if (KeyEventInfo.IsKeyDown(InputSettings.INPUT.Scene.Insert))
                    this.SubdivideSelectedPoints();
            }
        }

        #endregion

        #region DrawTool

        private void StartDrawAction()
        {
            undoDrawnStack = new List<IRevertable>();
        }

        private void UpdateDrawAction(int x, int y)
        {
            if (undoDrawnStack == null)
                return;


        }

        private void EndDrawAction()
        {
            if (undoDrawnStack == null || undoDrawnStack.Count == 0)
                return;

            //Add the undo stack to the scene for handling undo operations
            if (undoDrawnStack.Count > 0)
                GLContext.ActiveContext.Scene.AddToUndo(undoDrawnStack);
        }

        #endregion

        #region EraseTool

        public void StartEraseAction()
        {
            MouseEventInfo.MouseCursor = MouseEventInfo.Cursor.Eraser;

            //Disable camera/transform handling
            GLContext.ActiveContext.TransformTools.Enabled = false;
            GLContext.ActiveContext.DisableCameraMovement = true;
            GLContext.ActiveContext.PickingTools.Enabled = false;
            //Deselect everything
            GLContext.ActiveContext.Scene.DeselectAll(GLContext.ActiveContext);

            //Start an erase operation with an undoable stack
            undoErasedStack = new List<IRevertable>();
            UpdateEraseAction();
        }

        public void UpdateEraseAction()
        {
            //Make sure an erase operation as been started first before applying one.
            if (undoErasedStack == null)
                return;

            //Erase hovered points
            var hovered = GetHovered();
            if (hovered == null)
                return;

            if (hovered is RenderablePathPoint)
            {
                //Add erased points to the track and remove the points
                undoErasedStack.Add(new RevertableDelPointCollection(new List<RenderablePathPoint>() { (RenderablePathPoint)hovered }));
                this.RemovePoint((RenderablePathPoint)hovered);
            }
            if (hovered is LineObject)
            {
                var line = ((LineObject)hovered);
                undoErasedStack.Add(new RevertableDelPointCollection(new List<RenderablePathPoint>() { line.Point }));

                line.Point.Children.RemoveAt(line.ChildIndex);
            }
        }

        public void EndEraseAction()
        {
            //No erase operation set, skip
            if (undoErasedStack == null)
                return;

            //Add the undo stack to the scene for handling undo operations
            if (undoErasedStack.Count > 0)
                GLContext.ActiveContext.Scene.AddToUndo(undoErasedStack);

            //Reset data and allow transforming and camera movement now operation is complete
            undoErasedStack = null;
            GLContext.ActiveContext.PickingTools.Enabled = true;
            GLContext.ActiveContext.TransformTools.Enabled = true;
            GLContext.ActiveContext.DisableCameraMovement = false;
            MouseEventInfo.MouseCursor = MouseEventInfo.Cursor.Arrow;
        }

        #endregion

        #region ConnectionTool

        public void StartConnecion()
        {
            //No source point to start a connection so skip
            var hovered = GetHoveredPoint();
            if (hovered == null)
                return;

            //Set the point to start a connection to
            this.StartConnecitonPoint = hovered;
            //Disable camera/transform handling
            GLContext.ActiveContext.TransformTools.Enabled = false;
            GLContext.ActiveContext.DisableCameraMovement = true;
            //Set an undo stack for undoing connections
            undoConnectionStack = new List<IRevertable>();
        }

        public void UpdateConnecion(int x, int y)
        {
            //Source is null so return
            if (this.StartConnecitonPoint == null)
                return;

            //Update the display for the end connection

            //hovered points
            var hovered = GetHoveredPoint();
            //Display screen coords if nothing is hovered
            if (hovered == null) {
                this.ConnectionPoint = GLContext.ActiveContext.CoordFor(x, y, 40);
                return;
            }

            //Display the direct point coords being connected if hovered
            this.ConnectionPoint = hovered.Transform.Position;

            //Auto connection.
            if (ConnectAuto && StartConnecitonPoint != hovered) {
                // Connect to the currently hovered point then make that the starting point.
                ConnectPoints(StartConnecitonPoint, hovered);
                StartConnecitonPoint = hovered;
            }
        }

        public void EndConnecion(int x, int y)
        {
            //Source is null so return
            if (this.StartConnecitonPoint == null)
                return;

            var context = GLContext.ActiveContext;

            //End connection with a given hovered dest point to connect from the starting point
            var hovered = context.RayPicker.FindPickableAtPosition(
                 context, this.PathPoints.Where(x => !x.IsSelected).Cast<IRayCastPicking>().ToList(),
                 new Vector2(context.CurrentMousePoint.X, context.Height - context.CurrentMousePoint.Y)) as RenderablePathPoint;
            //Display screen coords if nothing is hovered
            if (hovered != null)
                ConnectPoints(StartConnecitonPoint, hovered);

            //Add the undo stack to the scene for handling undo operations
            if (undoConnectionStack.Count > 0)
                GLContext.ActiveContext.Scene.AddToUndo(undoConnectionStack);

            //Reset data and allow transforming and camera movement now operation is complete
            this.StartConnecitonPoint = null;
            GLContext.ActiveContext.TransformTools.Enabled = true;
            GLContext.ActiveContext.DisableCameraMovement = false;
            undoConnectionStack = null;
        }

        //Coonects 2 points, a parent then a given child
        private void ConnectPoints(RenderablePathPoint point1, RenderablePathPoint point2)
        {
            //Cannot connect the same point to itself so skip.
            if (point1 == point2)
                return;

            //Make sure the point isn't already connected
            if (!point1.Children.Contains(point2)) {
                undoConnectionStack.Add(new RevertableParentChildCollection(point1));
                point1.AddChild(point2);
            }
        }

        #endregion

        #region PointEditing

        /// <summary>
        /// Makes the axis from the first selected point the same value for all other selected points.
        /// </summary>
        public void AlignAxis(int axis)
        {
            var selected = this.GetSelectedPoints();
            if (selected.Count <= 1)
                return;

            //Add to undo
            var transforms = selected.Select(x => x.Transform).ToArray();
            GLContext.ActiveContext.Scene.AddToUndo(new TransformUndo(transforms));

            foreach (var point in selected)
            {
                Vector3 pos = point.Transform.Position;
                //Update the axis value
                pos[axis] = selected[0].Transform.Position[axis];
                //Apply the setter
                point.Transform.Position = pos;
                //Apply transform
                point.Transform.UpdateMatrix(true);
            }
        }

        /// <summary>
        /// Divides a selected point between its selected children adding a mid point between
        /// </summary>
        public void SubdivideSelectedPoints()
        {
            List<RenderablePathPoint> addedPoints = new List<RenderablePathPoint>();
            GLContext.ActiveContext.Scene.BeginUndoCollection();

            var selected = GetSelectedPoints();
            foreach (var point in selected)
            {
                //Subdivide based on the next selected child
                var children = point.Children.Where(x => x.IsSelected).ToList();
                foreach (var child in children)
                {
                    //Blend the transform to get the center region.
                    var outputPos = Vector3.Lerp(point.Transform.Position, child.Transform.Position, 0.5f);
                    var outputScale = Vector3.Lerp(point.Transform.Scale, child.Transform.Scale, 0.5f);
                    var outputRot = Quaternion.Slerp(point.Transform.Rotation, child.Transform.Rotation, 0.5f);

                    //Insert point at the current index 
                    int index = PathPoints.IndexOf(child);
                    var newpoint = CreatePoint(outputPos);
                    AddPoint(newpoint, index);

                    //Revert back parents and children
                    GLContext.ActiveContext.Scene.AddToUndo(new RevertableParentChildCollection(child));

                    newpoint.Transform.Scale = outputScale;
                    newpoint.Transform.Rotation = outputRot;
                    newpoint.Transform.UpdateMatrix(true);

                    point.Children.Remove(child);
                    point.AddChild(newpoint);
                    addedPoints.Add(newpoint);

                    newpoint.AddChild(child);
                }
            }

            if (addedPoints.Count > 0)
            {
                GLContext.ActiveContext.Scene.AddToUndo(new RevertableAddPointCollection(addedPoints));
                foreach (var pt in addedPoints)
                    OnPointAdded(pt);
            }

            GLContext.ActiveContext.Scene.EndUndoCollection();
        }

        /// <summary>
        /// Fills a connection between 2 selected points
        /// </summary>
        public void FillSelectedPoints()
        {
            var selected = GetSelectedPoints();
            //Only fill a connection between 2 selected points.
            if (selected.Count != 2)
                return;

            if (!selected[0].Children.Contains(selected[1])) {
                //Revert back parents and children
                GLContext.ActiveContext.Scene.AddToUndo(new RevertableParentChildCollection(selected[0]));
                selected[0].AddChild(selected[1], true);
            }
        }

        /// <summary>
        /// Merges selected points to the first selected.
        /// </summary>
        public void MergeSelectedPoints()
        {
            var selected = GetSelectedPoints();
            //No selection found so skip.
            if (selected.Count == 0)
                return;

            GLContext.ActiveContext.Scene.BeginUndoCollection();

            var firstPoint = selected.FirstOrDefault();
            foreach (var pt in selected)
            {
                //Connect all selected that isn't the first selected point
                if (pt != firstPoint) {
                    GLContext.ActiveContext.Scene.AddToUndo(new RevertableConnectPointCollection(pt, firstPoint));
                    pt.ConnectToPoint(firstPoint);
                }
            }
            GLContext.ActiveContext.Scene.EndUndoCollection();
        }

        /// <summary>
        /// Connects a point that is currently hovered and selected onto another point.
        /// </summary>
        public void ConnectToHoveredPoints()
        {
            if (!ConnectHoveredPoints || !IsVisible)
                return;

            //For each point disable the current hovered point
            foreach (var point in PathPoints)
                point.IsPointOver = false;

            var selected = GetSelectedPoints();
            //Skip if no selection.
            if (selected.Count == 0)
                return;

            //hovered points
            var hovered = GetHoveredPoint();
            if (hovered == null)
                return;

            hovered.IsPointOver = true;
        }

        public void ExtrudePointsFromSelected(bool connectLast = false, bool connectSelected = false)
        {
            //For rail and bezier types
            if (InterpolationMode == Interpolation.Bezier || AutoConnectByNext)
            {
                ExtrudeBezierPointsFromSelected(connectSelected);
                return;
            }

            //Find the first selected or hovered point
            //Add the new point as the child of the previously selected point

            RenderablePathPoint parent = null;
            if (!connectSelected)
                parent = GetSelectedPoints().LastOrDefault();
            if (parent == null && !connectSelected)
                parent = GetHoveredPoints().LastOrDefault();

            var selected = PathPoints.LastOrDefault(x => x.IsSelected);
            var lastPoint = PathPoints.LastOrDefault();

            var context = GLContext.ActiveContext;

            Vector3 position = EditorUtility.GetObjectPlacementPosition();

            //Add the new point
            var point = CreatePoint(position);
            AddPoint(point);

            //Connect via parent if exists
            if (parent != null)
            {
                parent.AddChild(point);
                point.Transform.Position = parent.Transform.Position;
                point.Transform.Scale = parent.Transform.Scale;
                point.Transform.Rotation = parent.Transform.Rotation;
                point.Transform.UpdateMatrix(true);
            } //Else try extruding from the selected point
            else if (connectSelected && selected != null)
            {
                selected.AddChild(point);
            } //Else try selecting the last point
            else if (connectLast && lastPoint != null)
            {
                lastPoint.AddChild(point);
            }

            PrepareSelectNewPoint(point, parent != null);
        }

        /// <summary>
        /// Adds a single point with no connection. Added object is added to an undo stack.
        /// </summary>
        public RenderablePathPoint AddSinglePoint()
        {
            Vector3 position = EditorUtility.GetObjectPlacementPosition();

            //Add the new point
            var point = CreatePoint(position);
            AddPoint(point);
            PrepareSelectNewPoint(point, false);
            return point;
        }

        public void ExtrudeBezierPointsFromSelected(bool connectSelected = false)
        {
            if (!EditMode)
                return;

            //Bezier types we only want to create new unconnected points when there is none
            RenderablePathPoint parent = null;
            if (!connectSelected)
                parent = GetSelectedPoints().LastOrDefault();

            Vector3 position = EditorUtility.GetObjectPlacementPosition();

            //Add the new point
            var point = CreatePoint(position);

            if (parent != null)
            {
                int index = PathPoints.IndexOf(parent);
                if (index == PathPoints.Count - 1)
                    AddPoint(point);
                else
                    AddPoint(point, index);

                //Use the parents position and handle informatiom
                point.Transform.Position = parent.Transform.Position;
                point.Transform.Scale = parent.Transform.Scale;
                point.Transform.Rotation = parent.Transform.Rotation;
                point.ControlPoint1.Transform.Position = parent.ControlPoint1.Transform.Position;
                point.ControlPoint2.Transform.Position = parent.ControlPoint2.Transform.Position;
                point.UpdateMatrices();
            }
            else
            {
                //Use a default handle length with the 3d screen position
                AddPoint(point);
                point.ControlPoint1.Transform.Position = position + new Vector3(10, 0, 0);
                point.ControlPoint2.Transform.Position = position + new Vector3(-10, 0, 0);
                point.UpdateMatrices();
            }

            PrepareSelectNewPoint(point, parent != null);
        }

        private void PrepareSelectNewPoint(RenderablePathPoint point, bool moveSelected)
        {
            var context = GLContext.ActiveContext;

            //Only select the new point
            DeselectAll();
            point.IsSelected = true;
            point.IsHovered = true;

            context.Scene.OnSelectionChanged(context);

            //Only make extruded points start a translation action
            //New points stay in place
            if (moveSelected)
            {
                context.TransformTools.StartAction(context, TransformEngine.TransformActions.Translate);
                //Apply the undo operation while movement is occuring instead so it is one single undo operation
                context.TransformTools.UndoChanged += delegate
                {
                    context.Scene.AddToUndo(new RevertableAddPointCollection(point));

                    //Remove the event when finished
                    context.TransformTools.UndoChanged = null;
                };
            }
            else
                context.Scene.AddToUndo(new RevertableAddPointCollection(point));

            OnPointAdded(point);
        }

        #endregion

        private object GetHovered()
        {
            var context = GLContext.ActiveContext;
            var pickable = this.PathPoints.Where(x => !x.IsSelected).Cast<ITransformableObject>().ToList();
            pickable.Add(this);

            //hovered points
            return context.Scene.FindPickableAtPosition(
                 context, pickable,
                 new Vector2(context.CurrentMousePoint.X, context.Height - context.CurrentMousePoint.Y));
        }

        private RenderablePathPoint GetHoveredPoint()
        {
            var context = GLContext.ActiveContext;
            //hovered points
            return context.RayPicker.FindPickableAtPosition(
                 context, this.PathPoints.Where(x => !x.IsSelected).Cast<IRayCastPicking>().ToList(),
                 new Vector2(context.CurrentMousePoint.X, context.Height - context.CurrentMousePoint.Y)) as RenderablePathPoint;
        }

    }
}
