using System;
using System.Collections.Generic;
using System.Text;
using GLFrameworkEngine;
using ImGuiNET;
using OpenTK.Input;

namespace MapStudio.UI
{
    /// <summary>
    /// The input state of the application to determine what the current key/mouse state is.
    /// </summary>
    public class InputState
    {
        public static void UpdateKeyState()
        {
            KeyEventInfo.KeyChars.Clear();

            KeyEventInfo.KeyShift = ImGui.GetIO().KeyShift;
            KeyEventInfo.KeyCtrl = ImGui.GetIO().KeyCtrl;
            KeyEventInfo.KeyAlt = ImGui.GetIO().KeyAlt;

            int index = 0;
            for (char c = 'A'; c <= 'Z'; c++)
            {
                if (Keyboard.GetState().IsKeyDown(Key.A + index)) KeyEventInfo.KeyChars.Add(c.ToString());
                index++;
            }
            index = 0;
            for (char c = '0'; c <= '9'; c++)
            {
                if (Keyboard.GetState().IsKeyDown(Key.Number0 + index)) KeyEventInfo.KeyChars.Add(c.ToString());
                index++;
            }

            if (Keyboard.GetState().IsKeyDown(Key.Period)) KeyEventInfo.KeyChars.Add("period");
            if (Keyboard.GetState().IsKeyDown(Key.Space)) KeyEventInfo.KeyChars.Add("Space");
            if (Keyboard.GetState().IsKeyDown(Key.BackSpace)) KeyEventInfo.KeyChars.Add("backspace");
            if (Keyboard.GetState().IsKeyDown(Key.Delete)) KeyEventInfo.KeyChars.Add("Delete");
            if (Keyboard.GetState().IsKeyDown(Key.Tab)) KeyEventInfo.KeyChars.Add("Tab");
            if (Keyboard.GetState().IsKeyDown(Key.Minus)) KeyEventInfo.KeyChars.Add("-");
            if (Keyboard.GetState().IsKeyDown(Key.Plus)) KeyEventInfo.KeyChars.Add("+");

            for (int i = 0; i < 10; i++)
                if (Keyboard.GetState().IsKeyDown(Key.Keypad0 + i)) KeyEventInfo.KeyChars.Add($"keypad{i}");
        }


        public static void UpdateMouseState()
        {
            //Prepare info
            if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                MouseEventInfo.RightButton = ButtonState.Pressed;
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                MouseEventInfo.LeftButton = ButtonState.Pressed;

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                MouseEventInfo.RightButton = ButtonState.Released;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                MouseEventInfo.LeftButton = ButtonState.Released;

            if (ImGui.IsMouseDown(ImGuiMouseButton.Middle))
                MouseEventInfo.MiddleButton = ButtonState.Pressed;

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
        }
    }
}
