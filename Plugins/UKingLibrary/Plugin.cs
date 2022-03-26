using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Toolbox.Core;
using Toolbox.Core.ViewModels;
using GLFrameworkEngine;
using MapStudio.UI;

namespace UKingLibrary
{
    public class Plugin : IPlugin
    {
        public string Name => "BOTW Map Editor";

        private static bool FirstLoad = true;

        public Plugin()
        {
            //Add global shaders
            GlobalShaders.AddShader("TERRAIN", "Terrain");
            GlobalShaders.AddShader("WATER", "Water");
            GlobalShaders.AddShader("GRASS", "Grass");

            if (FirstLoad)
            {
                Outliner.NewItemContextMenu.MenuItems.Add(new MenuItemModel("UKingEditor", () => { CreateNewUKingFieldEditor(); }));
            }
            FirstLoad = false;

            //Load plugin specific data. This is where the game path is stored.
            if (!PluginConfig.init)
                PluginConfig.Load();

            if (PluginConfig.FirstStartup)
            {
                ActorDocs.Update();

                PluginConfig.FirstStartup = false;
                new PluginConfig().Save();
            } // Get our ActorDocs!
                
        }

        private void CreateNewUKingFieldEditor()
        {
            UKingEditorConfig config = new UKingEditorConfig()
            {
                Editor = "UKingFieldEditor",
                FolderName = "UKingEditor/000"
            };

            MemoryStream stream = new MemoryStream();
            string json = JsonConvert.SerializeObject(config);
            stream.Position = 0;
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();

            UKingEditor editor = new UKingEditor();
            editor.Load(stream);
            Workspace.ActiveWorkspace.Outliner.Nodes.Add(editor.Root);
        }
    }
}