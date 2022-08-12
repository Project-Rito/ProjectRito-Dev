#version 330 core

//#using BFRES_UTILITY

in vec3 v_PositionWorld;
in vec2 v_TexCoord0;
in vec2 v_TexCoord1;
in vec2 v_TexCoord2;
in vec2 v_TexCoord3;
in vec4 v_VtxColor;
in vec3 v_NormalWorld;
in vec4 v_TangentWorld;

uniform int drawDebugAreaID;
uniform int areaID;
uniform float uBrightness;

struct BakeResult {
    vec3 IndirectLight;
    float Shadow;
    float AO;
};
struct LightResult {
    vec3 DiffuseColor;
    vec3 SpecularColor;
};
struct DirectionalLight {
    vec3 Color;
    vec3 BacksideColor;
    vec3 Direction;
    bool Wrapped;
    bool VisibleInShadow;
};
struct SurfaceLightParams {
    vec3 SurfaceNormal;
    vec3 SurfacePointToEyeDir;
    vec3 SpecularColor;
    float IntensityFromShadow;
    float SpecularRoughness;
};
float G1V(float NoV, float k) {
    return 1.0 / (NoV * (1.0 - k) + k);
}

struct EnvLightParam {
    vec4 BacksideColor;
    vec4 DiffuseColor;
    vec4 Direction;
};
layout(std140) uniform ub_MaterialParams {
    mat3x4 u_TexCoordSRT0;
    vec4 u_TexCoordBake0ScaleBias;
    vec4 u_TexCoordBake1ScaleBias;
    mat3x4 u_TexCoordSRT1;
    mat3x4 u_TexCoordSRT2;
    mat3x4 u_TexCoordSRT3;
    vec4 u_AlbedoColorAndTransparency;
    vec4 u_EmissionColorAndNormalMapWeight;
    vec4 u_SpecularColorAndIntensity;
    vec4 u_BakeLightScaleAndRoughness;
    vec4 u_MultiTexReg[3];
    vec4 u_Misc[1];
    EnvLightParam u_EnvLightParams[2];

    float u_uking_wind_vtx_transform_intensity;
    float u_uking_wind_vtx_transform_lie_intensity;
    float u_uking_wind_vtx_transform_lie_height;
};


struct StandardSamplerInfo {
    int Enabled;
    int TexcoordIdx;
    int _Padding0;
    int _Padding1;
};
struct ArraySamplerInfo {
    int Enabled;
    int TexcoordIdx;
    float Index;
    int _Padding0;
};

layout(std140) uniform ub_SamplerInfo {
    StandardSamplerInfo u_TextureAlbedo0_Info;
    StandardSamplerInfo u_TextureAlbedo1_Info;
    StandardSamplerInfo u_TextureAlbedo2_Info;
    StandardSamplerInfo u_TextureAlbedo3_Info;

    StandardSamplerInfo u_TextureAlpha0_Info;
    StandardSamplerInfo u_TextureAlpha1_Info;
    StandardSamplerInfo u_TextureAlpha2_Info;
    StandardSamplerInfo u_TextureAlpha3_Info;

    StandardSamplerInfo u_TextureSpec0_Info;
    StandardSamplerInfo u_TextureSpec1_Info;
    StandardSamplerInfo u_TextureSpec2_Info;
    StandardSamplerInfo u_TextureSpec3_Info;

    StandardSamplerInfo u_TextureNormal0_Info;
    StandardSamplerInfo u_TextureNormal1_Info;
    StandardSamplerInfo u_TextureNormal2_Info;
    StandardSamplerInfo u_TextureNormal3_Info;

    StandardSamplerInfo u_TextureEmission0_Info;
    StandardSamplerInfo u_TextureEmission1_Info;
    StandardSamplerInfo u_TextureEmission2_Info;
    StandardSamplerInfo u_TextureEmission3_Info;

    StandardSamplerInfo u_TextureBake0_Info;
    StandardSamplerInfo u_TextureBake1_Info;
    StandardSamplerInfo u_TextureBake2_Info;
    StandardSamplerInfo u_TextureBake3_Info;

    ArraySamplerInfo u_TextureArrTma_Info;
    ArraySamplerInfo u_TextureArrTmc_Info;
};

// Samplers - we assume that all standard samplers have 4 variants.
// -------------------------------------------
uniform sampler2D u_TextureAlbedo0;   // _a0
uniform sampler2D u_TextureAlbedo1;   // _a1
uniform sampler2D u_TextureAlbedo2;   // _a2
uniform sampler2D u_TextureAlbedo3;   // _a3
// -------------------------------------------
uniform sampler2D u_TextureAlpha0;    // _ms0
uniform sampler2D u_TextureAlpha1;    // _ms1
uniform sampler2D u_TextureAlpha2;    // _ms2
uniform sampler2D u_TextureAlpha3;    // _ms3
// -------------------------------------------
uniform sampler2D u_TextureSpec0;     // _s0
uniform sampler2D u_TextureSpec1;     // _s1
uniform sampler2D u_TextureSpec2;     // _s2
uniform sampler2D u_TextureSpec3;     // _s3
// -------------------------------------------
uniform sampler2D u_TextureNormal0;   // _n0
uniform sampler2D u_TextureNormal1;   // _n1
uniform sampler2D u_TextureNormal2;   // _n2
uniform sampler2D u_TextureNormal3;   // _n3
// -------------------------------------------
uniform sampler2D u_TextureEmission0; // _e0
uniform sampler2D u_TextureEmission1; // _e1
uniform sampler2D u_TextureEmission2; // _e2
uniform sampler2D u_TextureEmission3; // _e3
// -------------------------------------------
uniform sampler2D u_TextureBake0;     // _b0
uniform sampler2D u_TextureBake1;     // _b1
uniform sampler2D u_TextureBake2;     // _b2
uniform sampler2D u_TextureBake3;     // _b3
// -------------------------------------------

// Array Samplers
// -------------------------------------------
uniform sampler2DArray u_TextureArrTma; // tma
// -------------------------------------------
uniform sampler2DArray u_TextureArrTmc; // tmc
// -------------------------------------------


// Extra stuff
uniform bool alphaTest;
uniform int alphaFunc;
uniform float alphaRefValue;
uniform float specMaskScalar;

// GL
uniform mat4 mtxCam;
uniform vec3 camPosition;
uniform int colorOverride;
uniform vec4 highlight_color;
uniform float millisecond;


out vec4 fragOutput;

float GetComponent(int Type, vec4 Texture);

// Used for passing vertex info to BfresUtility
struct VertexAttributes
{
    vec3 worldPosition;
    vec2 texCoord0;
    vec2 texCoord1;
    vec2 texCoord2;
    vec2 texCoord3;
    vec4 vertexColor;
    vec3 normal;
    vec3 tangent;
    vec3 bitangent;
};

// Defined in BfresUtility.frag.
vec3 CalcBumpedNormal(vec2 texNormal, VertexAttributes vert);
float CalcSpec(vec3 worldNormal, vec3 camPosition, vec3 lightVec, float specMask, VertexAttributes vert);
vec2 GetTexcoord(StandardSamplerInfo samplerInfo, VertexAttributes vert);
vec2 GetTexcoord(ArraySamplerInfo samplerInfo, VertexAttributes vert);
vec4 Blend_OneMinusSourceAlpha(vec4[4] colors);

vec3 getWorldNormal(VertexAttributes vert) {
    vec4[4] colors;

    if (u_TextureNormal0_Info.Enabled == 1 || (u_TextureNormal1_Info.Enabled == 0 && u_TextureNormal2_Info.Enabled == 0 && u_TextureNormal3_Info.Enabled == 0 && u_TextureArrTmc_Info.Enabled == 0)) {
        vec4 tex = texture(u_TextureAlbedo0, GetTexcoord(u_TextureAlbedo0_Info, vert));
        colors[0] = tex;
    }
    if (u_TextureNormal1_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureNormal1, GetTexcoord(u_TextureNormal1_Info, vert));
        colors[1] = tex;
    }
    if (u_TextureNormal2_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureNormal2, GetTexcoord(u_TextureNormal2_Info, vert));
        colors[2] = tex;
    }
    if (u_TextureNormal3_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureNormal3, GetTexcoord(u_TextureNormal3_Info, vert));
        colors[3] = tex;
    }
    if (u_TextureArrTmc_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureArrTmc, vec3(GetTexcoord(u_TextureArrTmc_Info, vert), u_TextureArrTmc_Info.Index));
        colors[1] = tex;
    }

    return CalcBumpedNormal(Blend_OneMinusSourceAlpha(colors).xy, vert);
}

// Todo: Make multiple blend modes
vec4 getDiffuse(VertexAttributes vert) {
    vec4[4] colors;

    if (u_TextureAlbedo0_Info.Enabled == 1 || (u_TextureAlbedo1_Info.Enabled == 0 && u_TextureAlbedo2_Info.Enabled == 0 && u_TextureAlbedo3_Info.Enabled == 0 && u_TextureArrTma_Info.Enabled == 0)) {
        vec4 tex = texture(u_TextureAlbedo0, GetTexcoord(u_TextureAlbedo0_Info, vert));
        colors[0] = tex;
    }
    if (u_TextureAlbedo1_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureAlbedo1, GetTexcoord(u_TextureAlbedo1_Info, vert));
        colors[1] = tex;
    }
    if (u_TextureAlbedo2_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureAlbedo2, GetTexcoord(u_TextureAlbedo2_Info, vert));
        colors[2] = tex;
    }
    if (u_TextureAlbedo3_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureAlbedo3, GetTexcoord(u_TextureAlbedo3_Info, vert));
        colors[3] = tex;
    }
    if (u_TextureArrTma_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureArrTma, vec3(GetTexcoord(u_TextureArrTma_Info, vert), u_TextureArrTma_Info.Index));
        colors[1] = tex;
    }

    return Blend_OneMinusSourceAlpha(colors);
}

float getSpecMask(VertexAttributes vert) {
    vec4[4] colors;

    if (u_TextureSpec0_Info.Enabled == 1 || (u_TextureSpec1_Info.Enabled == 0 && u_TextureSpec2_Info.Enabled == 0 && u_TextureSpec3_Info.Enabled == 0 && u_TextureArrTmc_Info.Enabled == 0)) {
        vec4 tex = texture(u_TextureSpec0, GetTexcoord(u_TextureAlbedo0_Info, vert));
        colors[0] = tex;
    }
    if (u_TextureSpec1_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureSpec1, GetTexcoord(u_TextureSpec1_Info, vert));
        colors[1] = tex;
    }
    if (u_TextureSpec2_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureSpec2, GetTexcoord(u_TextureSpec2_Info, vert));
        colors[2] = tex;
    }
    if (u_TextureSpec3_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureSpec3, GetTexcoord(u_TextureSpec3_Info, vert));
        colors[3] = tex;
    }
    if (u_TextureArrTmc_Info.Enabled == 1) {
        vec4 tex = texture(u_TextureArrTmc, vec3(GetTexcoord(u_TextureArrTmc_Info, vert), u_TextureArrTmc_Info.Index));
        colors[1] = vec4(tex.z, tex.w, 0.f, 0.f);
    }

    return Blend_OneMinusSourceAlpha(colors).x;
}



void main(){
    if (colorOverride == 1)
    {
        fragOutput = vec4(1);
        return;
    }

    VertexAttributes vert;
    vert.worldPosition = v_PositionWorld;
    vert.texCoord0 = v_TexCoord0;
    vert.texCoord1 = v_TexCoord1;
    vert.texCoord2 = v_TexCoord2;
    vert.texCoord3 = v_TexCoord3;
    vert.vertexColor = v_VtxColor;
    vert.normal = v_NormalWorld;
    vert.tangent = v_TangentWorld.xyz;
    vert.bitangent = cross(v_NormalWorld, v_TangentWorld.xyz);

    // Normals
    vec3 worldNormal = getWorldNormal(vert);


    // Diffuse
    vec4 diffuseMapColor = getDiffuse(vert);

    // Spec
    float specMask = getSpecMask(vert);
    float spec = CalcSpec(worldNormal, camPosition, vec3(0.f, -1.f, 0.f), specMask, vert);

    // Lighting
    float halfLambert = max(spec,0.5f);
    fragOutput = vec4(diffuseMapColor.rgb * halfLambert, diffuseMapColor.a);
    fragOutput.rgb *= uBrightness;

    //Highlight color
	if (highlight_color.w > 0.0)
	{
		//Highlight intensity for object selection
		float hc_a   = highlight_color.w;
		fragOutput = vec4(fragOutput.rgb * (1-hc_a) + highlight_color.rgb * hc_a, fragOutput.a);
	}

    //Alpha test
    if (alphaTest)
    {
        if (u_TextureAlpha0_Info.Enabled == 1) {
            vec4 alphaMapColor = vec4(1);
            alphaMapColor = texture(u_TextureAlpha0, GetTexcoord(u_TextureAlpha0_Info, vert));
            fragOutput.a = alphaMapColor.r;
        }

        switch (alphaFunc)
        {
            case 0: //gequal
                if (fragOutput.a <= alphaRefValue)
                {
                     discard;
                }
            break;
            case 1: //greater
                if (fragOutput.a < alphaRefValue)
                {
                     discard;
                }
            break;
            case 2: //equal
                if (fragOutput.a == alphaRefValue)
                {
                     discard;
                }
            break;
            case 3: //less
                if (fragOutput.a > alphaRefValue)
                {
                     discard;
                }
            break;
            case 4: //lequal
                if (fragOutput.a >= alphaRefValue)
                {
                     discard;
                }
            break;
        }
    }

    if (drawDebugAreaID == 1)
    {
         vec3 areaOverlay = vec3(0);
         if (areaID == 0) areaOverlay = vec3(1, 0, 0);
         if (areaID == 1) areaOverlay = vec3(0, 1, 0);
         if (areaID == 2) areaOverlay = vec3(0, 0, 1);
         if (areaID == 3) areaOverlay = vec3(1, 1, 0);
         if (areaID == 4) areaOverlay = vec3(0, 1, 1);
         if (areaID == 5) areaOverlay = vec3(1, 0, 1);

         fragOutput.rgb = mix(fragOutput.rgb, areaOverlay, 0.5);
    }
}