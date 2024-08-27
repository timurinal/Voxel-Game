vec3 triplanar(vec3 position, vec3 normal, vec2 offset, sampler2D tex, float blendSharpness, float scale)
{
    vec2 coordX = (position.zy + offset) * scale;
    vec2 coordY = (position.xz + offset) * scale;
    vec2 coordZ = (position.xy + offset) * scale;
    
    if (normal.x > 0)
        coordX.x = -coordX.x;
    if (normal.y > 0)
        coordY.x = -coordY.x;
    if (normal.z < 0)
        coordZ.x = -coordZ.x;

    vec4 colX = texture(tex, coordX);
    vec4 colY = texture(tex, coordY);
    vec4 colZ = texture(tex, coordZ);

    vec3 blendWeight = pow(abs(normal), vec3(blendSharpness));
    blendWeight /= (blendWeight.x + blendWeight.y + blendWeight.z);

    return colX.rgb * blendWeight.x + colY.rgb * blendWeight.y + colZ.rgb * blendWeight.z;
}