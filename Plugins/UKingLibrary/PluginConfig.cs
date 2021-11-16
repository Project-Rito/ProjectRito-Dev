using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using MapStudio.UI;
using Newtonsoft.Json;

namespace UKingLibrary
{
    public class PluginConfig : IPluginConfig
    {
        [JsonProperty]
        static string GamePath = @"";
        [JsonProperty]
        static string UpdatePath = @"";
        [JsonProperty]
        static string AocPath = "";

        static bool HasValidGamePath;
        static bool HasValidUpdatePath;

        //Only load the config once when this constructor is activated.
        internal static bool init = false;

        public PluginConfig() { init = true; }

        public void DrawUI()
        {
            if (ImguiCustomWidgets.PathSelector("BOTW Game Path", ref GamePath, HasValidGamePath))
                Save();

            if (ImguiCustomWidgets.PathSelector("BOTW Update Path", ref UpdatePath, HasValidUpdatePath))
                Save();
        }

        public static string GetContentPath(string relativePath)
        {
            //DLC content
            if (File.Exists($"{AocPath}\\{relativePath}"))    return $"{AocPath}\\{relativePath}";
            //Update content
            if (File.Exists($"{UpdatePath}\\{relativePath}")) return $"{UpdatePath}\\{relativePath}";
            //Base game content
            if (File.Exists($"{GamePath}\\{relativePath}"))   return $"{GamePath}\\{relativePath}";

            return relativePath;
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
        }
    }
}
