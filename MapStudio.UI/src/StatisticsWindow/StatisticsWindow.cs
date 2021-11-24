using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;
using MapStudio.UI;
using GLFrameworkEngine;

namespace MapStudio.UI
{
   public class StatisticsWindow : Window
    {
        public static int SkeletalAnims = 0;
        public static int MaterialAnims = 0;

        public override string Name => TranslationSource.GetText("STATS");

        public override void Render()
        {
            ImGui.Text($"{TranslationSource.GetText("DRAW_CALL_NUM")} {ResourceTracker.NumDrawCalls}");
            ImGui.Text($"{TranslationSource.GetText("DRAW_TRI_NUM")} {ResourceTracker.NumDrawTriangles}");

            ImGui.Text($"{TranslationSource.GetText("FX_DRAW_CALL_NUM")} {ResourceTracker.NumEffectDrawCalls}");
            ImGui.Text($"{TranslationSource.GetText("FX_INST_NUM")} {ResourceTracker.NumEffectInstances}");
            ImGui.Text($"{TranslationSource.GetText("FX_TRI_NUM")} {ResourceTracker.NumEffectTriangles}");

            ImGui.Text($"{TranslationSource.GetText("SK_ANIM_NUM")} {SkeletalAnims}");
            ImGui.Text($"{TranslationSource.GetText("MAT_ANIM_NUM")} {MaterialAnims}");

            ImGui.Text($"{TranslationSource.GetText("CAM_POS_X")} {GLContext.ActiveContext.Camera.TargetPosition.X}");
            ImGui.Text($"{TranslationSource.GetText("CAM_POS_Y")} {GLContext.ActiveContext.Camera.TargetPosition.Y}");
            ImGui.Text($"{TranslationSource.GetText("CAM_POS_Z")} {GLContext.ActiveContext.Camera.TargetPosition.Z}");
        }
    }
}
