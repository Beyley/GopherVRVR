#version 460

layout (location = 0) in vec3 VertexPosition;
layout (location = 1) in vec2 VertexTextureCoordinate;
layout (location = 2) in vec4 VertexColor;

uniform mat4 ProjectionMatrix;
uniform mat4 ModelMatrix;
uniform mat4 ViewMatrix;

layout (location = 0) out vec4 f_VertexColor;
layout (location = 1) out vec2 f_VertexTextureCoordinates;

void main() {
    gl_Position = ProjectionMatrix * ViewMatrix * ModelMatrix * vec4(VertexPosition, 1.0);
    f_VertexColor = VertexColor;
    f_VertexTextureCoordinates = VertexTextureCoordinate;
}