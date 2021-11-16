using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImGuiNET;

namespace MapStudio.UI
{
    public class DialogHandler
    {
        static string Name = "";

        static Action DialogRender = null;

        static bool open = false;
        static bool isPopupOpen = true;
        static Action<bool> Result;

        public static void RenderActiveWindows()
        {
            if (DialogRender == null)
                return;

            if (open)
            {
                ImGui.OpenPopup(Name);
                isPopupOpen = true;
                open = false;
            }

            if (!isPopupOpen) ClosePopup(false);

            // Always center this window when appearing
            var center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new System.Numerics.Vector2(0.5f, 0.5f));

            if (ImGui.BeginPopupModal(Name, ref isPopupOpen))
            {
                DialogRender?.Invoke();
                ImGui.EndPopup();
            }
        }

        public static void Show(string name, Action dialogRender, Action<bool> dialogResult)
        {
            Name = name;
            DialogRender = dialogRender;
            open = true;
            Result = dialogResult;
        }

        public static bool Show(string name, Action dialogRender) {
            return Task.Run(() => ShowAsync(name, dialogRender)).Result;
        }

        public static async Task<bool> ShowAsync(string name, Action dialogRender)
        {
            Name = name;
            DialogRender = dialogRender;
            open = true;

            var tcs = new TaskCompletionSource<bool>();
            Result = (e) => { tcs.TrySetResult(e); };

            return await tcs.Task.ConfigureAwait(false);
        }

        public static void ClosePopup(bool isOk)
        {
            DialogRender = null;
            ImGui.CloseCurrentPopup();

            Result?.Invoke(isOk);
            Result = null;
        }
    }
}
