#version 460

layout(location = 0) out vec3 outColor;

struct Triangle {
    vec3 Point1;
    float _padding1;
    vec3 Point2;
    float _padding2;
    vec3 Point3;
    float _padding3;
    vec3 Color;
    float _padding4;
};

layout(std430, set = 0, binding = 0) readonly buffer TriangleBuffer {
    Triangle triangles[];
} triangles;

void main(){
    Triangle triangle = triangles.triangles[gl_InstanceIndex];
    
    outColor = triangle.Color;
    
    switch(gl_VertexIndex){
        case 0:
            gl_Position = vec4(triangle.Point1, 1.0);
            break;
        case 1:
            gl_Position = vec4(triangle.Point2, 1.0);
            break;
        case 2:
            gl_Position = vec4(triangle.Point3, 1.0);
            break;
    }
}