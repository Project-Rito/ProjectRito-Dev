using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;

namespace MapStudio.UI
{
    public class Window
    {
        public virtual string Name { get; } = "Window";
        public virtual ImGuiWindowFlags Flags { get; }

        public bool Opened = false;

        public virtual void Render()
        {

        }
    }
}
