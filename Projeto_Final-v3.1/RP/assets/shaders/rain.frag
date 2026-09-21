#version 430 core

in vec2 f_Uv;
in vec3 f_Color;
out vec4 out_Color;

void main() {
    float alpha = smoothstep(0.0, 0.2, f_Uv.y) * smoothstep(1.0, 0.7, f_Uv.y);
    out_Color = vec4(f_Color, 0.38 * alpha);
}
