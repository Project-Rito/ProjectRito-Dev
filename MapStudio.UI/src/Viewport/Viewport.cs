using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;
using ImGuiNET;
using MapStudio.UI;
using GLFrameworkEngine;
using OpenTK;
using Toolbox.Core;
using Toolbox.Core.Animations;
using Toolbox.Core.ViewModels;

namespace MapStudio.UI
{
    public class Viewport : DockWindow
    {
        public override string Name => "VIEWPORT";

        public ViewportRenderer Pipeline { get; set; }

        public List<MenuItemModel> DisplayMenuItems = new List<MenuItemModel>();

        public bool IsFocused = false;

        GlobalSettings GlobalSettings { get; set; }

        private IDragDropPicking DragDroppedModel;
        private bool contextMenuOpen = false;
        private Workspace ParentWorkspace;

        List<MenuItemModel> ToolMenuBarItems = new List<MenuItemModel>();
        List<MenuItemModel> PathToolMenuBarItems = new List<MenuItemModel>();
        
        public Viewport(Workspace workspace, GlobalSettings settings)
        {
            GlobalSettings = settings;
            ParentWorkspace = workspace;

            Pipeline = new ViewportRenderer();
            Pipeline.InitBuffers();
            Pipeline._context.UseSRBFrameBuffer = true;

            GlobalSettings.ReloadContext(Pipeline._context);
            Pipeline._context.SetActive();

            ReloadMenus();
        }

        public void LoadAddContextMenu()
        {
            if (DisplayMenuItems.Count > 0)
                return;

            var scene = GLContext.ActiveContext.Scene;
            DisplayMenuItems.AddRange(scene.MenuItemsAdd);
            contextMenuOpen = false;
        }

        public void SetActive() {
            Pipeline._context.SetActive();
        }

        public void Dispose()
        {
            foreach (var render in Pipeline._context.Scene.Objects)
                render.Dispose();

            Pipeline.Files.Clear();
            Pipeline._context.Scene.Objects.Clear();
        }

        public override void Render()
        {
            var width = ImGui.GetWindowWidth();
            var height = ImGui.GetFrameHeight();
            if (ImGui.BeginChild("viewport_iconmenu2", new System.Numerics.Vector2(width, height), false, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    DrawViewportIconMenu(ToolMenuBarItems);
                    ImGui.Checkbox(TranslationSource.GetText("DROP_TO_COLLISION"), ref Pipeline._context.EnableDropToCollision);
                    ImGui.EndMenuBar();
                }
            }
            ImGui.EndChild();

            var viewportWidth = ImGui.GetWindowWidth();
            var viewportHeight = ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - 2;
            var pos = ImGui.GetCursorPos();

            if (ImGui.BeginChild("viewport_child1", new System.Numerics.Vector2(viewportWidth, viewportHeight)))
            {
                RenderViewportDisplay();
            }

            ImGui.SetCursorPos(new System.Numerics.Vector2(pos.X, pos.Y - 45));
            if (ImGui.BeginChild("viewport_child2", new System.Numerics.Vector2(viewportWidth, 22)))
            {
                var color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
                var colorH = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered];

                ImGui.PushStyleColor(ImGuiCol.FrameBg, new System.Numerics.Vector4(color.X, color.Y, color.Z, 0.78f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new System.Numerics.Vector4(colorH.X, colorH.Y, colorH.Z, 0.78f));

                DrawShadingMenu();

                ImGui.SameLine();
                DrawCameraMenu();

                ImGui.SameLine();
                DrawEditorMenu();

                ImGui.SetCursorPos(new System.Numerics.Vector2(viewportWidth - 150, 0));
                DrawGizmoMenu();

                ImGui.PopStyleColor(2);

                ImGui.EndChild();
            }

            var fcolor = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new System.Numerics.Vector4(fcolor.X, fcolor.Y, fcolor.Z, 0.58f));

            ImGui.SetCursorPos(new System.Numerics.Vector2(pos.X, pos.Y));
            if (ImGui.BeginChild("viewport_child3", new System.Numerics.Vector2(24, PathToolMenuBarItems.Count * 28)))
            {
                DrawViewportIconMenu(PathToolMenuBarItems, true);
                ImGui.EndChild();
            }
            ImGui.PopStyleColor(1);

            ImGui.EndChild();

            if (DisplayMenuItems.Count > 0 && !contextMenuOpen) {
                ImGui.CloseCurrentPopup();

                ImGui.OpenPopup("contextMenuPopup");
                contextMenuOpen = true;
            }
            if (contextMenuOpen)
            {
                if (ImGui.BeginPopupContextItem("contextMenuPopup"))
                {
                    foreach (var item in DisplayMenuItems)
                        ImGuiHelper.LoadMenuItem(item);
                    ImGui.EndPopup();
                }
                else
                {
                    GLContext.ActiveContext.Focused = true;
                    DisplayMenuItems.Clear();
                    contextMenuOpen = false;
                }
            }
        }

        private void DrawShadingMenu()
        {
            string text = $"{TranslationSource.GetText("SHADING")} : [{DebugShaderRender.DebugRendering}]";

            ImGui.PushItemWidth(150);
            ImguiCustomWidgets.ComboScrollable($"##debugShading", text, ref DebugShaderRender.DebugRendering, () =>
                {
                    GLContext.ActiveContext.UpdateViewport = true;
                }, ImGuiComboFlags.NoArrowButton);

            ImGui.PopItemWidth();
        }

        private void DrawGizmoMenu()
        {
            var settings = Pipeline._context.TransformTools.TransformSettings;
            var mode = settings.TransformMode;

            ImGui.PushItemWidth(150);

            ImguiCustomWidgets.ComboScrollable($"##transformSpace",
                $"{TranslationSource.GetText("MODE")} : [{settings.TransformMode}]", ref mode, () =>
           {
               settings.TransformMode = mode;
               GLContext.ActiveContext.UpdateViewport = true;
           }, ImGuiComboFlags.NoArrowButton);

            ImGui.PopItemWidth();
        }

        private void DrawEditorMenu()
        {
            List<string> editorList = ParentWorkspace.ActiveEditor.SubEditors;
            string activeEditor = ParentWorkspace.ActiveEditor.SubEditor;

            string text = $"{TranslationSource.GetText("EDITORS")} : [{TranslationSource.GetText(activeEditor)}]";

            ImGui.PushItemWidth(150);
            ImguiCustomWidgets.ComboScrollable<string>($"##editorMenu", text, ref activeEditor,
                editorList, () =>
                {
                    Workspace.ActiveWorkspace.ActiveEditor.SubEditor = activeEditor;
                    GLContext.ActiveContext.UpdateViewport = true;
                }, ImGuiComboFlags.NoArrowButton);
            ImGui.PopItemWidth();
        }

        private void DrawCameraMenu()
        {
            bool updateSettings = false;
            bool refreshScene = false;

            var w = ImGui.GetCursorPosX();

            string mode = Pipeline._context.Camera.IsOrthographic ? "Ortho" : "Persp";

            var size = new System.Numerics.Vector2(120, 22);
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);
            if (ImGui.Button($"{TranslationSource.GetText("CAMERA")} : [{mode}]", size))
            {
                ImGui.OpenPopup("cameraMenu");
            }
            ImGui.PopStyleColor();

            var pos = ImGui.GetCursorScreenPos();

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(pos.X + w, pos.Y));
            if (ImGui.BeginPopup("cameraMenu"))
            {
                if (ImGui.Button(TranslationSource.GetText("RESET_TRANSFORM")))
                {
                    Pipeline._context.Camera.ResetViewportTransform();
                }

                ImGuiHelper.ComboFromEnum<Camera.FaceDirection>(TranslationSource.GetText("DIRECTION"), Pipeline._context.Camera, "Direction");
                if (ImGuiHelper.ComboFromEnum<Camera.CameraMode>(TranslationSource.GetText("MODE"), Pipeline._context.Camera, "Mode"))
                {
                    updateSettings = true;
                }

                updateSettings |= ImGuiHelper.InputFromBoolean(TranslationSource.GetText("ORTHOGRAPHIC"), Pipeline._context.Camera, "IsOrthographic");
                ImGuiHelper.InputFromBoolean(TranslationSource.GetText("LOCK_ROTATION"), Pipeline._context.Camera, "LockRotation");

                updateSettings |= ImGuiHelper.InputFromFloat(TranslationSource.GetText("FOV_(DEGREES)"), Pipeline._context.Camera, "FovDegrees", true, 1f);
                if (Pipeline._context.Camera.FovDegrees != 45)
                {
                    ImGui.SameLine(); if (ImGui.Button(TranslationSource.GetText("RESET"))) { Pipeline._context.Camera.FovDegrees = 45; }
                }

                updateSettings |= ImGuiHelper.InputFromFloat(TranslationSource.GetText("ZFAR"), Pipeline._context.Camera, "ZFar", true, 1f);
                if (Pipeline._context.Camera.ZFar != 100000.0f)
                {
                    ImGui.SameLine(); if (ImGui.Button(TranslationSource.GetText("RESET"))) { Pipeline._context.Camera.ZFar = 100000.0f; }
                }

                updateSettings |= ImGuiHelper.InputFromFloat(TranslationSource.GetText("ZNEAR"), Pipeline._context.Camera, "ZNear", true, 0.1f);
                if (Pipeline._context.Camera.ZNear != 0.1f)
                {
                    ImGui.SameLine(); if (ImGui.Button(TranslationSource.GetText("RESET"))) { Pipeline._context.Camera.ZNear = 0.1f; }
                }

                updateSettings |= ImGuiHelper.InputFromFloat(TranslationSource.GetText("ZOOM_SPEED"), Pipeline._context.Camera, "ZoomSpeed", true, 0.1f);
                if (Pipeline._context.Camera.ZoomSpeed != 1.0f)
                {
                    ImGui.SameLine(); if (ImGui.Button(TranslationSource.GetText("RESET"))) { Pipeline._context.Camera.ZoomSpeed = 1.0f; }
                }

                updateSettings |= ImGuiHelper.InputFromFloat(TranslationSource.GetText("PAN_SPEED"), Pipeline._context.Camera, "PanSpeed", true, 0.1f);
                if (Pipeline._context.Camera.PanSpeed != 1.0f)
                {
                    ImGui.SameLine(); if (ImGui.Button(TranslationSource.GetText("RESET"))) { Pipeline._context.Camera.PanSpeed = 1.0f; }
                }

                updateSettings |= ImGuiHelper.InputFromFloat(TranslationSource.GetText("KEY_MOVE_SPEED"), Pipeline._context.Camera, "KeyMoveSpeed", true, 0.1f);
                if (Pipeline._context.Camera.PanSpeed != 1.0f)
                {
                    ImGui.SameLine(); if (ImGui.Button(TranslationSource.GetText("RESET"))) { Pipeline._context.Camera.KeyMoveSpeed = 1.0f; }
                }

                if (updateSettings)
                    Pipeline._context.UpdateViewport = true;

                if (updateSettings)
                {
                    //Reload existing set values then save
                    GlobalSettings.LoadCurrentSettings();
                    GlobalSettings.Save();
                }

                ImGui.EndPopup();
            }

            if (refreshScene || updateSettings)
                Pipeline._context.UpdateViewport = true;

            if (updateSettings)
            {
                //Reload existing set values then save
                GlobalSettings.LoadCurrentSettings();
                GlobalSettings.Save();
            }
        }

        public void ReloadMenus()
        {
            ToolMenuBarItems = SetupIconMenu();
            PathToolMenuBarItems = SetupPathToolIconMenu();
        }

        //For the edit menu in main toolbar
        public List<MenuItemModel> GetEditMenuItems()
        {
            List<MenuItemModel> menus = new List<MenuItemModel>();
            menus.Add(new MenuItemModel($"   {IconManager.SELECT_ICON}    {TranslationSource.GetText("SELECT_ALL")}", () => Pipeline._context.Scene.SelectAll(Pipeline._context)));
            menus.Add(new MenuItemModel($"   {IconManager.DESELECT_ICON}    {TranslationSource.GetText("DESELECT_ALL")}", () => Pipeline._context.Scene.DeselectAll(Pipeline._context)));

            menus.Add(new MenuItemModel($"   {IconManager.UNDO_ICON}    {TranslationSource.GetText("UNDO")}", () => Pipeline._context.Scene.Undo()));
            menus.Add(new MenuItemModel($"   {IconManager.REDO_ICON}    {TranslationSource.GetText("REDO")}", () => Pipeline._context.Scene.Redo()));
            menus.Add(new MenuItemModel(""));
            menus.Add(new MenuItemModel($"   {IconManager.COPY_ICON}    {TranslationSource.GetText("COPY")}", Pipeline._context.Scene.CopySelected));
            menus.Add(new MenuItemModel($"   {IconManager.PASTE_ICON}    {TranslationSource.GetText("PASTE")}", Pipeline._context.Scene.PasteSelected));
            menus.Add(new MenuItemModel($"   {IconManager.DELETE_ICON}    {TranslationSource.GetText("REMOVE")}", Pipeline._context.Scene.DeleteSelected));

            return menus;
        }

        private List<MenuItemModel> SetupPathToolIconMenu()
        {
            List<MenuItemModel> menus = new List<MenuItemModel>();
            menus.Add(new MenuItemModel($"{IconManager.PATH_MOVE}", () =>
            {
                RenderablePath.EditToolMode = RenderablePath.ToolMode.Transform;
                ReloadMenus();
            }, "TRANSFORM_PATH", RenderablePath.EditToolMode == RenderablePath.ToolMode.Transform));
            menus.Add(new MenuItemModel($"{IconManager.PATH_CONNECT}", () =>
            {
                RenderablePath.EditToolMode = RenderablePath.ToolMode.Connection;
                RenderablePath.ConnectAuto = false;

                ReloadMenus();
            }, "CONNECT_PATH", !RenderablePath.ConnectAuto  && RenderablePath.EditToolMode == RenderablePath.ToolMode.Connection));
            menus.Add(new MenuItemModel($"{IconManager.PATH_CONNECT_AUTO}", () =>
            {
                RenderablePath.EditToolMode = RenderablePath.ToolMode.Connection;
                RenderablePath.ConnectAuto = true;
                ReloadMenus();
            }, "CONNECT_PATH_AUTO", RenderablePath.ConnectAuto && RenderablePath.EditToolMode == RenderablePath.ToolMode.Connection));
            menus.Add(new MenuItemModel($"{IconManager.ERASER}", () =>
            {
                RenderablePath.EditToolMode = RenderablePath.ToolMode.Erase;
                ReloadMenus();
            }, "ERASE_PATH", RenderablePath.EditToolMode == RenderablePath.ToolMode.Erase));
            return menus;
        }

        private List<MenuItemModel> SetupIconMenu()
        {
            bool isSelectionMode = Pipeline._context.SelectionTools.IsSelectionMode;
            bool isTranslationActive = Pipeline._context.TransformTools.ActiveMode == TransformEngine.TransformActions.Translate;
            bool isRotationActive = Pipeline._context.TransformTools.ActiveMode == TransformEngine.TransformActions.Rotate;
            bool isScaleActive = Pipeline._context.TransformTools.ActiveMode == TransformEngine.TransformActions.Scale;
            bool isRectScaleActive = Pipeline._context.TransformTools.ActiveMode == TransformEngine.TransformActions.RectangleScale;
            bool isPlaying = StudioSystem.Instance != null && StudioSystem.Instance.IsPlaying;

            List<MenuItemModel> menus = new List<MenuItemModel>();
            if (Pipeline.IsViewport2D)
                menus.Add(new MenuItemModel($"{IconManager.ICON_3D}", () => Pipeline.IsViewport2D = !Pipeline.IsViewport2D, "VIEWPORT_2D"));
            else
                menus.Add(new MenuItemModel($"{IconManager.ICON_2D}", () => Pipeline.IsViewport2D = !Pipeline.IsViewport2D, "VIEWPORT_2D"));

            if (!isPlaying)
                menus.Add(new MenuItemModel($"{IconManager.PLAY_ICON}", () => 
                {
                    StudioSystem.Instance.Run();
                    ReloadMenus();
                }, "PLAY"));
            else
                menus.Add(new MenuItemModel($"{IconManager.PAUSE_ICON}", () =>
                {
                    StudioSystem.Instance.Pause();
                    ReloadMenus();
                }, "STOP"));
            menus.Add(new MenuItemModel($"{IconManager.UNDO_ICON}", () => Pipeline._context.Scene.Undo(), "UNDO"));
            menus.Add(new MenuItemModel($"{IconManager.REDO_ICON}", () => Pipeline._context.Scene.Redo(), "REDO"));
            menus.Add(new MenuItemModel($"{IconManager.CAMERA_ICON}", TakeScreenshot, "SCREENSHOT"));
            menus.Add(new MenuItemModel(""));
            menus.Add(new MenuItemModel($"{IconManager.ARROW_ICON}", EnterSelectionMode, "SELECT", isSelectionMode));
            menus.Add(new MenuItemModel($"{IconManager.TRANSLATE_ICON}", () =>
            {
                EnterGizmoMode(TransformEngine.TransformActions.Translate);
            }, "TRANSLATE", isTranslationActive && !isSelectionMode));
            menus.Add(new MenuItemModel($"{IconManager.ROTATE_ICON}", () =>
            {
                EnterGizmoMode(TransformEngine.TransformActions.Rotate);
            }, "ROTATE", isRotationActive && !isSelectionMode));
            menus.Add(new MenuItemModel($"{IconManager.SCALE_ICON}", () =>
            {
                EnterGizmoMode(TransformEngine.TransformActions.Scale);
            }, "SCALE", isScaleActive && !isSelectionMode));
            menus.Add(new MenuItemModel($"{IconManager.RECT_SCALE_ICON}", () =>
            {
                EnterGizmoMode(TransformEngine.TransformActions.RectangleScale);
            }, "RECTANGLE_SCALE", isRectScaleActive && !isSelectionMode));
            menus.Add(new MenuItemModel(""));
            menus.Add(new MenuItemModel(""));
            menus.Add(new MenuItemModel($"{IconManager.COPY_ICON}", () => { }, "COPY"));
            menus.Add(new MenuItemModel($"{IconManager.PASTE_ICON}", () => { }, "PASTE"));
            menus.Add(new MenuItemModel($"{IconManager.DELETE_ICON}", Pipeline._context.Scene.DeleteSelected, "REMOVE"));
            //A workspace can have its own menu icons per editor
            if (Workspace.ActiveWorkspace != null)
                menus.AddRange(Workspace.ActiveWorkspace.GetViewportMenuIcons());

            return menus;
        }

        private void DrawViewportIconMenu(List<MenuItemModel> items, bool vertical = false)
        {
            var h = ImGui.GetWindowHeight();
            if (vertical)
                h = 23;

            var menuSize = new System.Numerics.Vector2(h, h);

            //Make icon buttons invisible aside from the icon itself.
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4());
            {
                foreach (var item in items)
                {
                    if (item.Header == "")
                    {
                        ImGui.Separator();
                        continue;
                    }

                    if (item.IsChecked)
                    {
                        var selectionColor = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];
                        ImGui.PushStyleColor(ImGuiCol.Button, selectionColor);
                    }

                    if (ImGui.Button(item.Header, menuSize)) {
                        item.Command.Execute(item);
                    }
                    if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(item.ToolTip))
                        ImGui.SetTooltip(TranslationSource.GetText(item.ToolTip));

                    if (!vertical)
                        ImGui.SameLine();

                    if (item.IsChecked)
                        ImGui.PopStyleColor(1);
                }
            }
            ImGui.PopStyleColor();
        }

        private void TakeScreenshot()
        {
            bool enableAlpha = true;

            string date = DateTime.Now.ToFileTime().ToString();

            var size = new OpenTK.Vector2(Pipeline.Width, Pipeline.Height);
            var upscaled = new OpenTK.Vector2(1920, 1080) * 2;

            var bitmap = Pipeline.SaveAsScreenshot((int)upscaled.X, (int)upscaled.Y, enableAlpha);
            bitmap.Save($"ScreenShot{date}.png");

            Pipeline.Width = (int)size.X;
            Pipeline.Height = (int)size.Y;
            Pipeline.OnResize();
        }

        private void EnterGizmoMode(TransformEngine.TransformActions action)
        {
            Pipeline._context.SelectionTools.IsSelectionMode = false;
            Pipeline._context.TransformTools.TransformSettings.DisplayGizmo = true;
            Pipeline._context.TransformTools.UpdateTransformMode(action);
            ReloadMenus();
        }

        private void EnterSelectionMode()
        {
            Pipeline._context.SelectionTools.IsSelectionMode = true;
            Pipeline._context.TransformTools.TransformSettings.DisplayGizmo = false;
            Pipeline._context.TransformTools.UpdateTransformMode(TransformEngine.TransformActions.Translate);

            ReloadMenus();
        }

        private void RenderViewportDisplay()
        {
            var size = ImGui.GetWindowSize();
            if (Pipeline.Width != (int)size.X || Pipeline.Height != (int)size.Y)
            {
                Pipeline.Width = (int)size.X;
                Pipeline.Height = (int)size.Y;
                Pipeline.OnResize();
            }
            Pipeline.RenderScene();

            //Store the focus state for handling key events
            IsFocused = ImGui.IsWindowFocused();

            if (ImGui.IsAnyMouseDown() && ImGui.IsWindowHovered() && !IsFocused)
            {
                IsFocused = true;
                ImGui.FocusWindow(ImGui.GetCurrentWindow());
            }

             //Make sure the viewport is always focused during transforming
             var transformTools = Pipeline._context.TransformTools;
            bool isTransforming = false;
            bool isPicking = false;
            if (transformTools.ActiveActions.Count > 0 && transformTools.TransformSettings.ActiveAxis != TransformEngine.Axis.None)
                isTransforming = true;
            if (Pipeline._context.PickingTools.UseEyeDropper)
                isPicking = true;

            if ((IsFocused && _mouseDown) ||
                ImGui.IsWindowHovered() || _mouseDown ||
                isTransforming || isPicking)
            {
                Pipeline._context.Focused = true;

                if (!onEnter)
                {
                    Pipeline._context.ResetPrevious();
                    onEnter = true;
                }

                //Only update scene when necessary
                if (ImGuiController.ApplicationHasFocus)
                    UpdateCamera(Pipeline._context);
            }
            else
            {
                onEnter = false;
                Pipeline._context.Focused = false;

                //Reset drag/dropped model data if mouse leaves the viewport during a drag event
                if (DragDroppedModel != null)
                {
                    DragDroppedModel.DragDroppedOnLeave();
                    DragDroppedModel = null;
                }
            }

            var id = Pipeline.GetViewportTexture();
            ImGui.Image((IntPtr)id, size, new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
            ImGui.SetItemAllowOverlap();

            DrawCustomCursors();

            if (ImGui.BeginDragDropTarget())
            {
                ImGuiPayloadPtr outlinerDrop = ImGui.AcceptDragDropPayload("OUTLINER_ITEM",
                    ImGuiDragDropFlags.AcceptNoDrawDefaultRect | ImGuiDragDropFlags.AcceptBeforeDelivery);
                ImGuiPayloadPtr assetDrop = ImGui.AcceptDragDropPayload("ASSET_ITEM",
                    ImGuiDragDropFlags.AcceptNoDrawDefaultRect);

                //Dropping from asset window
                if (assetDrop.IsValid()) {
                    //Set focus to the viewport
                    this.IsFocused = true;
                    this.Pipeline._context.Focused = true;

                    Workspace.ActiveWorkspace.OnAssetViewportDrop();
                }
                //Dropping from outliner window
                if (outlinerDrop.IsValid())
                {
                    //Drag/drop things onto meshes
                    InputState.UpdateMouseState();
                    var picked = Pipeline.GetPickedObject() as IDragDropPicking;
                    //Picking object changed.
                    if (DragDroppedModel != picked)
                    {
                        //Set exit drop event for previous model
                        if (DragDroppedModel != null)
                            DragDroppedModel.DragDroppedOnLeave();

                        DragDroppedModel = picked;

                        //Model has changed so call the enter event
                        if (picked != null)
                            picked.DragDroppedOnEnter();
                    }

                    if (picked != null)
                    {
                        //Set the drag/drop event
                        var node = Outliner.GetDragDropNode();
                        picked.DragDropped(node.Tag);
                    }
                    if (MouseEventInfo.LeftButton == ButtonState.Released)
                        DragDroppedModel = null;
                }
                ImGui.EndDragDropTarget();
            }
        }

        private void DrawCustomCursors()
        {
            if (MouseEventInfo.MouseCursor == MouseEventInfo.Cursor.Eraser)
            {
                var image = IconManager.GetTextureIcon("ERASER");
                var p = ImGui.GetMousePos();
                p = new System.Numerics.Vector2(p.X - 5, p.Y - 5);

                var csize = new System.Numerics.Vector2(22, 22);
                ImGui.GetWindowDrawList().AddImage((IntPtr)image, p,
                    new System.Numerics.Vector2(p.X + csize.X, p.Y + csize.Y),
                    new System.Numerics.Vector2(0, 0),
                    new System.Numerics.Vector2(1, 1));
            }
        }

        private GLFrameworkEngine.UI.UIEditToolMenu ActiveToolMenu;
        private string selectedTool = "";

        private void LoadDropMenu(List<MenuItemModel> menuItems, bool isTool)
        {
            var menuSize = new System.Numerics.Vector2(100, 22);

            foreach (var item in menuItems)
            {
                bool isSelectedTool = selectedTool == item.Header && isTool;
                if (isSelectedTool)
                {
                    var selectionColor = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered];
                    ImGui.PushStyleColor(ImGuiCol.Button, selectionColor);
                }

                if (ImGui.Button(item.Header, menuSize))
                {
                    item.Command?.Execute(this);
                    Pipeline._context.UpdateViewport = true;

                    if (isTool)
                        selectedTool = item.Header;
                }

                if (isSelectedTool)
                    ImGui.PopStyleColor();
            }
        }

        private void DrawTransformInfo()
        {
            var transformTools = Pipeline._context.TransformTools;
            if (transformTools.ActiveActions.Count == 0)
                return;

            var origin = transformTools.TransformSettings.Origin;
            var screenPoint = Pipeline._context.ScreenCoordFor(origin);

            ImGui.SetCursorPos(new System.Numerics.Vector2(screenPoint.X, screenPoint.Y));
            foreach (var action in transformTools.ActiveActions)
                ImGui.Text(action.ToString());
        }


        private bool onEnter = false;
        private bool _mouseDown = false;
        private long _lastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        private void UpdateCamera(GLContext context)
        {
            InputState.UpdateMouseState();
            InputState.UpdateKeyState();

            float frameTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - _lastTime; // Todo - change things so this is a long or something, not a float.
            _lastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (ImGui.IsAnyMouseDown() && !_mouseDown)
            {
                context.OnMouseDown(frameTime);
                _mouseDown = true;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Right) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
            {
                context.OnMouseUp();
                _mouseDown = false;
            }
            
            context.OnMouseMove(_mouseDown, frameTime);

            if (ImGuiController.ApplicationHasFocus)
                context.OnMouseWheel(frameTime);

            context.Camera.Controller.KeyPress(frameTime);
        }
    }
}
