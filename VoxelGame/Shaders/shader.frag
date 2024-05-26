#version 330 core

out vec4 finalCol;

in vec2 texCoord;
in float faceId;

uniform sampler2D texture0;

const float faceShading[6] = float[6](
    0.7, 0.8,  // front back
    1.0, 0.7,  // top bottom
    0.7, 0.8   // right left
);

void main() {
    // finalCol = vec4(0.1, 0.1, 0.1, 1.0);
    // finalCol = fragColour;
    finalCol = texture(texture0, texCoord) * faceShading[int(faceId)];
    // finalCol = vec4(texCoord, 0, 1);
}