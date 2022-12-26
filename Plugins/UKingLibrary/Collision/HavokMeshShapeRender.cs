using System;
using System.Collections.Generic;
using System.Linq;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using HKX2;
using HKX2Builders;
using HKX2Builders.Extensions;


namespace UKingLibrary
{
    public class HavokMeshShapeRender : EditableObject, IColorPickable
    {
        private RenderMesh<HavokMeshShapeVertex> ShapeMesh;
        public HavokMeshShapeVertex[] Vertices;
        public int[] Indices;

        private BoundingNode _boundingNode;
        public override BoundingNode BoundingNode => _boundingNode;

        private const bool COLLISIONSHAPE_DEBUG = false;

        public HavokMeshShapeRender(NodeBase parent) : base(parent)
        {
            UINode.Tag = this;
            UINode.Header = "Havok Shape";

            if (!COLLISIONSHAPE_DEBUG)
                CanSelect = false;
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if (pass == Pass.TRANSPARENT)
            {
                var shader = GlobalShaders.GetShader("HAVOK_SHAPE");
                context.CurrentShader = shader;
                shader.SetTransform(GLConstants.ModelMatrix, Transform);

                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.PointSize(4);
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(-4f, 1f);
                ShapeMesh.Draw(context);
                GL.Disable(EnableCap.PolygonOffsetFill);
                GL.Disable(EnableCap.CullFace);
                GL.Disable(EnableCap.Blend);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                BoundingNode?.Box.DrawSolid(context, Matrix4.Identity, Vector4.One);
            }
        }

        public void DrawColorPicking(GLContext context)
        {
            if (!COLLISIONSHAPE_DEBUG)
                return; // We want to be able to click through this.

            var shader = GlobalShaders.GetShader("PICKING");
            context.CurrentShader = shader;

            shader.SetTransform(GLConstants.ModelMatrix, this.Transform);

            context.ColorPicker.SetPickingColor(this, shader);
            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(-4f, 1f);
            ShapeMesh.Draw(context);
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.Disable(EnableCap.CullFace);
        }

        public void DrawForNavmeshPaint(GLContext context, MapNavmeshEditor.NavmeshEditFilter editFilter, int shapeIndex, int resX, int resY)
        {
            var shader = GlobalShaders.GetShader("NAVMESH_PAINT");
            context.CurrentShader = shader;

            shader.SetInt("u_shapeIndex", shapeIndex);
            shader.SetVector2("u_resolution", new Vector2(resX, resY));

            shader.SetFloat("u_filterAngleMax", editFilter.AngleMax);

            shader.SetTransform(GLConstants.ModelMatrix, this.Transform);

            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(-4f, 1f);
            ShapeMesh.Draw(context);
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.Disable(EnableCap.CullFace);
        }

        public void LoadShape(hkpBvCompressedMeshShape shape)
        {
            // Obtain mesh data
            MeshContainer mesh = shape.ToMesh();

            // Get indices in a good format for this.
            // We're also gonna triangulate our quad data while we're at it:
            List<int> indices = new List<int>(mesh.Primitives.Count * 6); // 6 because for each quad we'll have 6 indices once triangulated.
            for (int i = 0; i < mesh.Primitives.Count; i++)
            {
                indices.Add(mesh.Primitives[i][0]);
                indices.Add(mesh.Primitives[i][1]);
                indices.Add(mesh.Primitives[i][2]);

                indices.Add(mesh.Primitives[i][2]);
                indices.Add(mesh.Primitives[i][3]);
                indices.Add(mesh.Primitives[i][0]);
            }

            // Oftentimes the mesh quads will have duplicate data to turn them into effective triangles.
            // Since we triangulated this, we might have a few corrupted triangles (effective lines) that we need to get rid of.
            for (int i = indices.Count - 1; i >= 0; i-= 3)
            {
                bool removeTri = false;
                if (indices[i] == indices[i - 1])
                    removeTri = true;
                else if (indices[i] == indices[i - 2])
                    removeTri = true;
                else if (indices[i - 1] == indices[i - 2])
                    removeTri= true;

                if (removeTri)
                    indices.RemoveRange(i - 2, 3);
            }

            DrawingHelper.VerticesIndices<Vector3> splitVertexMesh = DrawingHelper.SplitVertices(mesh.Vertices.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray(), indices.ToArray());
            indices = splitVertexMesh.Indices;

            // Get vertices in a good format for this
            HavokMeshShapeVertex[] vertices = splitVertexMesh.Vertices.Select(v => new HavokMeshShapeVertex
            {
                Position = new Vector3(v.X, v.Y, v.Z) * GLContext.PreviewScale
            }).ToArray();

            // Set misc data
            var normals = DrawingHelper.CalculateNormals(vertices.Select(x => x.Position).ToList(), indices.ToList());
            for (int i = 0; i < vertices.Count(); i++)
            {
                vertices[i].Normal = normals[i];
                vertices[i].VertexColor = new Vector4(0, 0.5f, 1, 0.5f);
                vertices[i].VertexIndex = (float)i;
            }


            ShapeMesh = new RenderMesh<HavokMeshShapeVertex>(vertices, indices.ToArray(), OpenTK.Graphics.OpenGL.PrimitiveType.Triangles);
            Vertices = vertices;
            Indices = indices.ToArray();
        }

        public void LoadNavmesh(hkaiNavMesh navmesh, Vector3 origin = default)
        {
            for (int i = 0; i < navmesh.m_vertices.Count; i++)
            {
                break;
                TransformableObject r = new TransformableObject(UINode);
                var pos = navmesh.m_vertices[i];
                r.Transform.Position = new OpenTK.Vector3(pos.X * GLContext.PreviewScale + origin.X, pos.Y * GLContext.PreviewScale + origin.Y, pos.Z * GLContext.PreviewScale + origin.Z);
                r.Transform.UpdateMatrix(true); ;
                GLContext.ActiveContext.Scene.AddRenderObject(r);
            }
            for (int i = 0; i < navmesh.m_edges.Count; i++)
            {
                break;
                if (navmesh.m_edges[i].m_oppositeFace == 0xFFFFFFFF)
                {
                    TransformableObject r = new TransformableObject(UINode);
                    var midpoint = (navmesh.m_vertices[navmesh.m_edges[i].m_a] + navmesh.m_vertices[navmesh.m_edges[i].m_b]) / 2;
                    r.Transform.Position = new OpenTK.Vector3(midpoint.X * GLContext.PreviewScale + origin.X, midpoint.Y * GLContext.PreviewScale + origin.Y, midpoint.Z * GLContext.PreviewScale + origin.Z);
                    r.Transform.UpdateMatrix(true);;
                    GLContext.ActiveContext.Scene.AddRenderObject(r);
                    continue;
                    var a = navmesh.m_vertices[navmesh.m_edges[i].m_a];
                    a.Y += 10f;
                    navmesh.m_vertices[navmesh.m_edges[i].m_a] = a;

                    var b = navmesh.m_vertices[navmesh.m_edges[i].m_b];
                    b.Y += 10f;
                    navmesh.m_vertices[navmesh.m_edges[i].m_b] = b;
                }
            }
            foreach (hkaiStreamingSet ss in navmesh.m_streamingSets)
            {
                break;
                foreach (hkaiStreamingSetNavMeshConnection meshConnection in ss.m_meshConnections)
                {
                    TransformableObject r = new TransformableObject(UINode);
                    var midpoint = (navmesh.m_vertices[navmesh.m_edges[meshConnection.m_edgeIndex].m_a] + navmesh.m_vertices[navmesh.m_edges[meshConnection.m_edgeIndex].m_b]) / 2;
                    r.Transform.Position = new OpenTK.Vector3(midpoint.X * GLContext.PreviewScale + origin.X, midpoint.Y * GLContext.PreviewScale + origin.Y, midpoint.Z * GLContext.PreviewScale + origin.Z);
                    r.Transform.UpdateMatrix(true);

                    GLContext.ActiveContext.Scene.AddRenderObject(r);
                }
            }

            List<int> indices = new List<int>(navmesh.m_vertices.Count); // Setting capacity just as rough estimate.
            foreach (hkaiNavMeshFace face in navmesh.m_faces)
            {
                if (face.m_numEdges == 3)
                {
                    // Version for triangles is pretty simple
                    indices.Add(navmesh.m_edges[face.m_startEdgeIndex + 0].m_a);
                    indices.Add(navmesh.m_edges[face.m_startEdgeIndex + 0].m_b);
                    indices.Add(navmesh.m_edges[face.m_startEdgeIndex + 1].m_b);
                    continue;
                }

                List<Tuple<Vector3, int>> faceVertices = new List<Tuple<Vector3, int>>(face.m_numEdges + 1);

                List<hkaiNavMeshEdge> faceEdges = navmesh.m_edges.GetRange(face.m_startEdgeIndex, face.m_numEdges);
                int edgeIdx = 0;
                for (int i = 0; i < faceEdges.Count; i++)
                {
                    System.Numerics.Vector4 vertex = navmesh.m_vertices[faceEdges[edgeIdx].m_b];
                    faceVertices.Add(new Tuple<Vector3, int>(new Vector3(vertex.X, vertex.Y, vertex.Z), faceEdges[edgeIdx].m_b));

                    edgeIdx = faceEdges.FindIndex(x => x.m_a == faceEdges[edgeIdx].m_b);
                }
                // Improper way but should work just as well:
                //for (int i = 0; i < face.m_numEdges; i++)
                //{
                //    System.Numerics.Vector4 vertex = navmesh.m_vertices[navmesh.m_edges[face.m_startEdgeIndex + i].m_a];
                //    faceVertices.Add(new Tuple<Vector3, int>(new Vector3(vertex.X, vertex.Y, vertex.Z), navmesh.m_edges[face.m_startEdgeIndex + i].m_a));
                //}

                indices.AddRange(DrawingHelper.TriangulateEarClip(faceVertices.Select(x=>x.Item1).ToArray()).Select(x => faceVertices[x].Item2));
            }

            DrawingHelper.VerticesIndices<Vector3> splitVertexMesh = DrawingHelper.SplitVertices(navmesh.m_vertices.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray(), indices.ToArray());
            indices = splitVertexMesh.Indices;

            HavokMeshShapeVertex[] vertices = splitVertexMesh.Vertices.Select(v => new HavokMeshShapeVertex
            {
                Position = new Vector3(v.X, v.Y, v.Z) * GLContext.PreviewScale
            }).ToArray();

            Vector3[] normals = DrawingHelper.CalculateNormals(vertices.Select(x => x.Position).ToList(), indices);
            for (int i = 0; i < vertices.Count(); i++)
            {
                vertices[i].Normal = normals[i];
                vertices[i].VertexColor = new Vector4(0, 1f, 0.5f, 0.5f);
                vertices[i].VertexIndex = (float)i;
            }

            ShapeMesh = new RenderMesh<HavokMeshShapeVertex>(vertices, indices.ToArray(), OpenTK.Graphics.OpenGL.PrimitiveType.Triangles);
            Vertices = vertices;
            Indices = indices.ToArray();

            Vector3 bMin = new Vector3(navmesh.m_aabb.m_min.X, navmesh.m_aabb.m_min.Y, navmesh.m_aabb.m_min.Z) * GLContext.PreviewScale + origin;
            Vector3 bMax = new Vector3(navmesh.m_aabb.m_max.X, navmesh.m_aabb.m_max.Y, navmesh.m_aabb.m_max.Z) * GLContext.PreviewScale + origin;
            SetBounding(new BoundingNode(bMin, bMax));
        }

        public void SetBounding(BoundingNode boundingNode)
        {
            _boundingNode = boundingNode;
        }

        public struct HavokMeshShapeVertex
        {
            [RenderAttribute("vPosition", VertexAttribPointerType.Float)]
            public Vector3 Position;

            [RenderAttribute("vNormalWorld", VertexAttribPointerType.Float)]
            public Vector3 Normal;

            [RenderAttribute("vVertexColor", VertexAttribPointerType.Float)]
            public Vector4 VertexColor;

            [RenderAttribute("vVertexIndex", VertexAttribPointerType.Float)]
            public float VertexIndex;
        }
    }
}
