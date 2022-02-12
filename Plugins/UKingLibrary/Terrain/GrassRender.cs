using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;

namespace UKingLibrary.Rendering
{
    public class GrassRender : EditableObject, IFrustumCulling
    {
        public override bool UsePostEffects => false;

        public const float MAP_HEIGHT_SCALE = 0.0122075f;

        public const int MAP_TILE_LENGTH = 64;
        public const int MAP_TILE_SIZE = MAP_TILE_LENGTH * MAP_TILE_LENGTH;
        public const int INDEX_COUNT_SIDE = MAP_TILE_LENGTH - 1;

        float[] TEXTURE_INDEX_MAP = new float[] { 0, 1, 2, 3, 4, 5, 6, 7 };

        RenderMesh<GrassVertex> GrassMesh;

        static int[] IndexBuffer;

        public bool EnableFrustumCulling => true;
        public bool InFrustum { get; set; } = true;

        BoundingNode Bounding = new BoundingNode();

        public bool IsInsideFrustum(GLContext context)
        {
            return context.Camera.InFustrum(Bounding);
        }

        public GrassRender(NodeBase parent = null) : base(null)
        {
            IsVisible = true;
            CanSelect = false;
            this.Transform.TransformUpdated += delegate {
                Bounding.UpdateTransform(this.Transform.TransformMatrix);
            };
        }

        public void LoadGrassData(byte[] heightBuffer, byte[] terrHeightBuffer, float tileSectionScale)
        {
            //Load all attribute data.
            var positionData = GetGrassVertices(heightBuffer, terrHeightBuffer);
            var texCoords = GetTexCoords();
            //Normals calculation
            Vector3[] positions = new Vector3[positionData.Length];
            for (int i = 0; i < positionData.Length; i++)
                positions[i] = positionData[i].Translate;
            //Fixed index buffer. It can be kept static as all terrain tiles use the same index layout.
            if (IndexBuffer == null)
                IndexBuffer = GetIndexBuffer();

            // Apply X and Z scaling
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i].X *= tileSectionScale;
                positions[i].Z *= tileSectionScale;
            }

            //Normals calculation
            var normals = DrawingHelper.CalculateNormals(positions.ToList(), IndexBuffer.ToList());
            //Tangents calculation
            var tangents = GetTangents(positions);
            
            //Prepare the terrain vertices for rendering
            GrassVertex[] vertices = new GrassVertex[positionData.Length];
            for (int i = 0; i < positionData.Length; i++)
            {
                vertices[i] = new GrassVertex()
                {
                    Position = positions[i] * GLContext.PreviewScale,
                    Normal = normals[i],
                    TangentWorld = tangents[i],
                    Color = positionData[i].Color,
                    DebugHighlight = (
                    i % MAP_TILE_LENGTH == 0 ||
                    i % MAP_TILE_LENGTH == INDEX_COUNT_SIDE ||
                    i / MAP_TILE_LENGTH == 0 ||
                    i / MAP_TILE_LENGTH == INDEX_COUNT_SIDE
                    ) ? new Vector3(1) : new Vector3(0),
                };
            }
            //Calculate bounding data for frustum culling
            Bounding.Box = BoundingBox.FromVertices(vertices.Select(x => x.Position).ToArray());
            Bounding.Radius = (Bounding.Box.Max - Bounding.Box.Min).Length;

            //Finish loading the terrain mesh
            GrassMesh = new RenderMesh<GrassVertex>(vertices, IndexBuffer, PrimitiveType.Triangles);
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if ((GrassMesh == null || pass != Pass.TRANSPARENT || !InFrustum))
                return;

            var shader = GlobalShaders.GetShader("GRASS");
            context.CurrentShader = shader;
            shader.SetTransform(GLConstants.ModelMatrix, this.Transform);
            shader.SetFloat("uBrightness", 2.0f); // Hack to fit in (normals calculation is kinda off or something)
            shader.SetBool("uDebugSections", PluginConfig.DebugTerrainSections);

            GLL.Enable(EnableCap.Blend);
            GLL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GLL.Disable(EnableCap.CullFace);

            GrassMesh.Draw(context);
        }

        private GrassVertexData[] GetGrassVertices(byte[] grassHeightBuffer, byte[] terrHeightBuffer)
        {

            GrassVertexData[] vertices = new GrassVertexData[MAP_TILE_SIZE];
            using (var grassReader = new FileReader(grassHeightBuffer))
            using (var terrReader = new FileReader(terrHeightBuffer))
            {
                int vertexIndex = 0;
                for (float y = 0; y < MAP_TILE_LENGTH; y++)
                {
                    float normY = y / (float)INDEX_COUNT_SIDE;
                    for (float x = 0; x < MAP_TILE_LENGTH; x++)
                    {
                        // HGHTs have a 16-1 point ratio to EXTMs.. We should average HGHT points.. maybe? I'm not quite sure if they line up or not.
                        float terrHeightValue = terrReader.ReadUInt16(((((int)y * 4) * TerrainRender.MAP_TILE_LENGTH) + ((int)x * 4)) * 2) * MAP_HEIGHT_SCALE;
                        float grassHeightValue = grassReader.ReadByte() * MAP_HEIGHT_SCALE;
                        float red = ((float)grassReader.ReadByte()) / 256; // R value
                        float green = ((float)grassReader.ReadByte()) / 256; // G value
                        float blue = ((float)grassReader.ReadByte()) / 256; // B value

                        GrassVertexData vertexData = new GrassVertexData()
                        {
                            Translate = new Vector3(x / (float)INDEX_COUNT_SIDE - 0.5f, terrHeightValue + grassHeightValue, normY - 0.5f),
                            Color = new Vector3(red, green, blue)
                        };

                        vertices[vertexIndex++] = vertexData;
                    }
                }
            }
            return vertices;
        }

        public class GrassVertexData
        {
            public Vector3 Translate;
            public Vector3 Color;
        }

        private Vector2[] GetTexCoords()
        {
            Vector2[] vertices = new Vector2[MAP_TILE_SIZE];

            float uvBaseScale = 10;
            int vertexIndex = 0;

            for (float y = 0; y < MAP_TILE_LENGTH; y++)
            {
                float normY = y / (float)INDEX_COUNT_SIDE;
                for (float x = 0; x < MAP_TILE_LENGTH; x++)
                {
                    float normX = x / (float)INDEX_COUNT_SIDE;

                    vertices[vertexIndex++] = new Vector2(
                        uvBaseScale * normX,
                        uvBaseScale * normY
                        );
                }
            }
            return vertices;
        }

        private Vector3[] GetTangents(Vector3[] vertices)
        {
            Vector3[] tangents = new Vector3[MAP_TILE_SIZE];
            for (int y = 0; y < MAP_TILE_LENGTH; y++)
            {
                for (int x = 0; x < MAP_TILE_LENGTH; x++)
                {
                    int index = (y * MAP_TILE_LENGTH) + x;

                    if(x == MAP_TILE_LENGTH - 1) // If this is the last vertex in the row use the last tangent
                    {
                        tangents[index] = tangents[index - 1];
                        break;
                    }

                    var vertex = vertices[index];
                    var nextVertex = vertices[index + 1];
                    // I have no idea why I have to do this... didn't we already?
                    vertex.Y *= MAP_HEIGHT_SCALE;
                    nextVertex.Y *= MAP_HEIGHT_SCALE;

                    Vector3 tangent = new Vector3(nextVertex - vertex);
                    tangent.Normalize();

                    tangents[index] = tangent;
                }
            }
            return tangents;
        }

        private int[] GetIndexBuffer(int indexCountSide = INDEX_COUNT_SIDE, int tileLength = MAP_TILE_LENGTH)
        {
            int[] indexBuffer = new int[indexCountSide * indexCountSide * 2 * 3];// x*y, 2 triangles per square, 3 points per triangle

            int i = 0;
            for (int y = 0; y < indexCountSide; y++)
            {
                int indexTop = (y) * tileLength;
                int indexBottom = (y + 1) * tileLength;

                for (int x = 0; x < indexCountSide; x++)
                {
                    indexBuffer[i++] = indexTop;
                    indexBuffer[i++] = indexBottom;
                    indexBuffer[i++] = indexBottom + 1;

                    indexBuffer[i++] = indexBottom + 1;
                    indexBuffer[i++] = indexTop + 1;
                    indexBuffer[i++] = indexTop;

                    ++indexTop;
                    ++indexBottom;

                }
            }
            return indexBuffer;
        }

        private Vector3[] GetTexIndexBuffer(byte[] materialBuffer)
        {
            Vector3[] vertices = new Vector3[materialBuffer.Length / 4];
            int vertexIndex = 0;
            for (int i = 0; i < materialBuffer.Length; i += 4)
            {
                vertices[vertexIndex++] = new Vector3(
                    TEXTURE_INDEX_MAP[materialBuffer[i]],
                    TEXTURE_INDEX_MAP[materialBuffer[i + 1]],
                    materialBuffer[i + 2]);
            }
            return vertices;
        }

        public struct GrassVertex
        {
            [RenderAttribute("vPosition", VertexAttribPointerType.Float, 0)]
            public Vector3 Position;

            [RenderAttribute("vNormalWorld", VertexAttribPointerType.Float, 12)]
            public Vector3 Normal;

            [RenderAttribute("vTangentWorld", VertexAttribPointerType.Float, 24)]
            public Vector3 TangentWorld;

            [RenderAttribute("vColor", VertexAttribPointerType.Float, 36)]
            public Vector3 Color;

            [RenderAttribute("vDebugHighlight", VertexAttribPointerType.Float, 48)]
            public Vector3 DebugHighlight;

            public GrassVertex(Vector3 position, Vector3 normal, Vector3 tangentWorld, Vector3 color, Vector3 debugHighlight)
            {
                Normal = normal;
                TangentWorld = tangentWorld;
                Position = position;
                Color = color;
                DebugHighlight = debugHighlight;
            }
        }
    }
}