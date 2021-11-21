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
    public partial class GLScene
    {
        //Scene menus
        public List<MenuItemModel> MenuItemsAdd = new List<MenuItemModel>();

        /// <summary>
        /// The shadow rendering engine used to draw cascaded shadow maps.
        /// </summary>
        public ShadowMainRenderer ShadowRenderer;

        public Vector3 LightDirection = new Vector3(0.3f, -1, 0.01f);

        /// <summary>
        /// The 3D cursor for placing objects down.
        /// </summary>
        public Cursor3D Cursor3D;

        /// <summary>
        /// The object list drawn in the scene.
        /// </summary>
        public List<IDrawable> Objects = new List<IDrawable>();

        /// <summary>
        /// A list of objects in edit mode.
        /// </summary>
        public List<ITransformableObject> EditModeObjects = new List<ITransformableObject>();

        public EventHandler SelectionChanged;

        /// <summary>
        /// Gets objects which can be transformed and edited.
        /// </summary>
        public List<ITransformableObject> GetSelectableObjects()
        {
            if (EditModeObjects.Count != 0)
                return FindSelectableObjects(EditModeObjects);
            else
                return FindSelectableObjects(Objects);
        }

        public List<ITransformableObject> GetSelected() {
            return GetSelectableObjects().Where(x => x.IsSelected).ToList();
        }

        public void SetCursor(GLContext context, int x, int y)
        {
            if (Cursor3D == null)
                return;

            Cursor3D.SetCursor(context, x, y);
        }

        public void AddRenderObject(IDrawable render, bool undoOperation = false) {
            if (undoOperation)
                AddToUndo(new EditableObjectAddUndo(this, new List<IDrawable>() { render }));

            Objects.Add(render);
            GLContext.ActiveContext.UpdateViewport = true;

            if (render is EditableObject)
                ((EditableObject)render).OnAdded();
            if (render is RenderablePath)
                ((RenderablePath)render).OnAdded();
        }

        public void RemoveRenderObject(IEnumerable<IDrawable> renders, bool undoOperation = false)
        {
            foreach (var render in renders)
                RemoveRenderObject(render, undoOperation);
        }

        public void RemoveRenderObject(IDrawable render, bool undoOperation = false) {
            if (undoOperation)
                AddToUndo(new EditableObjectAddUndo(this, new List<IDrawable>() { render }));

            Objects.Remove(render);
            GLContext.ActiveContext.UpdateViewport = true;

            if (render is EditableObject)
                ((EditableObject)render).OnRemoved();
            if (render is RenderablePath)
                ((RenderablePath)render).OnRemoved();
        }

        public void SelectAll(GLContext context)
        {
            foreach (var file in GetSelectableObjects())
                if (file.CanSelect)
                    file.IsSelected = true;

            OnSelectionChanged(context);
        }

        public void DeselectAll(GLContext context)
        {
            foreach (var file in GetSelectableObjects())
            {
                file.IsSelected = false;
                file.IsHovered = false;
            }

            OnSelectionChanged(context);
        }

        public void OnSelectionChanged(GLContext context)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);

            if (GetSelected().Count > 0) {
                context.TransformTools.InitAction(GetSelected());
            }
            else
                context.TransformTools.ActiveActions.Clear();

            context.UpdateViewport = true;
        }


        public void OnMouseDown(GLContext context) {
            if (KeyInfo.EventInfo.KeyAlt && MouseEventInfo.LeftButton == OpenTK.Input.ButtonState.Pressed)
                SetCursor(context, MouseEventInfo.X, MouseEventInfo.Y);

            foreach (IDrawableInput ob in Objects.Where(x => x is IDrawableInput))
                ob.OnMouseDown();
        }

        public void OnMouseMove(GLContext context)
        {
            foreach (IDrawableInput ob in Objects.Where(x => x is IDrawableInput))
                ob.OnMouseMove();
        }

        public void OnMouseUp(GLContext context)
        {
            foreach (IDrawableInput ob in Objects.Where(x => x is IDrawableInput))
                ob.OnMouseUp();
        }

        public ITransformableObject FindPickableAtPosition(GLContext context, Vector2 point) {
            return FindPickableAtPosition(context, GetSelectableObjects(), point);
        }

        public ITransformableObject FindPickableAtPosition(GLContext context, List<ITransformableObject> objects, Vector2 point) {
            //Do ray check first
            List<IRayCastPicking> rayPickables = new List<IRayCastPicking>();
            foreach (var pickable in objects)
            {
                if (pickable is IRayCastPicking)
                    rayPickables.Add(pickable as IRayCastPicking);
            }
            if (rayPickables.Count > 0)
            {
                var pickable = (ITransformableObject)context.RayPicker.FindPickableAtPosition(context, rayPickables, point);
                if (pickable != null)
                    return pickable;
            }
            return context.ColorPicker.FindPickableAtPosition(context, objects, point) as ITransformableObject;
        }

        private List<ITransformableObject> FindSelectableObjects(IEnumerable<object> objects)
        {
            List<ITransformableObject> transformables = new List<ITransformableObject>();
            foreach (IDrawable obj in objects)
            {
                if (!obj.IsVisible)
                    continue;

                if (obj is IEditModeObject && ((IEditModeObject)obj).EditMode)
                {
                    foreach (var ob in ((IEditModeObject)obj).Selectables)
                        transformables.Add(ob);
                }

                if (obj is ISelectableContainer) {
                    foreach (var ob in ((ISelectableContainer)obj).Selectables)
                        transformables.Add(ob);
                }
                if (obj is ITransformableObject)
                    transformables.Add((ITransformableObject)obj);
            }
            return transformables;
        }
    }
}
