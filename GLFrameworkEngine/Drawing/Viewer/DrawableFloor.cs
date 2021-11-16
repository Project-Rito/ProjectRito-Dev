using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class DrawableFloor : RenderMesh<Vector3>
    {
        public static System.Numerics.Vector4 GridColor = new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 1.0f);

        public static int CellAmount = 10;
        public static int CellSize = 1;

        public static bool Display = true;

        public DrawableFloor() : base(Vertices, PrimitiveType.Lines)
        {

        }

        static Vector3[] Vertices => FillVertices(CellAmount, CellSize).ToArray();

        static List<Vector3> FillVertices(int amount, int size)
        {
            var vertices = new List<Vector3>();
            for (var i = -amount; i <= amount; i++)
            {
                vertices.Add(new Vector3(-amount * size, 0f, i * size));
                vertices.Add(new Vector3(amount * size, 0f, i * size));
                vertices.Add(new Vector3(i * size, 0f, -amount * size));
                vertices.Add(new Vector3(i * size, 0f, amount * size));
            }
            return vertices;
        }

        public void Draw(GLContext control, Pass pass)
        {
            if (pass != Pass.OPAQUE || !Display)
                return;

            var gridShaderProgram = GlobalShaders.GetShader("GRID");

            if (Runtime.GridSettings.CellSize != CellSize || Runtime.GridSettings.CellAmount != CellAmount)
                UpdateVertexData(Vertices);

            control.CurrentShader = gridShaderProgram;

            Matrix4 previewScale = Matrix4.CreateScale(1.0f);
            gridShaderProgram.SetMatrix4x4("previewScale", ref previewScale);

            gridShaderProgram.SetVector4("gridColor", new Vector4(GridColor.X, GridColor.Y, GridColor.Z, GridColor.W));
            this.Draw(control);

            GL.UseProgram(0);
        }
    }
}