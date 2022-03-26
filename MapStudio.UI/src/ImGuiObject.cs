using System.Numerics;
using System.Collections.Generic;
using ImGuiNET;

namespace MapStudio.UI
{
    public class ImGuiObject
    {
        public Dictionary<ImGuiCol, Vector4> OverrideColors = new Dictionary<ImGuiCol, Vector4>();

        public virtual void RenderWithStyling()
        {
            foreach (ImGuiCol col in OverrideColors.Keys) {
                ImGui.PushStyleColor(col, OverrideColors[col]);
            }

            Render();

            ImGui.PopStyleColor(OverrideColors.Count);
        }

        public virtual void Render()
        {

        }
    }
}
