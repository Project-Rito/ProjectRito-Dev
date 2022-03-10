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
        FieldCollisionLoader FieldCollisionLoader;

        public bool Identify(File_Info fileInfo, Stream stream) {
            //Just load maps from checking the smubin extension atm.
            return fileInfo.Extension == ".smubin" || fileInfo.Extension == ".pack";
        }

        public void Load(Stream stream)
        {
            FieldCollisionLoader = new FieldCollisionLoader();
            
            /* HKX2 testing code
            var cStream = File.Open("A-1-1.shksc", FileMode.Open);
            FieldCollisionLoader.Load(cStream);
            var shapes = FieldCollisionLoader.GetShapes(19687114);
            foreach (var shape in shapes)
            {
                FieldCollisionLoader.AddShape(shape, System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(-3824.655f, 392.795f, -3590.277f)), 4246760621);
            }
            cStream.Seek(0, SeekOrigin.Begin);
            FieldCollisionLoader.Save(cStream);
            */

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
            //Save the editor data
            if (DungeonLoader != null)
                DungeonLoader.Save(stream);
            else
                FieldMapLoader.Save(stream);
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
