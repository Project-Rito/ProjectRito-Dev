using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace GLFrameworkEngine
{
    public class SelectionToolEngine
    {
        public bool IsActive => SelectionBox != null || SelectionCircle != null;

        public bool IsSelectionMode = false;

        SelectionBox SelectionBox = null;
        SelectionCircle SelectionCircle = null;

        public void OnKeyDown(KeyEventInfo e, GLContext context)
        {
            if (IsActive)
                return;

            //Shortcuts for selection tools
            if (!e.KeyCtrl && e.IsKeyDown(InputSettings.INPUT.Scene.SelectionBox))
                SelectionBox = new SelectionBox();
            if (!e.KeyCtrl && e.IsKeyDown(InputSettings.INPUT.Scene.SelectionCircle))
            {
                SelectionCircle = new SelectionCircle();
                SelectionCircle?.Start(context,
                    context.CurrentMousePoint.X,
                    context.CurrentMousePoint.Y);
            }
        }

        public void OnMouseWheel(GLContext context)
        {
            //Resize the selection tools
            SelectionCircle?.Resize(MouseEventInfo.Delta);
        }

        public void OnMouseMove(GLContext context)
        {
            //During selection mode, the user can freely create a box during a mouse down/move
            if (IsSelectionMode && previousMouseDown != Vector2.Zero && SelectionBox == null) {
                //Check for the mouse to actually move to create a selection box
                var delta = previousMouseDown - new Vector2(MouseEventInfo.X, MouseEventInfo.Y);
                if (delta != Vector2.Zero)
                {
                    SelectionBox = new SelectionBox();
                    SelectionBox.StartObjectSelection(context, MouseEventInfo.X, MouseEventInfo.Y);
                }
            }

            //Apply selection
            if (MouseEventInfo.LeftButton == OpenTK.Input.ButtonState.Pressed)
                SelectionCircle?.ApplyObjectSelection(context, MouseEventInfo.X, MouseEventInfo.Y, true);
            //Apply deselection
            if (MouseEventInfo.MiddleButton == OpenTK.Input.ButtonState.Pressed)
                SelectionCircle?.ApplyObjectSelection(context, MouseEventInfo.X, MouseEventInfo.Y, false);
        }

        private Vector2 previousMouseDown;

        public void OnMouseDown(GLContext context)
        {
            if (IsSelectionMode && MouseEventInfo.LeftButton == OpenTK.Input.ButtonState.Pressed)
                previousMouseDown = new Vector2(MouseEventInfo.X, MouseEventInfo.Y);

            //Start deselection
            if (MouseEventInfo.MiddleButton == OpenTK.Input.ButtonState.Pressed)
                SelectionBox?.StartObjectDeselection(context, MouseEventInfo.X, MouseEventInfo.Y);
            //Start selection
            if (MouseEventInfo.LeftButton == OpenTK.Input.ButtonState.Pressed)
                SelectionBox?.StartObjectSelection(context, MouseEventInfo.X, MouseEventInfo.Y);

            //Disable selection tools
            if (MouseEventInfo.RightButton == OpenTK.Input.ButtonState.Pressed)
            {
                SelectionBox = null;
                SelectionCircle = null;
            }
        }

        public void OnMouseUp(GLContext context)
        {
            previousMouseDown = new Vector2(0, 0);

            //Apply then disable selection tools
            if (MouseEventInfo.LeftButton == OpenTK.Input.ButtonState.Released)
            {
                SelectionBox?.ApplyObjectSelection(context, MouseEventInfo.X, MouseEventInfo.Y);
            }
            SelectionBox = null;
        }

        public void Render(GLContext context, float x, float y)
        {
            //Draw the selection tools
            SelectionBox?.Render(context, x, y);
            SelectionCircle?.Render(context, x, y);
        }
    }
}
