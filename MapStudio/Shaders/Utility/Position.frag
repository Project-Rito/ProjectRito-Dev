#version 330 core
in float faceIndex;
in vec3 posWorld;

layout (location = 0) out vec4 fragOutput;

uniform int pickFace;
uniform int pickedIndex;
uniform vec4 color;

vec4 PickFace()
{
    int pick = pickedIndex + int(gl_PrimitiveID);

    return vec4(
        ((pick >> 16) & 0xFF) / 255.0,
        ((pick >> 8) & 0xFF) / 255.0,
        (pick & 0xFF) / 255.0,
        ((pick >> 24) & 0xFF) / 255.0
        );
}

void main() {
    fragOutput = vec4(posWorld, 1.f);
}