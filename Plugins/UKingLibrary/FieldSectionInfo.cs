﻿using System;

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
                return new OpenTK.Vector2(((XIndex - 4) * 1000) - 500, ((ZIndex - 3) * 1000) - 500);
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

        public FieldSectionInfo(System.Numerics.Vector3 pos) : this(pos.X, pos.Z) { }
        public FieldSectionInfo(System.Numerics.Vector2 pos) : this(pos.X, pos.Y) { }
        public FieldSectionInfo(OpenTK.Vector3 pos) : this(pos.X, pos.Z) { }
        public FieldSectionInfo(OpenTK.Vector2 pos) : this(pos.X, pos.Y) { }
        public FieldSectionInfo(float x, float z)
        {
            XIndex = (int)Math.Floor(x / 1000) + 5;
            ZIndex = (int)Math.Floor(z / 1000) + 4;
        }
        public FieldSectionInfo(string name)
        {
            Name = name;
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