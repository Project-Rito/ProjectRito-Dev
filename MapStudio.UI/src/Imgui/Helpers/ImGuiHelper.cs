using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using ImGuiNET;
using System.Numerics;
using GLFrameworkEngine;
using System.Reflection;
using Toolbox.Core.ViewModels;

namespace MapStudio.UI
{
    public partial class ImGuiHelper
    {
        public class PropertyInfo
        {
            public bool CanDrag = false;

            public float Speed = 1.0f;
        }

        public static void LoadMenuItem(MenuItemModel item)
        {
            string header = item.Header;
            if (TranslationSource.HasKey(header))
                header = TranslationSource.GetText(header);

            if (item.Icon != null && IconManager.HasIcon(item.Icon)) {
                IconManager.DrawIcon(item.Icon);
            }

            if (string.IsNullOrEmpty(header)) {
                ImGui.Separator();
                return;
            }

            if (item.MenuItems.Count == 0)
            {
                if (ImGui.MenuItem(header, "", item.IsChecked, item.IsEnabled)) {
                    if (item.CanCheck)
                        item.IsChecked = !item.IsChecked;
                    item.Command.Execute(item);
                }
            }
            else
            {
                if (ImGui.BeginMenu(header)) {
                    foreach (var child in item.MenuItems)
                        LoadMenuItem(child);

                    ImGui.EndMenu();
                }
            }
        }

        public static void BoldTextLabel(string key, string label)
        {
            ImGuiHelper.BeginBoldText();
            ImGui.Text($"{key}:");
            ImGuiHelper.EndBoldText();

            ImGui.SameLine();
            ImGui.TextColored(ImGui.GetStyle().Colors[(int)ImGuiCol.Text], label);
        }

        public static void BeginBoldText() {
            ImGui.PushFont(ImGuiController.DefaultFontBold);
        }

        public static void EndBoldText() {
            ImGui.PopFont();
        }

        public static void IncrementCursorPosX(float amount) {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + amount);
        }

        public static void IncrementCursorPosY(float amount) {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + amount);
        }

        public static void DisplayFramebufferImage(int id, int width, int height) {
            DisplayFramebufferImage(id, new Vector2(width, height));
        }

        public static void DisplayFramebufferImage(int id, Vector2 size) {
            ImGui.Image((IntPtr)id, size, new Vector2(0, 1), new Vector2(1, 0));
        }

        public static void DrawCenteredText(string text)
        {
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(text).X;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(text);
        }
    }
}
