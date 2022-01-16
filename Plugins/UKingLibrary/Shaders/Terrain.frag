#version 330 core

uniform sampler2DArray texTerrain_Alb;
uniform sampler2DArray texTerrain_Nrm;

uniform float uBrightness;
uniform bool uDebugSections;

in vec4 v_TexCoords;
in vec3 v_NormalWorld;
in vec3 v_TangentWorld;
in vec3 v_DebugHighlight;

in float materialWeight;

flat in vec2 texIndex;

out vec4 fragColor;

void main(void)
{
    vec4 color0 = texture(texTerrain_Alb, vec3(v_TexCoords.xy, texIndex[0]));
    vec4 color1 = texture(texTerrain_Alb, vec3(v_TexCoords.zw, texIndex[1]));


    // Normals
    vec4 texNormal0 = texture(texTerrain_Nrm, vec3(v_TexCoords.xy, texIndex[0]));
    vec4 texNormal1 = texture(texTerrain_Nrm, vec3(v_TexCoords.zw, texIndex[1]));
    vec4 texNormal = mix(texNormal0, texNormal1, materialWeight);
    vec3 N = v_NormalWorld;
    vec3 T = v_TangentWorld;
    vec3 BiT = cross(N, T) * texNormal.w;

    // World normal calculation
    vec3 worldNormal = texNormal.r * T + texNormal.g * N + texNormal.b * BiT;

    // Base color
    fragColor = mix(color0, color1, materialWeight);

    // Debug highlight
    if (uDebugSections)
        fragColor.rgb += v_DebugHighlight;

    // Lighting
    float halfLambert = max(worldNormal.y,0.5);
    fragColor = vec4(fragColor.rgb * halfLambert, fragColor.a); // Use that lighting here
    fragColor.rgb *= uBrightness;
}