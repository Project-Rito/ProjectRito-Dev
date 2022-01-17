using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Toolbox.Core;
using MapStudio.UI;
using Newtonsoft.Json;
using ImGuiNET;
using GLFrameworkEngine;

namespace UKingLibrary
{
    public class PluginConfig : IPluginConfig
    {
        public static readonly string PluginName = "UKing";

        [JsonProperty]
        static string GamePath = @"";
        [JsonProperty]
        static string UpdatePath = @"";
        [JsonProperty]
        static string AocPath = @"";
        [JsonProperty]
        static List<string> ModPaths = new List<string>();

        static bool HasValidGamePath;
        static bool HasValidUpdatePath;
        static bool HasValidAocPath;
        static List<bool> HasValidModPaths;

        [JsonProperty]
        public static string FieldName = @"MainField";

        [JsonProperty]
        public static int MaxTerrainLOD = 5;

        [JsonProperty]
        public static bool DebugTerrainSections = false;

        //Only load the config once when this constructor is activated.
        internal static bool init = false;

        public PluginConfig() { init = true; }

        public void DrawUI()
        {
            if (ImGui.BeginMenu($"{TranslationSource.GetText("MOD PATHS")}##uk_vmenu01"))
            {
                for (int i = 0; i < ModPaths.Count; i++)
                {
                    string path = ModPaths[i];
                    if (ImguiCustomWidgets.PathSelector($"##uk_modpath{i}", ref path, HasValidModPaths[i]))
                    {
                        ModPaths[i] = path;
                        Save();
                    }
                }
                

                ImGui.EndMenu();
            }

            if (ImguiCustomWidgets.PathSelector(TranslationSource.GetText("BOTW GAME PATH"), ref GamePath, HasValidGamePath))
                Save();

            if (ImguiCustomWidgets.PathSelector(TranslationSource.GetText("BOTW UPDATE PATH"), ref UpdatePath, HasValidUpdatePath))
                Save();

            if (ImguiCustomWidgets.PathSelector(TranslationSource.GetText("BOTW DLC Path"), ref AocPath, HasValidAocPath))
                Save();

            string[] fieldNames = { "AocField", "MainField" };
            if (ImGui.BeginCombo("Field Name", FieldName))
            {
                for (int i = 0; i < fieldNames.Length; i++)
                {
                    bool is_selected = (FieldName == fieldNames[i]);
                    if (ImGui.Selectable(fieldNames[i], is_selected))
                        FieldName = fieldNames[i];
                    if (is_selected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
                Save();
            }

            if (ImGui.BeginMenu($"{TranslationSource.GetText("TERRAIN")}##uk_vmenu02"))
            {
                if (ImGui.SliderInt(TranslationSource.GetText("MAX DETAIL"), ref MaxTerrainLOD, 0, 7))
                    Save();
#if DEBUG
                if (ImGui.Checkbox(TranslationSource.GetText("DEBUG SECTIONS"), ref DebugTerrainSections))
                    Save();
#endif
                ImGui.EndMenu();
            }
        }

        public static string GetContentPath(string relativePath)
        {
            //Mod content
            foreach (string modPath in ModPaths) {
                if (File.Exists($"{modPath}\\aoc\\0010\\{relativePath}"))    return $"{modPath}\\aoc\\0010\\{relativePath}";
                if (File.Exists($"{modPath}\\aoc\\0011\\{relativePath}"))    return $"{modPath}\\aoc\\0011\\{relativePath}";
                if (File.Exists($"{modPath}\\aoc\\0012\\{relativePath}"))    return $"{modPath}\\aoc\\0012\\{relativePath}";

                if (File.Exists($"{modPath}\\content\\{relativePath}")) return $"{modPath}\\content\\{relativePath}";
            }

            //DLC content
            if (File.Exists($"{AocPath}\\0010\\{relativePath}"))    return $"{AocPath}\\0010\\{relativePath}";
            if (File.Exists($"{AocPath}\\0011\\{relativePath}"))    return $"{AocPath}\\0011\\{relativePath}";
            if (File.Exists($"{AocPath}\\0012\\{relativePath}"))    return $"{AocPath}\\0012\\{relativePath}";
            //Update content
            if (File.Exists($"{UpdatePath}\\{relativePath}")) return $"{UpdatePath}\\{relativePath}";
            //Base game content
            if (File.Exists($"{GamePath}\\{relativePath}"))   return $"{GamePath}\\{relativePath}";

            return relativePath;
        }

        public static string GetCachePath(string relativePath)
        {
            return $"{Toolbox.Core.Runtime.ExecutableDir}\\Cache\\{PluginName}\\{relativePath}";
        }

        /// <summary>
        /// Loads the config json file on disc or creates a new one if it does not exist.
        /// </summary>
        /// <returns></returns>
        public static PluginConfig Load()
        {
            if (!File.Exists($"{Runtime.ExecutableDir}\\UKingConfig.json")) { new PluginConfig().Save(); }

            var config = JsonConvert.DeserializeObject<PluginConfig>(File.ReadAllText($"{Runtime.ExecutableDir}\\UKingConfig.json"));
            config.Reload();
            return config;
        }

        /// <summary>
        /// Saves the current configuration to json on disc.
        /// </summary>
        public void Save()
        {
            File.WriteAllText($"{Runtime.ExecutableDir}\\UKingConfig.json", JsonConvert.SerializeObject(this));
            Reload();
        }

        /// <summary>
        /// Called when the config file has been loaded or saved.
        /// </summary>
        public void Reload()
        {
            HasValidGamePath = File.Exists($"{GamePath}\\Actor\\ActorInfo.product.sbyml");
            HasValidUpdatePath = File.Exists($"{UpdatePath}\\Actor\\ActorInfo.product.sbyml");
            HasValidAocPath = File.Exists($"{AocPath}\\0010\\Pack\\AocMainField.pack");

            HasValidModPaths = Enumerable.Repeat(false, ModPaths.Count).ToList();
            bool allModPathsValid = true;
            for (int i = 0; i < ModPaths.Count; i++)
            {
                bool valid = Directory.Exists($"{ModPaths[i]}\\content") || Directory.Exists($"{ModPaths[i]}\\aoc");
                HasValidModPaths[i] = valid;
                if (!valid)
                    allModPathsValid = false;
            }
            if (allModPathsValid)
            {
                ModPaths.Add(@"");
                HasValidModPaths.Add(false);
            }
                

            if (GLContext.ActiveContext != null)
                GLContext.ActiveContext.UpdateViewport = true;
        }
    }
}
