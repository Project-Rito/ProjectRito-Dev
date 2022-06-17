using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ImGuiNET;
using MapStudio.UI;
using GLFrameworkEngine;

namespace UKingLibrary
{
    public class MubinToolSettings : IToolWindowDrawer
    {
        public MubinToolSettings() {
        }

        private string _removeOnlyOneUnitConfigName = @"";
        private string _removeOnlyOneFieldName = @"MainField";
        public void Render()
        {
            UKingEditor editor = (UKingEditor)Workspace.ActiveWorkspace.ActiveEditor; // If this is being rendered we know that the active editor is a UKingEditor.

            bool refreshScene = false;
            if (ImGui.CollapsingHeader(TranslationSource.GetText("OBJS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_VISIBLE_ACTORS")}", ref MapData.ShowVisibleActors);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_INVISIBLE_ACTORS")}", ref MapData.ShowInvisibleActors);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_MAP_MODEL")}", ref MapData.ShowMapModel);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_ACTOR_LINKS")}", ref MapData.ShowActorLinks);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_COLLISION_SHAPES")}", ref MapData.ShowCollisionShapes);
            }
            if (ImGui.CollapsingHeader(TranslationSource.GetText("RAILS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("POINT_SIZE")}##vmenu11", ref RenderablePath.LinearPointScale, 0.05f, 0f, 10f);
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("BEZIER_POINT_SIZE")}##vmenu12", ref RenderablePath.BezierPointScale, 0.05f, 0f, 10f);
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("LINE_WIDTH")}##vmenu13", ref RenderablePath.BezierLineWidth, 0.05f, 0f, 10f);
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("ARROW_LENGTH")}##vmenu14", ref RenderablePath.BezierArrowLength, 0.05f, 0f, 10f);
            }

            // Task tools
            if (ImGui.CollapsingHeader(TranslationSource.GetText("COLLISION_TOOLS")))
            {
                if (ImGui.Button(TranslationSource.GetText("CACHE_BAKED_COLLISION")))
                    CollisionCacher.Cache(PluginConfig.CollisionCachePath);
            }

            if (ImGui.CollapsingHeader(TranslationSource.GetText("ONLYONE_TOOLS"))) {
                // Select UnitConfigName
                ImGui.InputText(TranslationSource.GetText("ACTOR_NAME"), ref _removeOnlyOneUnitConfigName, 128);

                // Select field to act on
                if (ImGui.BeginCombo("##removeOnlyOneFieldName", _removeOnlyOneFieldName))
                {
                    foreach (string fieldName in GlobalData.FieldNames)
                    {
                        bool isSelected = _removeOnlyOneFieldName == fieldName;

                        if (ImGui.Selectable(fieldName, isSelected))
                        {
                            _removeOnlyOneFieldName = fieldName;
                        }

                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }

                if (ImGui.Button($"{TranslationSource.GetText("REMOVE_ONLYONE")}"))
                    OnlyOneRemover.Remove(_removeOnlyOneUnitConfigName, _removeOnlyOneFieldName, Path.GetFullPath(Path.Join(Path.GetDirectoryName(editor.FileInfo.FilePath), $"{editor.EditorConfig.FolderName}")));
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(TranslationSource.GetText("ONLYONE_REMOVER_NOTICE"));
            }

            if (refreshScene)
                GLContext.ActiveContext.UpdateViewport = true;
        }
    }
}
