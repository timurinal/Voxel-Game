#version 330 core

out vec4 finalCol;

in vec4 fragColour;
in vec2 texCoord;

uniform sampler2D texture0;

void main() {
    // finalCol = fragColour;
    finalCol = texture(texture0, texCoord);
    // finalCol = vec4(texCoord, 0, 1);
}