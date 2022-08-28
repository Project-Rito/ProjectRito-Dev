using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;

namespace GLFrameworkEngine
{
    public class DrawingHelper
    {
        public static VertexPositionNormalTexCoord[] GetPlaneVertices(float size)
        {
            VertexPositionNormalTexCoord[] vertices = new VertexPositionNormalTexCoord[4];
            vertices[0] = new VertexPositionNormalTexCoord(new Vector3(-1, -1, 0), new Vector3(-1, -1, 0), new Vector2(0, 0)); //Bottom Left
            vertices[1] = new VertexPositionNormalTexCoord(new Vector3(1, -1, 0), new Vector3(1, -1, 0), new Vector2(1, 0)); //Bottom Right
            vertices[2] = new VertexPositionNormalTexCoord(new Vector3(1, 1, 0), new Vector3(1, 1, 0), new Vector2(1, 1)); //Top Right
            vertices[3] = new VertexPositionNormalTexCoord(new Vector3(-1, 1, 0), new Vector3(-1, 1, 0), new Vector2(0, 1)); //Top Left
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position *= size;
            return vertices;
        }

        public static VerticesIndices<VertexPositionNormalTexCoord> GetUVCubeVertices(float size)
        {
            return FromObj(new System.IO.MemoryStream(Properties.Resources.Cube), size);
        }

        public static VertexPositionNormal[] GetCubeVertices(float size)
        {
            VertexPositionNormal[] vertices = new VertexPositionNormal[8];
            vertices[0] = new VertexPositionNormal(new Vector3(-1, -1, 1), new Vector3(-1, -1, 1)); //Bottom Left
            vertices[1] = new VertexPositionNormal(new Vector3(1, -1, 1), new Vector3(1, -1, 1)); //Bottom Right
            vertices[2] = new VertexPositionNormal(new Vector3(1, 1, 1), new Vector3(1, 1, 1)); //Top Right
            vertices[3] = new VertexPositionNormal(new Vector3(-1, 1, 1), new Vector3(-1, 1, 1)); //Top Left
            vertices[4] = new VertexPositionNormal(new Vector3(-1, -1, -1), new Vector3(-1, -1, -1)); //Bottom Left -Z
            vertices[5] = new VertexPositionNormal(new Vector3(1, -1, -1), new Vector3(1, -1, -1)); //Bottom Right -Z
            vertices[6] = new VertexPositionNormal(new Vector3(1, 1, -1), new Vector3(1, 1, -1)); //Top Right -Z
            vertices[7] = new VertexPositionNormal(new Vector3(-1, 1, -1), new Vector3(-1, 1, -1)); //Top Left -Z
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position *= size;
            return vertices;
        }

        public static VertexPositionNormalTexCoord[] GetUVSphereVertices(float radius, float u_segments, float v_segments)
        {
            List<VertexPositionNormalTexCoord> vertices = new List<VertexPositionNormalTexCoord>();

            var sphereVertices = GetSphereVertices(radius, u_segments, v_segments);
            for (int i = 0; i < sphereVertices.Length; i++)
            {
                vertices.Add(new VertexPositionNormalTexCoord()
                {
                    Position = sphereVertices[i].Position,
                    Normal = sphereVertices[i].Normal,
                    TexCoord = new Vector2(),
                });
            }
            return vertices.ToArray();
        }

        public static VertexPositionNormal[] GetSphereVertices(float radius, float u_segments, float v_segments)
        {
            List<VertexPositionNormal> vertices = new List<VertexPositionNormal>();

            float halfPi = (float)(Math.PI * 0.5);
            float oneThroughPrecision = 1.0f / u_segments;
            float twoPiThroughPrecision = (float)(Math.PI * 2.0 * oneThroughPrecision);

            float theta1, theta2, theta3;
            Vector3 norm = new Vector3(), pos = new Vector3();

            for (uint j = 0; j < u_segments / 2; j++)
            {
                theta1 = (j * twoPiThroughPrecision) - halfPi;
                theta2 = ((j + 1) * twoPiThroughPrecision) - halfPi;

                for (uint i = 0; i <= v_segments; i++)
                {
                    theta3 = i * twoPiThroughPrecision;

                    norm.X = (float)(Math.Cos(theta1) * Math.Cos(theta3));
                    norm.Y = (float)Math.Sin(theta1);
                    norm.Z = (float)(Math.Cos(theta1) * Math.Sin(theta3));
                    pos.X = radius * norm.X;
                    pos.Y = radius * norm.Y;
                    pos.Z = radius * norm.Z;

                    vertices.Add(new VertexPositionNormal() { Position = pos, Normal = norm });

                    norm.X = (float)(Math.Cos(theta2) * Math.Cos(theta3));
                    norm.Y = (float)Math.Sin(theta2);
                    norm.Z = (float)(Math.Cos(theta2) * Math.Sin(theta3));
                    pos.X = radius * norm.X;
                    pos.Y = radius * norm.Y;
                    pos.Z = radius * norm.Z;

                    vertices.Add(new VertexPositionNormal() { Position = pos, Normal = norm });
                }
            }

            return vertices.ToArray();
        }

        public static VertexPositionNormal[] GetCylinderVertices(float radius, float height, float slices)
        {
            List<VertexPositionNormal> vertices = new List<VertexPositionNormal>();

            List<Vector3> discPointsBottom = new List<Vector3>();
            List<Vector3> discPointsTop = new List<Vector3>();

            float sliceArc = 360.0f / (float)slices;
            float angle = 0;
            for (int i = 0; i < slices; i++)
            {
                float x = radius * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                float z = radius * (float)Math.Sin(MathHelper.DegreesToRadians(angle));
                discPointsBottom.Add(new Vector3(x, 0, z));

                x = radius * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                z = radius * (float)Math.Sin(MathHelper.DegreesToRadians(angle));

                discPointsTop.Add(new Vector3(x, height, z));
                angle += sliceArc;
            }

            for (int i = 0; i < slices; i++)
            {
                Vector3 p2 = discPointsBottom[i];
                Vector3 p1 = new Vector3(discPointsBottom[(i + 1) % discPointsBottom.Count]);

                vertices.Add(new VertexPositionNormal() { Position = new Vector3(0, 0, 0) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p2.X, 0, p2.Z) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p1.X, 0, p1.Z) });

                p2 = discPointsTop[i % discPointsTop.Count];
                p1 = discPointsTop[(i + 1) % discPointsTop.Count];

                vertices.Add(new VertexPositionNormal() { Position = new Vector3(0, height, 0) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p1.X, height, p1.Z) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p2.X, height, p2.Z) });
            }

            for (int i = 0; i < slices; i++)
            {
                Vector3 p1 = discPointsBottom[i];
                Vector3 p2 = discPointsBottom[((i + 1) % discPointsBottom.Count)];
                Vector3 p3 = discPointsTop[i];
                Vector3 p4 = discPointsTop[(i + 1) % discPointsTop.Count];

                vertices.Add(new VertexPositionNormal() { Position = p1 });
                vertices.Add(new VertexPositionNormal() { Position = p3 });
                vertices.Add(new VertexPositionNormal() { Position = p4 });

                vertices.Add(new VertexPositionNormal() { Position = p1 });
                vertices.Add(new VertexPositionNormal() { Position = p4 });
                vertices.Add(new VertexPositionNormal() { Position = p2 });
            }

            return vertices.ToArray();
        }

        public static VertexPositionNormal[] GetConeVertices(float radiusBottom, float radiusTop, float height, float slices)
        {
            List<VertexPositionNormal> vertices = new List<VertexPositionNormal>();

            List<Vector3> discPointsBottom = new List<Vector3>();
            List<Vector3> discPointsTop = new List<Vector3>();

            float sliceArc = 360.0f / (float)slices;
            float angle = 0;
            for (int i = 0; i < slices; i++)
            {
                float x = radiusBottom * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                float z = radiusBottom * (float)Math.Sin(MathHelper.DegreesToRadians(angle));
                discPointsBottom.Add(new Vector3(x, 0, z));

                x = radiusTop * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                z = radiusTop * (float)Math.Sin(MathHelper.DegreesToRadians(angle));

                discPointsTop.Add(new Vector3(x, height, z));
                angle += sliceArc;
            }

            for (int i = 0; i < slices; i++)
            {
                Vector3 p2 = discPointsBottom[i];
                Vector3 p1 = new Vector3(discPointsBottom[(i + 1) % discPointsBottom.Count]);

                vertices.Add(new VertexPositionNormal() { Position = new Vector3(0, 0, 0) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p2.X, 0, p2.Z) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p1.X, 0, p1.Z) });

                p2 = discPointsTop[i % discPointsTop.Count];
                p1 = discPointsTop[(i + 1) % discPointsTop.Count];

                vertices.Add(new VertexPositionNormal() { Position = new Vector3(0, height, 0) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p1.X, height, p1.Z) });
                vertices.Add(new VertexPositionNormal() { Position = new Vector3(p2.X, height, p2.Z) });
            }

            for (int i = 0; i < slices; i++)
            {
                Vector3 p1 = discPointsBottom[i];
                Vector3 p2 = discPointsBottom[((i + 1) % discPointsBottom.Count)];
                Vector3 p3 = discPointsTop[i];
                Vector3 p4 = discPointsTop[(i + 1) % discPointsTop.Count];

                vertices.Add(new VertexPositionNormal() { Position = p1 });
                vertices.Add(new VertexPositionNormal() { Position = p3 });
                vertices.Add(new VertexPositionNormal() { Position = p4 });

                vertices.Add(new VertexPositionNormal() { Position = p1 });
                vertices.Add(new VertexPositionNormal() { Position = p4 });
                vertices.Add(new VertexPositionNormal() { Position = p2 });
            }

            var normals = CalculateNormals(vertices.Select(x => x.Position).ToList());
            for (int i = 0; i < vertices.Count; i++)
                vertices[i] = new VertexPositionNormal()
                {
                    Position = vertices[i].Position,
                    Normal = normals[i],
                };

            return vertices.ToArray();
        }

        public static VerticesIndices<VertexPositionNormalTexCoord> FromObj(byte[] data, float size = 1.0f)
        {
            return FromObj(new System.IO.MemoryStream(data), size);
        }

        public static VerticesIndices<VertexPositionNormalTexCoord> FromObj(System.IO.Stream stream, float size = 1.0f)
        {
            var obj = new ObjLoader();
            obj.LoadObj(stream);

            List<int> indices = new List<int>();
            List<VertexPositionNormalTexCoord> vertices = new List<VertexPositionNormalTexCoord>();
            foreach (var mesh in obj.Meshes) {
                foreach (var poly in mesh.Polygons.Values) {
                    indices.AddRange(poly.Indices.ToArray());

                    foreach (var vertex in poly.Vertices) {
                        vertices.Add(new VertexPositionNormalTexCoord()
                        {
                            Position = vertex.Position * size,
                            Normal = vertex.Normal,
                            TexCoord = vertex.TexCoord,
                        });
                    }
                }
            }
            return new VerticesIndices<VertexPositionNormalTexCoord>(vertices, indices);
        }

        public static Vector3[] CalculateNormals(List<Vector3> positions)
        {
            Vector3[] normals = new Vector3[positions.Count];
            for (int i = 0; i < positions.Count; i += 3)
            {
                if (i + 3 >= positions.Count)
                    break;

                var v1 = positions[i + 0];
                var v2 = positions[i + 1];
                var v3 = positions[i + 2];

                var v1to2 = v2 - v1;
                var v1to3 = v3 - v1;

                var normal = Vector3.Cross(v1to2, v1to3).Normalized();

                normals[i + 0] = normal;
                normals[i + 1] = normal;
                normals[i + 2] = normal;
            }
            return normals;
        }

        public static Vector3[] CalculateNormals(List<Vector3> positions, List<int> indices)
        {
            Vector3[] normals = new Vector3[positions.Count];
            for (int i = 0; i < indices.Count; i += 3)
            {
                if (i + 3 >= indices.Count)
                    break;

                var v1 = positions[indices[i + 0]];
                var v2 = positions[indices[i + 1]];
                var v3 = positions[indices[i + 2]];

                var v1to2 = v2 - v1;
                var v1to3 = v3 - v1;

                var normal = Vector3.Cross(v1to2, v1to3).Normalized();

                normals[indices[i + 0]] = normal;
                normals[indices[i + 1]] = normal;
                normals[indices[i + 2]] = normal;
            }
            return normals;
        }

        /// <summary>
        /// Checks if a point is inside a triangle.
        /// </summary>
        public static bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 point)
        {
            float abcArea = (float)Math.Abs((a.X * (b.Y - c.Y) +
                         b.X * (c.Y - a.Y) +
                         c.X * (a.Y - b.Y)) / 2.0);

            float pbcArea = (float)Math.Abs((point.X * (b.Y - c.Y) +
                         b.X * (c.Y - point.Y) +
                         c.X * (point.Y - b.Y)) / 2.0);

            float pacArea = (float)Math.Abs((a.X * (point.Y - c.Y) +
                         point.X * (c.Y - a.Y) +
                         c.X * (a.Y - point.Y)) / 2.0);

            float pabArea = (float)Math.Abs((a.X * (b.Y - point.Y) +
                         b.X * (point.Y - a.Y) +
                         point.X * (a.Y - b.Y)) / 2.0);

            return (abcArea == pbcArea + pacArea + pabArea);
        }

        /// <summary>
        /// Ensure an array of vertices is in a specific order.
        /// </summary>
        /// <param name="vertices">The vertices to reorder.</param>
        /// <param name="clockwise">True for clockwise. False for counterclockwise.</param>
        public static VertexIndex<Vector2>[] EnsureVertexOrder(Vector2[] vertices, bool clockwise = true)
        {
            VertexIndex<Vector2>[] indexedVertices = new VertexIndex<Vector2>[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                indexedVertices[i] = new VertexIndex<Vector2>(vertices[i], i);

            if (vertices.Length <= 1)
                return indexedVertices;
            float sum = 0;
            for (int i = 0; i < vertices.Length - 1; i++)
                sum += (vertices[i + 1].X - vertices[i].X) * (vertices[i + 1].Y + vertices[i].Y);
            sum += (vertices.First().X - vertices.Last().X) * (vertices.First().Y + vertices.Last().Y);
            if (clockwise && sum <= 0)
                return indexedVertices.Reverse().ToArray();
            else if (!clockwise && sum > 0)
                return indexedVertices.Reverse().ToArray();

            return indexedVertices;
        }

        public static int[] GetCollinearIndices(Vector3[] vertices, float tolerance = 0.00001f)
        {
            List<int> collinearIndices = new List<int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3d previousVertex = (Vector3d)(i != 0 ? vertices[i - 1] : vertices.Last());
                Vector3d nextVertex = (Vector3d)(i != vertices.Length - 1 ? vertices[i + 1] : vertices.First());

                if (Math.Abs(Vector3d.Dot(Vector3d.Normalize((Vector3d)vertices[i] - previousVertex), Vector3d.Normalize(nextVertex - (Vector3d)vertices[i])) - 1) <= tolerance)
                    collinearIndices.Add(i);
            }

            return collinearIndices.ToArray();
        }
        public static int[] GetCollinearIndices(Vector2[] vertices, float tolerance = 0.00001f)
        {
            List<int> collinearIndices = new List<int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2d previousVertex = (Vector2d)(i != 0 ? vertices[i - 1] : vertices.Last());
                Vector2d nextVertex = (Vector2d)(i != vertices.Length - 1 ? vertices[i + 1] : vertices.First());

                if (Math.Abs(Vector2d.Dot(Vector2d.Normalize((Vector2d)vertices[i] - previousVertex), Vector2d.Normalize(nextVertex - (Vector2d)vertices[i])) - 1) <= tolerance)
                    collinearIndices.Add(i);
            }

            return collinearIndices.ToArray();
        }

        /// <summary>
        /// Triangulate an ngon using the ear clipping method.
        /// Vertices should be inputted in counter-clockwise order.
        /// </summary>
        public static int[] TriangulateEarClip(Vector3[] vertices)
        {
            int[] collinearIndices = GetCollinearIndices(vertices);
            Matrix3 flatten;
            {
                List<Vector3> _verticesNoCollinear = vertices.ToList();
                for (int i = collinearIndices.Length - 1; i >= 0; i--)
                    _verticesNoCollinear.RemoveAt(collinearIndices[i]);

                Vector3 x = Vector3.Zero;
                Vector3 y = Vector3.Zero;
                Vector3 z = Vector3.Zero;
                for (int i = 0; i < _verticesNoCollinear.Count; i++)
                {
                    Vector3 previous = i > 0 ? _verticesNoCollinear[i - 1] : _verticesNoCollinear[_verticesNoCollinear.Count - 1];
                    Vector3 next = i < _verticesNoCollinear.Count - 1 ? _verticesNoCollinear[i + 1] : _verticesNoCollinear[0];

                    Vector3 _x = _verticesNoCollinear[i] - previous;
                    _x.X = Math.Abs(_x.X);
                    _x.Y = Math.Abs(_x.Y);
                    _x.Z = Math.Abs(_x.Z);
                    Vector3 _y = Vector3.Cross(_verticesNoCollinear[i] - previous, next - _verticesNoCollinear[i]); // We're assuming the ngon is flat
                    _y.X = Math.Abs(_y.X);
                    _y.Y = Math.Abs(_y.Y);
                    _y.Z = Math.Abs(_y.Z);
                    Vector3 _z = Vector3.Cross(x, y);
                    _z.X = Math.Abs(_z.X);
                    _z.Y = Math.Abs(_z.Y);
                    _z.Z = Math.Abs(_z.Z);

                    x += _x;
                    y += _y;
                    z += _z;
                }
                flatten = Matrix3.Invert(new Matrix3(Vector3.Normalize(x), Vector3.Normalize(y), Vector3.Normalize(z))); // A matrix to flatten to 2
            }

            LinkedList<VertexIndex<Vector2>> flattenedVertices = new LinkedList<VertexIndex<Vector2>>();
            {
                VertexIndex<Vector2>[] _flattenedVertices = EnsureVertexOrder(vertices.Select(x => (x * flatten).Xz).ToArray(), false);

                for (int i = 0; i < _flattenedVertices.Length; i++)
                {
                    if (collinearIndices.Contains(_flattenedVertices[i].Index))
                        continue;
                    flattenedVertices.AddLast(new VertexIndex<Vector2>(_flattenedVertices[i].Vertex, _flattenedVertices[i].Index));
                }
            }

            LinkedList<int> indices = new LinkedList<int>();

            if (flattenedVertices.Count == 3)
            {
                for (LinkedListNode<VertexIndex<Vector2>> vertexNode = flattenedVertices.First; vertexNode != null; vertexNode = vertexNode.Next)
                {
                    indices.AddLast(vertexNode.Value.Index);
                }
                return indices.ToArray();
            }
            else if (flattenedVertices.Count <= 2)
                return new int[0];
            
            LinkedListNode<VertexIndex<Vector2>> nextNode;
            for (LinkedListNode<VertexIndex<Vector2>> convexVertexNode = flattenedVertices.First; convexVertexNode != null; convexVertexNode = nextNode)
            {
                nextNode = convexVertexNode.Next;
                VertexIndex<Vector2>[] convexTri = new VertexIndex<Vector2>[3];
                convexTri[0] = (convexVertexNode.Previous ?? convexVertexNode.List.Last).Value;
                convexTri[1] = convexVertexNode.Value;
                convexTri[2] = (convexVertexNode.Next ?? convexVertexNode.List.First).Value;

                bool convex = (convexTri[1].Vertex.X - convexTri[0].Vertex.X) * (convexTri[2].Vertex.Y - convexTri[1].Vertex.Y) - (convexTri[2].Vertex.X - convexTri[1].Vertex.X) * (convexTri[1].Vertex.Y - convexTri[0].Vertex.Y) > 0;

                if (!convex)
                    continue;

                bool vertexContained = false;
                for (LinkedListNode<VertexIndex<Vector2>> reflexVertexNode = flattenedVertices.First; reflexVertexNode != null; reflexVertexNode = reflexVertexNode.Next)
                {
                    VertexIndex<Vector2>[] reflexTri = new VertexIndex<Vector2>[3];
                    reflexTri[0] = (reflexVertexNode.Previous ?? reflexVertexNode.List.Last).Value;
                    reflexTri[1] = reflexVertexNode.Value;
                    reflexTri[2] = (reflexVertexNode.Next ?? reflexVertexNode.List.First).Value;
                    bool reflex = (convexTri[1].Vertex.X - convexTri[0].Vertex.X) * (convexTri[2].Vertex.Y - convexTri[1].Vertex.Y) - (convexTri[2].Vertex.X - convexTri[1].Vertex.X) * (convexTri[1].Vertex.Y - convexTri[0].Vertex.Y) < 0;
                    if (reflex && PointInTriangle(convexTri[0].Vertex, convexTri[1].Vertex, convexTri[2].Vertex, reflexVertexNode.Value.Vertex))
                    {
                        vertexContained = true;
                        break;
                    }
                }
                if (vertexContained)
                    continue;

                indices.AddLast(convexTri[0].Index);
                indices.AddLast(convexTri[1].Index);
                indices.AddLast(convexTri[2].Index);
                flattenedVertices.Remove(convexVertexNode);

                if (flattenedVertices.Count == 3)
                {
                    for (LinkedListNode<VertexIndex<Vector2>> vertexNode = flattenedVertices.First; vertexNode != null; vertexNode = vertexNode.Next)
                    {
                        indices.AddLast(vertexNode.Value.Index);
                    }
                    break;
                }
            }

            return indices.ToArray();
        }

        public static VerticesIndices<Vector3> SplitVertices(Vector3[] vertices, int[] indices)
        {
            List<Vector3> newVertices = new List<Vector3>(vertices.Length); // Might as well feed in a capacity. Will probably be resized past this point anyway though
            List<int> newIndices = new List<int>(indices.Length);

            for (int i = 0; i < indices.Length; i += 3)
            {
                newIndices.Add(newVertices.Count + 0);
                newIndices.Add(newVertices.Count + 1);
                newIndices.Add(newVertices.Count + 2);

                newVertices.Add(vertices[indices[i + 0]]);
                newVertices.Add(vertices[indices[i + 1]]);
                newVertices.Add(vertices[indices[i + 2]]);
            }

            return new VerticesIndices<Vector3>(newVertices, newIndices);
        }

        /// <summary>
        /// A list of vertices and a list of indices
        /// </summary>
        public class VerticesIndices<T>
        {
            public List<T> Vertices;
            public List<int> Indices;

            public VerticesIndices()
            {
                Vertices = new List<T>();
                Indices = new List<int>();
            }

            public VerticesIndices(List<T> vertices, List<int> indices)
            {
                Vertices = vertices;
                Indices = indices;
            }
        }

        /// <summary>
        /// A vertex labeled with an arbitrary index.
        /// Basically just a vertex that remembers a number.
        /// </summary>
        public struct VertexIndex<T>
        {
            public T Vertex;
            public int Index;

            public VertexIndex(T vertex, int index)
            {
                Vertex = vertex;
                Index = index;
            }
        }
    }
}
