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
        public MuuntByamlPlugin MubinPlugin;

        List<MapData> MapFiles = new List<MapData>();

        public SARC DungeonData;

        public ProdInfo ClusteringInstances;
        public ProdInfo TeraTreeInstances;

        public static Dictionary<string, dynamic> Actors = new Dictionary<string, dynamic>();

        private string DungeonName;

        public void Load(MuuntByamlPlugin plugin, MapMuuntEditor editor, Stream stream)
        {
            GLFrameworkEngine.GLContext.PreviewScale = 25;

            MubinPlugin = plugin;
            DungeonName = Path.GetFileNameWithoutExtension(plugin.FileInfo.FileName);

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
                MapFiles.Add(new MapData(new MemoryStream(GetMubin("Static")), $"{DungeonName}_Static.smubin"));
            var dynamicFile = GetMubin("Dynamic");
            if (dynamicFile != null)
                MapFiles.Add(new MapData(new MemoryStream(GetMubin("Dynamic")), $"{DungeonName}_Dynamic.smubin"));

            if (dynamicFile == null && staticFile == null)
                StudioLogger.WriteErrorException("yeah umm... can't really find any mubins....");

            //Load into muunt editor
            editor.Load(MapFiles);
            //editor.LoadProd(ClusteringInstances, $"{DungeonName}_Clustering.sblwp");
            //editor.LoadProd(TeraTreeInstances, $"{DungeonName}_TeraTree.sblwp");

            //Load model data into editor
            var dungeonModel = GetModel();
            if (dungeonModel != null)
            {
                var render = new BfresRender(new MemoryStream(GetModel()), $"DgnMrgPrt_{DungeonName}.sbfres", null);
                render.Textures = BfresLoader.GetTextures(new MemoryStream(GetTexture()));
                render.CanSelect = false;
                GLFrameworkEngine.GLContext.ActiveContext.Scene.AddRenderObject(render);

                render.IsVisibleCallback += delegate
                {
                    return MapMuuntEditor.ShowMapModel;
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
            DungeonData.SarcData.Files.TryGetValue($"Map/CDungeon/{DungeonName}/{DungeonName}_{type}.smubin", out data);
            DungeonData.SarcData.Files.TryGetValue($"Map/MainFieldDungeon/{DungeonName}/{DungeonName}_{type}.smubin", out data);
            if (data != null)
                return Toolbox.Core.IO.YAZ0.Decompress(data);
            return null;
        }

        private byte[] GetBlwp(string type)
        {
            byte[] data;
            DungeonData.SarcData.Files.TryGetValue($"Map/CDungeon/{DungeonName}/{DungeonName}_{type}.sblwp", out data);
            DungeonData.SarcData.Files.TryGetValue($"Map/MainFieldDungeon/{DungeonName}/{DungeonName}_{type}.sblwp", out data);
            if (data != null)
                return Toolbox.Core.IO.YAZ0.Decompress(data);
            return null;
        }

        public void Save(Stream stream)
        {

        }

        public void Dispose()
        {
        }
    }
}
