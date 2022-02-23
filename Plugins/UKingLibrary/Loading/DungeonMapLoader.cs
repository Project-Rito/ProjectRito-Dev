using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using ByamlExt.Byaml;
using OpenTK;
using MapStudio.UI;
using CafeLibrary;
using CafeLibrary.Rendering;
using Toolbox.Core;

namespace UKingLibrary
{
    public class DungeonMapLoader
    {
        public List<MapData> MapFiles = new List<MapData>();

        public SARC DungeonData;

        public ProdInfo ClusteringInstances;
        public ProdInfo TeraTreeInstances;

        public BfresRender MapRender;

        public static Dictionary<string, dynamic> Actors = new Dictionary<string, dynamic>();

        private string DungeonName;

        public void Load(UKingEditor editor, string fileName, Stream stream)
        {
            GLFrameworkEngine.GLContext.PreviewScale = 25;

            DungeonName = Path.GetFileNameWithoutExtension(fileName);

            //Load dungeon data
            DungeonData = new SARC();
            DungeonData.Load(stream);

            //Load mubin data from sarc
            ProcessLoading.Instance.Update(20, 100, "Loading map units");
            var teraTree = GetBlwp("TeraTree");
            if (teraTree != null)
                TeraTreeInstances = new ProdInfo(new MemoryStream(teraTree));
            var clustering = GetBlwp("Clustering");
            if (clustering != null)
                ClusteringInstances = new ProdInfo(new MemoryStream(clustering));

            ProcessLoading.Instance.Update(0, 100, "Loading map files.");

            //Global actor list
            GlobalData.LoadActorDatabase();

            //Static and dynamic actors
            var staticFile = GetMubin("Static");
            if (staticFile != null)
                MapFiles.Add(new MapData(new MemoryStream(GetMubin("Static")), editor, $"{DungeonName}_Static.smubin"));
            var dynamicFile = GetMubin("Dynamic");
            if (dynamicFile != null)
                MapFiles.Add(new MapData(new MemoryStream(GetMubin("Dynamic")), editor, $"{DungeonName}_Dynamic.smubin"));

            if (dynamicFile == null && staticFile == null)
                StudioLogger.WriteErrorException("yeah umm... can't really find any mubins....");

            //editor.LoadProd(ClusteringInstances, $"{DungeonName}_Clustering.sblwp");
            //editor.LoadProd(TeraTreeInstances, $"{DungeonName}_TeraTree.sblwp");

            //Load model data into editor
            var dungeonModel = GetModel();
            if (dungeonModel != null)
            {
                MapRender = new BfresRender(new MemoryStream(GetModel()), $"DgnMrgPrt_{DungeonName}.sbfres", null);
                MapRender.Textures = BfresLoader.GetTextures(new MemoryStream(GetTexture()));
                MapRender.CanSelect = false;
                MapRender.IsVisibleCallback += delegate
                {
                    return MapData.ShowMapModel;
                };
            }
        }

        private byte[] GetModel()
        {
            byte[] data;
            DungeonData.SarcData.Files.TryGetValue($"Model/DgnMrgPrt_{DungeonName}.sbfres", out data);
            if (data != null)
                return Toolbox.Core.IO.YAZ0.Decompress(data);
            return null;
        }

        private byte[] GetTexture()
        {
            byte[] data;
            DungeonData.SarcData.Files.TryGetValue($"Model/DgnMrgPrt_{DungeonName}.Tex.sbfres", out data);
            if (data != null)
                return Toolbox.Core.IO.YAZ0.Decompress(data);
            return null;
        }

        private byte[] GetMubin(string type) {
            byte[] data;
            if (DungeonData.SarcData.Files.TryGetValue($"Map/CDungeon/{DungeonName}/{DungeonName}_{type}.smubin", out data))
                return Toolbox.Core.IO.YAZ0.Decompress(data);

            if (DungeonData.SarcData.Files.TryGetValue($"Map/MainFieldDungeon/{DungeonName}/{DungeonName}_{type}.smubin", out data))
                return Toolbox.Core.IO.YAZ0.Decompress(data);
            return null;
        }

        private byte[] GetBlwp(string type)
        {
            byte[] data;
            if (DungeonData.SarcData.Files.TryGetValue($"Map/CDungeon/{DungeonName}/{DungeonName}_{type}.sblwp", out data))
                return Toolbox.Core.IO.YAZ0.Decompress(data);

            if (DungeonData.SarcData.Files.TryGetValue($"Map/MainFieldDungeon/{DungeonName}/{DungeonName}_{type}.sblwp", out data))
                return Toolbox.Core.IO.YAZ0.Decompress(data);
            return null;
        }

        private byte[] TryDecompressFile(byte[] data)
        {
            if (data == null) return null;

            return Toolbox.Core.IO.YAZ0.Decompress(data);
        }

        public void Save(Stream stream)
        {

        }

        public void Dispose()
        {
            foreach (var file in MapFiles)
                file?.Dispose();
            MapRender?.Dispose();
        }
    }
}
