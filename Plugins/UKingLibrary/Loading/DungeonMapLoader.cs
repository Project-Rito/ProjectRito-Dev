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
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;

namespace UKingLibrary
{
    public class DungeonMapLoader
    {
        public List<MapData> MapFiles = new List<MapData>();
        public NodeBase RootNode = new NodeBase();

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
            DungeonData.Load(stream, fileName);

            //Load mubin data from sarc
            ProcessLoading.Instance.Update(20, 100, "Loading map units");
            var teraTree = GetBlwp("TeraTree");
            if (teraTree != null)
                TeraTreeInstances = new ProdInfo(teraTree);
            var clustering = GetBlwp("Clustering");
            if (clustering != null)
                ClusteringInstances = new ProdInfo(clustering);

            ProcessLoading.Instance.Update(0, 100, "Loading map files.");

            //Global actor list
            GlobalData.LoadActorDatabase();

            //Static and dynamic actors
            var staticFile = GetMubin("Static");
            if (staticFile != null)
                MapFiles.Add(new MapData(GetMubin("Static"), editor, $"{DungeonName}_Static.smubin"));
            var dynamicFile = GetMubin("Dynamic");
            if (dynamicFile != null)
                MapFiles.Add(new MapData(GetMubin("Dynamic"), editor, $"{DungeonName}_Dynamic.smubin"));

            if (dynamicFile == null && staticFile == null)
                StudioLogger.WriteErrorException("yeah umm... can't really find any mubins....");

            RootNode = new NodeBase(fileName);
            foreach (var mapFile in MapFiles)
                RootNode.AddChild(mapFile.RootNode);

            //editor.LoadProd(ClusteringInstances, $"{DungeonName}_Clustering.sblwp");
            //editor.LoadProd(TeraTreeInstances, $"{DungeonName}_TeraTree.sblwp");

            //Load model data into editor
            var dungeonModel = GetModel();
            if (dungeonModel != null)
            {
                MapRender = new BfresRender(GetModel(), $"DgnMrgPrt_{DungeonName}.sbfres", null);

                var dungeonTextures = GetTexture();
                if (dungeonTextures != null)
                    BfresLoader.GetTextures(new MemoryStream(YAZ0.Decompress(dungeonTextures.ReadAllBytes()))).ToList().ForEach(x => MapRender.Textures.Add(x.Key, x.Value)); // Merge pack textures
                else
                    StudioLogger.WriteWarning("Couldn't find textures for dungeon model!");
                MapRender.CanSelect = false;
                MapRender.IsVisibleCallback += delegate
                {
                    return MapData.ShowMapModel;
                };
            }
            else
                StudioLogger.WriteWarning("Couldn't find dungeon model!");
        }

        private Stream GetModel()
        {
            byte[] data;
            DungeonData.SarcData.Files.TryGetValue($"Model/DgnMrgPrt_{DungeonName}.sbfres", out data);
            if (data != null)
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));
            return null;
        }

        private Stream GetTexture()
        {
            byte[] data;
            DungeonData.SarcData.Files.TryGetValue($"Model/DgnMrgPrt_{DungeonName}.Tex.sbfres", out data);
            if (data != null)
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));
            DungeonData.SarcData.Files.TryGetValue($"Model/DgnMrgPrt_{DungeonName}.Tex1.sbfres", out data);
            if (data != null)
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));

            if (File.Exists(PluginConfig.GetContentPath($"Model/DgnMrgPrt_{DungeonName}.Tex.sbfres")))
                return File.OpenRead(PluginConfig.GetContentPath($"Model/DgnMrgPrt_{DungeonName}.Tex.sbfres"));
            if (File.Exists(PluginConfig.GetContentPath($"Model/DgnMrgPrt_{DungeonName}.Tex1.sbfres")))
                return File.OpenRead(PluginConfig.GetContentPath($"Model/DgnMrgPrt_{DungeonName}.Tex1.sbfres"));

            return null;
        }

        private Stream GetMubin(string type) {
            byte[] data;
            if (DungeonData.SarcData.Files.TryGetValue($"Map/CDungeon/{DungeonName}/{DungeonName}_{type}.smubin", out data))
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));

            if (DungeonData.SarcData.Files.TryGetValue($"Map/MainFieldDungeon/{DungeonName}/{DungeonName}_{type}.smubin", out data))
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));
            return null;
        }

        private Stream GetBlwp(string type)
        {
            byte[] data;
            if (DungeonData.SarcData.Files.TryGetValue($"Map/CDungeon/{DungeonName}/{DungeonName}_{type}.sblwp", out data))
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));

            if (DungeonData.SarcData.Files.TryGetValue($"Map/MainFieldDungeon/{DungeonName}/{DungeonName}_{type}.sblwp", out data))
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));
            return null;
        }

        private byte[] TryDecompress(byte[] data) // Why aren't we using this yet? For stuff we save we wanna run into an error trying to open it if we're not gonna save it the same way.
        {
            if (data == null) return null;

            return YAZ0.Decompress(data);
        }

        public void Save(Stream stream)
        {
            foreach (var mapFile in MapFiles)
            {
                MemoryStream mapStream = new MemoryStream();
                mapFile.Save(mapStream);

                DungeonData.SarcData.Files[$"Map/CDungeon/{DungeonName}/{mapFile.RootNode.Header}"] = YAZ0.Compress(mapStream.ToArray());
            }
            DungeonData.Save(stream);
        }

        public void Dispose()
        {
            foreach (var file in MapFiles)
                file?.Dispose();
            MapRender?.Dispose();
        }
    }
}
