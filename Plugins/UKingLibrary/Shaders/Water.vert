#version 330 core

in vec3 vPosition;
in uint vMaterialIndex;
in vec2 vTexCoord;
in vec3 vNormalWorld;
in vec3 vTangentWorld;

out vec2 v_TexCoords;
out vec3 v_NormalWorld;
out vec3 v_TangentWorld;
flat out uint texIndex;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main()
{
    texIndex = vMaterialIndex;

    v_TexCoords = vTexCoord;
    v_NormalWorld = vNormalWorld;
    v_TangentWorld = vTangentWorld;

    gl_Position = mtxCam * mtxMdl * vec4(vPosition, 1.0);
}