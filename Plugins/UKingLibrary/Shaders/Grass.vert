#version 330 core

in vec3 vPosition;
in vec3 vColor;
in vec3 vNormalWorld;
in vec3 vTangentWorld;
in vec3 vDebugHighlight;

out vec3 v_NormalWorld;
out vec3 v_TangentWorld;
out vec3 v_Color;
out vec3 v_DebugHighlight;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main()
{
    v_Color = vColor;
    v_NormalWorld = vNormalWorld;
    v_TangentWorld = vTangentWorld;

    v_DebugHighlight = vDebugHighlight;

    gl_Position = mtxCam * mtxMdl * vec4(vPosition, 1.0);
}