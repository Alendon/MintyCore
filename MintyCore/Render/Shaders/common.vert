#version 460

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;
layout(location = 3) in vec2 inUV;

layout(location = 0) out vec3 out_Color;

layout(set = 0, binding = 0) uniform CameraBuffer{
    mat4 viewProj;
} cameraData;


layout(std140, set = 1, binding = 0) readonly buffer TransformBuffer{
    mat4 data[];
} transformBuffer;

void main()
{
    mat4 transformMatrix = (cameraData.viewProj * transformBuffer.data[gl_BaseInstance]);
    gl_Position = transformMatrix * vec4(inPosition, 1.f);
    out_Color = inPosition;
}