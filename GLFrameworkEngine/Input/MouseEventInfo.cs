using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;
using OpenTK;

namespace GLFrameworkEngine
{
    public class MouseEventInfo
    {
        public static bool HasValue { get; set; } = true;

        public static int X => Position.X;
        public static int Y => Position.Y;

        public static System.Drawing.Point Position { get; set; } // Setting does not affect OS mouse position because of some scaling differences between ImGui and OpenTK. Use FullPosition instead.

        public static System.Drawing.Point FullPosition { get; set; }

        public static System.Drawing.Point ViewCenter { get; set; } // Relative to the window pos. Maybe this isn't the best place for this...

        public static ButtonState RightButton { get; set; }
        public static ButtonState LeftButton { get; set; }
        public static ButtonState MiddleButton { get; set; }

        public static float Delta { get; set; }

        public static float WheelPrecise { get; set; }

        public static Cursor MouseCursor = Cursor.Arrow;

        public enum Cursor
        {
            Arrow,
            Eraser,
            EyeDropper,
            None,
            Default,
        }
    }
}
