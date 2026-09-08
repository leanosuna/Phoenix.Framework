#version 450

layout(set = 0, binding = 0) uniform CommonUBO {
    mat4 uView;
    mat4 uProjection;
    vec3 uCameraPosition;
    float uTime;
    float uDeltaTime;
};

layout(push_constant) uniform PushConstants {
    mat4 uWorld;
    uint uAlbedoIndex;
    uint uNormalIndex;
    uint uMaterialIndex;
    uint uCustomFlags;
};

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec4 inColor;

layout(location = 0) out vec4 fragColor;

void main() {
    gl_Position = uProjection * uView * uWorld * vec4(inPosition, 1.0);
    fragColor = inColor;
}
