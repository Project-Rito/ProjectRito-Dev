using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;

namespace MapStudio.UI
{
    public class DockWindow : Window
    {
        public ImGuiDir DockDirection = ImGuiDir.None;

        public float SplitRatio = 0.0f;

        public DockWindow ParentDock;

        public uint DockID = 0;

        public override string ToString()
        {
            return $"{Name}_{DockDirection}_{SplitRatio}_{DockID}";
        }
    }
}
