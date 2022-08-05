using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using Nintendo.Bfres;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using AGraphicsLibrary;

namespace CafeLibrary.Rendering
{
    public class BfresMaterialRender : MaterialAsset
    {
        public int AreaIndex { get; set; } = -1;

        public List<STGenericTextureMap> TextureMaps = new List<STGenericTextureMap>();

        public List<string> Samplers { get; set; } 
        public Dictionary<string, string> AnimatedSamplers { get; set; }

        public Dictionary<string, ShaderParam> ShaderParams { get; set; } 
        public Dictionary<string, ShaderParam> AnimatedParams { get; set; }
        public Dictionary<string, string> ShaderOptions { get; set; }

        public ResDict<ResString> SamplerAssign { get; set; }

        public GLMaterialBlendState BlendState { get; set; } = new GLMaterialBlendState();

        public string LightMap = "";

        public static float Brightness = 1.0f;

        public bool IsTransparent { get; set; }

        public string ShaderArchiveName { get; set; }
        public string ShaderModelName { get; set; }

        public bool CullBack { get; set; }
        public bool CullFront { get; set; }

        public BfresRender ParentRenderer { get; set; }

        //GL resources
        UniformBlock MaterialBlock;

        public BfresMaterialRender() { }

        public BfresMaterialRender(BfresRender render, BfresModelRender model) {
            ParentRenderer = render;
            Samplers = new List<string>();
            AnimatedSamplers = new Dictionary<string, string>();
            ShaderParams = new Dictionary<string, ShaderParam>();
            AnimatedParams = new Dictionary<string, ShaderParam>();
            ShaderOptions = new Dictionary<string, string>();

            MaterialBlock = new UniformBlock();
        }

        /// <summary>
        /// Gets the param area from a bgenv lighting file.
        /// The engine uses a collect model which as boundings for areas to set various things like fog or lights.
        /// This renderer uses it to determine what fog to render and cubemap (if a map object that dynamically changes areas)
        /// </summary>
        /// <returns></returns>
        public int GetAreaIndex(Vector3 position)
        {
            if (this.ShaderParams.ContainsKey("gsys_area_env_index_diffuse"))
            {
                var areaParam = this.ShaderParams["gsys_area_env_index_diffuse"];

                if (position != Vector3.Zero)
                {
                    var collectRes = LightingEngine.LightSettings.Resources.CollectFiles.FirstOrDefault().Value;
                    var area = collectRes.GetArea(position.X, position.Y, position.Z);
                    return area.AreaIndex;
                }

                float index = (float)areaParam.DataValue;
                return (int)index;
            }

            return 0;
        }

        private void UpdateMaterialBlock()
        {
            TexSrt texcoordSrt0 = GetTexcoordSRT(0);
            TexSrt texcoordSrt1 = GetTexcoordSRT(1);
            TexSrt texcoordSrt2 = GetTexcoordSRT(2);
            TexSrt texcoordSrt3 = GetTexcoordSRT(3);
            float[] bake0ScaleBias = GetParameterArray<float>("gsys_bake_st0");
            float[] bake1ScaleBias = GetParameterArray<float>("gsys_bake_st1");
            float[] bakeLightScale = GetParameterArray<float>("gsys_bake_light_scale");
            float[] albedoColor = GetParameterArray<float>("albedo_tex_color", 3);
            float[] emissionColor = GetParameterArray<float>("emission_color", 3);
            float[] specularColor = GetParameterArray<float>("specular_color", 3);

            float normalmapWeight = GetParameter<float>("normal_map_weight");
            float specularIntensity = GetParameter<float>("specular_intensity");
            float specularRoughness = GetParameter<float>("specular_roughness");
            float emissionIntensity = GetParameter<float>("emission_intensity");
            float transparency = GetParameter<float>("transparency");

            float[] multiTexReg0 = GetParameterArray<float>("multi_tex_reg0");
            float[] multiTexReg1 = GetParameterArray<float>("multi_tex_reg1");
            float[] multiTexReg2 = GetParameterArray<float>("multi_tex_reg2");
            float[] indirectMag = GetParameterArray<float>("indirect_mag");

            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.Write(CalculateSRT3x4(texcoordSrt0));
                writer.Write(bake0ScaleBias);
                writer.Write(bake1ScaleBias);
                writer.Write(CalculateSRT3x4(texcoordSrt1));
                writer.Write(CalculateSRT3x4(texcoordSrt2));
                writer.Write(CalculateSRT3x4(texcoordSrt3));

                writer.Write(albedoColor);
                writer.Write(transparency);

                writer.Write(emissionColor[0] * emissionIntensity);
                writer.Write(emissionColor[1] * emissionIntensity);
                writer.Write(emissionColor[2] * emissionIntensity);
                writer.Write(normalmapWeight);

                writer.Write(specularColor);
                writer.Write(specularIntensity);

                writer.Write(bakeLightScale);
                writer.Write(specularRoughness);

                writer.Write(multiTexReg0);
                writer.Write(multiTexReg1);
                writer.Write(multiTexReg2);

                writer.Write(indirectMag);
                writer.Write(new float[2]);
            }
            MaterialBlock.Buffer.Clear();
            MaterialBlock.Add(mem.ToArray());
        }

        private T GetParameter<T>(string name)
        {
            if (AnimatedParams.ContainsKey(name))
                return (T)AnimatedParams[name].DataValue;
            if (ShaderParams.ContainsKey(name))
                return (T)ShaderParams[name].DataValue;
            return (T)Activator.CreateInstance(typeof(T));
        }

        private T[] GetParameterArray<T>(string name, int length = 0)
        {
            if (AnimatedParams.ContainsKey(name))
                return (T[])AnimatedParams[name].DataValue;
            if (ShaderParams.ContainsKey(name))
                return (T[])ShaderParams[name].DataValue;
            return new T[length];
        }

        private float[] CalculateSRT3x4(Nintendo.Bfres.TexSrt texSrt)
        {
            var m = CalculateSRT2x3(texSrt);
            return new float[12]
            {
                m[0], m[2], m[4], 0.0f,
                m[1], m[3], m[5], 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
            };
        }

        private float[] CalculateSRT2x3(Nintendo.Bfres.TexSrt texSrt)
        {
            var scaling = texSrt.Scaling;
            var translate = texSrt.Translation;
            float cosR = (float)Math.Cos(texSrt.Rotation);
            float sinR = (float)Math.Sin(texSrt.Rotation);
            float scalingXC = scaling.X * cosR;
            float scalingXS = scaling.X * sinR;
            float scalingYC = scaling.Y * cosR;
            float scalingYS = scaling.Y * sinR;

            switch (texSrt.Mode)
            {
                default:
                case Nintendo.Bfres.TexSrtMode.ModeMaya:
                    return new float[8]
                    {
                        scalingXC, -scalingYS, //0 1
                        scalingXS, scalingYC, // 4 5
                        -0.5f * (scalingXC + scalingXS - scaling.X) - scaling.X * translate.X, //12
                        -0.5f * (scalingYC - scalingYS + scaling.Y) + scaling.Y * translate.Y + 1.0f, //13
                        0.0f, 0.0f,
                    };
                case Nintendo.Bfres.TexSrtMode.Mode3dsMax:
                    return new float[8]
                    {
                        scalingXC, -scalingYS,
                        scalingXS, scalingYC,
                        -scalingXC * (translate.X + 0.5f) + scalingXS * (translate.Y - 0.5f) + 0.5f, scalingYS * (translate.X + 0.5f) + scalingYC * (translate.Y - 0.5f) + 0.5f,
                        0.0f, 0.0f
                    };
                case Nintendo.Bfres.TexSrtMode.ModeSoftimage:
                    return new float[8]
                    {
                        scalingXC, scalingYS,
                        -scalingXS, scalingYC,
                        scalingXS - scalingXC * translate.X - scalingXS * translate.Y, -scalingYC - scalingYS * translate.X + scalingYC * translate.Y + 1.0f,
                        0.0f, 0.0f,
                    };
            }
        }

        private float[] CalculateSRT(Nintendo.Bfres.Srt2D texSrt)
        {
            var scaling = texSrt.Scaling;
            var translate = texSrt.Translation;
            float cosR = (float)Math.Cos(texSrt.Rotation);
            float sinR = (float)Math.Sin(texSrt.Rotation);

            return new float[8]
            {
                scaling.X * cosR, scaling.X * sinR,
                -scaling.Y * sinR, scaling.Y * cosR,
                translate.X, translate.Y,
                0.0f, 0.0f
            };
        }

        private TexSrt GetTexcoordSRT(int texCoord)
        {
            string optionsKey = $"uking_texcoord{texCoord}_srt";

            if (!ShaderOptions.ContainsKey(optionsKey))
                return GetIdentitySRT(); // Idk what to do so we'll just do this

            int idx = int.Parse(ShaderOptions[optionsKey]);
            if (idx == -1)
                return GetIdentitySRT(); // Idk what to do so we'll just do this

            string paramsKey = $"tex_srt{idx}";

            TexSrt? res = GetParameter<TexSrt?>(paramsKey);
            if (res == null)
                return GetIdentitySRT();
            return (TexSrt)res;
        }

        private TexSrt GetIdentitySRT()
        {
            return new TexSrt // I hope this is identity
            {
                Translation = new Syroot.Maths.Vector2F(0.0f, 0.0f),
                Rotation = 0.0f,
                Scaling = new Syroot.Maths.Vector2F(1.0f, 1.0f)
            };
        }

        private int GetTexTexcoordIdx(int texIdx)
        {
            string optionsKey = $"uking_texture{texIdx}_texcoord";
            if (!ShaderOptions.ContainsKey(optionsKey))
                return 0;

            return int.Parse(ShaderOptions[optionsKey]);
        }

        public Dictionary<string, GenericRenderer.TextureView> GetTextures()
        {
            return ParentRenderer.Textures;
        }

        public void ResetAnimations() {
            AnimatedSamplers.Clear();
            AnimatedParams.Clear();
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh) {
        }

        public void RenderDefaultMaterials(GLContext control, GLTransform transform, ShaderProgram shader, GenericPickableMesh mesh)
        {
            AreaIndex = GetAreaIndex(transform.Position);

            SetRenderState();
            SetBlendState();
            SetTextureUniforms(control, shader);
            SetShadows(control, shader);

            shader.SetFloat("uBrightness", Brightness);

            shader.SetBool("alphaTest", BlendState.AlphaTest);
            shader.SetFloat("alphaRefValue", BlendState.AlphaValue);
            shader.SetInt("alphaFunc", GetAlphaFunc(BlendState.AlphaFunction));
            shader.SetFloat("specMaskScalar", ShaderOptions.ContainsKey("uking_specmask_scaler") ? float.Parse(ShaderOptions["uking_specmask_scaler"]) : 1f);

            shader.SetBoolToInt("drawDebugAreaID", BfresRender.DrawDebugAreaID);
            shader.SetInt("areaID", AreaIndex);

            UpdateMaterialBlock();
            MaterialBlock.RenderBuffer(shader.program, "ub_MaterialParams");
        }

        public void RenderDebugMaterials(GLContext control, GLTransform transform, ShaderProgram shader, GenericPickableMesh mesh)
        {
            AreaIndex = GetAreaIndex(transform.Position);

            SetRenderState();
            SetBlendState();
            SetTextureUniforms(control, shader);
            SetShadows(control, shader);

            shader.SetBool("alphaTest", BlendState.AlphaTest);
            shader.SetFloat("alphaRefValue", BlendState.AlphaValue);
            shader.SetInt("alphaFunc", GetAlphaFunc(BlendState.AlphaFunction));

            shader.SetBoolToInt("drawDebugAreaID", BfresRender.DrawDebugAreaID);
            shader.SetInt("areaID", AreaIndex);

            UpdateMaterialBlock();
            MaterialBlock.RenderBuffer(shader.program, "ub_MaterialParams");
        }

        static int GetAlphaFunc(AlphaFunction func)
        {
            if (func == AlphaFunction.Gequal) return 0;
            if (func == AlphaFunction.Greater) return 1;
            if (func == AlphaFunction.Equal) return 2;
            if (func == AlphaFunction.Less) return 3;
            if (func == AlphaFunction.Lequal) return 4;
            return 0;
        }

        public void RenderShadowMaterial(GLContext context)
        {
            context.CurrentShader.SetBoolToInt("hasAlpha", false);

            BlendState.RenderAlphaTest();
            if (BlendState.AlphaTest || BlendState.BlendColor)
            {
                context.CurrentShader.SetBoolToInt("hasAlpha", true);
                SetTextureUniforms(context, context.CurrentShader);
            }
        }

        public virtual void SetShadows(GLContext control,ShaderProgram shader)
        {
            if (control.Scene.ShadowRenderer == null)
                return;

            var shadowRender = control.Scene.ShadowRenderer;

            var lightSpaceMatrix = shadowRender.GetLightSpaceViewProjMatrix();
            var shadowMap = shadowRender.GetProjectedShadow();
            var lightDir = shadowRender.GetLightDirection();

            shader.SetMatrix4x4("mtxLightVP", ref lightSpaceMatrix);
            shader.SetTexture(shadowMap, "shadowMap", 22);
            shader.SetVector3("lightDir", lightDir);
        }

        public virtual void SetBlendState()
        {
            BlendState.RenderAlphaTest();
            BlendState.RenderBlendState();
            BlendState.RenderDepthTest();
        }

        public virtual void SetRenderState()
        {
            GL.Enable(EnableCap.CullFace);

            if (CullFront && CullBack)
                GL.CullFace(CullFaceMode.FrontAndBack);
            else if (CullFront)
                GL.CullFace(CullFaceMode.Front);
            else if (CullBack)
                GL.CullFace(CullFaceMode.Back);
            else
                GL.Disable(EnableCap.CullFace);
        }

        public virtual int SetTextureUniforms(GLContext control, ShaderProgram shader)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);
            shader.SetInt("u_TextureAlbedo0", 1); // Attach default texture


            shader.SetBoolToInt("u_TextureAlbedo0_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureAlbedo1_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureAlbedo2_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureAlbedo3_Info.Enabled", false);
            
            shader.SetBoolToInt("u_TextureAlpha0_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureAlpha1_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureAlpha2_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureAlpha3_Info.Enabled", false);

            shader.SetBoolToInt("u_TextureSpec0_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureSpec1_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureSpec2_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureSpec3_Info.Enabled", false);

            shader.SetBoolToInt("u_TextureNormal0_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureNormal1_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureNormal2_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureNormal3_Info.Enabled", false);

            shader.SetBoolToInt("u_TextureEmission0_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureEmission1_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureEmission2_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureEmission3_Info.Enabled", false);

            shader.SetBoolToInt("u_TextureBake0_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureBake1_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureBake2_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureBake3_Info.Enabled", false);

            shader.SetBoolToInt("u_TextureArrTma_Info.Enabled", false);
            shader.SetBoolToInt("u_TextureArrTmc_Info.Enabled", false);

            int id = 2;
            int arrayIdx = 0;
            for (int i = 0; i < this.TextureMaps?.Count; i++)
            {
                var name = TextureMaps[i].Name;
                var sampler = TextureMaps[i].Sampler;
                //Lookup samplers targeted via animations and use that texture instead if possible
                if (AnimatedSamplers.ContainsKey(sampler))
                    name = AnimatedSamplers[sampler];


                string uniformName = GetUniformName(sampler);
                if (uniformName == string.Empty)
                    continue;

                // Unbind stuff
                GL.ActiveTexture(TextureUnit.Texture0 + id);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindTexture(TextureTarget.Texture2DArray, 0);

                var binded = BindTexture(shader, GetTextures(), TextureMaps[i], name, id);
                bool hasTexture = binded != null;

                switch (sampler)
                {
                    case "_a0":
                        shader.SetBoolToInt("u_TextureAlbedo0_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureAlbedo0_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_a1":
                        shader.SetBoolToInt("u_TextureAlbedo1_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureAlbedo1_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_a2":
                        shader.SetBoolToInt("u_TextureAlbedo2_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureAlbedo2_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_a3":
                        shader.SetBoolToInt("u_TextureAlbedo3_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureAlbedo3_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_ms0":
                        shader.SetBoolToInt("u_TextureAlpha0_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureAlpha0_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_ms1":
                        shader.SetBoolToInt("u_TextureAlpha1_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureAlpha1_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_ms2":
                        shader.SetBoolToInt("u_TextureAlpha2_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureAlpha2_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_ms3":
                        shader.SetBoolToInt("u_TextureAlpha3_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureAlpha3_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_s0":
                        shader.SetBoolToInt("u_TextureSpec0_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureSpec0_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_s1":
                        shader.SetBoolToInt("u_TextureSpec1_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureSpec1_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_s2":
                        shader.SetBoolToInt("u_TextureSpec2_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureSpec2_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_s3":
                        shader.SetBoolToInt("u_TextureSpec3_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureSpec3_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_n0":
                        shader.SetBoolToInt("u_TextureNormal0_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureNormal0_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_n1":
                        shader.SetBoolToInt("u_TextureNormal1_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureNormal1_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_n2":
                        shader.SetBoolToInt("u_TextureNormal2_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureNormal2_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_n3":
                        shader.SetBoolToInt("u_TextureNormal3_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureNormal3_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_e0":
                        shader.SetBoolToInt("u_TextureEmission0_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureEmission0_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_e1":
                        shader.SetBoolToInt("u_TextureEmission1_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureEmission1_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_e2":
                        shader.SetBoolToInt("u_TextureEmission2_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureEmission2_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_e3":
                        shader.SetBoolToInt("u_TextureEmission3_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureEmission3_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_b0":
                        shader.SetBoolToInt("u_TextureBake0_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureBake0_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_b1":
                        shader.SetBoolToInt("u_TextureBake1_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureBake1_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_b2":
                        shader.SetBoolToInt("u_TextureBake2_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureBake2_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "_b3":
                        shader.SetBoolToInt("u_TextureBake3_Info.Enabled", hasTexture);
                        shader.SetInt("u_TextureBake3_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;

                    case "tma":
                        shader.SetBoolToInt("u_TextureArrTma_Info.Enabled", hasTexture);
                        if (ShaderParams.ContainsKey("texture_array_index" + i))
                            shader.SetFloat("u_TextureArrTma_Info.Index", (float)ShaderParams["texture_array_index" + arrayIdx++].DataValue);
                        shader.SetInt("u_TextureArrTma_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                    case "tmc":
                        shader.SetBoolToInt("u_TextureArrTmc_Info.Enabled", hasTexture);
                        if (ShaderParams.ContainsKey("texture_array_index" + i))
                            shader.SetFloat("u_TextureArrTmc_Info.Index", (float)ShaderParams["texture_array_index" + arrayIdx++].DataValue);
                        shader.SetInt("u_TextureArrTmc_Info.TexcoordIdx", GetTexTexcoordIdx(i));
                        break;
                }

                if (hasTexture)
                    shader.SetInt(uniformName, id++);
            }

            return id; // Might not be the best way to implement this...
        }

        private string GetUniformName(string sampler)
        {
            switch (sampler)
            {
                case "_a0": return "u_TextureAlbedo0";
                case "_a1": return "u_TextureAlbedo1";
                case "_a2": return "u_TextureAlbedo2";
                case "_a3": return "u_TextureAlbedo3";
                
                case "_ms0": return "u_TextureAlpha0";
                case "_ms1": return "u_TextureAlpha1";
                case "_ms2": return "u_TextureAlpha2";
                case "_ms3": return "u_TextureAlpha3";

                case "_s0": return "u_TextureSpec0";
                case "_s1": return "u_TextureSpec1";
                case "_s2": return "u_TextureSpec2";
                case "_s3": return "u_TextureSpec3";

                case "_n0": return "u_TextureNormal0";
                case "_n1": return "u_TextureNormal1";
                case "_n2": return "u_TextureNormal2";
                case "_n3": return "u_TextureNormal3";

                case "_e0": return "u_TextureEmission0";
                case "_e1": return "u_TextureEmission1";
                case "_e2": return "u_TextureEmission2";
                case "_e3": return "u_TextureEmission3";

                case "_b0": return "u_TextureBake0";
                case "_b1": return "u_TextureBake1";
                case "_b2": return "u_TextureBake2";
                case "_b3": return "u_TextureBake3";

                case "tma": return "u_TextureArrTma";
                case "tmc": return "u_TextureArrTmc"; // Theory: normal data in xy, specular in zw
                default:
                    return "";
            }
        }

        /// <summary>
        /// Binds a texture to the given id
        /// </summary>
        /// <param name="shader">The shader to interact with.</param>
        /// <param name="textures">A texture dictionary to look to for the texture.</param>
        /// <param name="textureMap">Texture mapping info.</param>
        /// <param name="name">The texture name to look for in the textures dictionary or in other scene models.</param>
        /// <param name="id">The OpenGL id to bind to.</param>
        /// <returns>A GLTexture on success and null on failure.</returns>
        public static GLTexture BindTexture(ShaderProgram shader, Dictionary<string, GenericRenderer.TextureView> textures,
            STGenericTextureMap textureMap, string name, int id)
        {
            if (name == null)
                return null;

            GL.ActiveTexture(TextureUnit.Texture0 + id);

            if (textures.ContainsKey(name))
            {
                GLTexture tex = BindGLTexture(textures[name], textureMap, shader);
                if (tex != null)
                    return tex;
            }

            foreach (var model in DataCache.ModelCache.Values)
            {
                if (model.Textures.ContainsKey(name))
                {
                    GLTexture tex = BindGLTexture(model.Textures[name], textureMap, shader);
                    if (tex != null)
                        return tex;
                }
            }
            
            return null;
        }

        private static GLTexture BindGLTexture(GenericRenderer.TextureView texture, STGenericTextureMap textureMap, ShaderProgram shader)
        {
            if (texture.RenderTexture == null)
                return null;

            var target = ((GLTexture)texture.RenderTexture).Target;

            GL.BindTexture(target, texture.RenderTexture.ID);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)OpenGLHelper.WrapMode[textureMap.WrapU]);
            GL.TexParameter(target, TextureParameterName.TextureWrapT, (int)OpenGLHelper.WrapMode[textureMap.WrapV]);
            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)OpenGLHelper.MinFilter[textureMap.MinFilter]);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)OpenGLHelper.MagFilter[textureMap.MagFilter]);
            GL.TexParameter(target, TextureParameterName.TextureLodBias, textureMap.LODBias);
            GL.TexParameter(target, TextureParameterName.TextureMaxLod, textureMap.MaxLOD);
            GL.TexParameter(target, TextureParameterName.TextureMinLod, textureMap.MinLOD);

            int[] mask = new int[4]
            {
                    OpenGLHelper.GetSwizzle(texture.RedChannel),
                    OpenGLHelper.GetSwizzle(texture.GreenChannel),
                    OpenGLHelper.GetSwizzle(texture.BlueChannel),
                    OpenGLHelper.GetSwizzle(texture.AlphaChannel),
            };
            GL.TexParameter(target, TextureParameterName.TextureSwizzleRgba, mask);
            return (GLTexture)texture.RenderTexture;
        }
    }
}
