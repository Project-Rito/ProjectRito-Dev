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
    public class WaterRender : EditableObject, IFrustumCulling
    {
        public override bool UsePostEffects => false;

        const float MAP_HEIGHT_SCALE = 0.0122075f;

        const int MAP_TILE_LENGTH = 64;
        const int MAP_TILE_SIZE = MAP_TILE_LENGTH * MAP_TILE_LENGTH;
        const int INDEX_COUNT_SIDE = MAP_TILE_LENGTH - 1;

        float[] TEXTURE_INDEX_MAP = new float[] { 0, 1, 2, 3, 4, 5, 6, 7 };

        RenderMesh<WaterVertex> WaterMesh;
        static GLTexture2DArray WaterTexture_Emm;
        static GLTexture2DArray WaterTexture_Nrm;

        static int[] IndexBuffer;

        public bool EnableFrustumCulling => true;
        public bool InFrustum { get; set; } = true;

        BoundingNode Bounding = new BoundingNode();

        public bool IsInsideFrustum(GLContext context)
        {
            return context.Camera.InFustrum(Bounding);
        }

        public WaterRender(NodeBase parent = null) : base(null)
        {
            IsVisible = true;
            CanSelect = false;
            this.Transform.TransformUpdated += delegate {
                Bounding.UpdateTransform(this.Transform.TransformMatrix);
            };
        }

        public void LoadWaterData(byte[] heightBuffer)
        {
            //Load all attribute data.
            var positionData = GetWaterVertices(heightBuffer);
            var texCoords = GetTexCoords();
            //Normals calculation
            Vector3[] positions = new Vector3[positionData.Length];
            for (int i = 0; i < positionData.Length; i++)
                positions[i] = positionData[i].Translate;

            var normals = DrawingHelper.CalculateNormals(positions.ToList());
            //Fixed index buffer. It can be kept static as all terrain tiles use the same index layout.
            if (IndexBuffer == null)
                IndexBuffer = GetIndexBuffer();
            //Prepare the terrain vertices for rendering
            WaterVertex[] vertices = new WaterVertex[positionData.Length];
            for (int i = 0; i < positionData.Length; i++)
            {
                vertices[i] = new WaterVertex()
                {
                    Position = positions[i] * GLContext.PreviewScale,
                    Normal = normals[i],
                    TexCoords = texCoords[i],
                    MaterialIndex = (uint)positionData[i].MaterialIndex,
                };
            }
            //Calculate bounding data for frustum culling
            Bounding.Box = BoundingBox.FromVertices(vertices.Select(x => x.Position).ToArray());
            Bounding.Radius = (Bounding.Box.Max - Bounding.Box.Min).Length;

            //Finish loading the terrain mesh
            WaterMesh = new RenderMesh<WaterVertex>(vertices, IndexBuffer, PrimitiveType.Triangles);
            LoadWaterTextures();
        }

        public override void DrawModel(GLContext context, Pass pass)
        {
            if ((WaterMesh == null || pass != Pass.TRANSPARENT || !InFrustum))
                return;

            var shader = GlobalShaders.GetShader("WATER");
            context.CurrentShader = shader;
            shader.SetTransform(GLConstants.ModelMatrix, this.Transform);
            shader.SetTexture(WaterTexture_Emm, "texWater_Emm", 1);
            shader.SetTexture(WaterTexture_Nrm, "texWater_Nrm", 2);


            WaterMesh.Draw(context);
        }

        private WaterVertexData[] GetWaterVertices(byte[] heightBuffer)
        {

            WaterVertexData[] vertices = new WaterVertexData[MAP_TILE_SIZE];
            using (var reader = new FileReader(heightBuffer))
            {
                int vertexIndex = 0;
                for (float y = 0; y < MAP_TILE_LENGTH; y++)
                {
                    float normY = y / (float)INDEX_COUNT_SIDE;
                    for (float x = 0; x < MAP_TILE_LENGTH; x++)
                    {
                        float heightValue = reader.ReadUInt16() * MAP_HEIGHT_SCALE;
                        ushort xAxisFlowRate = reader.ReadUInt16(); // xAxisFlowRate
                        ushort zAxisFlowRate = reader.ReadUInt16(); // zAxisFlowRate
                        reader.ReadByte(); // materialIndex + 3
                        byte materialIndex = reader.ReadByte(); // materialIndex
                        //Terrain vertices range from 0 - 1

                        WaterVertexData vertexData = new WaterVertexData()
                        {
                            Translate = new Vector3(x / (float)INDEX_COUNT_SIDE - 0.5f, heightValue, normY - 0.5f),
                            MaterialIndex = materialIndex,
                            XAxisFlowRate = xAxisFlowRate,
                            ZAxisFlowRate = zAxisFlowRate
                        };

                        vertices[vertexIndex++] = vertexData;
                    }
                }
            }
            return vertices;
        }

        public class WaterVertexData
        {
            public Vector3 Translate;
            public ushort XAxisFlowRate;
            public ushort ZAxisFlowRate;
            public byte MaterialIndex;
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

        private void LoadWaterTextures()
        {
            //Only load the terrain texture once
            if (WaterTexture_Emm != null || WaterTexture_Nrm != null)
                return;

            Toolbox.Core.StudioLogger.WriteLine($"Loading water textures...");

            //Load all 8 water textures into a 2D array. // Eventually don't hardcode this.... same with res
            WaterTexture_Emm = GLTexture2DArray.CreateUncompressedTexture(512, 512, 8, 1, PixelInternalFormat.Rgba, PixelFormat.Bgra);
            WaterTexture_Emm.WrapS = TextureWrapMode.Repeat;
            WaterTexture_Emm.WrapT = TextureWrapMode.Repeat;
            WaterTexture_Emm.MinFilter = TextureMinFilter.LinearMipmapLinear;

            WaterTexture_Nrm = GLTexture2DArray.CreateUncompressedTexture(512, 512, 8, 1, PixelInternalFormat.Rgba, PixelFormat.Bgra);
            WaterTexture_Nrm.WrapS = TextureWrapMode.Repeat;
            WaterTexture_Nrm.WrapT = TextureWrapMode.Repeat;
            WaterTexture_Nrm.MinFilter = TextureMinFilter.LinearMipmapLinear;

            //Load the terrain data as cached images.
            string cache = PluginConfig.GetCachePath("Images\\Terrain");

            // Alb ------------------------------------------------
            for (int i = 0; i < WaterTexture_Emm.ArrayCount; i++)
            {
                string tex = $"{cache}\\WaterEmm_{i}.png";
                if (System.IO.File.Exists(tex))
                {
                    var image = new System.Drawing.Bitmap(tex);
                    WaterTexture_Emm.InsertImage(image, i);
                    image.Dispose();
                }
            }
            //Update the terrain sampler parameters and generate mips.
            WaterTexture_Emm.Bind();
            WaterTexture_Emm.UpdateParameters();
            WaterTexture_Emm.GenerateMipmaps();
            WaterTexture_Emm.Unbind();

            // Nrm ------------------------------------------------
            for (int i = 0; i < WaterTexture_Nrm.ArrayCount; i++)
            {
                string tex = $"{cache}\\WaterNm_{i}.png";
                if (System.IO.File.Exists(tex))
                {
                    var image = new System.Drawing.Bitmap(tex);
                    WaterTexture_Nrm.InsertImage(image, i);
                    image.Dispose();
                }
            }
            //Update the terrain sampler parameters and generate mips.
            WaterTexture_Nrm.Bind();
            WaterTexture_Nrm.UpdateParameters();
            WaterTexture_Nrm.GenerateMipmaps();
            WaterTexture_Nrm.Unbind();
        }

        public struct WaterVertex
        {
            [RenderAttribute("vPosition", VertexAttribPointerType.Float, 0)]
            public Vector3 Position;

            [RenderAttribute("vNormal", VertexAttribPointerType.Float, 12)]
            public Vector3 Normal;

            [RenderAttribute("vMaterialIndex", VertexAttribPointerType.Float, 24)]
            public uint MaterialIndex;

            [RenderAttribute("vTexCoord", VertexAttribPointerType.Float, 28)]
            public Vector2 TexCoords;

            public WaterVertex(Vector3 position, Vector3 normal, uint materialIndex, Vector2 texCoords)
            {
                Normal = normal;
                Position = position;
                MaterialIndex = materialIndex;
                TexCoords = texCoords;
            }
        }
    }
}