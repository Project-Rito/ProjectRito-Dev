#version 330 core

uniform sampler2DArray texWater_Emm;
uniform sampler2DArray texWater_Nrm;

in vec2 fTexCoords;
in vec3 fNormals;
in vec4 fTangentWorld;

flat in uint texIndex;

out vec4 fragColor;

void main(void)
{
    // Normals
    vec4 texNormal = texture(texWater_Nrm, vec3(fTexCoords.xy, texIndex));
    vec3 N = fNormals;
    vec3 T = fTangentWorld.xyz;
    vec3 BiT = cross(N, T) * texNormal.w;

    // Not used right now, since we need to calculate tangents.
    vec3 displayNormal = texNormal.r * T + texNormal.g * N + texNormal.b * BiT;

    // Emmission
    float emm = texture(texWater_Emm, vec3(fTexCoords.xy, texIndex)).r;


    // Lighting
    float halfLambert = max(texNormal.y,0.5);


    // Base water color
    fragColor = vec4(0.1, 0.1, 0.25, 1);

    fragColor = vec4(fragColor.rgb * (emm + 1), fragColor.a);

    fragColor = vec4(fragColor.rgb * halfLambert, fragColor.a); // Use that lighting here
}