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
        string ProjectName;

        public string GetProjectDirectory()
        {
            var settings = GlobalSettings.Current;
            return $"{settings.Program.ProjectDirectory}/{ProjectName}";
        }

        public ProjectSaveDialog(string name) {
            ProjectName = name;
        }

        public void LoadUI()
        {
            var settings = GlobalSettings.Current;

            ImGui.InputText(TranslationSource.GetText("PROJECT_NAME"), ref ProjectName, 100);
            string projectDir = settings.Program.ProjectDirectory;
            ImguiCustomWidgets.PathSelector(TranslationSource.GetText("PROJECT_FOLDER"), ref projectDir);

            var cancel = ImGui.Button(TranslationSource.GetText("CANCEL")); ImGui.SameLine();
            var save = ImGui.Button(TranslationSource.GetText("SAVE"));
            if (cancel)
                DialogHandler.ClosePopup(false);

            if (save) {
                DialogHandler.ClosePopup(true);
            }
        }
    }
}
