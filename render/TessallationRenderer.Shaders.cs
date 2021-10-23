
namespace AnimLib {
    partial class TessallationRenderer : IRenderer {

string vertShader = @"#version 330 core 
layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
out vec4 v_color;
uniform mat4 _ModelToClip;
void main() {
    gl_Position = _ModelToClip*position;
    v_color = color;
}";

string fragShader = @"#version 330 core
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 v_color;
uniform mat4 _ModelToClip;
uniform int _EntityId;
uniform vec4 _Color;
void main() {
    outColor = v_color*_Color;
    outEntityId = _EntityId;
}";
    }
}
