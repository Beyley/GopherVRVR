#version 460

layout (location = 0) in vec4 f_VertexColor;
layout (location = 1) in vec2 f_VertexTextureCoordinates;

out vec4 FragColor;

void main() {
    FragColor = f_VertexColor;
}