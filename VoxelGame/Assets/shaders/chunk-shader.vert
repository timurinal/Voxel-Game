#version 450 core

layout (location = 0) in uint vData;

struct VertexData {
    vec3 position;
    vec3 normal;
    int tex;
};

out vec2 texcoord;

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;

out vec3 fragPos;

uniform mat4 m_proj;
uniform mat4 m_view;

uniform vec3 chunkPosition;

const vec3 normals[] = vec3[6](
    vec3(0, 0, -1), vec3( 0,  0, 1),
    vec3(0, 1,  0), vec3( 0, -1, 0),
    vec3(1, 0,  0), vec3(-1,  0, 0)
);

VertexData unpackData(uint data);

void main() {
    VertexData unpackedData = unpackData(vData); 
    
    vec3 vertexPosition = unpackedData.position + chunkPosition;
    vec4 pos = vec4(vertexPosition, 1.0);
    gl_Position = m_proj * m_view * pos;
    
    texcoord = vec2(0);

    normal = normalize(unpackedData.normal);
//    tangent = vTangent;
//    bitangent = cross(normal, tangent);
    
    fragPos = pos.xyz;
}

VertexData unpackData(uint data) {
    VertexData vData;
    vData.tex = int(data >> 24);
    
    int normalId = int((data >> 21) & 0x07);
    vData.normal = normals[normalId];
    
    vec3 pos;
    pos.z = float((data >> 15) & 0x3f);
    pos.y = float((data >> 9) & 0x3f);
    pos.x = float(data & 0x3f);
    vData.position = pos;
    
    return vData;
}