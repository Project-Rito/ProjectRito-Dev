using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Toolbox.Core.IO;
using OpenTK;

namespace UKingLibrary
{
    public class TSCB
    {
        public float WorldScale { get; set; }
        public float MaxHeight { get; set; }
        public float TileSize { get; set; }

        public Material[] Materials;
        public TerrainArea[] TerrainAreas;
        public string[] TerrainAreaFileNames;

        public TSCB(string filePath)
        {
            using (var reader = new FileReader(filePath)) {
                Read(reader);
            }
        }

        void Read(FileReader reader)
        {
            reader.SetByteOrder(true);

            reader.ReadSignature(4, "TSCB");
            reader.ReadUInt32(); //version
            reader.ReadUInt32(); //1
            reader.ReadUInt32(); //name table offset
            WorldScale = reader.ReadSingle();
            MaxHeight = reader.ReadSingle();
            uint numMaterials = reader.ReadUInt32();
            uint numAreas = reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            TileSize = reader.ReadSingle();
            reader.ReadUInt32();

            Materials = new Material[numMaterials];

            long pos = reader.Position;
            uint sectionSize = reader.ReadUInt32();
            for (uint i = 0; i < numMaterials; i++)
            {
                var sectionOffset = reader.ReadUInt32();
                using (reader.TemporarySeek(sectionOffset - 4, System.IO.SeekOrigin.Current)) {
                    Materials[i] = reader.ReadStruct<Material>();
                }
            }

            //Seek to next section
            reader.SeekBegin(pos + sectionSize);

            TerrainAreas = new TerrainArea[numAreas];
            TerrainAreaFileNames = new string[numAreas];

            for (uint i = 0; i < numAreas; i++)
            {
                var sectionOffset = reader.ReadUInt32();

                using (reader.TemporarySeek(sectionOffset - 4, System.IO.SeekOrigin.Current)) {
                    TerrainAreas[i] = new TerrainArea(reader.ReadStruct<TerrainAreaCore>());


                    // Deal with extra infos
                    for (uint x = 0; x < TerrainAreas[i].Core.ExtraFlags; x++)
                    {
                        using (reader.TemporarySeek())
                        {
                            uint extraInfoLength = reader.ReadUInt32();
                            if (extraInfoLength == 8)
                                reader.ReadUInt32(); // Unknown

                            TerrainAreas[i].Extra = new TerrainAreaExtra[extraInfoLength / 4];
                            for (int y = 0; y < extraInfoLength / 4; y++)
                            {
                                TerrainAreas[i].Extra[y] = reader.ReadStruct<TerrainAreaExtra>();
                            }
                        }
                    }

                    //Name offset relative to offset position
                    long nameOfsPos = reader.Position - 16;

                    reader.SeekBegin(nameOfsPos + TerrainAreas[i].Core.FileBaseNameOfs);
                    TerrainAreaFileNames[i] = reader.ReadZeroTerminatedString();
                }
            }
        }

        public Dictionary<string, TerrainArea> GetSectionTilesByPos(float targetLODScale, Vector3 sectionMidpoint, float sectionWidth = 2f, bool ensureWater = false, bool ensureGrass = false)
        {
            Vector2 sectionPos = new Vector2(
                (sectionMidpoint.X / sectionWidth) / 6 * 12,
                (sectionMidpoint.Z / sectionWidth) / 6 * 12);

            Dictionary<string, TerrainArea> tiles = new Dictionary<string, TerrainArea>();
            for (int x = 0; x < TerrainAreas.Length; x++)
            {
                if (TerrainAreas[x].Core.AreaSize < targetLODScale)
                    continue;

                // Ensure we have extra stuff of type if requested, otherwise this area is worthless to us
                if (ensureWater && !TileHasExtraType(TerrainAreas[x], ExtraSectionType.Water))
                    continue;
                if (ensureGrass && !TileHasExtraType(TerrainAreas[x], ExtraSectionType.Grass))
                    continue;

                var candidateTilePos = new Vector2(TerrainAreas[x].Core.PositionX, TerrainAreas[x].Core.PositionZ);
                float candidateTileScale = TerrainAreas[x].Core.AreaSize;
                
                // Is our candidate inside the section
                if (TileOverlapsTile(candidateTilePos, candidateTileScale, sectionPos, 2))
                {
                    // Can we replace this tile with a complete set of fully-filled higher-detail tiles? If not, keep this as well as higher-detailed tiles.
                    // (In future we should stitch stuff together, but we're not there yet)
                    float percentageHasHigherDetail = 0;
                    for (int y = 0; y < TerrainAreas.Length; y++) {
                        if (x == y)
                            continue;
                        if (TerrainAreas[y].Core.AreaSize < targetLODScale)
                            continue;

                        // Ensure we have extra stuff of type if requested, otherwise this area is worthless to us
                        if (ensureWater && !TileHasExtraType(TerrainAreas[y], ExtraSectionType.Water))
                            continue;
                        if (ensureGrass && !TileHasExtraType(TerrainAreas[y], ExtraSectionType.Grass))
                            continue;

                        var thisTilePos = new Vector2(TerrainAreas[y].Core.PositionX, TerrainAreas[y].Core.PositionZ);
                        var thisTileScale = TerrainAreas[y].Core.AreaSize;

                        if (TileInTile(thisTilePos, thisTileScale, candidateTilePos, candidateTileScale))
                        {
                            percentageHasHigherDetail += (thisTileScale * thisTileScale) / (candidateTileScale * candidateTileScale);
                            if (percentageHasHigherDetail >= 1f)
                                break;
                        }
                    }

                    if ((percentageHasHigherDetail < 1f && !ensureGrass) || percentageHasHigherDetail == 0) // We don't need low-LOD grass lol, in the meantime before stitching
                        tiles.Add(TerrainAreaFileNames[x], TerrainAreas[x]);
                }
                
            }

            return tiles;
        }

        private static bool TileHasExtraType(TerrainArea terrainArea, ExtraSectionType type)
        {
            if (terrainArea.Extra == null)
                return false;
            foreach (TerrainAreaExtra extraData in terrainArea.Extra)
            {
                if (extraData.Type == type)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if tile 1 overlaps tile 2
        /// </summary>
        /// <param name="t1Pos">Tile 1 translate</param>
        /// <param name="t1Scale">Tile 1 scale</param>
        /// <param name="t2Pos">Tile 2 translate</param>
        /// <param name="t2Scale">Tile 2 scale</param>
        /// <returns></returns>
        public static bool TileOverlapsTile(Vector2 t1Pos, float t1Scale, Vector2 t2Pos, float t2Scale)
        {
            if (t1Pos == t2Pos && t1Scale == t2Scale)
                return true;

            float t1ScaleHalf = t1Scale / 2;
            float t2ScaleHalf = t2Scale / 2;

            float t1MinX = t1Pos.X - t1ScaleHalf;
            float t1MaxX = t1Pos.X + t1ScaleHalf;
            float t1MinY = t1Pos.Y - t1ScaleHalf;
            float t1MaxY = t1Pos.Y + t1ScaleHalf;

            float t2MinX = t2Pos.X - t2ScaleHalf;
            float t2MaxX = t2Pos.X + t2ScaleHalf;
            float t2MinY = t2Pos.Y - t2ScaleHalf;
            float t2MaxY = t2Pos.Y + t2ScaleHalf;

            Vector2 t1C1 = new Vector2(t1MinX, t1MinY);
            Vector2 t1C2 = new Vector2(t1MaxX, t1MinY);
            Vector2 t1C3 = new Vector2(t1MinX, t1MaxY);
            Vector2 t1C4 = new Vector2(t1MaxX, t1MaxY);

            Vector2 t2C1 = new Vector2(t2MinX, t2MinY);
            Vector2 t2C2 = new Vector2(t2MaxX, t2MinY);
            Vector2 t2C3 = new Vector2(t2MinX, t2MaxY);
            Vector2 t2C4 = new Vector2(t2MaxX, t2MaxY);


            if (
            (
                (t1C1.X > t2MinX && t1C1.X < t2MaxX && t1C1.Y > t2MinY && t1C1.Y < t2MaxY)
                || (t1C2.X > t2MinX && t1C2.X < t2MaxX && t1C2.Y > t2MinY && t1C2.Y < t2MaxY)
                || (t1C3.X > t2MinX && t1C3.X < t2MaxX && t1C3.Y > t2MinY && t1C3.Y < t2MaxY)
                || (t1C4.X > t2MinX && t1C4.X < t2MaxX && t1C4.Y > t2MinY && t1C4.Y < t2MaxY)
            )
            ||
            (
                (t2C1.X > t1MinX && t2C1.X < t1MaxX && t2C1.Y > t1MinY && t2C1.Y < t1MaxY)
                || (t2C2.X > t1MinX && t2C2.X < t1MaxX && t2C2.Y > t1MinY && t2C2.Y < t1MaxY)
                || (t2C3.X > t1MinX && t2C3.X < t1MaxX && t2C3.Y > t1MinY && t2C3.Y < t1MaxY)
                || (t2C4.X > t1MinX && t2C4.X < t1MaxX && t2C4.Y > t1MinY && t2C4.Y < t1MaxY)
            ))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if tile 1 is in tile 2
        /// </summary>
        /// <param name="t1Pos">Tile 1 translate</param>
        /// <param name="t1Scale">Tile 1 scale</param>
        /// <param name="t2Pos">Tile 2 translate</param>
        /// <param name="t2Scale">Tile 2 scale</param>
        /// <returns></returns>
        public static bool TileInTile(Vector2 t1Pos, float t1Scale, Vector2 t2Pos, float t2Scale)
        {
            if (t1Scale >= t2Scale)
                return false; // It can't be inside if it's bigger/the same size!

            float t1ScaleHalf = t1Scale / 2;
            float t2ScaleHalf = t2Scale / 2;

            if ((t1Pos.X + t1ScaleHalf) <= (t2Pos.X + t2ScaleHalf) // X
            && (t1Pos.X - t1ScaleHalf) >= (t2Pos.X - t2ScaleHalf)
            && (t1Pos.Y + t1ScaleHalf) <= (t2Pos.Y + t2ScaleHalf) // Y
            && (t1Pos.Y - t1ScaleHalf) >= (t2Pos.Y - t2ScaleHalf))
            {
                return true;
            }

            return false;
        }

        public Tuple<Vector2, Vector2> GetTileGridRect(float lodScale)
        {
            float lodScaleHalf = lodScale / 2.0f;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (var tile in this.TerrainAreas)
            {
                if (tile.Core.AreaSize == lodScale)
                {
                    if ((tile.Core.PositionX - lodScaleHalf) < min.X) // Min - X
                        min.X = (tile.Core.PositionX - lodScaleHalf);

                    if ((tile.Core.PositionZ - lodScaleHalf) < min.Y) // Min - Y
                        min.Y = (tile.Core.PositionZ - lodScaleHalf);

                    if ((tile.Core.PositionX + lodScaleHalf) > max.X) // Max - X
                        max.X = (tile.Core.PositionX + lodScaleHalf);

                    if ((tile.Core.PositionZ + lodScaleHalf) > max.Y) // Max - Y
                        max.Y = (tile.Core.PositionZ + lodScaleHalf);
                }
            }
            return Tuple.Create(min, max);
        }


        // Directly readable structs:
        // ------------------

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Material
        {
            public int ArrayIndex;
            public float TextureU;
            public float TextureV;
            public float Unknown1;
            public float Unknown2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class TerrainAreaCore
        {
            public float PositionX;
            public float PositionZ;
            public float AreaSize;
            public float MinTerrainHeight;
            public float MaxTerrainHeight;
            public float MinWaterHeight;
            public float MaxWaterHeight;
            public uint Unknown;
            public uint FileBaseNameOfs;
            public uint Unknown2;
            public uint Unknown3;
            public uint ExtraFlags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class TerrainAreaExtra
        {
            public uint Unknown1; //3
            public ExtraSectionType Type; //grass or water (0, 1)
            public float TextureV;
            public float Unknown2; //1
            public float Unknown3; //0
        }


        // Abstraction stuff:
        // ------------------

        /// <summary>
        /// Wrapper class for all terrain info for a section
        /// </summary>
        public class TerrainArea
        {
            public TerrainArea(TerrainAreaCore core)
            {
                Core = core;
            }

            public TerrainAreaCore Core;
            public TerrainAreaExtra[] Extra;
        }

        public enum ExtraSectionType : uint
        {
            Grass,
            Water
        }
    }
}
