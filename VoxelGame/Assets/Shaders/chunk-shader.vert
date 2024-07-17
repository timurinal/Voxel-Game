#version 450 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vTexCoord;
layout (location = 3) in float vFaceId;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_model;

uniform mat4 m_lightOrtho;
uniform mat4 m_lightView;

out vec2 texcoord;
out float faceId;
out vec3 normal;
out vec3 fragPos;
out vec4 fragPosLightSpace;
out float fogDepth;

void main() {
    vec4 pos = m_model * vec4(vPosition, 1.0);
    gl_Position = m_proj * m_view * pos;

    normal = mat3(transpose(inverse(m_model))) * vNormal;
    fragPos = vec3(m_model * vec4(vPosition, 1.0));
    texcoord = vTexCoord;
    faceId = vFaceId;
    fragPosLightSpace = m_lightOrtho * m_lightView * pos;
    fogDepth = -((m_view * m_model) * vec4(vPosition, 1.0)).z;
}