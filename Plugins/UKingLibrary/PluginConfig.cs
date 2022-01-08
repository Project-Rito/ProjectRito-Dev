using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using MapStudio.UI;
using Newtonsoft.Json;
using ImGuiNET;

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

        static bool HasValidGamePath;
        static bool HasValidUpdatePath;
        static bool HasValidAocPath;

        [JsonProperty]
        public static string FieldName = @"MainField";

        [JsonProperty]
        public static int MaxTerrainLOD = 5;

        //Only load the config once when this constructor is activated.
        internal static bool init = false;

        public PluginConfig() { init = true; }

        public void DrawUI()
        {
            if (ImguiCustomWidgets.PathSelector("BOTW Game Path", ref GamePath, HasValidGamePath))
                Save();

            if (ImguiCustomWidgets.PathSelector("BOTW Update Path", ref UpdatePath, HasValidUpdatePath))
                Save();

            if (ImguiCustomWidgets.PathSelector("BOTW DLC Path", ref AocPath, HasValidAocPath))
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

            if (ImGui.SliderInt("Max Terrain Detail", ref MaxTerrainLOD, 0, 7))
            {
                Save();
            }
        }

        public static string GetContentPath(string relativePath)
        {
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
        }
    }
}
