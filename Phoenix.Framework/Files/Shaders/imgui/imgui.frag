#version 450 core
#extension GL_EXT_nonuniform_qualifier : enable

layout(location = 0) in vec2 vUV;
layout(location = 1) in vec4 vColor;

layout(push_constant) uniform PushConstants {
    vec2 uScale;
    vec2 uTranslate;
    uint uTextureId;
} pc;

layout(set = 1, binding = 0) uniform sampler2D uTextures[];

layout(location = 0) out vec4 fColor;

void main() {
    fColor = vColor * texture(uTextures[nonuniformEXT(pc.uTextureId)], vUV);
}
