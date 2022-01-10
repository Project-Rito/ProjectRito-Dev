﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using CafeLibrary;
using GLFrameworkEngine;
using MapStudio.UI;
using UKingLibrary.Rendering;

namespace UKingLibrary
{
    public class Terrain
    {
        /// <summary>
        /// The section width,
        /// </summary>
        const float SECTION_WIDTH = 1000.0f;

        /// <summary>
        /// The size of a tile grid.
        /// </summary>
        const float TILE_GRID_SIZE = 32;

        /// <summary>
        /// The max possible LOD level to subdivde tiles to.
        /// </summary>
        const float LOD_MAX = 0.125f;

        /// <summary>
        /// The smallest possible LOD level to subdivde tiles to.
        /// </summary>
        const float LOD_MIN = 32;

        /// <summary>
        /// The max amount of levels used terrain detal.
        /// </summary>
        const int LOD_LEVEL_MAX = 8;

        /// <summary>
        /// The ratio for scaling tiles to sections.
        /// </summary>
        const float TILE_TO_SECTION_SCALE = 0.5f;

        /// <summary>
        /// The LOD subdivision levels for terrain detail.
        /// </summary>
        float[] DetailLevels = new float[LOD_LEVEL_MAX]
        {
            32, 16, 4, 2, 1, 0.5f, 0.25f, 0.125f,
        };

        /// <summary>
        /// The terrain table for looking up area tiles.
        /// </summary>
        TSCB TerrainTable;

        /// <summary>
        /// The rendered terrrain meshes in the scene.
        /// </summary>
        public List<TerrainRender> TerrainMeshes = new List<TerrainRender>();

        /// <summary>
        /// The rendered water meshes in the scene
        /// </summary>
        public List<WaterRender> WaterMeshes = new List<WaterRender>();

        /// <summary>
        /// Loads the terrain table.
        /// </summary>
        public void LoadTerrainTable(string fieldName)
        {
            ProcessLoading.Instance.Update(40, 100, "Loading terrain table");

            var terrainPath = PluginConfig.GetContentPath($"Terrain\\A\\{fieldName}.tscb");
            TerrainTable = new TSCB(terrainPath);
        }

        /// <summary>
        /// Loads all the terrain data in a given area.
        /// </summary>
        public void LoadTerrainSection(int areaID, int sectionID, int lodLevel = LOD_LEVEL_MAX)
        {
            float lodScale = DetailLevels[Math.Clamp(lodLevel, 0, 7)];
            Vector3 midpoint = CalculateMidPoint(areaID, sectionID);
            

            // Terrain
            int index = 1; // For reporting progress

            var sectionTilesCore = TerrainTable.GetSectionTilesByPos(lodScale, midpoint, SECTION_WIDTH);
            
            foreach (var tile in sectionTilesCore)
            {
                ProcessLoading.Instance.Update((index * 100) / sectionTilesCore.Count, 100, $"Loading terrain mesh {index++} / {sectionTilesCore.Count}");

                var tileSectionScale = TILE_GRID_SIZE / (LOD_MIN / tile.Value.Core.AreaSize) * SECTION_WIDTH * TILE_TO_SECTION_SCALE;

                CreateTile(tile.Value.Core, tile.Key, tileSectionScale);
            }

            
            // Water
            index = 1; // For reporting progress

            var sectionTilesExtra = TerrainTable.GetSectionTilesByPos(lodScale, midpoint, SECTION_WIDTH, true, false);

            foreach (var tile in sectionTilesExtra)
            {
                ProcessLoading.Instance.Update((index * 100) / sectionTilesExtra.Count, 100, $"Loading tile extra groups {index++} / {sectionTilesExtra.Count}");

                var tileSectionScale = TILE_GRID_SIZE / (LOD_MIN / tile.Value.Core.AreaSize) * SECTION_WIDTH * TILE_TO_SECTION_SCALE;

                foreach (TSCB.TerrainAreaExtra extmData in tile.Value.Extra)
                {
                    if (extmData.Type == TSCB.ExtraSectionType.Water)
                        CreateWaterTile(tile.Value.Core, extmData, tile.Key, tileSectionScale);
                }
            }

            // Collision
            CreateCollisionTile(areaID, sectionID, 0);
        }

        public void CreateCollisionTile(int areaID, int sectionID, int lodLevel = LOD_LEVEL_MAX)
        {
            float lodScale = DetailLevels[Math.Clamp(lodLevel, 0, 7)];

            Vector3 mid_point = CalculateMidPoint(areaID, sectionID);
            var sectionTiles = TerrainTable.GetSectionTilesByPos(lodScale, mid_point, SECTION_WIDTH);

            foreach (var tile in sectionTiles)
            {
                var tileSectionScale = TILE_GRID_SIZE / (LOD_MIN / tile.Value.Core.AreaSize) * SECTION_WIDTH * TILE_TO_SECTION_SCALE;

                CreateCollisionTile(tile.Value.Core, tile.Key, tileSectionScale);
            }

            GLContext.ActiveContext.CollisionCaster.UpdateCache();
        }

        private void CreateCollisionTile(TSCB.TerrainAreaCore tile, string name, float tileSectionScale)
        {
            string packName = GetTilePackName(name);
            var meshData = LoadTerrainFiles(packName, name, "hght");
            Vector3 position = new Vector3(
             tile.PositionX * SECTION_WIDTH * TILE_TO_SECTION_SCALE,
             0,
             tile.PositionZ * SECTION_WIDTH * TILE_TO_SECTION_SCALE) * GLContext.PreviewScale;
            Vector3 scale = new Vector3(tileSectionScale, 1, tileSectionScale);

            Matrix4 matrix = Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(position);

            //Height map
            Vector3[] vertices = new Vector3[256 * 256];
            using (var reader = new Toolbox.Core.IO.FileReader(meshData))
            {
                int vertexIndex = 0;
                for (float y = 0; y < 256; y++)
                {
                    float normY = y / 255.0f;
                    for (float x = 0; x < 256; x++)
                    {
                        float heightValue = reader.ReadUInt16() * 0.0122075f;
                        //Terrain vertices range from 0 - 1
                        vertices[vertexIndex] = new Vector3(x / 255.0f - 0.5f, heightValue, normY - 0.5f) * GLContext.PreviewScale;
                        vertices[vertexIndex] = Vector3.TransformPosition(vertices[vertexIndex], matrix);
                        vertexIndex++;
                    }
                }
            }
            //Add to colllision map
            for (int y = 0; y < 255; y++)
            {
                int indexTop = (y) * 256;
                int indexBottom = (y + 1) * 256;

                for (int x = 0; x < 255; x++)
                {
                    int index1 = indexTop;
                    int index2 = indexBottom;
                    int index3 = indexBottom + 1;

                    int index4 = indexBottom + 1;
                    int index5 = indexTop + 1;
                    int index6 = indexTop;

                    GLContext.ActiveContext.CollisionCaster.AddTri(
                        vertices[index1], vertices[index2], vertices[index3]);
                    GLContext.ActiveContext.CollisionCaster.AddTri(
                        vertices[index4], vertices[index5], vertices[index6]);

                    ++indexTop;
                    ++indexBottom;
                }
            }
        }

        //Get the section placement of an area section.
        //These should match LocationPosX / LocationPosZ in mubin
        private Vector3 CalculateMidPoint(int x, int y)
        {
            return new Vector3(
                (x - 3.5f) * SECTION_WIDTH,
                300,
                (y - 4.5f) * SECTION_WIDTH
            );
        }

        //Gets the tile archive name for a tile entry.
        private string GetTilePackName(string tileName) {
            return ((Convert.ToInt64($"0x{tileName}", 16) / 4) * 4).ToString("X").ToUpper();
        }

        //Creates a terrain mesh from a given tile
        private void CreateTile(TSCB.TerrainAreaCore tile, string name, float tileSectionScale)
        {
            string packName = GetTilePackName(name);

            Toolbox.Core.StudioLogger.WriteLine($"Creating terrain tile {name} in pack {packName}...");

            //Material info
            var materialData = LoadTerrainFiles(packName, name, "mate");
            //Height map
            var heightBuffer = LoadTerrainFiles(packName, name, "hght");

            //Create a terrain mesh for rendering
            var meshRender = new TerrainRender();
            meshRender.LoadTerrainData(heightBuffer, materialData);
            TerrainMeshes.Add(meshRender);
            //Scale and place the title in the correct place
            meshRender.Transform.Position = new Vector3(
                tile.PositionX * SECTION_WIDTH * TILE_TO_SECTION_SCALE, 
                0,
                tile.PositionZ * SECTION_WIDTH * TILE_TO_SECTION_SCALE) * GLContext.PreviewScale;
            meshRender.Transform.Scale = new Vector3(tileSectionScale, 1, tileSectionScale);
            meshRender.Transform.UpdateMatrix(true);
            meshRender.UINode.Tag = tile;
            meshRender.UINode.Header = name;
            meshRender.IsVisibleCallback += delegate
            {
                return MapMuuntEditor.ShowMapModel;
            };

            GLContext.ActiveContext.Scene.AddRenderObject(meshRender);
        }

        private void CreateWaterTile(TSCB.TerrainAreaCore tile, TSCB.TerrainAreaExtra extmData, string name, float tileSectionScale)
        {
            string packName = GetTilePackName(name);

            Toolbox.Core.StudioLogger.WriteLine($"Creating terrain tile {name} in pack {packName}...");

            //Material info
            var materialData = LoadTerrainFiles(packName, name, "mate");
            //Height map;
            var heightBuffer = LoadTerrainFiles(packName, name, "water.extm");

            //Create a terrain mesh for rendering
            var meshRender = new WaterRender();
            meshRender.LoadWaterData(heightBuffer, materialData);
            WaterMeshes.Add(meshRender);
            //Scale and place the title in the correct place
            meshRender.Transform.Position = new Vector3(
                tile.PositionX * SECTION_WIDTH * TILE_TO_SECTION_SCALE,
                0,
                tile.PositionZ * SECTION_WIDTH * TILE_TO_SECTION_SCALE) * GLContext.PreviewScale;
            meshRender.Transform.Scale = new Vector3(tileSectionScale, 1, tileSectionScale);
            meshRender.Transform.UpdateMatrix(true);
            meshRender.UINode.Tag = tile;
            meshRender.UINode.Header = name;
            meshRender.IsVisibleCallback += delegate
            {
                return MapMuuntEditor.ShowMapModel;
            };

            GLContext.ActiveContext.Scene.AddRenderObject(meshRender);
        }

        private byte[] LoadTerrainFiles(string packName, string name, string type)
        {
            var path = PluginConfig.GetContentPath($"Terrain\\A\\{PluginConfig.FieldName}\\{packName}.{type}.sstera");
           return SARC.GetFile(path, $"{name}.{type}");
        }

        public void Dispose()
        {
            foreach (var mesh in TerrainMeshes)
            {
                GLFrameworkEngine.GLContext.ActiveContext.Scene.RemoveRenderObject(mesh);
                mesh?.Dispose();
            }
            TerrainMeshes.Clear();
            foreach (var mesh in WaterMeshes)
            {
                GLFrameworkEngine.GLContext.ActiveContext.Scene.RemoveRenderObject(mesh);
                mesh?.Dispose();
            }
            WaterMeshes.Clear();
        }
    }
}
