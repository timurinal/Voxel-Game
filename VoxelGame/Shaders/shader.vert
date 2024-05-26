#version 330 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec2 vTexCoord;
layout (location = 2) in float vFaceId;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_model;

out vec2 texCoord;
out float faceId;

void main() {
    gl_Position = m_proj * m_view * m_model * vec4(vPosition, 1.0);
    
    texCoord = vTexCoord;
    faceId = vFaceId;
}