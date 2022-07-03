using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using MapStudio.UI;

namespace MapStudio.UI
{
    public class ProjectSaveDialog
    {
        string ProjectName = "";

        public string GetProjectDirectory()
        {
            var settings = GlobalSettings.Current;
            return $"{settings.Program.ProjectDirectory}/{ProjectName}";
        }

        public ProjectSaveDialog(string name)
        {
            if (name != TranslationSource.GetText("NEW_PROJECT"))
                ProjectName = name;
        }

        public void LoadUI()
        {
            ImGui.InputText(TranslationSource.GetText("PROJECT_NAME"), ref ProjectName, 100);

            var cancel = ImGui.Button(TranslationSource.GetText("CANCEL")); ImGui.SameLine();
            var save = ImGui.Button(TranslationSource.GetText("SAVE"));
            if (cancel)
                DialogHandler.ClosePopup(false);

            if (save) {
                if (ProjectName != TranslationSource.GetText("NEW_PROJECT"))
                    DialogHandler.ClosePopup(true);
            }
        }
    }
}
