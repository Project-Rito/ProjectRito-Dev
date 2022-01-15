#version 330 core

uniform sampler2DArray texTerrain_Alb;
uniform sampler2DArray texTerrain_Nrm;

in vec4 fTexCoords;
in vec3 fNormals;
in vec4 fTangentWorld;

in float materialWeight;

flat in vec2 texIndex;

out vec4 fragColor;

void main(void)
{
    vec4 color0 = texture(texTerrain_Alb, vec3(fTexCoords.xy, texIndex[0]));
    vec4 color1 = texture(texTerrain_Alb, vec3(fTexCoords.zw, texIndex[1]));


    // Normals
    vec4 texNormal0 = texture(texTerrain_Nrm, vec3(fTexCoords.xy, texIndex[0]));
    vec4 texNormal1 = texture(texTerrain_Nrm, vec3(fTexCoords.zw, texIndex[1]));
    vec4 texNormal = mix(texNormal0, texNormal1, materialWeight);
    vec3 N = fNormals;
    vec3 T = fTangentWorld.xyz;
    vec3 BiT = cross(N, T) * texNormal.w;

    // Not used right now, since we need to calculate tangents.
    vec3 displayNormal = texNormal.r * T + texNormal.g * N + texNormal.b * BiT;

    // Base color
    fragColor = mix(color0, color1, materialWeight);

    // Lighting
    float halfLambert = max(texNormal.y,0.5);
    fragColor = vec4(fragColor.rgb * halfLambert, fragColor.a); // Use that lighting here
}