#version 330 core

//#using UTILITY

// Used to receive vertex info
struct VertexAttributes
{
    vec3 worldPosition;
    vec2 texCoord;
    vec4 vertexColor;
    vec3 normal;
    vec3 tangent;
    vec3 bitangent;
};

// Defined in Utility.frag.
float Luminance(vec3 rgb);

vec3 CalcBumpedNormal(vec3 texNormal, VertexAttributes vert)
{
    float normalIntensity = 1;

	//if (normal_map_weight != 0) //MK8 and splatoon 1/2 uses this param
	//      normalIntensity = normal_map_weight;

    // Calculate the resulting normal map and intensity.
	vec3 normalMapColor = vec3(1);
    normalMapColor = vec3(texNormal.rg, 1);
    normalMapColor = mix(vec3(0.5, 0.5, 1), normalMapColor, normalIntensity);

    // Remap the normal map to the correct range.
    vec3 normalMapNormal = 2.0 * normalMapColor - vec3(1);

    // TBN Matrix.
    vec3 T = vert.tangent;
    vec3 B = vert.bitangent;

    if (Luminance(B) < 0.01)
        B = normalize(cross(T,  vert.normal));
    mat3 tbnMatrix = mat3(T, B,  vert.normal);

    vec3 newNormal = tbnMatrix * normalMapNormal;
    return normalize(newNormal);
}