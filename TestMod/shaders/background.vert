#version 460

layout(location = 0) out vec3 outColor;

vec3 vertices[] = {
vec3(-1.0, 1.0, 0.0),
vec3(1.0, 1.0, 0.0),
vec3(-1.0, -1.0, 0.0),
vec3(1.0, 1.0, 0.0),
vec3(1.0, -1.0, 0.0),
vec3(-1.0, -1.0, 0.0)
};

void main(){
    outColor = vec3(0, 1, 0);
    gl_Position = vec4(vertices[gl_VertexIndex], 1.0);
}