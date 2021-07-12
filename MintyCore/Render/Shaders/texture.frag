#version 450 

layout (location = 0) in vec3 in_Color;
layout (location = 1) in vec2 in_TexCoords;

layout (location = 0) out vec4 outFragColor;

layout(set = 2, binding = 0) uniform sampler Sampler;
layout(set = 2, binding = 1) uniform texture2D Texture;

void main(){
    outFragColor = texture(sampler2D(Texture, Sampler), in_TexCoords);
}