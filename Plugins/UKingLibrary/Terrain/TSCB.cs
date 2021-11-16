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
            for (int i = 0; i < numMaterials; i++)
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

            for (int i = 0; i < numAreas; i++)
            {
                var sectionOffset = reader.ReadUInt32();

                using (reader.TemporarySeek(sectionOffset - 4, System.IO.SeekOrigin.Current)) {
                    TerrainAreas[i] = reader.ReadStruct<TerrainArea>();

                    //Name offset relative to offset position
                    long nameOfsPos = reader.Position - 16;

                    reader.SeekBegin(nameOfsPos + TerrainAreas[i].FileBaseNameOfs);
                    TerrainAreaFileNames[i] = reader.ReadZeroTerminatedString();
                }
            }
        }

        public Dictionary<string, TerrainArea> GetSectionTilesByPos(float lodScale, Vector3 sectionMidpoint, float sectionWidth)
        {
            float sectionWidthHalf = 0.5f + 0.5f; // add an bit of size to get a edge around the section
            float lodScaleHalf = lodScale / 2.0f;
            Vector2 sPos = new Vector2(
                (sectionMidpoint.X / sectionWidth) / 6 * 12,
                (sectionMidpoint.Z / sectionWidth) / 6 * 12);

            Dictionary<string, TerrainArea> tiles = new Dictionary<string, TerrainArea>();
            for (int i = 0; i < TerrainAreas.Length; i++)
            {
                if (TerrainAreas[i].AreaSize == lodScale)
                {
                    var tPos = new Vector2(TerrainAreas[i].PositionX, TerrainAreas[i].PositionZ);

                    if ((tPos[0] - lodScaleHalf) <= (sPos[0] + sectionWidthHalf) // X
                     && (tPos[0] + lodScaleHalf) >= (sPos[0] - sectionWidthHalf)
                     && (tPos[1] - lodScaleHalf) <= (sPos[1] + sectionWidthHalf) // Y
                     && (tPos[1] + lodScaleHalf) >= (sPos[1] - sectionWidthHalf))
                    {
                        tiles.Add(TerrainAreaFileNames[i], TerrainAreas[i]);
                    }
                }
            }

            return tiles;
        }

        public Tuple<Vector2, Vector2> GetTileGridRect(float lodScale)
        {
            float lodScaleHalf = lodScale / 2.0f;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (var tile in this.TerrainAreas)
            {
                if (tile.AreaSize == lodScale)
                {
                    if ((tile.PositionX - lodScaleHalf) < min.X) // Min - X
                        min.X = (tile.PositionX - lodScaleHalf);

                    if ((tile.PositionZ - lodScaleHalf) < min.Y) // Min - Y
                        min.Y = (tile.PositionZ - lodScaleHalf);

                    if ((tile.PositionX + lodScaleHalf) > max.X) // Max - X
                        max.X = (tile.PositionX + lodScaleHalf);

                    if ((tile.PositionZ + lodScaleHalf) > max.Y) // Max - Y
                        max.Y = (tile.PositionZ + lodScaleHalf);
                }
            }
            return Tuple.Create(min, max);
        }


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
        public class TerrainArea
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
        public class ExtraSection
        {
            public uint Unknown1; //3
            public uint Type; //grass or water (0, 1)
            public float TextureV;
            public float Unknown2; //1
            public float Unknown3; //0
        }
    }
}
