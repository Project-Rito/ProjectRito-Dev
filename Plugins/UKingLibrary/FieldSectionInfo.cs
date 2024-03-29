﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace UKingLibrary
{
    public class FieldSectionInfo
    {
        public int XIndex { get; set; }
        public int ZIndex { get; set; }

        public OpenTK.Vector2 Center
        {
            get
            {
                return Origin + new OpenTK.Vector2(500, -500);
            }
        }

        public OpenTK.Vector3 TerrCenter
        {
            get
            {
                return new OpenTK.Vector3(Center.X, 300, Center.Y);
            }
        }

        public OpenTK.Vector2 Origin
        {
            get
            {
                return new OpenTK.Vector2((XIndex - 5) * 1000, (ZIndex - 3) * 1000);
            }
        }

        public string Name
        { 
            get
            {
                return $"{(char)(XIndex + 65)}-{ZIndex + 1}";
            }
            set
            {
                XIndex = value[0] - 65;
                ZIndex = value[2] - '0' - 1;
            }
        }

        public List<FieldNavmeshSectionInfo> NavmeshSections
        {
            get
            {
                int[] xIndices = new int[] { 0, 1, 2, 3 }.Select(x => x += (XIndex * 1000) / 250).ToArray();
                int[] zIndices = new int[] { 0, 1, 2, 3 }.Select(x => x += (ZIndex * 1000) / 250).ToArray();

                List<FieldNavmeshSectionInfo> navmeshSections = new List<FieldNavmeshSectionInfo>(16);
                foreach (int xIndex in xIndices)
                    foreach (int zIndex in zIndices)
                        navmeshSections.Add(new FieldNavmeshSectionInfo(xIndex, zIndex));

                return navmeshSections;
            }
        }
        public List<FieldNavmeshSectionInfo> NavmeshSectionsWithStreamables
        {
            get
            {
                int[] xIndices = new int[] { -1, 0, 1, 2, 3, 4 }.Select(x => x += (XIndex * 1000) / 250).ToArray();
                int[] zIndices = new int[] { -1, 0, 1, 2, 3, 4 }.Select(x => x += (ZIndex * 1000) / 250).ToArray();

                List<FieldNavmeshSectionInfo> navmeshSections = new List<FieldNavmeshSectionInfo>(36);
                foreach (int xIndex in xIndices)
                    foreach (int zIndex in zIndices)
                        if (xIndex >= 0 && zIndex >= 0) // Can't be having navmesh sections outside the map.
                            navmeshSections.Add(new FieldNavmeshSectionInfo(xIndex, zIndex));

                return navmeshSections;
            }
        }
        public List<FieldNavmeshSectionInfo> NavmeshSectionsOnlyStreamables
        {
            get
            {
                int[] xRelativeIndices = new int[] { -1, 0, 1, 2, 3, 4 }.ToArray();
                int[] zRelativeIndices = new int[] { -1, 0, 1, 2, 3, 4 }.ToArray();

                List<FieldNavmeshSectionInfo> navmeshSections = new List<FieldNavmeshSectionInfo>(36);
                foreach (int xRelativeIndex in xRelativeIndices)
                {
                    foreach (int zRelativeIndex in zRelativeIndices)
                    {
                        if (xRelativeIndex >= 0 && xRelativeIndex <= 3 && zRelativeIndex >= 0 && zRelativeIndex <= 3) // Exclude non-streamables
                            continue;

                        int xIndex = xRelativeIndex + (XIndex * 1000) / 250;
                        int zIndex = zRelativeIndex + (ZIndex * 1000) / 250;

                        if (xIndex >= 0 && zIndex >= 0) // Can't be having navmesh sections outside the map.
                            navmeshSections.Add(new FieldNavmeshSectionInfo(xIndex, zIndex));
                    }
                }

                return navmeshSections;
            }
        }

        public FieldSectionInfo(System.Numerics.Vector3 pos) : this(pos.X, pos.Z) { }
        public FieldSectionInfo(System.Numerics.Vector2 pos) : this(pos.X, pos.Y) { }
        public FieldSectionInfo(OpenTK.Vector3 pos) : this(pos.X, pos.Z) { }
        public FieldSectionInfo(OpenTK.Vector2 pos) : this(pos.X, pos.Y) { }
        public FieldSectionInfo(float x, float z)
        {
            XIndex = (int)Math.Floor(x / 1000) + 5;
            ZIndex = (int)Math.Floor(z / 1000) + 4;
        }
        public FieldSectionInfo(int xIndex, int zIndex)
        {
            XIndex = xIndex;
            ZIndex = zIndex;
        }
        public FieldSectionInfo(string name)
        {
            Name = name;
        }

        public static bool operator ==(FieldSectionInfo a, FieldSectionInfo b)
        {
            if ((a is null) != (b is null))
                return false;
            if (a is null && b is null)
                return true;

            return a.XIndex == b.XIndex && a.ZIndex == b.ZIndex;
        }
        public static bool operator !=(FieldSectionInfo a, FieldSectionInfo b)
        {
            return a.XIndex != b.XIndex || a.ZIndex != b.ZIndex;
        }

        public int GetQuadIndex(System.Numerics.Vector3 pos) { return GetQuadIndex(pos.X, pos.Z); }
        public int GetQuadIndex(System.Numerics.Vector2 pos) { return GetQuadIndex(pos.X, pos.Y); }
        public int GetQuadIndex(OpenTK.Vector3 pos) { return GetQuadIndex(pos.X, pos.Z); }
        public int GetQuadIndex(OpenTK.Vector2 pos) { return GetQuadIndex(pos.X, pos.Y); }
        public int GetQuadIndex(float x, float z)
        {
            if (x < Center.X && z < Center.Y)
                return 0;
            else if (x >= Center.X && z < Center.Y)
                return 1;
            else if (x < Center.X && z >= Center.Y)
                return 2;
            else if (x >= Center.X && z >= Center.Y)
                return 3;

            return -1; // This will never happen lol
        }
    }
}
