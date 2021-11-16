using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
   public class Circle2DRenderer : RenderMesh<Vector2>
    {
        public Circle2DRenderer(int length, PrimitiveType type = PrimitiveType.LineLoop) : base(GetVertices(length), type)
        {

        }

        public void SetCustomSegment(float angleStart, float angleEnd)
        {

        }

        static Vector2[] GetVertices(int length)
        {
            Vector2[] vertices = new Vector2[length];
            for (int i = 0; i < length; i++)
            {
                double angle = 2 * Math.PI * i / length;
                vertices[i] = new Vector2(
                    MathF.Cos((float)angle),
                    MathF.Sin((float)angle));
            }
            return vertices;
        }
    }
}
