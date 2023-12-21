#version 460

layout(location = 0) in vec4 inColor;
layout(location = 1) in vec2 inUV;

layout(location = 0) out vec4 fragColor;

layout(set = 0, binding = 0) uniform sampler2D tex;

void main() {
    fragColor = inColor * texture(tex, inUV);
}
