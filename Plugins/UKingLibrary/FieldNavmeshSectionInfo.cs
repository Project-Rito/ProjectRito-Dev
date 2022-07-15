using System;
using System.Collections.Generic;
using System.Linq;

namespace UKingLibrary
{
    public class FieldNavmeshSectionInfo
    {
        public int XIndex { get; set; }
        public int ZIndex { get; set; }

        public string Name
        {
            get
            {
                return $"{XIndex}-{ZIndex}";
            }
        }

        public OpenTK.Vector2 Origin
        {
            get
            {
                return new OpenTK.Vector2((XIndex - 20) * 250, (ZIndex - 16) * 250);
            }
        }

        public FieldNavmeshSectionInfo(System.Numerics.Vector3 pos) : this(pos.X, pos.Z) { }
        public FieldNavmeshSectionInfo(System.Numerics.Vector2 pos) : this(pos.X, pos.Y) { }
        public FieldNavmeshSectionInfo(OpenTK.Vector3 pos) : this(pos.X, pos.Z) { }
        public FieldNavmeshSectionInfo(OpenTK.Vector2 pos) : this(pos.X, pos.Y) { }
        public FieldNavmeshSectionInfo(float x, float z)
        {
            XIndex = (int)Math.Floor(x / 250) + 20;
            ZIndex = (int)Math.Floor(z / 250) + 16;
        }
        public FieldNavmeshSectionInfo(int xIndex, int zIndex)
        {
            XIndex = xIndex;
            ZIndex = zIndex;
        }
    }
}
