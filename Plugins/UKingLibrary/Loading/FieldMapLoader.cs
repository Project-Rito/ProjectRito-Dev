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
        /// but references are also present in MapData.RootNode.Tag.
        /// </summary>
        public List<MapData> MapData { get; set; } = new List<MapData>();

        /// <summary>
        /// All baked collision for the field. Included here for convenience,
        /// but references are also present in MapCollisionLoader.RootNode.Tag.
        /// </summary>
        public List<MapCollisionLoader> BakedCollision { get; set; } = new List<MapCollisionLoader>();

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
            //ParentEditor.Workspace.Windows.Add(new ActorLinkNodeUI());

            ProcessLoading.Instance.Update(0, 100, "Loading map files");

            ApplyPreviewScale();
            GlobalData.LoadActorDatabase();

            var mapData = new MapData(stream, this, fileName);
            InitSectionFolder(GetSectionName(fileName));
            ((NodeFolder)RootNode.FolderChildren[GetSectionName(fileName)]).FolderChildren["Muunt"].AddChild(mapData.RootNode);
            MapData.Add(mapData);

            return mapData;
        }

        public void LoadTerrain(string fieldName, int areaID, int sectionID, int lodLevel = 8)
        {
            ApplyPreviewScale();
            Terrain.LoadTerrainSection(fieldName, areaID, sectionID, this, lodLevel);
        }

        public void LoadBakedCollision(string fileName, Stream stream)
        {
            if (RootNode.FolderChildren.ContainsKey(GetSectionName(fileName)))
                if (((NodeFolder)RootNode.FolderChildren[GetSectionName(fileName)]).FolderChildren["Collision"].Children.Any(x => x.Header == fileName))
                    return;

            MapCollisionLoader loader = new MapCollisionLoader();
            loader.Load(stream, fileName);

            InitSectionFolder(GetSectionName(fileName));
            ((NodeFolder)RootNode.FolderChildren[GetSectionName(fileName)]).FolderChildren["Collision"].AddChild(loader.RootNode);
            BakedCollision.Add(loader);
        }

        public void LoadNavmesh(string fileName, Stream stream, Vector3 origin)
        {
            string sectionName = new FieldSectionInfo(origin).Name;

            if (RootNode.FolderChildren.ContainsKey(sectionName))
                if (((NodeFolder)RootNode.FolderChildren[sectionName]).FolderChildren["NavMesh"].Children.Any(x => x.Header == fileName))
                    return;

            MapNavmeshLoader loader = new MapNavmeshLoader();
            loader.Load(stream, fileName, origin, Scene);

            InitSectionFolder(sectionName);
            ((NodeFolder)RootNode.FolderChildren[sectionName]).FolderChildren["NavMesh"].AddChild(loader.RootNode);
        }

        public void AddBakedCollisionShape(uint hashId, string muuntFileName, BakedCollisionShapeCacheable info, System.Numerics.Vector3 translation, System.Numerics.Quaternion rotation, System.Numerics.Vector3 scale)
        {
            FieldSectionInfo section = new FieldSectionInfo(muuntFileName);
            int quadIndex = section.GetQuadIndex(translation);

            ((MapCollisionLoader)((NodeFolder)RootNode.FolderChildren[GetSectionName(muuntFileName)]).FolderChildren["Collision"].Children[quadIndex].Tag).AddShape(info, hashId, translation, rotation, scale);
        }

        public void RemoveBakedCollisionShape(uint hashId)
        {
            foreach (MapCollisionLoader bakedCollision in BakedCollision)
                bakedCollision.RemoveShape(hashId);
        }

        public bool BakedCollisionShapeExists(uint hashId)
        {
            return (BakedCollision.Any(x => x.ShapeExists(hashId)));
        }

        public bool UpdateBakedCollisionShapeTransform(uint hashId, System.Numerics.Vector3 translation, System.Numerics.Quaternion rotation, System.Numerics.Vector3 scale)
        {
            FieldSectionInfo section = GetBakedCollisionShapeSection(hashId);
            if (section == null)
                return false;
            int quadIndex = section.GetQuadIndex(translation);

            if (BakedCollision[quadIndex].UpdateShapeTransform(hashId, translation, rotation, scale))
                return true;

            BakedCollisionShapeCacheable[] infos = BakedCollision.First(x => x.ShapeExists(hashId))?.GetCacheables(hashId);
            if (infos == null)
                return false;
            RemoveBakedCollisionShape(hashId);
            foreach (BakedCollisionShapeCacheable info in infos)
                AddBakedCollisionShape(hashId, section.Name, info, translation, rotation, scale);


            return true;
        }

        public FieldSectionInfo GetBakedCollisionShapeSection(uint hashId)
        {
            foreach (string sectionName in RootNode.FolderChildren.Keys)
            {
                if (((NodeFolder)RootNode.FolderChildren[sectionName]).FolderChildren["Collision"].Children.Any(x => ((MapCollisionLoader)x.Tag).ShapeExists(hashId)))
                    return new FieldSectionInfo(sectionName);
            }

            return null;
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

        private void InitSectionFolder(string sectionName)
        {
            RootNode.TryAddChild(new NodeFolder(sectionName) {
                FolderChildren = new Dictionary<string, NodeBase> 
                { 
                    { "Muunt", new NodeFolder("Muunt") }, 
                    { "Collision", new NodeFolder("Collision") },
                    { "NavMesh", new NodeFolder("NavMesh") }
                } 
            });
        }

        public static string GetSectionName(string fileName)
        {
            return fileName.Substring(0, 3);
        }
        public static string GetSectionName(System.Numerics.Matrix4x4 transform)
        {
            int letterIndex = (int)(transform.Translation.X / 1000) + 5;
            int numberIndex = (int)(transform.Translation.Z / 1000) + 5;

            char[] letters = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J' };

            return $"{letters[letterIndex]}-{numberIndex}";
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

            foreach (NodeFolder sectionFolder in RootNode.Children)
            {
                // Muunt saving
                foreach (NodeBase file in sectionFolder.FolderChildren["Muunt"].Children)
                {
                    string fileName = file.Header;
                    MapData mapData = (MapData)file.Tag;

                    MemoryStream saveStream = new MemoryStream(); // Might not actually need this in a memorystream here, pretty sure it will automatically do that. Have to look at compression formats though.
                    mapData.Save(saveStream);

                    Directory.CreateDirectory(Path.Join(savePath, $"aoc/0010/Map/{RootNode.Header}/{GetSectionName(fileName)}"));
                    FileStream fileStream = File.Open(Path.Join(savePath, $"aoc/0010/Map/{RootNode.Header}/{GetSectionName(fileName)}/{fileName}"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

                    saveStream.WriteTo(fileStream);

                    fileStream.SetLength(fileStream.Position);
                    fileStream.Flush();
                    fileStream.Close();
                }

                // Baked collision saving
                foreach (NodeBase file in sectionFolder.FolderChildren["Collision"].Children)
                {
                    string fileName = file.Header;
                    MapCollisionLoader loader = (MapCollisionLoader)file.Tag;

                    Directory.CreateDirectory(Path.Join(savePath, $"content/Physics/StaticCompound/{RootNode.Header}/"));
                    FileStream fileStream = File.Open(Path.Join(savePath, $"content/Physics/StaticCompound/{RootNode.Header}/{fileName}"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

                    loader.Save(fileStream);

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