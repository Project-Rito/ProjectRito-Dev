#version 330 core

uniform float uBrightness;
uniform bool uDebugSections;

in vec3 v_NormalWorld;
in vec3 v_TangentWorld;
in vec3 v_Color;
in vec3 v_DebugHighlight;

flat in uint texIndex;

out vec4 fragColor;

void main(void)
{
    // Grass color
    fragColor = vec4(v_Color, 1);

    // Debug highlight
    if (uDebugSections)
        fragColor.rgb += v_DebugHighlight;

    // Lighting
    float halfLambert = max(v_NormalWorld.y,0.5);
    fragColor = vec4(fragColor.rgb * halfLambert, 0.5); // Use that lighting here
    fragColor.rgb *= uBrightness;
}