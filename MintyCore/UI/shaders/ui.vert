#version 460

layout(push_constant) uniform PushConstants
{
    vec2[4] position;
    vec2[4] uv;
    uint[4] colors;
} pushContants;

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec2 outUV;

layout(set = 1, binding = 0) uniform transform {
    mat4 mat;
} transformBuffer;

void main()
{
    //6 vertices for 2 triangles, defining a rectangle.

    int index;
    switch (gl_VertexIndex) {
        case 0: index = 0; break;
        case 1:
        case 4: index = 1; break;
        case 2:
        case 3: index = 2; break;
        case 5: index = 3; break;
    }

    outColor = unpackUnorm4x8(pushContants.colors[index]);
    outUV = pushContants.uv[index];
    gl_Position = transformBuffer.mat * vec4(pushContants.position[index], 0.0, 1.0);
}
