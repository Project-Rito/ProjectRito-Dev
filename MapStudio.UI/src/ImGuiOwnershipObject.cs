using System.Numerics;
using ImGuiNET;

namespace MapStudio.UI
{
    public class ImGuiOwnershipObject : ImGuiObject
    {
        private Vector4 _ownershipColor = Vector4.Zero;
        public Vector4 OwnershipColor
        {
            get
            {
                return _ownershipColor;
            }
            set
            {
                _ownershipColor = value;

                if (_ownershipColor != Vector4.Zero)
                {
                    // Show that this owns something
                    OverrideColors[ImGuiCol.TitleBg] = _ownershipColor;
                    OverrideColors[ImGuiCol.TabUnfocusedActive] = _ownershipColor;
                    OverrideColors[ImGuiCol.TabActive] = _ownershipColor;
                    OverrideColors[ImGuiCol.TabHovered] = _ownershipColor;
                }
                else
                {
                    OverrideColors.Remove(ImGuiCol.TitleBg);
                    OverrideColors.Remove(ImGuiCol.TabUnfocusedActive);
                    OverrideColors.Remove(ImGuiCol.TabActive);
                    OverrideColors.Remove(ImGuiCol.TabHovered);
                }
            }
        }

        private ImGuiOwnershipObject _owner;
        public ImGuiOwnershipObject Owner 
        {
            get
            {
                return _owner;
            }
            set
            {
                _owner = value;

                if (_owner != null)
                {
                    // Show that this is owned by something
                    OverrideColors[ImGuiCol.TitleBgActive] = _owner.OwnershipColor;
                }
                else
                {
                    OverrideColors.Remove(ImGuiCol.TitleBgActive);
                }
            }
        }
    }
}
