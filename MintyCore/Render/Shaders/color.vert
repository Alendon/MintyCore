#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;

layout(location = 0) out vec3 out_Color;

layout (push_constant) uniform constants{
    mat4 renderMatrix;
} PushConstants;

void main()
{
    gl_Position = renderMatrix * vec4(inPosition, 1.f);
    out_Color = Color;
}