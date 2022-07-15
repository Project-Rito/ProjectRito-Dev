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

        [JsonProperty]
        public static string CollisionCacheDir = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/MapStudio/UKing/CollisionCache";

        static bool HasValidGamePath;
        static bool HasValidUpdatePath;
        static bool HasValidAocPath;
        static List<bool> HasValidModPaths;

        [JsonProperty]
        public static int MaxTerrainLOD = 5;

        [JsonProperty]
        public static bool DebugTerrainSections = false;

        [JsonProperty]
        public static bool AreasSelectByBorders = true;

        [JsonProperty]
        public static float AreaOpacity = 0.1f;

        [JsonProperty]
        public static bool FirstStartup = true;

        public static Action PathsChanged = null;

        //Only load the config once when this constructor is activated.
        internal static bool init = false;

        public PluginConfig() { init = true; }

        public void DrawUI()
        {
            if (ImGui.Button(TranslationSource.GetText("UPDATE_ACTORDOCS")))
                ActorDocs.Update();

            if (ImGui.BeginMenu($"{TranslationSource.GetText("MOD_PATHS")}##uk_vmenu01"))
            {
                for (int i = 0; i < ModPaths.Count; i++)
                {
                    string path = ModPaths[i];
                    if (ImguiCustomWidgets.PathSelector($"##uk_modpath{i}", ref path, HasValidModPaths[i]))
                    {
                        ModPaths[i] = path;
                        Save();
                        PathsChanged?.Invoke();
                    }
                }

                ImGui.EndMenu();
            }

            if (ImguiCustomWidgets.PathSelector(TranslationSource.GetText("BOTW_GAME_PATH"), ref GamePath, HasValidGamePath))
                Save();

            if (ImguiCustomWidgets.PathSelector(TranslationSource.GetText("BOTW_UPDATE_PATH"), ref UpdatePath, HasValidUpdatePath))
                Save();

            if (ImguiCustomWidgets.PathSelector(TranslationSource.GetText("BOTW_DLC_PATH"), ref AocPath, HasValidAocPath))
                Save();

            // All of this should eventually be moved per-3D-scene
            if (ImGui.BeginMenu($"{TranslationSource.GetText("TERRAIN")}##uk_vmenu02"))
            {
                if (ImGui.SliderInt(TranslationSource.GetText("MAX_DETAIL"), ref MaxTerrainLOD, 0, 7))
                    Save();
#if DEBUG
                if (ImGui.Checkbox(TranslationSource.GetText("DEBUG_SECTIONS"), ref DebugTerrainSections))
                    Save();
#endif
                ImGui.EndMenu();
            }

            ImGui.Checkbox($"{TranslationSource.GetText("AREAS_SELECT_BY_BORDERS")}", ref AreasSelectByBorders);

            ImGui.SliderFloat($"{TranslationSource.GetText("AREA_OPACITY")}", ref AreaOpacity, 0f, 1f, "%.1f");
        }

        public static string GetContentPath(string relativePath)
        {
            //Mod content
            foreach (string modPath in ModPaths) {
                if (File.Exists($"{modPath}/aoc/0010/{relativePath}") || Directory.Exists($"{modPath}/aoc/0010/{relativePath}"))    return $"{modPath}/aoc/0010/{relativePath}";
                if (File.Exists($"{modPath}/aoc/0011/{relativePath}") || Directory.Exists($"{modPath}/aoc/0011/{relativePath}"))    return $"{modPath}/aoc/0011/{relativePath}";
                if (File.Exists($"{modPath}/aoc/0012/{relativePath}") || Directory.Exists($"{modPath}/aoc/0012/{relativePath}"))    return $"{modPath}/aoc/0012/{relativePath}";

                if (File.Exists($"{modPath}/content/{relativePath}") || Directory.Exists($"{modPath}/content/{relativePath}")) return $"{modPath}/content/{relativePath}";
            }

            //DLC content
            if (File.Exists($"{AocPath}/0010/{relativePath}") || Directory.Exists($"{AocPath}/0010/{relativePath}"))    return $"{AocPath}/0010/{relativePath}";
            if (File.Exists($"{AocPath}/0011/{relativePath}") || Directory.Exists($"{AocPath}/0011/{relativePath}"))    return $"{AocPath}/0011/{relativePath}";
            if (File.Exists($"{AocPath}/0012/{relativePath}") || Directory.Exists($"{AocPath}/0012/{relativePath}"))    return $"{AocPath}/0012/{relativePath}";
            //Update content
            if (File.Exists($"{UpdatePath}/{relativePath}") || Directory.Exists($"{UpdatePath}/{relativePath}")) return $"{UpdatePath}/{relativePath}";
            //Base game content
            if (File.Exists($"{GamePath}/{relativePath}") || Directory.Exists($"{GamePath}/{relativePath}"))   return $"{GamePath}/{relativePath}";

            return relativePath;
        }

        /// <summary>
        /// Gets all content paths in an array ordered from base to extension
        /// </summary>
        public static string[] GetContentPaths(string relativePath)
        {
            List<string> relativePaths = new List<string>(1);

            //Mod content
            foreach (string modPath in ModPaths)
            {
                if (File.Exists($"{modPath}/aoc/0010/{relativePath}") || Directory.Exists($"{modPath}/aoc/0010/{relativePath}")) relativePaths.Add($"{modPath}/aoc/0010/{relativePath}");
                if (File.Exists($"{modPath}/aoc/0011/{relativePath}") || Directory.Exists($"{modPath}/aoc/0011/{relativePath}")) relativePaths.Add($"{modPath}/aoc/0011/{relativePath}");
                if (File.Exists($"{modPath}/aoc/0012/{relativePath}") || Directory.Exists($"{modPath}/aoc/0012/{relativePath}")) relativePaths.Add($"{modPath}/aoc/0012/{relativePath}");

                if (File.Exists($"{modPath}/content/{relativePath}") || Directory.Exists($"{modPath}/content/{relativePath}")) relativePaths.Add($"{modPath}/content/{relativePath}");
            }

            //DLC content
            if (File.Exists($"{AocPath}/0010/{relativePath}") || File.Exists($"{AocPath}/0010/{relativePath}")) relativePaths.Add($"{AocPath}/0010/{relativePath}");
            if (File.Exists($"{AocPath}/0011/{relativePath}") || File.Exists($"{AocPath}/0011/{relativePath}")) relativePaths.Add($"{AocPath}/0011/{relativePath}");
            if (File.Exists($"{AocPath}/0012/{relativePath}") || File.Exists($"{AocPath}/0012/{relativePath}")) relativePaths.Add($"{AocPath}/0012/{relativePath}");
            //Update content
            if (File.Exists($"{UpdatePath}/{relativePath}") || Directory.Exists($"{UpdatePath}/{relativePath}")) relativePaths.Add($"{UpdatePath}/{relativePath}");
            //Base game content
            if (File.Exists($"{GamePath}/{relativePath}") || Directory.Exists($"{GamePath}/{relativePath}")) relativePaths.Add($"{GamePath}/{relativePath}");

            relativePath.Reverse();
            return relativePaths.ToArray();
        }

        public static string GetCachePath(string relativePath)
        {
            return $"{Toolbox.Core.Runtime.ExecutableDir}/Cache/{PluginName}/{relativePath}";
        }

        /// <summary>
        /// Loads the config json file on disc or creates a new one if it does not exist.
        /// </summary>
        /// <returns></returns>
        public static PluginConfig Load()
        {
            if (!File.Exists($"{Runtime.ExecutableDir}/UKingConfig.json")) { new PluginConfig().Save(); }

            var config = JsonConvert.DeserializeObject<PluginConfig>(File.ReadAllText($"{Runtime.ExecutableDir}/UKingConfig.json"));
            config.Reload();
            return config;
        }

        /// <summary>
        /// Saves the current configuration to json on disc.
        /// </summary>
        public void Save()
        {
            File.WriteAllText($"{Runtime.ExecutableDir}/UKingConfig.json", JsonConvert.SerializeObject(this));
            Reload();
        }

        /// <summary>
        /// Called when the config file has been loaded or saved.
        /// </summary>
        public void Reload()
        {
            HasValidGamePath = File.Exists($"{GamePath}/Actor/ActorInfo.product.sbyml");
            HasValidUpdatePath = File.Exists($"{UpdatePath}/Actor/ActorInfo.product.sbyml");
            HasValidAocPath = File.Exists($"{AocPath}/0010/Pack/AocMainField.pack");

            HasValidModPaths = Enumerable.Repeat(false, ModPaths.Count).ToList();
            bool allModPathsValid = true;
            for (int i = 0; i < ModPaths.Count; i++)
            {
                bool valid = Directory.Exists($"{ModPaths[i]}/content") || Directory.Exists($"{ModPaths[i]}/aoc");
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
