#version 450 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec3 vTangent;
layout (location = 3) in vec4 vColour;
layout (location = 4) in vec2 vUv;

out vec3 normal;
out vec2 texcoord;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_model;

void main() {
    vec4 vertexPosition = m_model * vec4(vPosition, 1.0);
    gl_Position = m_proj * m_view * vertexPosition;
    
    normal = normalize(vNormal);
    texcoord = vUv;
}