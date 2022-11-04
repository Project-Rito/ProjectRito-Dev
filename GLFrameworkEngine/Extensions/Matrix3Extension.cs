using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace GLFrameworkEngine
{
    public static class Matrix3Extension
    {
        public static Matrix3 FromEulerAngles(Vector3 eulerAngles)
        {
         return Matrix3.CreateRotationX(eulerAngles.X) *
                Matrix3.CreateRotationY(eulerAngles.Y) *
                Matrix3.CreateRotationZ(eulerAngles.Z) *
                  Matrix3.Identity;
        }

        public static Vector3 ExtractEulerAngles(this Matrix3 mtx)
        {
            bool CompareEpsilon(float a, float b) => Math.Abs(a - b) <= float.Epsilon;

            double x, y, z;

            //0,1, 2
            //4,5, 6
            //8,9,10

            if (CompareEpsilon(mtx.M13, 1f))
            {
                x = Math.Atan2(-mtx.M21, -mtx.M31);
                y = -Math.PI / 2;
                z = 0.0;
            }
            else if (CompareEpsilon(mtx.M13, -1f))
            {
                x = Math.Atan2(mtx.M21, mtx.M31);
                y = Math.PI / 2;
                z = 0.0;
            }
            else
            {
                x = Math.Atan2(mtx.M23, mtx.M33);
                y = -Math.Asin(mtx.M13);
                z = Math.Atan2(mtx.M12, mtx.M11);
            }

            return new Vector3((float)x, (float)y, (float)z);
        }

        public static float GetRotationX(Matrix3 mtx)
        {
            return (float)(180 * Math.Atan2(mtx.M23, mtx.M33) / Math.PI);
        }

        public static float GetRotationY(Matrix3 mtx)
        {
            return (float)(180 * Math.Atan2(mtx.M31, mtx.M11) / Math.PI);
        }

        public static float GetRotationZ(Matrix3 mtx)
        {
            return (float)(180 * Math.Atan2(mtx.M12, mtx.M22) / Math.PI);
        }
    }
}
