#version 330 core

//#using BFRES_UTILITY

uniform sampler2DArray texWater_Emm;
uniform sampler2DArray texWater_Nrm;

uniform float uBrightness;
uniform bool uDebugSections;

in vec3 v_PositionWorld;
in vec2 v_TexCoords;
in float v_XAxisFlowRate;
in float v_ZAxisFlowRate;
in vec3 v_NormalWorld;
in vec3 v_TangentWorld;
in vec3 v_DebugHighlight;

flat in uint texIndex;

out vec4 fragColor;

// GL
uniform vec3 camPosition;
uniform float millisecond;

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
    vert.texCoord0 = v_TexCoords;
    vert.texCoord1 = v_TexCoords;
    vert.texCoord2 = v_TexCoords;
    vert.texCoord3 = v_TexCoords;
    vert.vertexColor = vec4(1.f);
    vert.normal = v_NormalWorld;
    vert.tangent = v_TangentWorld.xyz;
    vert.bitangent = cross(v_NormalWorld, v_TangentWorld.xyz);

    // Flow stuff
    vec2 flowTexCoord0 = vec2((v_TexCoords.x * v_XAxisFlowRate), (v_TexCoords.y * v_ZAxisFlowRate)) * (millisecond / 20000.f);
    vec2 flowTexCoord1 = vec2((v_TexCoords.x * -v_XAxisFlowRate), (v_TexCoords.y * -v_ZAxisFlowRate)) * (millisecond / 30000.f);

    // Normals
    vec4 texNormal0 = texture(texWater_Nrm, vec3(flowTexCoord0, texIndex));
    vec4 texNormal1 = texture(texWater_Nrm, vec3(flowTexCoord1, texIndex));
    vec4 texNormal = mix(texNormal0, texNormal1, 0.5f);
    vec3 worldNormal = CalcBumpedNormal(texNormal.xy, vert);

    // Base water color
    if (texIndex == 0u) // Water
        fragColor = vec4(0.1, 0.1, 0.25, 0.5);
    if (texIndex == 1u) // Hot Water
        fragColor = vec4(0.2, 0.2, 0.5, 0.75);
    if (texIndex == 2u) // Poison Water
        fragColor = vec4(0.1, 0.25, 0.1, 0.5);
    if (texIndex == 3u) // Lava
        fragColor = vec4(0.25, 0.1, 0.1, 0.8);
    if (texIndex == 4u) // Cold/Ice Water
        fragColor = vec4(0.05, 0.05, 0.125, 0.75);
    if (texIndex == 5u) // Bog
        fragColor = vec4(0.25, 0.25, 0.25, 0.90);
    if (texIndex == 6u) // Clear Water
        fragColor = vec4(0.2, 0.2, 0.5, 0.3);
    if (texIndex == 7u) // Ocean Water
        fragColor = vec4(0.05, 0.05, 0.25, 0.75);

    // Debug highlight
    if (uDebugSections)
        fragColor.rgb += v_DebugHighlight;

    // Fake emmission
    float emm0 = texture(texWater_Emm, vec3(flowTexCoord0, texIndex)).r;
    float emm1 = texture(texWater_Emm, vec3(flowTexCoord1, texIndex)).r;
    float emm = mix(emm0, emm1, 0.5f);
    fragColor = vec4(fragColor.rgb * (emm + 1), fragColor.a);

    // Spec
    float spec = CalcSpec(worldNormal, camPosition, vec3(0.f, -1.f, 0.f), 1.f, vert);

    // Lighting
    float halfLambert = max(spec, 0.5f);
    fragColor = vec4(fragColor.rgb * halfLambert, fragColor.a); // Use that lighting here
    fragColor.rgb *= uBrightness;
}