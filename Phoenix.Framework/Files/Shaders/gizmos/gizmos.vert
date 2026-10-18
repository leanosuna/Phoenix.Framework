#version 450 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec4 aColor;

layout(set = 0, binding = 0) uniform FrameUniforms {
    mat4 uView;
    mat4 uProjection;
    mat4 uViewProjection;
    mat4 uInverseView;
    mat4 uInverseProjection;
    vec4 uCameraPosition;
    vec2 uResolution;
    float uTime;
    float uDeltaTime;
};

layout(location = 0) out vec4 vColor;

void main() {
    vColor = aColor;
    gl_Position = uViewProjection * vec4(aPos, 1.0);
}