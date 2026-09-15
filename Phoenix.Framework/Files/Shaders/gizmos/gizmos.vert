#version 450 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec4 aColor;

layout(set = 0, binding = 0) uniform CommonData {
    mat4 uView;
    mat4 uProjection;
    vec3 uCamPos;
    float uTime;
    float uDeltaTime;
};

layout(location = 0) out vec4 vColor;

void main() {
    vColor = aColor;
    gl_Position = uProjection * uView * vec4(aPos, 1.0);
}