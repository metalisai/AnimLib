
namespace AnimLib;

internal partial class OpenTKPlatform {
string blitVert = @"#version 330 core
layout(location = 0) in vec4 position;
void main() {
gl_Position = position;
}";

string blitFrag = @"#version 330 core
layout(location = 0) out vec4 outColor;
uniform sampler2D _MainTex;
uniform ivec2 _ViewportSize;
void main() {
vec2 texCoord = gl_FragCoord.xy / _ViewportSize;
outColor = texture(_MainTex, texCoord);
//outColor.a = 0.5;
//outColor = texture(_MainTex, gl_FragCoord.xy);
//outColor = vec4(1.0, 0.0, 0.0, 1.0);
}";

string imguiVert = @"#version 330 core 
layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 texCoord;
out vec4 v_color;
out vec2 v_texCoord;
uniform mat4 _ModelToClip;
void main() {
gl_Position = _ModelToClip*position;
v_color = color;
v_texCoord = texCoord;
}";

string imguiFrag = @"#version 330
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 v_color;
in vec2 v_texCoord;
uniform vec4 _Color;
uniform sampler2D _AtlasTex;
uniform sampler2D _depthPeelTex;
uniform bool _correctGamma = true;
uniform int _entityId;
void main() {
float depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
if(gl_FragCoord.z >= depth) {
    discard;
}

vec3 outColorRGB = _Color.rgb*v_color.rgb;
float alpha = _Color.a*v_color.a;
//outColor = vec4(outColorRGB*alpha, alpha);
outColor = v_color*texture(_AtlasTex, v_texCoord);
if(_correctGamma) {
    outColor.rgb = pow(outColor.rgb, vec3(2.2));
}

outEntityId = _entityId;
}";

string vertShader = @"#version 330 core 
layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
out vec4 v_color;
uniform mat4 _ModelToClip;
void main() {
gl_Position = _ModelToClip*position;
v_color = color;
}";

string fragShader = @"#version 330
layout(location = 0) out vec4 outColor;
in vec4 v_color;
uniform vec4 _Color;
uniform sampler2D _depthPeelTex;
void main() {
float depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
if(gl_FragCoord.z >= depth) {
    discard;
}

vec3 outColorRGB = _Color.rgb*v_color.rgb;
float alpha = _Color.a*v_color.a;
//outColor = vec4(outColorRGB, alpha);
outColor = v_color;
}";
}
