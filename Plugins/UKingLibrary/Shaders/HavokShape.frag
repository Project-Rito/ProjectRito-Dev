#version 330 core

flat in vec3 v_NormalWorld;
flat in vec4 v_VertexColor;

uniform float u_Opacity;

out vec4 fragColor;

void main(void)
{
    fragColor = v_VertexColor;

    float halfLambert = max(v_NormalWorld.y,0.5);
    fragColor = vec4(fragColor.rgb * halfLambert, u_Opacity);
}