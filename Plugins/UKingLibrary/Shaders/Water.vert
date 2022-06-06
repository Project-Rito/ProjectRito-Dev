#version 330 core

in vec3 vPosition;
in uint vMaterialIndex;
in vec2 vTexCoord;
in vec3 vNormalWorld;
in vec3 vTangentWorld;
in vec3 vDebugHighlight;

out vec2 v_TexCoords;
out vec3 v_NormalWorld;
out vec3 v_TangentWorld;
flat out uint texIndex;
out vec3 v_DebugHighlight;

uniform mat4[64] mtxMdl;
uniform mat4 mtxCam;

void main()
{
    texIndex = vMaterialIndex;

    v_TexCoords = vTexCoord;
    v_NormalWorld = vNormalWorld;
    v_TangentWorld = vTangentWorld;

    v_DebugHighlight = vDebugHighlight;

    gl_Position = mtxCam * mtxMdl[gl_InstanceID] * vec4(vPosition, 1.0);
}