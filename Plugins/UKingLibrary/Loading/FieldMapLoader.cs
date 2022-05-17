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
using Toolbox.Core.ViewModels;
using GLFrameworkEngine;

namespace UKingLibrary
{
    public class FieldMapLoader : IMapLoader
    {
        /// <summary>
        /// The preview scale to use for everything loaded by this.
        /// </summary>
        public const float PreviewScale = 25;

        /// <summary>
        /// The editor that this is associated with.
        /// </summary>
        public UKingEditor ParentEditor { get; set; }

        /// <summary>
        /// The scene associated with the field.
        /// </summary>
        public GLScene Scene { get; set; } = new GLScene();

        /// <summary>
        /// The node associated with the field. Duh.
        /// </summary>
        public NodeFolder RootNode { get; set; } = new NodeFolder();


        /// <summary>
        /// All loaded MapData. Included here for convenience,
        /// but MapData is also present in RootNode as a tag.
        /// </summary>
        public List<MapData> MapData { get; set; } = new List<MapData>();

        /// <summary>
        /// The terrain for this field
        /// </summary>
        public Terrain Terrain { get; set; } = new Terrain();

        /// <summary>
        /// Instancing info related to trees.
        /// </summary>
        public ProdInfo TreeInstancesInfo;

        /// <summary>
        /// Instancing info related to clusters.
        /// </summary>
        public List<ProdInfo> Clusters = new List<ProdInfo>();

        public FieldMapLoader(string fieldName)
        {
            RootNode.Header = fieldName;
            RootNode.Tag = this;
            Scene.Init();

            Terrain.LoadTerrainTable(fieldName);
        }

        public MapData LoadMuunt(string fileName, Stream stream, UKingEditor editor)
        {
            ParentEditor = editor;
            ParentEditor.Workspace.Windows.Add(new ActorLinkNodeUI());

            ProcessLoading.Instance.Update(0, 100, "Loading map files");

            ApplyPreviewScale();
            GlobalData.LoadActorDatabase();

            var mapData = new MapData(stream, this, fileName);
            RootNode.TryAddChild(new NodeFolder(GetSectionName(fileName)));
            RootNode.FolderChildren[GetSectionName(fileName)].AddChild(mapData.RootNode);
            MapData.Add(mapData);

            return mapData;
        }

        public void LoadTerrain(string fieldName, int areaID, int sectionID, int lodLevel = 8)
        {
            ApplyPreviewScale();
            Terrain.LoadTerrainSection(fieldName, areaID, sectionID, this, lodLevel);
        }

        public static string GetSectionName(string fileName)
        {
            return fileName.Substring(0, 3);
        }

        public void Save(string savePath)
        {
            foreach (NodeFolder sectionFolder in RootNode.Children)
            {
                foreach (NodeBase file in sectionFolder.Children)
                {
                    string fileName = file.Header;
                    MapData mapData = (MapData)file.Tag;

                    MemoryStream saveStream = new MemoryStream();
                    mapData.Save(saveStream);

                    Directory.CreateDirectory(Path.Join(savePath, $"aoc/0010/Map/{RootNode.Header}/{GetSectionName(fileName)}"));
                    FileStream fileStream = File.Open(Path.Join(savePath, $"aoc/0010/Map/{RootNode.Header}/{GetSectionName(fileName)}/{fileName}"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

                    saveStream.WriteTo(fileStream);

                    fileStream.SetLength(fileStream.Position);
                    fileStream.Flush();
                    fileStream.Close();
                }
            }
        }

        /*
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
        */

        public static void ApplyPreviewScale()
        {
            GLFrameworkEngine.GLContext.PreviewScale = PreviewScale;
        }

        public void Dispose()
        {
            foreach (MapData mapData in MapData)
                mapData.Dispose();
        }
    }
}