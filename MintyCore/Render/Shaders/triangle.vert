#version 460

layout(location = 0) in vec3 in_Position;
layout(location = 1) in vec3 in_Color;
layout(location = 2) in vec3 in_Normal;
layout(location = 3) in vec2 in_UV;

layout(location = 4) in mat4 transformMatrix;

layout(location = 0) out vec3 out_Color;
layout(location = 1) out vec2 out_UV;

layout(set = 0, binding = 0) uniform CameraBuffer{
    mat4 viewProj;
} cameraData;

void main()
{
    mat4 transform = cameraData.viewProj * transformMatrix;
    gl_Position = transform * vec4(in_Position, 1.f);
    out_Color = in_Color;
    out_UV = in_UV;
}