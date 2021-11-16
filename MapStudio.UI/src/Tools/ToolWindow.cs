using System;
using System.Collections.Generic;
using System.Text;

namespace MapStudio.UI
{
    public class ToolWindow : DockWindow
    {
        public override string Name => "TOOLS";

        public IToolWindowDrawer ToolDrawer;

        public override void Render() {
            ToolDrawer?.Render();
        }
    }
}
