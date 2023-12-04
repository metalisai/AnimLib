namespace AnimLib;

internal partial class EffectBuffer {
    string effectVert = @"#version 330 core
layout(location = 0) in vec4 position;
void main() {
    gl_Position = position;
}";

    string acesFrag = @"#version 330 core
layout(location = 0) out vec4 outColor;

uniform sampler2D _MainTex;
uniform ivec2 _ViewportSize;

vec3 ACESFilm(vec3 x) {
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0f, 1.0f);
}

void main() {
    vec2 texCoord = gl_FragCoord.xy / _ViewportSize;
    vec4 color = texture(_MainTex, texCoord, 0);
    color.rgb = ACESFilm(color.rgb);
    outColor = color;
}
";

}
