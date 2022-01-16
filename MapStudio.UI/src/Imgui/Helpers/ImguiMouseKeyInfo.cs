using ImGuiNET;
using System.Drawing;
using GLFrameworkEngine;
using OpenTK.Input;

namespace MapStudio.UI
{
    public partial class ImGuiHelper
    {
        public static void UpdateMouseState()
        {
            //Prepare info
            if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                MouseEventInfo.RightButton = ButtonState.Pressed;
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                MouseEventInfo.RightButton = ButtonState.Released;

            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                MouseEventInfo.LeftButton = ButtonState.Pressed;
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                MouseEventInfo.LeftButton = ButtonState.Released;

            if (ImGui.IsMouseDown(ImGuiMouseButton.Middle))
                MouseEventInfo.MiddleButton = ButtonState.Pressed;
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
                MouseEventInfo.MiddleButton = ButtonState.Released;

            MouseState mouseState = Mouse.GetState();
            MouseEventInfo.WheelPrecise = mouseState.WheelPrecise;

            //Construct relative position
            var windowPos = ImGui.GetWindowPos();

            var pos = ImGui.GetIO().MousePos;
            pos = new System.Numerics.Vector2(pos.X - windowPos.X, pos.Y - windowPos.Y);

            if (ImGui.IsMousePosValid())
                MouseEventInfo.Position = new System.Drawing.Point((int)pos.X, (int)pos.Y);
            else
                MouseEventInfo.HasValue = false;

            MouseEventInfo.FullPosition = new System.Drawing.Point(Mouse.GetCursorState().X, Mouse.GetCursorState().Y);

            MouseEventInfo.ViewCenter = new System.Drawing.Point((int)ImGui.GetWindowWidth() / 2, (int)ImGui.GetWindowHeight() / 2);
        }
    }
}
