#version 330 core

in vec3 vPosition;
in vec3 vNormalWorld;
in vec4 vVertexColor;
in float vVertexIndex;

out vec3 fragPosWorld;
flat out vec3 vertPosWorld;
flat out vec4 vertPosScreen;
out vec3 normalWorld;
flat out float vertexIndex;

uniform mat4[64] mtxMdl;
uniform mat4 mtxCam;

void main(){
    vec4 worldPosition = vec4(vPosition.xyz, 1);

    normalWorld = vNormalWorld;
    vertexIndex = vVertexIndex;

    
    fragPosWorld = (mtxMdl[gl_InstanceID] * worldPosition).xyz;
    vertPosWorld = (mtxMdl[gl_InstanceID] * worldPosition).xyz;

    vertPosScreen = (mtxCam * mtxMdl[gl_InstanceID] * worldPosition);
    gl_Position = mtxCam * mtxMdl[gl_InstanceID] * worldPosition;
}