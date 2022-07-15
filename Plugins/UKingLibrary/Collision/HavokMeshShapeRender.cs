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
        RenderMesh<HavokMeshShapeVertex> ShapeMesh;

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
                GL.Enable(EnableCap.CullFace);
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(-4f, 1f);
                ShapeMesh.Draw(context);
                GL.Disable(EnableCap.PolygonOffsetFill);
                GL.Disable(EnableCap.CullFace);
                GL.Disable(EnableCap.Blend);
                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

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

        public void LoadShape(hkpBvCompressedMeshShape shape)
        {
            // Obtain mesh data
            MeshContainer mesh = shape.ToMesh();

            // Get vertices in a good format for this
            HavokMeshShapeVertex[] vertices = new HavokMeshShapeVertex[mesh.Vertices.Count];
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                vertices[i] = new HavokMeshShapeVertex()
                {
                    Position = new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z) * GLContext.PreviewScale
                };
            }

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

            // Set misc data
            var normals = DrawingHelper.CalculateNormals(vertices.Select(x => x.Position).ToList(), indices.ToList());
            for (int i = 0; i < vertices.Count(); i++)
            {
                vertices[i].Normal = normals[i];
                vertices[i].VertexColor = new Vector4(0, 0.5f, 1, 0.5f);
            }


            ShapeMesh = new RenderMesh<HavokMeshShapeVertex>(vertices, indices.ToArray(), OpenTK.Graphics.OpenGL.PrimitiveType.Triangles);
        }

        public void LoadNavmesh(hkaiNavMesh navmesh)
        {
            HavokMeshShapeVertex[] vertices = navmesh.m_vertices.Select(v => new HavokMeshShapeVertex
            {
                Position = new Vector3(v.X, v.Y, v.Z) * GLContext.PreviewScale
            }).ToArray();

            List<int> indices = new List<int>(vertices.Length); // Setting capacity just as rough estimate.
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
                System.Numerics.Vector4 firstVertex = navmesh.m_vertices[navmesh.m_edges[face.m_startEdgeIndex + 0].m_a];
                faceVertices.Add(new Tuple<Vector3, int>(new Vector3(firstVertex.X, firstVertex.Y, firstVertex.Z), navmesh.m_edges[face.m_startEdgeIndex + 0].m_a));
                for (int i = 1; i < face.m_numEdges; i++)
                {
                    System.Numerics.Vector4 vertex = navmesh.m_vertices[navmesh.m_edges[face.m_startEdgeIndex + i].m_a];
                    faceVertices.Add(new Tuple<Vector3, int>(new Vector3(vertex.X, vertex.Y, vertex.Z), navmesh.m_edges[face.m_startEdgeIndex + i].m_a));
                }
                indices.AddRange(DrawingHelper.TriangulateEarClip(faceVertices.Select(x=>x.Item1).ToArray()).Select(x => faceVertices[x].Item2));
            }

            Vector3[] normals = DrawingHelper.CalculateNormals(vertices.Select(x => x.Position).ToList(), indices);
            for (int i = 0; i < vertices.Count(); i++)
            {
                vertices[i].Normal = normals[i];
                vertices[i].VertexColor = new Vector4(0, 1f, 0.5f, 0.5f);
            }

            ShapeMesh = new RenderMesh<HavokMeshShapeVertex>(vertices, indices.ToArray(), OpenTK.Graphics.OpenGL.PrimitiveType.Triangles);
        }

        public void SetBounding(BoundingNode boundingNode)
        {
            _boundingNode = boundingNode;
        }

        public struct HavokMeshShapeVertex
        {
            [RenderAttribute("vPosition", VertexAttribPointerType.Float, 0)]
            public Vector3 Position;

            [RenderAttribute("vNormalWorld", VertexAttribPointerType.Float, 12)]
            public Vector3 Normal;

            [RenderAttribute("vVertexColor", VertexAttribPointerType.Float, 24)]
            public Vector4 VertexColor;
        }
    }
}
