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
        public string Name => TranslationSource.GetText("RITO_EDITOR");

        private static bool FirstLoad = true;

        public Plugin()
        {
            //Add global shaders
            GlobalShaders.AddShader("TERRAIN", "Terrain");
            GlobalShaders.AddShader("WATER", "Water");
            GlobalShaders.AddShader("GRASS", "Grass");
            GlobalShaders.AddShader("HAVOK_SHAPE", "HavokShape");

            if (FirstLoad)
            {
                Outliner.NewItemContextMenu.MenuItems.Add(new MenuItemModel("UKingEditor", () => { CreateNewUKingEditor(); }));
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

        private void CreateNewUKingEditor()
        {
            int editorIndex = GetEditorIndex();

            UKingEditorConfig config = new UKingEditorConfig()
            {
                Editor = "UKingEditor",
                FolderName = $"UKingEditor_{editorIndex.ToString("D3")}/"
            };

            MemoryStream stream = new MemoryStream();
            string json = JsonConvert.SerializeObject(config);
            stream.Position = 0;
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();

            UKingEditor editor = new UKingEditor();
            editor.Load(stream);

            editor.FileInfo.FileName = $"UKingEditor_{editorIndex.ToString("D3")}.json";
            editor.FileInfo.FilePath = Path.Join(GlobalSettings.Current.Program.ProjectDirectory, Workspace.ActiveWorkspace.Name, editor.FileInfo.FileName);

            Workspace.ActiveWorkspace.Resources.ProjectFile.FileAssets.Add(editor.FileInfo.FileName);
            Workspace.ActiveWorkspace.Outliner.Nodes.Add(editor.Root);
            Workspace.ActiveWorkspace.Resources.AddFile(editor);
            Workspace.ActiveWorkspace.ToolWindow.ToolDrawer = editor.ToolWindowDrawer;
        }

        private int GetEditorIndex()
        {
            for (int i = 0; ; i++)
            {
                if (!Workspace.ActiveWorkspace.Resources.ProjectFile.FileAssets.Contains($"UKingEditor_{i.ToString("D3")}.json"))
                    return i;
            }
        }
    }
}