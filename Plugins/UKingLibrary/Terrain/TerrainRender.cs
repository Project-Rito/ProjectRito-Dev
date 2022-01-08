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
    public class TerrainRender : EditableObject, IFrustumCulling
    {
        public override bool UsePostEffects => false;

        const float MAP_HEIGHT_SCALE = 0.0122075f;

        const int MAP_TILE_LENGTH = 256;
        const int MAP_TILE_SIZE = MAP_TILE_LENGTH * MAP_TILE_LENGTH;
        const int INDEX_COUNT_SIDE = MAP_TILE_LENGTH - 1;

        float[] TEXTURE_INDEX_MAP = new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 17, 18, 0, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 7, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 0, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82 };
        float[] TEXTURE_UV_MAP = new float[] { 0.1f, 0.1f, 0.05f, 0.05f, 0.1f, 0.1f, 0.04f, 0.04f, 0.05f, 0.05f, 0.1f, 0.1f, 0.05f, 0.05f, 0.05f, 0.05f, 0.1f, 0.1f, 0.1f, 0.1f, 0.05f, 0.05f, 0.09f, 0.09f, 0.05f, 0.05f, 0.1f, 0.1f, 0.2f, 0.2f, 0.14f, 0.14f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.07f, 0.07f, 0.07f, 0.07f, 0.05f, 0.05f, 0.15f, 0.15f, 0.1f, 0.1f, 0.1f, 0.1f, 0.07f, 0.07f, 0.04f, 0.04f, 0.05f, 0.16f, 0.03f, 0.03f, 0.05f, 0.05f, 0.05f, 0.05f, 0.03f, 0.03f, 0.05f, 0.05f, 0.45f, 0.45f, 0.2f, 0.2f, 0.1f, 0.1f, 0.59f, 0.59f, 0.15f, 0.15f, 0.2f, 0.2f, 0.35f, 0.35f, 0.2f, 0.2f, 0.1f, 0.1f, 0.15f, 0.15f, 0.2f, 0.2f, 0.15f, 0.15f, 0.2f, 0.2f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.1f, 0.1f, 0.1f, 0.1f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.1f, 0.1f, 0.08f, 0.08f, 0.04f, 0.04f, 0.1f, 0.1f, 0.05f, 0.05f, 0.05f, 0.05f, 0.1f, 0.1f, 0.25f, 0.25f, 0.04f, 0.05f, 0.08f, 0.08f, 0.08f, 0.08f, 0.2f, 0.2f, 0.1f, 0.1f, 0.15f, 0.15f, 0.04f, 0.04f, 0.25f, 0.25f, 0.05f, 0.05f, 0.15f, 0.15f, 0.05f, 0.05f, 0.08f, 0.08f, 0.1f, 0.1f, 0.07f, 0.07f, 0.05f, 0.05f, 0.23f, 0.23f, 0.16f, 0.16f, 0.16f, 0.16f, 0.04f, 0.04f, 0.1f, 0.1f, 0.05f, 0.05f, 0.1f, 0.1f };

        RenderMesh<TerrainVertex> TerrainMesh;
        static GLTexture2DArray TerrainTexture_Alb;
        static GLTexture2DArray TerrainTexture_Nrm;

        static int[] IndexBuffer;

        public bool EnableFrustumCulling => true;
        public bool InFrustum { get; set; } = true;

        BoundingNode Bounding = new BoundingNode();

        public bool IsInsideFrustum(GLContext context)
        {
            return context.Camera.InFustrum(Bounding);
        }

        public TerrainRender(NodeBase parent = null) : base(null)
        {
            IsVisible = true;
            CanSelect = false;
            this.Transform.TransformUpdated += delegate {
                Bounding.UpdateTransform(this.Transform.TransformMatrix);
            };
        }

        public void LoadTerrainData(byte[] heightBuffer, byte[] materialBuffer)
        {
            //Load all attribute data.
            var positions = GetTerrainVertices(heightBuffer);
            var texCoords = GetTexCoords(materialBuffer);
            var materialMap = GetTexIndexBuffer(materialBuffer);
            //Normals calculation
            var normals = DrawingHelper.CalculateNormals(positions.ToList());
            //Fixed index buffer. It can be kept static as all terrain tiles use the same index layout.
            if (IndexBuffer == null)
                IndexBuffer = GetIndexBuffer();
            //Prepare the terrain vertices for rendering
            TerrainVertex[] vertices = new TerrainVertex[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                vertices[i] = new TerrainVertex()
                {
                    Position = positions[i] * GLContext.PreviewScale,
                    Normal = normals[i],
                    TexCoords = texCoords[i],
                    MaterialMap = materialMap[i],
                };
            }
            //Calculate bounding data for frustum culling
            Bounding.Box = BoundingBox.FromVertices(vertices.Select(x => x.Position).ToArray());
            Bounding.Radius = (Bounding.Box.Max - Bounding.Box.Min).Length;

            //Finish loading the terrain mesh
            TerrainMesh = new RenderMesh<TerrainVertex>(vertices, IndexBuffer, PrimitiveType.Triangles);
            LoadTerrainTextures();
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if (TerrainMesh == null || pass == Pass.TRANSPARENT || !InFrustum)
                return;

            var shader = GlobalShaders.GetShader("TERRAIN");
            context.CurrentShader = shader;
            shader.SetTransform(GLConstants.ModelMatrix, this.Transform);
            shader.SetTexture(TerrainTexture_Alb, "texTerrain_Alb", 1);
            shader.SetTexture(TerrainTexture_Nrm, "texTerrain_Nrm", 2);

            TerrainMesh.Draw(context);
        }

        private Vector3[] GetTerrainVertices(byte[] heightBuffer)
        {
            Vector3[] vertices = new Vector3[MAP_TILE_SIZE];
            using (var reader = new FileReader(heightBuffer))
            {
                int vertexIndex = 0;
                for (float y = 0; y < MAP_TILE_LENGTH; y++)
                {
                    float normY = y / (float)INDEX_COUNT_SIDE;
                    for (float x = 0; x < MAP_TILE_LENGTH; x++)
                    {
                        float heightValue = reader.ReadUInt16() * MAP_HEIGHT_SCALE;
                        //Terrain vertices range from 0 - 1
                        vertices[vertexIndex++] = new Vector3(x / (float)INDEX_COUNT_SIDE - 0.5f, heightValue, normY - 0.5f);
                    }
                }
            }
            return vertices;
        }

        private Vector4[] GetTexCoords(byte[] materialBuffer)
        {
            Vector4[] vertices = new Vector4[MAP_TILE_SIZE];

            float uvBaseScale = 100;
            int vertexIndex = 0;
            int matIndex = 0;

            for (float y = 0; y < MAP_TILE_LENGTH; y++)
            {
                float normY = y / (float)INDEX_COUNT_SIDE;
                for (float x = 0; x < MAP_TILE_LENGTH; x++)
                {
                    float normX = x / (float)INDEX_COUNT_SIDE;
                    Vector2 uvScaleA = new Vector2(
                        TEXTURE_UV_MAP[materialBuffer[matIndex] * 2],
                        TEXTURE_UV_MAP[materialBuffer[matIndex] * 2 + 1]);
                    Vector2 uvScaleB = new Vector2(
                        TEXTURE_UV_MAP[materialBuffer[matIndex + 1] * 2],
                        TEXTURE_UV_MAP[materialBuffer[matIndex + 1] * 2 + 1]);

                    vertices[vertexIndex++] = new Vector4(
                        uvBaseScale * normX * uvScaleA.X,
                        uvBaseScale * normY * uvScaleA.Y,
                        uvBaseScale * normX * uvScaleB.X,
                        uvBaseScale * normY * uvScaleB.Y);

                    matIndex += 4;
                }
            }
            return vertices;
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

        private void LoadTerrainTextures()
        {
            //Only load the terrain texture once
            if (TerrainTexture_Alb != null || TerrainTexture_Nrm != null)
                return;

            Toolbox.Core.StudioLogger.WriteLine($"Loading terrain textures...");

            //Load all 83 terrain textures into a 2D array. // Eventually don't hardcode this.... same with res
            TerrainTexture_Alb = GLTexture2DArray.CreateUncompressedTexture(1024, 1024, 83, 1, PixelInternalFormat.Rgba, PixelFormat.Bgra);
            TerrainTexture_Alb.WrapS = TextureWrapMode.Repeat;
            TerrainTexture_Alb.WrapT = TextureWrapMode.Repeat;
            TerrainTexture_Alb.MinFilter = TextureMinFilter.LinearMipmapLinear;

            TerrainTexture_Nrm = GLTexture2DArray.CreateUncompressedTexture(512, 512, 83, 1, PixelInternalFormat.Rgba, PixelFormat.Bgra);
            TerrainTexture_Nrm.WrapS = TextureWrapMode.Repeat;
            TerrainTexture_Nrm.WrapT = TextureWrapMode.Repeat;
            TerrainTexture_Nrm.MinFilter = TextureMinFilter.LinearMipmapLinear;

            //Load the terrain data as cached images.
            string cache = PluginConfig.GetCachePath("Images\\Terrain");

            // Alb ------------------------------------------------
            for (int i = 0; i < TerrainTexture_Alb.ArrayCount; i++) 
            {
                string tex = $"{cache}\\MaterialAlb_{i}.png";
                if (System.IO.File.Exists(tex))
                {
                    var image = new System.Drawing.Bitmap(tex);
                    TerrainTexture_Alb.InsertImage(image, i);
                    image.Dispose();
                }
            }
            //Update the terrain sampler parameters and generate mips.
            TerrainTexture_Alb.Bind();
            TerrainTexture_Alb.UpdateParameters();
            TerrainTexture_Alb.GenerateMipmaps();
            TerrainTexture_Alb.Unbind();

            // Nrm ------------------------------------------------
            for (int i = 0; i < TerrainTexture_Nrm.ArrayCount; i++) 
            {
                string tex = $"{cache}\\MaterialCmb_{i}.png";
                if (System.IO.File.Exists(tex))
                {
                    var image = new System.Drawing.Bitmap(tex);
                    TerrainTexture_Nrm.InsertImage(image, i);
                    image.Dispose();
                }
            }
            //Update the terrain sampler parameters and generate mips.
            TerrainTexture_Nrm.Bind();
            TerrainTexture_Nrm.UpdateParameters();
            TerrainTexture_Nrm.GenerateMipmaps();
            TerrainTexture_Nrm.Unbind();
        }

        public struct TerrainVertex
        {
            [RenderAttribute("vPosition", VertexAttribPointerType.Float, 0)]
            public Vector3 Position;

            [RenderAttribute("vNormal", VertexAttribPointerType.Float, 12)]
            public Vector3 Normal;

            [RenderAttribute("vMaterialMap", VertexAttribPointerType.Float, 24)]
            public Vector3 MaterialMap;

            [RenderAttribute("vTexCoord", VertexAttribPointerType.Float, 36)]
            public Vector4 TexCoords;

            public TerrainVertex(Vector3 position, Vector3 normal, Vector3 materialMap, Vector4 texCoords)
            {
                Normal = normal;
                Position = position;
                MaterialMap = materialMap;
                TexCoords = texCoords;
            }
        }
    }
}