#version 330 core

//#using BFRES_UTILITY

in vec3 v_PositionWorld;
in vec2 v_TexCoord0;
in vec4 v_TexCoordBake;
in vec4 v_TexCoord23;
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
    mat3x4 u_TexCoordSRT2;
    mat3x4 u_TexCoordSRT3;
    vec4 u_AlbedoColorAndTransparency;
    vec4 u_EmissionColorAndNormalMapWeight;
    vec4 u_SpecularColorAndIntensity;
    vec4 u_BakeLightScaleAndRoughness;
    vec4 u_MultiTexReg[3];
    vec4 u_Misc[1];
    EnvLightParam u_EnvLightParams[2];
};

//Samplers
uniform sampler2D u_TextureAlbedo0;   // _a0
uniform sampler2D u_TextureAlpha;     // _ms0
uniform sampler2D u_TextureSpecMask;  // _s0
uniform sampler2D u_TextureNormal0;   // _n0
uniform sampler2D u_TextureNormal1;   // _n1
uniform sampler2D u_TextureEmission0; // _e0
uniform sampler2D u_TextureBake0;     // _b0
uniform sampler2D u_TextureBake1;     // _b1
uniform sampler2D u_TextureMultiA;    // _a1
uniform sampler2D u_TextureMultiB;    // _a2
uniform sampler2D u_TextureIndirect;  // _a3

//Array Samplers
uniform sampler2DArray u_TextureArrAlbedo; // tma
uniform sampler2DArray u_TextureArrCombined; // tmc

uniform float u_TextureArrAlbedo_Index;
uniform float u_TextureArrAlpha_Index;
uniform float u_TextureArrSpecMask_Index;
uniform float u_TextureArrCombined_Index;

#define enable_diffuse
#define enable_alpha_map
#define enable_diffuse2
#define enable_albedo
#define enable_emission
#define enable_emission_map
#define enable_specular
#define enable_specular_mask
#define enable_specular_mask_rougness
#define enable_specular_physical
#define enable_vtx_color_diff
#define enable_vtx_color_emission
#define enable_vtx_color_spec
#define enable_vtx_alpha_trans

uniform bool alphaTest;
uniform int alphaFunc;
uniform float alphaRefValue;
uniform float specMaskScalar;

//Toggles
uniform int hasDiffuseMap;
uniform int hasDiffuseMultiA;
uniform int hasDiffuseMultiB;
uniform int hasAlphaMap;
uniform int hasSpecMap;
uniform int hasNormalMap0;
uniform int hasNormalMap1;

uniform int hasDiffuseArray;
uniform int hasAlphaArray;
uniform int hasCombinedArray;

//GL
uniform mat4 mtxCam;
uniform vec3 camPosition;
uniform int colorOverride;
uniform vec4 highlight_color;


out vec4 fragOutput;

float GetComponent(int Type, vec4 Texture);

// Used for passing vertex info to BfresUtility
struct VertexAttributes
{
    vec3 worldPosition;
    vec2 texCoord;
    vec4 vertexColor;
    vec3 normal;
    vec3 tangent;
    vec3 bitangent;
};

// Defined in BfresUtility.frag.
vec3 CalcBumpedNormal(vec3 texNormal, VertexAttributes vert);

vec3 getWorldNormal(vec2 texCoord0) {
    vec3 worldNormal = vec3(0);

    VertexAttributes vert;
    vert.worldPosition = v_PositionWorld;
    vert.texCoord = texCoord0;
    vert.vertexColor = v_VtxColor;
    vert.normal = v_NormalWorld;
    vert.tangent = v_TangentWorld.xyz;
    vert.bitangent = cross(v_NormalWorld, v_TangentWorld.xyz);
    
    float averageCount = 0u;
    if (hasNormalMap0 == 1) {
        vec4 tex = texture(u_TextureNormal0, texCoord0);
        vec3 normal = CalcBumpedNormal(tex.xyz, vert);
        worldNormal.rgb += normal;
        averageCount++;;
    }
    if (hasNormalMap1 == 1) {
        vec4 tex = texture(u_TextureNormal1, texCoord0);
        vec3 normal = CalcBumpedNormal(tex.xyz, vert);
        worldNormal.rgb += normal;
        averageCount++;
    }
    if (hasCombinedArray == 1) {
        vec4 tex = texture(u_TextureArrCombined, vec3(texCoord0, u_TextureArrCombined_Index));
        vec3 normal = CalcBumpedNormal(tex.xyz, vert);
        worldNormal += normal;
        averageCount++;
    }
    worldNormal /= averageCount;

    return worldNormal;
}

vec4 getDiffuse(vec2 texCoord0) {
    vec4 diffuseMapColor = vec4(0);

    float averageCountRGB = 0.f;
    uint averageCountA = 0u;
    if (hasDiffuseMap == 1 || (hasDiffuseArray == 0 && hasDiffuseMultiA == 0 && hasDiffuseMultiB == 0)) {
        vec4 tex = texture(u_TextureAlbedo0, texCoord0);
        diffuseMapColor.rgb += tex.rgb * tex.a;
        diffuseMapColor.a += tex.a;
        averageCountRGB += tex.a;
        averageCountA++;
    }
    if (hasDiffuseArray == 1) {
        vec4 tex = texture(u_TextureArrAlbedo, vec3(texCoord0, u_TextureArrAlbedo_Index));
        diffuseMapColor.rgb += tex.rgb * tex.a;
        diffuseMapColor.a += tex.a;
        averageCountRGB += tex.a;
        averageCountA++;
    }
    if (hasDiffuseMultiA == 1) {
        vec4 tex = texture(u_TextureMultiA, texCoord0);
        diffuseMapColor.rgb += tex.rgb * tex.a;
        diffuseMapColor.a += tex.a;
        averageCountRGB += tex.a;
        averageCountA++;
    }
    if (hasDiffuseMultiB == 1) {
        vec4 tex = texture(u_TextureMultiB, texCoord0);
        diffuseMapColor.rgb += tex.rgb * tex.a;
        diffuseMapColor.a += tex.a;
        averageCountRGB += tex.a;
        averageCountA++;
    }
    diffuseMapColor.rgb /= averageCountRGB;
    diffuseMapColor.a /= averageCountA;

    return diffuseMapColor;
}

float getSpec(vec2 texCoord0) {
    if (hasSpecMap == 0 && hasCombinedArray == 0) {
        return 1.f;
    }

    float specMask = 0.f;

    uint averageCount = 0u;
    if (hasSpecMap == 1) {
        vec4 tex = texture(u_TextureSpecMask, texCoord0);
        specMask += tex.x;
        averageCount += 1u;
    }
    if (hasCombinedArray == 1) {
        vec4 tex = texture(u_TextureArrCombined, vec3(texCoord0, u_TextureArrSpecMask_Index));
        specMask += tex.w;
        averageCount += 1u;
    }
    specMask /= averageCount;

    return specMask;
}



void main(){
    vec2 texCoord0 = v_TexCoord0;

    if (colorOverride == 1)
    {
        fragOutput = vec4(1);
        return;
    }

    // Normals
    vec3 worldNormal = getWorldNormal(texCoord0);


    // Diffuse
    vec4 diffuseMapColor = getDiffuse(texCoord0);

    // Spec
    float specMask = getSpec(texCoord0);
    vec3 i = vec3(0.f, -1.f, 0.f);
    vec3 o = i - (2 * (dot(i, worldNormal)) * worldNormal);
    vec3 c = camPosition - v_PositionWorld;
    float spec = min(dot(normalize(c), normalize(o)) * specMask, 1.f);

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
        if (hasAlphaMap == 1) {
            vec4 alphaMapColor = vec4(1);
            alphaMapColor = texture(u_TextureAlpha, texCoord0);
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