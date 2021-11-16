using System;
using System.Collections.Generic;
using Toolbox.Core.ViewModels;
using GLFrameworkEngine;
using ImGuiNET;
using MapStudio.UI;

namespace MapStudio.UI
{
    public class PropertyWindow : DockWindow
    {
        public override string Name => "PROPERTIES";

        public object SelectedObject = null;

        NodeBase ActiveNode = null;

        private object ActiveEditor = null;

        public void OnLoad()
        {

        }

        public override void Render()
        {
            if (SelectedObject is NodeBase)
                Render(SelectedObject as NodeBase);
            if (SelectedObject is AssetItem)
                Render(SelectedObject as AssetItem);
        }

        public void Render(AssetItem asset)
        {
            if (asset.Tag != null)
                ImguiBinder.LoadProperties(asset.Tag, (sender, e) =>
                {
                    var handler = (ImguiBinder.PropertyChangedCustomArgs)e;

                    var type = asset.Tag.GetType().GetProperty(handler.Name);
                    type.SetValue(asset.Tag, sender);
                });
        }

        public void Render(NodeBase node)
        {
            if (node == null)
                return;

            bool valueChanged = ActiveNode != node;
            if (valueChanged)
                ActiveNode = node;

            if (node.TagUI != null)
            {
                node.TagUI.UIDrawer?.Invoke(this, EventArgs.Empty);
            }

            TryLoadProperyUI(node, valueChanged, false);
            TryLoadProperyUI(node, valueChanged, true);
        }

        private bool TryLoadProperyUI(NodeBase obj, bool valueChanged, bool isTag)
        {
            bool hasGUI = false;

            //A UI type that can display rendered IMGUI code.
            if (obj is IPropertyUI)
            {
                var propertyUI = (MapStudio.UI.IPropertyUI)obj;
                if (ActiveEditor == null || ActiveEditor.GetType() != propertyUI.GetTypeUI())
                {
                    var instance = Activator.CreateInstance(propertyUI.GetTypeUI());
                    ActiveEditor = instance;
                }
                if (valueChanged)
                    propertyUI.OnLoadUI(ActiveEditor);

                propertyUI.OnRenderUI(ActiveEditor);
                hasGUI = true;
            }

            if (isTag)
                return hasGUI;

            //Editable object properties
            if (obj is EditableObjectNode)
            {
                DrawEditableObjectProperties((EditableObjectNode)obj);
                hasGUI = true;
            }
            else if (obj.Tag != null) //Generated UI properties using attributes
            {
                ImguiBinder.LoadProperties(obj.Tag, OnPropertyChanged);
                hasGUI = true;
            }

            //Render path point properties
            if (obj is RenderablePath.PointNode)
            {
                var point = (obj as RenderablePath.PointNode).Point;
                var points = point.ParentPath.GetSelectedPoints();

                ImguiBinder.LoadProperties(point.Transform, (sender, e) =>
                {
                    var handler = (ImguiBinder.PropertyChangedCustomArgs)e;
                    var type = point.Transform.GetType().GetProperty(handler.Name);

                    List<IRevertable> revertables = new List<IRevertable>();
                    foreach (var pt in points)
                    {
                        var editTransform = pt.Transform;
                        revertables.Add(new TransformUndo(new TransformInfo(editTransform)));

                        type.SetValue(editTransform, sender);
                        editTransform.UpdateMatrix(true);
                    }
                    GLContext.ActiveContext.Scene.AddToUndo(revertables);
                    GLContext.ActiveContext.UpdateViewport = true;
                });

                if (point.ParentPath.InterpolationMode == RenderablePath.Interpolation.Bezier)
                {
                    if (ImGui.CollapsingHeader($"{TranslationSource.GetText("CONTROL_POINTS")}", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        bool updated = false;

                        ImGui.Columns(2);

                        ImGui.Text($"{TranslationSource.GetText("POINT")} 1");
                        ImGui.NextColumn();

                        float colwidth = ImGui.GetColumnWidth();
                        ImGui.SetColumnOffset(1, ImGui.GetWindowWidth() * 0.25f);
                        ImGui.PushItemWidth(colwidth);

                        updated |= ImGuiHelper.InputTKVector3($"##point1", point.ControlPoint1, "LocalPosition");
                        ImGui.PopItemWidth();
                        ImGui.NextColumn();

                        ImGui.Text($"{TranslationSource.GetText("POINT")} 2");
                        ImGui.NextColumn();

                        ImGui.SetColumnOffset(1, ImGui.GetWindowWidth() * 0.25f);
                        ImGui.PushItemWidth(colwidth);
                        updated |= ImGuiHelper.InputTKVector3($"##point2", point.ControlPoint2, "LocalPosition");
                        ImGui.PopItemWidth();
                        ImGui.NextColumn();

                        ImGui.Columns(1);

                        if (updated)
                        {
                            point.ControlPoint1.Transform.UpdateMatrix(true);
                            point.ControlPoint2.Transform.UpdateMatrix(true);
                            GLContext.ActiveContext.UpdateViewport = true;
                        }
                    }
                }
                hasGUI = true;
            }

            return hasGUI;
        }

        private void DrawEditableObjectProperties(NodeBase node)
        {
            var n = node as EditableObjectNode;
            var transform = n.Object.Transform;

            ImguiBinder.LoadProperties(transform, (sender, e) =>
            {
                var handler = (ImguiBinder.PropertyChangedCustomArgs)e;
                var type = transform.GetType().GetProperty(handler.Name);

                List<IRevertable> revertables = new List<IRevertable>();

                //Batch editing
                var selected = Workspace.ActiveWorkspace.GetSelected();
                foreach (var node in selected)
                {
                    if (node is EditableObjectNode)
                    {
                        var editTransform = ((EditableObjectNode)node).Object.Transform;
                        revertables.Add(new TransformUndo(new TransformInfo(editTransform)));

                        type.SetValue(editTransform, sender);
                        editTransform.UpdateMatrix(true);

                        GLContext.ActiveContext.TransformTools.UpdateOrigin();
                        GLContext.ActiveContext.TransformTools.UpdateBoundingBox();
                    }
                }
                GLContext.ActiveContext.Scene.AddToUndo(revertables);
                GLContext.ActiveContext.UpdateViewport = true;
            });

            if (n.UIProperyDrawer != null)
            {
                n.UIProperyDrawer.Invoke(this, EventArgs.Empty);
            }
            else
            {
                if (node.Tag != null)
                    ImguiBinder.LoadProperties(node.Tag, OnPropertyChanged);
            }
        }

        private void OnPropertyChanged(object sender, EventArgs e)
        {
            var handler = (ImguiBinder.PropertyChangedCustomArgs)e;
            //Apply the property
            handler.PropertyInfo.SetValue(handler.Object, sender);

            //Batch editing for selected
            var selected = Workspace.ActiveWorkspace.GetSelected();
            foreach (var node in selected)
            {
                if (node.Tag.GetType() != handler.Object.GetType())
                    continue;

                var tag = node.Tag;
                var type = tag.GetType().GetProperty(handler.Name);
                type.SetValue(tag, sender);
            }
        }
    }
}
