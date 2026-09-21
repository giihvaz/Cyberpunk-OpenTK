#version 430 core

in vec2 f_Uv;
out vec4 out_Color;

uniform sampler2D u_Texture;
uniform float u_Time;

void main() {
    vec2 texel = 1.0 / vec2(textureSize(u_Texture, 0));
    vec3 color = texture(u_Texture, f_Uv).rgb;

    float bloomRadius = 3.6;
    float bloomThreshold = 0.45;
    float bloomIntensity = 7.8;

    vec3 bloom = vec3(0.0);
    for (int x = -4; x <= 4; x++) {
        for (int y = -4; y <= 4; y++) {
            vec3 c = texture(u_Texture, f_Uv + vec2(x, y) * texel * bloomRadius).rgb;
            float bright = max(max(c.r, c.g), c.b);
            bloom += max(c - vec3(bloomThreshold), vec3(0.0)) * smoothstep(bloomThreshold, 2.5, bright);
        }
    }
    bloom /= 81.0;

    float scanline = 0.94 + 0.06 * sin((f_Uv.y + u_Time * 0.08) * 900.0);
    vec3 finalColor = (color + bloom * bloomIntensity) * scanline;

    // vinheta leve para estética noturna/cyberpunk
    float vignette = smoothstep(0.95, 0.35, distance(f_Uv, vec2(0.5)));
    finalColor *= vignette;

    out_Color = vec4(finalColor, 1.0);
}
