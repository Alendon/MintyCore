#version 450 

layout (location = 0) in vec3 in_Color;
layout (location = 1) in vec2 in_TexCoords;

layout (location = 0) out vec4 outFragColor;

layout(set = 1, binding = 0) uniform sampler2D Sampler;

void main(){
    outFragColor = texture(Sampler, in_TexCoords);
}