#version 450 core

layout (location = 0) in uint vData;

struct VertexData {
    vec3 position;
    vec3 normal;
    int tex;
    vec2 uvs;
    ivec2 uvi;
    int faceId;
};

uniform mat4 m_lightProj;
uniform mat4 m_lightView;

uniform vec3 chunkPosition;

VertexData unpackData(uint data);

void main() {
    VertexData unpackedData = unpackData(vData);

    vec3 vertexPosition = unpackedData.position + chunkPosition;
    vec4 pos = vec4(vertexPosition, 1.0);
    gl_Position = m_lightProj * m_lightView * pos;
}

VertexData unpackData(uint data) {
    VertexData vData;
    
    vec3 pos;
    pos.z = float((data >> 13) & 0x3F);
    pos.y = float((data >> 7) & 0x3F);
    pos.x = float(data & 0x3F);

    vData.position = pos;

    return vData;
}