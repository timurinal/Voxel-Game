#version 450 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vUv;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_model;

out vec2 texcoord;
out vec3 normal;
out vec3 fragPos;

void main() {
    vec4 pos = m_model * vec4(vPosition, 1.0);
    gl_Position = m_proj * m_view * pos;

    normal = mat3(transpose(inverse(m_model))) * vNormal;
    fragPos = vec3(m_model * vec4(vPosition, 1.0));
    texcoord = vUv;
}