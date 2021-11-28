#version 460

layout(location = 0) in vec3 in_Position;
layout(location = 1) in vec3 in_Color;
layout(location = 2) in vec3 in_Normal;
layout(location = 3) in vec2 in_UV;

layout(location = 0) out vec3 out_Color;
layout(location = 1) out vec2 out_UV;

void main()
{
    gl_Position =  vec4(in_Position, 1.f);
    out_Color = in_Color;
    out_UV = in_UV;
}