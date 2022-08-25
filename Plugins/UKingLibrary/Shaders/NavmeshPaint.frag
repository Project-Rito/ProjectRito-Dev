#version 330 core
in vec3 fragPosWorld;
flat in vec3 vertPosWorld;
flat in vec4 vertPosScreen;
in vec3 normalWorld;
flat in float vertexIndex;

uniform int u_shapeIndex;
uniform vec2 u_resolution;

uniform float u_filterAngleMax;

layout (location = 0) out vec4 fragOutput;

// Ensures that the exact position of the vertex is represented by the closest fragment.
vec3 getFragWorldPos() {
    if (abs(gl_FragCoord - vertPosScreen).x <= (1.f/u_resolution.x/2.f) && abs(gl_FragCoord - vertPosScreen).y <= (1.f/u_resolution.y/2.f)) {
        return vertPosWorld;
    }
    return fragPosWorld;
}

void main() {
    if (normalWorld.y < u_filterAngleMax) {
        fragOutput = vec4(0.f, 0.f, 0.f, 0.f); // Should only occlude
        return;
    }

    fragOutput = vec4(vertexIndex, float(u_shapeIndex), length(getFragWorldPos() - vertPosWorld), 1.f); // X: vertex index, Y: shape index, Z: Distance from fragment to closest vertex, W: Tool intensity
}