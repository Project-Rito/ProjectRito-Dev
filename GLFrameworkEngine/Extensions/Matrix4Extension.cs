using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace GLFrameworkEngine
{
    public static class Matrix4Extension
    {
        /// <summary>
        /// Creates a rotation matrix from normal and tangent data.
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="tangent"></param>
        /// <returns></returns>
        public static Matrix4 CreateRotation(Vector3 normal, Vector3 tangent)
        {
            Vector3 binormal = Vector3.Cross(normal, tangent);
            return new Matrix4(
                binormal.X, binormal.Y, binormal.Z, 0,
                normal.X, normal.Y, normal.Z, 0,
                tangent.X, tangent.Y, tangent.Z, 0,
                0, 0, 0, 1);
        }

        /// <summary>
        /// Creates a SRT matrix from translation, euler and scale vectors.
        /// </summary>
        public static Matrix4 CreateTransform(
             Vector3 translate, Vector3 rotateEuler, Vector3 scale)
        {
            return Matrix4.CreateScale(scale) *
                  (Matrix4.CreateRotationX(rotateEuler.X) *
                   Matrix4.CreateRotationY(rotateEuler.Y) *
                   Matrix4.CreateRotationZ(rotateEuler.Z)) *
                   Matrix4.CreateTranslation(translate);
        }

        /// <summary>
        /// Creates a SRT matrix from translation, quaternion and scale vectors.
        /// </summary>
        public static Matrix4 CreateTransform(
            Vector3 translate, Quaternion rotateQuat, Vector3 scale)
        {
            return Matrix4.CreateTranslation(translate) *
                   Matrix4.CreateFromQuaternion(rotateQuat) *
                   Matrix4.CreateScale(scale);
        }
    }
}
