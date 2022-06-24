
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
    outColor = /*v_color**/_Color;
    outEntityId = _EntityId;
}";

string tessVS = @"#version 410 core
layout (location = 0) in vec4 position;
uniform mat4 _ModelToWorld;
out vec4 worldPos;
void main() {
    worldPos = _ModelToWorld*position;
}
";
string tessTCS = @"#version 410 core
layout (vertices = 3) out;
uniform vec3 _CamPosWorld;
uniform float _Radius;
in vec4 worldPos[];
out vec4 worldPosES[];
void main()
{
    //worldPosES[gl_InvocationID] = worldPos[g_InvocationID];
    float a = _Radius*sqrt(3.0);
    switch(gl_InvocationID) {
        case 0:
        worldPosES[gl_InvocationID] = worldPos[0] + vec4(0.5*a, -(a*sqrt(3.0))/6.0, 0.0, 0.0);
        break;
        case 1:
        worldPosES[gl_InvocationID] = worldPos[0] + vec4(0.0, 0.5*a*sqrt(3.0) - (a*sqrt(3.0))/6.0, 0.0, 0.0);
        break;
        case 2:
        worldPosES[gl_InvocationID] = worldPos[0] + vec4(-0.5*a, -(a*sqrt(3.0))/6.0, 0.0, 0.0);
        break;
    }

    float dist = distance(worldPos[0].xyz, _CamPosWorld);
    float factor = max(1.0 / (0.0005*dist*dist), 6.0);
    gl_TessLevelOuter[0] = factor;
    gl_TessLevelOuter[1] = factor;
    gl_TessLevelOuter[2] = factor;
    gl_TessLevelInner[0] = 1.0;
}
";
string tessTES = @"#version 410 core
layout(triangles, equal_spacing, ccw) in;
in vec4 worldPosES[];
out vec4 worldPosFS;
uniform mat4 _WorldToClip;
uniform float _Radius;
vec3 interpolate3D(vec3 v0, vec3 v1, vec3 v2)
{
    return vec3(gl_TessCoord.x*v0 + gl_TessCoord.y*v1 + gl_TessCoord.z*v2);
}
void main()
{
    float minT = min(min(gl_TessCoord.x, gl_TessCoord.y), gl_TessCoord.z);
    vec3 tpos = interpolate3D(worldPosES[0].xyz, worldPosES[1].xyz, worldPosES[2].xyz);
    float third = 1.0/3.0;
    vec3 center = vec3(third*worldPosES[0].xyz + third*worldPosES[1].xyz + third*worldPosES[2].xyz);
    if(minT > 0.001) {
        gl_Position = _WorldToClip * vec4(tpos, 1.0);
    } else {
        vec3 pos = center + _Radius*normalize(tpos - center);
        gl_Position = _WorldToClip * vec4(pos, 1.0);
    }
}
";

    }
}
