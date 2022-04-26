using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using ByamlExt.Byaml;
using OpenTK;
using UKingLibrary.UI;
using MapStudio.UI;
using CafeLibrary;
using Toolbox.Core.IO;
using GLFrameworkEngine;

namespace UKingLibrary
{
    public class FieldMapLoader
    {
        public MapData MapFile;

        /// <summary>
        /// Instancing info related to trees.
        /// </summary>
        public ProdInfo TreeInstancesInfo;

        public List<ProdInfo> Clusters = new List<ProdInfo>();

        static string SectionName;

        public MapData Load(UKingEditor editor, string fileName, Stream stream)
        {
            GLFrameworkEngine.GLContext.PreviewScale = 25;

            ProcessLoading.Instance.Update(0, 100, "Loading map files");

            SectionName = fileName.Substring(0, 3);

            GlobalData.LoadActorDatabase();

            var mapData = new MapData(stream, editor, fileName);
            MapFile = mapData;
            Workspace.ActiveWorkspace.Windows.Add(new ActorLinkNodeUI());

            return mapData;
        }

        private void LoadProdInfo(string fieldName, UKingEditor editor)
        {
            TreeInstancesInfo = new ProdInfo(new MemoryStream(GetTreeProdInfo(fieldName, editor)));
            editor.LoadProd(TreeInstancesInfo, $"{SectionName}_TeraTree.sblwb");

            Clusters.Clear();
            string[] prodIDs = new string[] { "00", "01", "10", "11" };
            foreach (var id in prodIDs)
            {
                var path = PluginConfig.GetContentPath($"Map/{fieldName}/{SectionName}/{SectionName}.{id}_Clustering.sblwp");
                if (!File.Exists(path))
                    continue;

                var decomp = Toolbox.Core.IO.YAZ0.Decompress(path);
                var prodInfo = new ProdInfo(new MemoryStream(decomp));
                Clusters.Add(prodInfo);

                editor.LoadProd(prodInfo, $"{SectionName}.{id}_Clustering.sblwp");
            }
        }

        public void Save(Stream stream)
        {
            //Save the map data
            MapFile.Save(stream);
        }

        private byte[] GetTreeProdInfo(string fieldName, UKingEditor editor)
        {
            var data = editor.TitleBG.SarcData.Files[$"Map/{fieldName}/{SectionName}/{SectionName}_TeraTree.sblwp"];
            return Toolbox.Core.IO.YAZ0.Decompress(data);
        }

        private byte[] GetClusterProdInfo(string fieldName, string id)
        {
            var path = PluginConfig.GetContentPath($"Map/{fieldName}/{SectionName}/{SectionName}.{id}_Clustering.sblwp");
            return Toolbox.Core.IO.YAZ0.Decompress(path);
        }

        public void Dispose()
        {
            MapFile?.Dispose();
        }
    }
}