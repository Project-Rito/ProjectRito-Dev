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
            GLFrameworkEngine.GLContext.PreviewScale = 50;

            MubinPlugin = plugin;
            DungeonName = Path.GetFileNameWithoutExtension(plugin.FileInfo.FileName);

            //Load dungeon data
            DungeonData = new SARC();
            DungeonData.Load(stream);

            //Load mubin data from sarc
            ProcessLoading.Instance.Update(20, 100, "Loading map units");
            TeraTreeInstances = new ProdInfo(new MemoryStream(GetBlwp("TeraTree")));
            ClusteringInstances = new ProdInfo(new MemoryStream(GetBlwp("Clustering")));

            ProcessLoading.Instance.Update(0, 100, "Loading map files.");

            //Global actor list
            GlobalData.LoadActorDatabase();

            //Static and dynamic actors
            MapFiles.Add(new MapData(new MemoryStream(GetMubin("Static")), $"{DungeonName}_Static.smubin"));
            MapFiles.Add(new MapData(new MemoryStream(GetMubin("Dynamic")), $"{DungeonName}_Dynamic.smubin"));

            //Load into muunt editor
            editor.Load(MapFiles);
            //editor.LoadProd(ClusteringInstances, $"{DungeonName}_Clustering.sblwp");
            //editor.LoadProd(TeraTreeInstances, $"{DungeonName}_TeraTree.sblwp");

            //Load model data into editor
            var render = new BfresRender(new MemoryStream(GetModel()), $"DgnMrgPrt_{DungeonName}.sbfres", null);
            render.Textures = BfresLoader.GetTextures(new MemoryStream(GetTexture()));
            render.CanSelect = false;
            GLFrameworkEngine.GLContext.ActiveContext.Scene.AddRenderObject(render);

            render.IsVisibleCallback += delegate
            {
              return MapMuuntEditor.ShowMapModel;
            };
        }

        private byte[] GetModel()
        {
            var data = DungeonData.SarcData.Files[$"Model/DgnMrgPrt_{DungeonName}.sbfres"];
            return Toolbox.Core.IO.YAZ0.Decompress(data);
        }

        private byte[] GetTexture()
        {
            var data = DungeonData.SarcData.Files[$"Model/DgnMrgPrt_{DungeonName}.Tex.sbfres"];
            return Toolbox.Core.IO.YAZ0.Decompress(data);
        }

        private byte[] GetMubin(string type) {
            var data = DungeonData.SarcData.Files[$"Map/CDungeon/{DungeonName}/{DungeonName}_{type}.smubin"];
            return Toolbox.Core.IO.YAZ0.Decompress(data);
        }

        private byte[] GetBlwp(string type)
        {
            var data = DungeonData.SarcData.Files[$"Map/CDungeon/{DungeonName}/{DungeonName}_{type}.sblwp"];
            return Toolbox.Core.IO.YAZ0.Decompress(data);
        }

        public void Save(Stream stream)
        {

        }

        public void Dispose()
        {
        }
    }
}
