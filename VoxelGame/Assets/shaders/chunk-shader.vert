#version 450 core

layout (location = 0) in uint vData;

struct VertexData {
    vec3 position;
    vec3 normal;
    int tex;
    vec2 uvs;
    int faceId;
};

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;

flat out vec3 vertexPos;
out vec3 fragPos;

out vec2 uv;

flat out int faceId;

uniform mat4 m_proj;
uniform mat4 m_view;

uniform vec3 chunkPosition;

const vec3 normals[] = vec3[6](
vec3(0, 0, -1), vec3( 0,  0, 1),
vec3(0, 1,  0), vec3( 0, -1, 0),
vec3(1, 0,  0), vec3(-1,  0, 0)
);

VertexData unpackData(uint data);
vec2 computeCubeTextureCoordinates(vec3 position, vec3 normal);

void main() {
    VertexData unpackedData = unpackData(vData);

    vec3 vertexPosition = unpackedData.position + chunkPosition;
    vec4 pos = vec4(vertexPosition, 1.0);
    gl_Position = m_proj * m_view * pos;

    normal = unpackedData.normal;
    
    if (abs(normal.z) > abs(normal.y)) {
        tangent = normalize(cross(vec3(0.0, 1.0, 0.0), normal));
    } else {
        tangent = normalize(cross(vec3(0.0, 0.0, 1.0), normal));
    }
        bitangent = normalize(cross(normal, tangent));
    
    normal = unpackedData.normal;
    tangent = tangent;
    bitangent = bitangent;

    vertexPos = pos.xyz;
    fragPos = pos.xyz;

    uv = unpackedData.uvs;
    
    faceId = unpackedData.faceId;
}

VertexData unpackData(uint data) {
    VertexData vData;

    vData.tex = int((data >> 22) & 0xFF);
    int texU = int((data >> 31) & 0x01);    // u
    int texV = int((data >> 30) & 0x01);    // v
    vData.uvs = vec2(texU, texV);

    int normalId = int((data >> 19) & 0x07);
    vData.normal = normals[normalId];
    vData.faceId = normalId;

    vec3 pos;
    pos.z = float((data >> 13) & 0x3F);
    pos.y = float((data >> 7) & 0x3F);
    pos.x = float(data & 0x3F);

    vData.position = pos;

    return vData;
}

vec2 computeCubeTextureCoordinates(vec3 position, vec3 normal) {
    vec2 uv;
    vec3 absNormal = abs(normal);

    // Determine the face of the cube based on the dominant normal direction
    if (absNormal.x > absNormal.y && absNormal.x > absNormal.z) {
        // X face
        if (normal.x > 0.0) {
            uv = vec2(-position.y, -position.z); // +X face
        } else {
            uv = vec2(position.z, position.y);  // -X face
        }
    } else if (absNormal.y > absNormal.x && absNormal.y > absNormal.z) {
        // Y face
        if (normal.y > 0.0) {
            uv = vec2(position.z, position.x); // +Y face
        } else {
            uv = vec2(position.x, position.z);  // -Y face
        }
    } else {
        // Z face
        if (normal.z > 0.0) {
            uv = vec2(position.x, position.y); // +Z face
        } else {
            uv = vec2(-position.y, -position.x); // -Z face
        }
    }

//    vec2 signUv = sign(uv);
//    bool isNegative;
//    if (signUv.x < 0) {
//        float u = uv.x;
//        float v = uv.y;
//        uv = vec2(1.0 - u, 1.0 - v);
//    }
//
//    if (signUv.y < 0) {
//        float u = uv.x;
//        float v = uv.y;
//        uv = vec2(u, 1.0 - v);
//    }

    return abs(uv);
}