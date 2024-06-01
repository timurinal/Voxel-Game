#version 450 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec2 vTexcoord;
layout (location = 2) in vec3 vColour;

out vec2 texcoord;
out vec3 fragPos;
out vec3 vertexColour;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_model;

void main()
{
    gl_Position = m_proj * m_view * m_model * vec4(vPosition, 1.0);
    
    texcoord = vTexcoord;
    fragPos = vec3(m_model * vec4(vPosition, 1.0));
    vertexColour = vColour;
}