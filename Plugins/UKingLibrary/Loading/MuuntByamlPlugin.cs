using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using MapStudio.UI;
using Toolbox.Core;
using Toolbox.Core.ViewModels;
using ByamlExt.Byaml;

namespace UKingLibrary
{
    public class MuuntByamlPlugin : NodeBase, IFileFormat, ICustomFileEditor, IDisposable
    {
        public string[] Description => new string[] { "Compressed Map Unit Binary" };
        public string[] Extension => new string[] { "*.smubin", ".pack" };

        /// <summary>
        /// Whether or not the file can be saved.
        /// </summary>
        public bool CanSave { get; set; } = true;

        /// <summary>
        /// Information of the loaded file.
        /// </summary>
        public File_Info FileInfo { get; set; }

        public IEditor Editor { get; set; } = new MapMuuntEditor();

        FieldMapLoader FieldMapLoader;
        DungeonMapLoader DungeonLoader;

        public bool Identify(File_Info fileInfo, Stream stream) {
            //Just load maps from checking the smubin extension atm.
            return fileInfo.Extension == ".smubin" || fileInfo.Extension == ".pack";
        }

        public void Load(Stream stream)
        {
            Workspace.ActiveWorkspace.ActiveEditor = Editor;

            if (FileInfo.Extension == ".pack")
            {
                DungeonLoader = new DungeonMapLoader();
                DungeonLoader.Load(this, (MapMuuntEditor)Editor, stream);
            }
            else
            {
                FieldMapLoader = new FieldMapLoader();
                FieldMapLoader.Load(this, (MapMuuntEditor)Editor, stream);
            }
            Header = FileInfo.FileName;
        }

        public void Save(Stream stream) {
            Workspace.ActiveWorkspace.ActiveEditor = Editor;
            //Save the editor data
            if (DungeonLoader != null)
                DungeonLoader.Save(stream);
            else
                FieldMapLoader.Save(stream, (MapMuuntEditor)Editor);
        }

        public void Dispose()
        {
            ((MapMuuntEditor)Editor).Dispose();
            if (DungeonLoader != null)
                DungeonLoader.Dispose();
            if (FieldMapLoader != null)
                FieldMapLoader.Dispose();
        }
    }
}
