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

namespace UKingLibrary
{
    public class FieldMapLoader
    {
        public MuuntByamlPlugin MubinPlugin;

        public List<MapData> MapFiles = new List<MapData>();
        public SARC TitleBG;
        public Terrain Terrain;

        /// <summary>
        /// Instancing info related to trees.
        /// </summary>
        public ProdInfo TreeInstancesInfo;

        public List<ProdInfo> Clusters = new List<ProdInfo>();

        public Vector2 SectionIDs = new Vector2(0, 0);

        //Todo it would be ideal to generate these real time via quad trees
        //But for editing map regions it is not necessary atm.
        const int DISPLAY_LOD = 5;

        static string SectionName;

        public void Load(MuuntByamlPlugin plugin, MapMuuntEditor editor, Stream stream)
        {
            MubinPlugin = plugin;

            GLFrameworkEngine.GLContext.PreviewScale = 50;

            ProcessLoading.Instance.Update(0, 100, "Loading map files.");

            string fileName = plugin.FileInfo.FileName;
            SectionName = fileName.Substring(0, 3);

            bool IsDungeon = fileName.Contains("Dungeon");
            if (!IsDungeon)
            {
                SectionIDs = GetSectionIndex(SectionName);
                CacheBackgroundFiles();

                Terrain.LoadTerrainSection((int)SectionIDs.X, (int)SectionIDs.Y, DISPLAY_LOD);
            }

            ProcessLoading.Instance.Update(60, 100, "Loading map units");

            GlobalData.LoadActorDatabase();

            var mapData = new MapData(stream, plugin.FileInfo.FileName);
            MapFiles.Add(mapData);

            editor.Load(MapFiles);
            //LoadProdInfo(editor);

            Workspace.ActiveWorkspace.Windows.Add(new ActorLinkNodeUI());
        }

        private void LoadProdInfo(MapMuuntEditor editor)
        {
            TreeInstancesInfo = new ProdInfo(new MemoryStream(GetTreeProdInfo()));
            editor.LoadProd(TreeInstancesInfo, $"{SectionName}_TeraTree.sblwb");

            Clusters.Clear();
            string[] prodIDs = new string[] { "00", "01", "10", "11" };
            foreach (var id in prodIDs)
            {
                var path = PluginConfig.GetContentPath($"Map/MainField/{SectionName}/{SectionName}.{id}_Clustering.sblwp");
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
           // foreach (var map in MapFiles)
              //  map.Save();
        }

        private void CacheBackgroundFiles()
        {
            Terrain = new Terrain();
            Terrain.LoadTerrainTable();

            var path = PluginConfig.GetContentPath("Pack\\TitleBG.pack");
            TitleBG = new SARC();
            TitleBG.Load(File.OpenRead(path));
        }

        private byte[] GetTreeProdInfo()
        {
            var data = TitleBG.SarcData.Files[$"Map/MainField/{SectionName}/{SectionName}_TeraTree.sblwp"];
            return Toolbox.Core.IO.YAZ0.Decompress(data);
        }

        private byte[] GetClusterProdInfo(string id)
        {
            var path = PluginConfig.GetContentPath($"Map/MainField/{SectionName}/{SectionName}.{id}_Clustering.sblwp");
            return Toolbox.Core.IO.YAZ0.Decompress(path);
        }

        private Vector2 GetSectionIndex(string mapName)
        {
            string[] values = mapName.Split("-");
            int sectionID = values[0][0] - 'A' - 1;
            int sectionRegion = int.Parse(values[1]);
            return new Vector2(sectionID, sectionRegion);
        }

        public void Dispose()
        {
            Terrain?.Dispose();
        }
    }
}
