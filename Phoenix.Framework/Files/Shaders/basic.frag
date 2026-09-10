#version 450
#extension GL_EXT_nonuniform_qualifier : require

layout(set = 1, binding = 0) uniform sampler2D uTextures[];

layout(location = 0) in vec4 fragColor;
layout(location = 1) in vec2 fragTexCoord;
layout(location = 2) in flat uint fragAlbedoIndex;

layout(location = 0) out vec4 outColor;

void main() {
    vec4 texColor = texture(uTextures[nonuniformEXT(fragAlbedoIndex)], fragTexCoord);
    outColor = fragColor * texColor;
}
