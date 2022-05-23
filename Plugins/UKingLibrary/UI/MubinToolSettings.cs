using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using MapStudio.UI;
using GLFrameworkEngine;

namespace UKingLibrary
{
    public class MubinToolSettings : IToolWindowDrawer
    {
        public MubinToolSettings() {
        }

        public void Render()
        {
            bool refreshScene = false;
            if (ImGui.CollapsingHeader(TranslationSource.GetText("OBJS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_VISIBLE_ACTORS")}", ref MapData.ShowVisibleActors);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_INVISIBLE_ACTORS")}", ref MapData.ShowInvisibleActors);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_MAP_MODEL")}", ref MapData.ShowMapModel);
                refreshScene |= ImGui.Checkbox($"{TranslationSource.GetText("SHOW_ACTOR_LINKS")}", ref MapData.ShowActorLinks);
            }
            if (ImGui.CollapsingHeader(TranslationSource.GetText("RAILS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("POINT_SIZE")}##vmenu11", ref RenderablePath.LinearPointScale, 0.05f, 0f, 10f);
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("BEZIER_POINT_SIZE")}##vmenu12", ref RenderablePath.BezierPointScale, 0.05f, 0f, 10f);
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("LINE_WIDTH")}##vmenu13", ref RenderablePath.BezierLineWidth, 0.05f, 0f, 10f);
                refreshScene |= ImGui.DragFloat($"{TranslationSource.GetText("ARROW_LENGTH")}##vmenu14", ref RenderablePath.BezierArrowLength, 0.05f, 0f, 10f);
            }

            if (ImGui.Button($"{TranslationSource.GetText("CACHE_COLLISION")}"))
                CollisionCacher.Cache(PluginConfig.CollisionCachePath);

            if (refreshScene)
                GLContext.ActiveContext.UpdateViewport = true;
        }
    }
}
