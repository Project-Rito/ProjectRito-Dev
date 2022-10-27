using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace GLFrameworkEngine
{
    public static class Vector4Extension
    {
        public static bool IsUniform(this Vector4 value) {
            return value.X == value.Y && value.Y == value.Z && value.Z == value.W;
        }

        public static Vector4 Snap(this Vector4 value, Vector4 snap)
        {
            if (snap.Length > 0.0f)
                return new Vector4(
                    MathF.Floor(value.X / snap.X) * snap.X,
                    MathF.Floor(value.Y / snap.Y) * snap.Y,
                    MathF.Floor(value.Z / snap.Y) * snap.Z,
                    MathF.Floor(value.W / snap.Y) * snap.W);
            return value;
        }

        public static Vector4 Snap(this Vector4 value, float snap)
        {
            if (snap > 0.0f)
                return new Vector4(
                    MathF.Floor(value.X / snap) * snap,
                    MathF.Floor(value.Y / snap) * snap,
                    MathF.Floor(value.Z / snap) * snap,
                    MathF.Floor(value.W / snap) * snap);
            return value;
        }

        public static System.Numerics.Vector4 GetNumeric(this Vector4 value)
        {
            return new System.Numerics.Vector4(value.X, value.Y, value.Z, value.W);
        }
    }
}
