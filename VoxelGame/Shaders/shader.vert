#version 330 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec2 vTexCoord;
//layout (location = 2) in vec4 vColour;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_model;

out vec4 fragColour;
out vec2 texCoord;

void main() {
    gl_Position = m_proj * m_view * m_model * vec4(vPosition, 1.0);
    
//    fragColour = vColour;
    texCoord = vTexCoord;
}