#version 330 core

//#using BFRES_UTILITY

uniform sampler2DArray texTerrain_Alb;
uniform sampler2DArray texTerrain_Cmb;

uniform float uBrightness;
uniform bool uDebugSections;

in vec4 v_TexCoords;
in vec3 v_NormalWorld;
in vec3 v_TangentWorld;
in vec3 v_DebugHighlight;
in vec3 v_PositionWorld;

in float materialWeight;

flat in vec2 texIndex;

// GL
uniform vec3 camPosition;

out vec4 fragColor;

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

void main(void)
{
    VertexAttributes vert;
    vert.worldPosition = v_PositionWorld;
    vert.texCoord0 = mix(v_TexCoords.xy, v_TexCoords.zw, materialWeight);
    vert.vertexColor = vec4(1.f);
    vert.normal = v_NormalWorld;
    vert.tangent = v_TangentWorld;
    vert.bitangent = cross(v_NormalWorld, v_TangentWorld);

    // Base color texture mixing
    vec4 texColor0 = texture(texTerrain_Alb, vec3(v_TexCoords.xy, texIndex[0]));
    vec4 texColor1 = texture(texTerrain_Alb, vec3(v_TexCoords.zw, texIndex[1]));
    vec4 texColor = mix(texColor0, texColor1, materialWeight);

    // Combined texture mixing (Normal + Spec)
    vec4 texCmb0 = texture(texTerrain_Cmb, vec3(v_TexCoords.xy, texIndex[0]));
    vec4 texCmb1 = texture(texTerrain_Cmb, vec3(v_TexCoords.zw, texIndex[1]));
    vec4 texCmb = mix(texCmb0, texCmb1, materialWeight);

    // World normals
    vec3 worldNormal = CalcBumpedNormal(texCmb.xy, vert);

    // Spec
    float spec = CalcSpec(worldNormal, camPosition, vec3(0.f, -1.f, 0.f), texCmb.z, vert);

    fragColor = texColor;

    // Debug highlight
    if (uDebugSections)
        fragColor.rgb += v_DebugHighlight;

    // Lighting
    float halfLambert = max(spec,0.5);
    fragColor = vec4(fragColor.rgb * halfLambert, fragColor.a); // Use that lighting here
    //fragColor = vec4(vec3(spec), 1.f);
    fragColor.rgb *= uBrightness;
}