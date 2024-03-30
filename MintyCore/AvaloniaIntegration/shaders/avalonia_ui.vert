#version 460

layout(location = 0) out vec2 out_uv;

vec2 mesh[] = {
    vec2(-1,1), vec2(-1,-1), vec2(1,-1),
    vec2(-1,1), vec2(1,-1), vec2(1,1)
};

vec2 uv[] = {
    vec2(0,1), vec2(0,0), vec2(1,0),
    vec2(0,1), vec2(1,0), vec2(1,1)
};


void main(){
    out_uv = uv[gl_VertexIndex];
    gl_Position = vec4( mesh[gl_VertexIndex], 0.0, 1.0);
}