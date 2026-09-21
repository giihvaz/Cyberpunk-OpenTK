#version 430 core

layout(location=0)in vec3 v_Position;
layout(location=1)in vec3 v_Normal;
layout(location=2)in vec2 v_Uv;
layout(location=3)in mat4 i_Model;
layout(location=7)in vec3 i_Color;

out vec2 f_Uv;
out vec3 f_Color;

uniform mat4 u_View;
uniform mat4 u_Projection;
uniform float u_Time;

void main() {
    vec3 world = (vec4(v_Position, 1.0) * i_Model).xyz;
    float fall = mod(u_Time * 18.0 + world.x * 0.37 + world.z * 0.21, 18.0);
    world.y = 12.0 - fall;
    world.x += sin(u_Time * 3.0 + world.z) * 0.15;

    f_Uv = v_Uv;
    f_Color = i_Color;
    gl_Position = vec4(world, 1.0) * u_View * u_Projection;
}
