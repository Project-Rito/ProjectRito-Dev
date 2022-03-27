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
            PushStyling();

            Render();

            PopStyling();
        }

        public virtual void PushStyling()
        {
            foreach (ImGuiCol col in OverrideColors.Keys)
            {
                ImGui.PushStyleColor(col, OverrideColors[col]);
            }
        }

        public virtual void PopStyling()
        {
            ImGui.PopStyleColor(OverrideColors.Count);
        }

        public virtual void Render()
        {

        }
    }
}
