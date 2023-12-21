#version 460

layout(location = 0) out vec2 outUv;

vec3 vertices[] = {
vec3(-1.0, 1.0, 0.0),
vec3(1.0, 1.0, 0.0),
vec3(-1.0, -1.0, 0.0),
vec3(1.0, 1.0, 0.0),
vec3(1.0, -1.0, 0.0),
vec3(-1.0, -1.0, 0.0)
};

vec2 texCoords[] = {
vec2(0.0, 1.0),
vec2(1.0, 1.0),
vec2(0.0, 0.0),
vec2(1.0, 1.0),
vec2(1.0, 0.0),
vec2(0.0, 0.0)
};

void main(){
    outUv = texCoords[gl_VertexIndex];
    gl_Position = vec4(vertices[gl_VertexIndex], 1.0);
}