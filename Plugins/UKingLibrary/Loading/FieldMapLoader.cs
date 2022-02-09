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

        static string SectionName;

        public void Load(MuuntByamlPlugin plugin, MapMuuntEditor editor, Stream stream)
        {
            FieldCollisionLoader load = new FieldCollisionLoader();
            load.Load(new MemoryStream(YAZ0.Decompress(PluginConfig.GetContentPath("Physics/StaticCompound/MainField/A-1-0.shksc"))));
            MubinPlugin = plugin;

            GLFrameworkEngine.GLContext.PreviewScale = 25;

            ProcessLoading.Instance.Update(0, 100, "Loading map files");

            string fileName = plugin.FileInfo.FileName;
            SectionName = fileName.Substring(0, 3);

            bool IsDungeon = fileName.Contains("Dungeon");
            if (!IsDungeon)
            {
                SectionIDs = GetSectionIndex(SectionName);
                CacheBackgroundFiles();

                Terrain.LoadTerrainSection((int)SectionIDs.X, (int)SectionIDs.Y, PluginConfig.MaxTerrainLOD);
            }

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
                var path = PluginConfig.GetContentPath($"Map/{PluginConfig.FieldName}/{SectionName}/{SectionName}.{id}_Clustering.sblwp");
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
            foreach (var map in MapFiles)
                map.Save(stream);
        }

        private void CacheBackgroundFiles()
        {
            {
                // Where to cache to
                string cache = PluginConfig.GetCachePath($"Images\\Terrain");
                if (!Directory.Exists(cache))
                {

                    // Get the data from the bfres
                    string path = PluginConfig.GetContentPath("Model\\Terrain.Tex1.sbfres");
                    var texs = CafeLibrary.Rendering.BfresLoader.GetTextures(path);
                    if (texs == null)
                        return;

                    Directory.CreateDirectory(cache); // Ensure that our cache folder exists
                    foreach (var tex in texs)
                    {
                        string outputPath = $"{cache}\\{tex.Key}";
                        if (tex.Value.RenderTexture is GLTexture2D)
                            ((GLTexture2D)tex.Value.RenderTexture).Save(outputPath);
                        else
                            ((GLTexture2DArray)tex.Value.RenderTexture).Save(outputPath);
                    }
                }
            }
            {
                Terrain = new Terrain();
                Terrain.LoadTerrainTable(PluginConfig.FieldName);
            }
            {
                var path = PluginConfig.GetContentPath("Pack\\TitleBG.pack");
                TitleBG = new SARC();
                TitleBG.Load(File.OpenRead(path));
            }
        }

        private byte[] GetTreeProdInfo()
        {
            var data = TitleBG.SarcData.Files[$"Map/{PluginConfig.FieldName}/{SectionName}/{SectionName}_TeraTree.sblwp"];
            return Toolbox.Core.IO.YAZ0.Decompress(data);
        }

        private byte[] GetClusterProdInfo(string id)
        {
            var path = PluginConfig.GetContentPath($"Map/{PluginConfig.FieldName}/{SectionName}/{SectionName}.{id}_Clustering.sblwp");
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