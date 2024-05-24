#version 330 core

out vec4 finalCol;

in vec4 fragColour;

void main() {
    finalCol = fragColour;
}