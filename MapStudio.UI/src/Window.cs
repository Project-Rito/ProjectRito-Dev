using ImGuiNET;

namespace MapStudio.UI
{
    public class Window : ImGuiObject
    {
        public virtual string Name { get; } = "Window";
        public virtual ImGuiWindowFlags Flags { get; }

        public bool Opened = false;
    }
}
