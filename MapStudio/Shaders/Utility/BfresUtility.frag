#version 330 core

//#using UTILITY

// Used to receive vertex info
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

// Used to receive sampler info
struct StandardSamplerInfo {
    int Enabled;
    int TexcoordIdx;
};
struct ArraySamplerInfo {
    int Enabled;
    int TexcoordIdx;
    float Index;
};

// Defined in Utility.frag.
float Luminance(vec3 rgb);

vec3 CalcBumpedNormal(vec2 texNormal, VertexAttributes vert)
{
    float normalIntensity = 1;

	//if (normal_map_weight != 0) //MK8 and splatoon 1/2 uses this param
	//      normalIntensity = normal_map_weight;

    // Calculate the resulting normal map and intensity.
	vec3 normalMapColor = vec3(1);
    normalMapColor = vec3(texNormal, 1);
    normalMapColor = mix(vec3(0.5, 0.5, 1), normalMapColor, normalIntensity);

    // Remap the normal map to the correct range.
    vec3 normalMapNormal = 2.f * normalMapColor - vec3(1);

    // TBN Matrix.
    vec3 T = vert.tangent;
    vec3 B = vert.bitangent;

    if (Luminance(B) < 0.01) // This is hella bozo dumb.
        B = normalize(cross(T,  vert.normal));
    mat3 tbnMatrix = mat3(T, B,  vert.normal);

    vec3 newNormal = tbnMatrix * normalMapNormal;
    return normalize(newNormal);
}

// Keep in mind lightVec is incoming light *in the direction it's going.* Ex: straight-down light would be vec3(0.f, -1.f, 0.f).
float CalcSpec(vec3 worldNormal, vec3 camPosition, vec3 lightVec, float specMask, VertexAttributes vert) {
    vec3 i = lightVec;
    vec3 o = i - (2 * (dot(i, worldNormal)) * worldNormal);
    vec3 c = camPosition - vert.worldPosition;
    float spec = min(dot(normalize(c), normalize(o)) * specMask, 1.f);
    float diff = dot(worldNormal, -lightVec);

    return mix(diff, spec, specMask);
}

vec2 GetTexcoord(StandardSamplerInfo samplerInfo, VertexAttributes vert) {
    switch (samplerInfo.TexcoordIdx) {
        case 0:
            return vert.texCoord0;
        case 1:
            return vert.texCoord1;
        case 2:
            return vert.texCoord2;
        case 3:
            return vert.texCoord3;
        default:
            return vert.texCoord0;
    }
}
vec2 GetTexcoord(ArraySamplerInfo samplerInfo, VertexAttributes vert) {
    switch (samplerInfo.TexcoordIdx) {
        case 0:
            return vert.texCoord0;
        case 1:
            return vert.texCoord1;
        case 2:
            return vert.texCoord2;
        case 3:
            return vert.texCoord3;
        default:
            return vert.texCoord0;
    }
}

#define BLEND_COLOR_COUNT 4
vec4 Blend_OneMinusSourceAlpha(vec4[BLEND_COLOR_COUNT] colors) {
    vec4 result = vec4(0.f);
    float remainingAlpha = 1.f;
    for (int i = 0; i < BLEND_COLOR_COUNT; i++) { // using colors.length causes error on some drivers.
        result.rgb += colors[i].rgb * colors[i].a * remainingAlpha;
        result.a += colors[i].a * remainingAlpha;
        remainingAlpha -= colors[i].a;
        remainingAlpha = max(remainingAlpha, 0.f); // Ensure we don't go into negative
    }

    return result;
}