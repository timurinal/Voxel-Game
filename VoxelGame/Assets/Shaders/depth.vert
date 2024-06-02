#version 450 core

layout (location = 0) in vec3 vPosition;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_model;

void main()
{
    gl_Position = m_proj * m_view * m_model * vec4(vPosition, 1.0);
}