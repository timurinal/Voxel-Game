float linearizeDepth(float depth, float near, float far) {
    // Convert depth to the device coordinate space (-1 to 1)
    float ndc = depth * 2.0 - 1.0;
    // Convert the device coordinate to the view space depth
    float linearDepth = (2.0 * near * far) / (far + near - ndc * (far - near));
    return linearDepth;
}