using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Nintendo.Byml;
using OpenTK;
using UKingLibrary.UI;
using MapStudio.UI;
using CafeLibrary;
using CafeLibrary.Rendering;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;
using GLFrameworkEngine;

namespace UKingLibrary
{
    public class DungeonMapLoader : IMapLoader
    {
        /// <summary>
        /// The preview scale to use for everything loaded by this.
        /// </summary>
        public const float PreviewScale = 16;

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
        /// All baked collision for the field. Included here for convenience,
        /// but references are also present in MapCollisionLoader.RootNode.Tag.
        /// </summary>
        public List<MapCollisionLoader> BakedCollision { get; set; } = new List<MapCollisionLoader>();

        /// <summary>
        /// Instancing info related to trees.
        /// </summary>
        public ProdInfo TeraTreeInstances;

        /// <summary>
        /// Instancing info related to clusters.
        /// </summary>
        public ProdInfo ClusteringInstances;


        public SARC DungeonData;
        public BfresRender MapRender;

        public static Dictionary<string, dynamic> Actors = new Dictionary<string, dynamic>();

        private string DungeonName;

        public DungeonMapLoader()
        {
            RootNode.Header = "Some random dungeon, idk.";
            RootNode.Tag = this;
            RootNode.FolderChildren = new Dictionary<string, NodeBase>
                {
                    { "Muunt", new NodeFolder("Muunt") },
                    { "Collision", new NodeFolder("Collision") },
                    { "NavMesh", new NodeFolder("NavMesh") },
                };

            Scene.Init();
        }

        public void Load(string fileName, Stream stream, UKingEditor editor)
        {
            ParentEditor = editor;
            //ParentEditor.Workspace.Windows.Add(new ActorLinkNodeUI());
            DungeonName = Path.GetFileNameWithoutExtension(fileName);

            RootNode.Header = fileName;

            // Load dungeon data
            DungeonData = new SARC();
            DungeonData.Load(stream, fileName);

            // Load mubin data from sarc
            ProcessLoading.Instance.Update(20, 100, "Loading map units");
            var teraTree = GetBlwp("TeraTree");
            if (teraTree != null)
                TeraTreeInstances = new ProdInfo(teraTree);
            var clustering = GetBlwp("Clustering");
            if (clustering != null)
                ClusteringInstances = new ProdInfo(clustering);

            ProcessLoading.Instance.Update(0, 100, "Loading map files.");

            ApplyPreviewScale();
            GlobalData.LoadActorDatabase();

            // Load baked collision data
            MapCollisionLoader bakedCollisionLoader = new MapCollisionLoader();
            bakedCollisionLoader.Load(GetStaticCompound(), $"{DungeonName}.shksc", Scene);
            RootNode.FolderChildren["Collision"].Children.Add(bakedCollisionLoader.RootNode);
            BakedCollision.Add(bakedCollisionLoader);

            // Load navmesh data
            MapNavmeshLoader navmeshLoader = new MapNavmeshLoader();
            navmeshLoader.Load(GetNavmesh(), $"{DungeonName}.shknm2", Vector3.Zero, Scene);
            RootNode.FolderChildren["NavMesh"].Children.Add(navmeshLoader.RootNode);

            //Static and dynamic actors
            var staticFile = GetMubin("Static");
            if (staticFile != null)
                MapData.Add(new MapData(GetMubin("Static"), this, $"{DungeonName}_Static.smubin")); // TODO - REENABLE
            var dynamicFile = GetMubin("Dynamic");
            if (dynamicFile != null)
                MapData.Add(new MapData(GetMubin("Dynamic"), this, $"{DungeonName}_Dynamic.smubin")); // TODO - REENABLE

                if (dynamicFile == null && staticFile == null)
                StudioLogger.WriteErrorException("yeah umm... can't really find any mubins....");

            RootNode.Header = fileName;
            foreach (var mapFile in MapData)
                RootNode.FolderChildren["Muunt"].AddChild(mapFile.RootNode);

            //editor.LoadProd(ClusteringInstances, $"{DungeonName}_Clustering.sblwp");
            //editor.LoadProd(TeraTreeInstances, $"{DungeonName}_TeraTree.sblwp");

            // Load model data into editor
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
                    return UKingLibrary.MapData.ShowMapModel;
                };
                Scene.AddRenderObject(MapRender);
            }
            else
                StudioLogger.WriteWarning("Couldn't find dungeon model!");
        }

        public void AddBakedCollisionShape(uint hashId, string muuntFileName, BakedCollisionShapeCacheable info, System.Numerics.Vector3 translation, System.Numerics.Quaternion rotation, System.Numerics.Vector3 scale)
        {
            BakedCollision[0].AddShape(info, hashId, translation, rotation, scale);
        }

        public void RemoveBakedCollisionShape(uint hashId)
        {
            BakedCollision[0].RemoveShape(hashId);
        }

        public bool BakedCollisionShapeExists(uint hashId)
        {
            return BakedCollision[0].ShapeExists(hashId);
        }

        public bool UpdateBakedCollisionShapeTransform(uint hashId, System.Numerics.Vector3 translation, System.Numerics.Quaternion rotation, System.Numerics.Vector3 scale)
        {
            return BakedCollision[0].UpdateShapeTransform(hashId, translation, rotation, scale);
        }

        public MapObject MapObjectByHashId(uint hashId)
        {
            foreach (MapData mapData in MapData)
            {
                if (mapData.Objs.ContainsKey(hashId))
                    return mapData.Objs[hashId];
            }
            return null;
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

        private Stream GetStaticCompound()
        {
            byte[] data;
            if (DungeonData.SarcData.Files.TryGetValue($"Physics/StaticCompound/CDungeon/{DungeonName}.shksc", out data))
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));

            if (DungeonData.SarcData.Files.TryGetValue($"Physics/StaticCompound/MainFieldDungeon/{DungeonName}.shksc", out data))
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));
            return null;
        }

        private Stream GetNavmesh()
        {
            byte[] data;
            if (DungeonData.SarcData.Files.TryGetValue($"NavMesh/CDungeon/{DungeonName}/{DungeonName}.shknm2", out data))
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));

            if (DungeonData.SarcData.Files.TryGetValue($"Physics/MainFieldDungeon/{DungeonName}/{DungeonName}.shknm2", out data))
                return new MemoryStream(Toolbox.Core.IO.YAZ0.Decompress(data));
            return null;
        }

        private byte[] TryDecompress(byte[] data) // Why aren't we using this yet? For stuff we save we wanna run into an error trying to open it if we're not gonna save it the same way.
        {
            if (data == null) return null;

            return YAZ0.Decompress(data);
        }

        public void Save(string savePath)
        {
            foreach (EditableObject obj in Scene.DeletedObjects)
            {
                if (obj.UINode.Tag is MapObject)
                {
                    MapObject mapObject = (MapObject)obj.UINode.Tag;

                    RemoveBakedCollisionShape(mapObject.HashId);

                    foreach (MapObject.LinkInstance link in mapObject.DestLinks)
                    {
                        link.Object.SourceLinks.RemoveAll(x => x.Object == mapObject);
                        link.Object.Render.SourceObjectLinks.RemoveAll(x => x == mapObject.Render);
                    }
                    foreach (MapObject.LinkInstance link in mapObject.SourceLinks)
                    {
                        link.Object.DestLinks.RemoveAll(x => x.Object == mapObject);
                        link.Object.Render.DestObjectLinks.RemoveAll(x => x == mapObject.Render);
                    }
                }
            }
            Scene.DeletedObjects.RemoveAll(x => x.UINode.Tag is MapObject);

            foreach (MapData mapData in MapData)
            {
                MemoryStream mapStream = new MemoryStream();
                mapData.Save(mapStream);

                DungeonData.SarcData.Files[$"Map/CDungeon/{DungeonName}/{mapData.RootNode.Header}"] = YAZ0.Compress(mapStream.ToArray()); // Todo preserve CDungeon or MainFieldDungeon as string for proper saving.
            }
            foreach (MapCollisionLoader loader in BakedCollision)
            {
                string fileName = loader.RootNode.Header;

                MemoryStream memoryStream = new MemoryStream();
                loader.Save(memoryStream);

                DungeonData.SarcData.Files[$"Physics/StaticCompound/CDungeon/{DungeonName}.shksc"] = YAZ0.Compress(memoryStream.ToArray());
            }

            Directory.CreateDirectory(Path.Join(savePath, "content/Pack"));
            FileStream fileStream = File.Open(Path.Join(savePath, $"content/Pack/{RootNode.Header}"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

            DungeonData.Save(fileStream);

            fileStream.Close();
        }

        public static void ApplyPreviewScale()
        {
            GLFrameworkEngine.GLContext.PreviewScale = PreviewScale;
        }

        public void Dispose()
        {
            foreach (MapData mapData in MapData)
                mapData?.Dispose();
            MapRender?.Dispose();
        }
    }
}
