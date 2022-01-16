#version 330 core

uniform sampler2DArray texWater_Emm;
uniform sampler2DArray texWater_Nrm;

uniform float uBrightness;
uniform bool uDebugSections;

in vec2 v_TexCoords;
in vec3 v_NormalWorld;
in vec3 v_TangentWorld;
in vec3 v_DebugHighlight;

flat in uint texIndex;

out vec4 fragColor;

void main(void)
{
    // Normals
    vec4 texNormal = texture(texWater_Nrm, vec3(v_TexCoords.xy, texIndex));
    vec3 N = v_NormalWorld;
    vec3 T = v_TangentWorld;
    vec3 BiT = cross(N, T) * texNormal.w;

    // World normal calculation
    vec3 worldNormal = texNormal.r * T + texNormal.g * N + texNormal.b * BiT;

    // Base water color
    fragColor = vec4(0.1, 0.1, 0.25, 1);

    // Debug highlight
    if (uDebugSections)
        fragColor.rgb += v_DebugHighlight;

    // Fake emmission
    float emm = texture(texWater_Emm, vec3(v_TexCoords.xy, texIndex)).r;
    fragColor = vec4(fragColor.rgb * (emm + 1), fragColor.a);

    // Lighting
    float halfLambert = max(worldNormal.y,0.5);
    fragColor = vec4(fragColor.rgb * halfLambert, 0.5); // Use that lighting here
    fragColor.rgb *= uBrightness;
}