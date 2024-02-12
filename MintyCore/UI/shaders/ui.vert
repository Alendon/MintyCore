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
    vec2 position;
    vec2 uv;
    uint color;
    switch (gl_VertexIndex) {
        case 0: {
            position = pushContants.position[0];
            uv = pushContants.uv[0];
            color = pushContants.colors[0];
            break; 
        }
        case 1:
        case 4: {
            position = pushContants.position[1];
            uv = pushContants.uv[1];
            color = pushContants.colors[1];
            break;
        }
        case 2:
        case 3: {
            position = pushContants.position[2];
            uv = pushContants.uv[2];
            color = pushContants.colors[2];
            break;
        }
        case 5: {
            position = pushContants.position[3];
            uv = pushContants.uv[3];
            color = pushContants.colors[3];
            break;
        }
    }

    outColor = unpackUnorm4x8(color);
    outUV = uv;
    gl_Position = transformBuffer.mat * vec4(position, 0.0, 1.0);
}
