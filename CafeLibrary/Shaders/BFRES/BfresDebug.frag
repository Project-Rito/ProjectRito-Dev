#version 330 core

//#using BFRES_UTILITY

uniform sampler2D UVTestPattern;

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

uniform int debugShading;

uniform int DrawAreaID;
uniform int AreaIndex;

//GL
uniform mat4 mtxCam;
uniform vec3 camPosition;
uniform int colorOverride;
uniform vec4 highlight_color;

in vec2 texCoord0;
in vec3 posWorld;
in vec3 normal;
in vec3 boneWeightsColored;
in vec3 tangent;
in vec3 bitangent;
in vec4 vertexColor;

layout (location = 0) out vec4 fragOutput;
layout (location = 1) out vec4 brightColor;

const int DISPLAY_NORMALS = 1;
const int DISPLAY_LIGHTING = 2;
const int DISPLAY_DIFFUSE = 3;
const int DISPLAY_VTX_CLR = 4;
const int DISPLAY_UV = 5;
const int DISPLAY_UV_PATTERN = 6;
const int DISPLAY_WEIGHTS = 7;
const int DISPLAY_TANGENT = 8;
const int DISPLAY_BITANGENT = 9;
const int DISPLAY_SPECULAR = 10;

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

void main(){
    vec4 outputColor = vec4(1);

    VertexAttributes vert;
    vert.worldPosition = posWorld;
    vert.texCoord = texCoord0;
    vert.vertexColor = vertexColor;
    vert.normal = normal;
    vert.tangent = tangent;
    vert.bitangent = bitangent;

    if (debugShading == DISPLAY_NORMALS)
    {
        vec3 displayNormal = (normal * 0.5) + 0.5;
        outputColor.rgb = displayNormal;
    }

    else if (debugShading == DISPLAY_LIGHTING)
    {
        vec3 displayNormal = (normal * 0.5) + 0.5;
        float halfLambert = max(displayNormal.y,0.5);
        outputColor.rgb = vec3(0.5) * halfLambert;
    }

    else if (debugShading == DISPLAY_DIFFUSE)
    {
        vec4 displayDiffuse = texture(u_TextureAlbedo0, texCoord0);
        outputColor = displayDiffuse;
    }

    else if (debugShading == DISPLAY_UV)
         outputColor.rgb = vec3(texCoord0.x, texCoord0.y, 1.0);
    else if (debugShading == DISPLAY_UV_PATTERN)
        outputColor.rgb = texture(UVTestPattern, texCoord0).rgb;
    else if (debugShading == DISPLAY_WEIGHTS)
        outputColor.rgb = boneWeightsColored;
    else if (debugShading == DISPLAY_TANGENT)
    {
        vec3 displayTangent = (tangent * 0.5) + 0.5;
        outputColor.rgb = displayTangent;
    }
    else if (debugShading == DISPLAY_BITANGENT)
    {
        vec3 displayBitangent = (bitangent * 0.5) + 0.5;
        outputColor.rgb = displayBitangent;
    }
    else if (debugShading == DISPLAY_VTX_CLR)
    {
        outputColor.rgb = vertexColor.rgb;
    }
    else if (debugShading == DISPLAY_SPECULAR)
    {
        float specMask = texture(u_TextureSpecMask, texCoord0).r;
        vec3 worldNormal = CalcBumpedNormal(texture(u_TextureNormal0, texCoord0).xyz, vert);

        vec3 i = vec3(0.f, -1.f, 0.f);
        vec3 o = i - (2 * (dot(i, worldNormal)) * worldNormal);
        vec3 c = camPosition - posWorld;
        float displaySpec = dot(normalize(c), normalize(o)) * specMask;

        outputColor.rgb = vec3(min(displaySpec, 1.f));
    }

    fragOutput = outputColor;
    brightColor = vec4(0,0,0,1);

	if (highlight_color.w > 0.0)
	{
		//Highlight intensity for object selection
		float hc_a   = highlight_color.w;
		fragOutput = vec4(fragOutput.rgb * (1-hc_a) + highlight_color.rgb * hc_a, fragOutput.a);
	}

    if (DrawAreaID == 1)
    {
         if (AreaIndex == 0) fragOutput = vec4(1, 0, 0, 1);
         if (AreaIndex == 1) fragOutput = vec4(0, 1, 0, 1);
         if (AreaIndex == 2) fragOutput = vec4(0, 0, 1, 1);
         if (AreaIndex == 3) fragOutput = vec4(1, 1, 0, 1);
         if (AreaIndex == 4) fragOutput = vec4(0, 1, 1, 1);
         if (AreaIndex == 5) fragOutput = vec4(1, 0, 1, 1);
    }
}