#version 430 core

in vec3 f_Position;
in vec3 f_Normal;
in vec2 f_Uv;
in vec3 f_LightPosition;

out vec4 out_Color;

uniform sampler2D u_Texture;
uniform vec4 u_Color = vec4(1, 0, 0, 1);
uniform float u_Smoothness = 1.0;
uniform float u_Wetness = 0.0;
uniform vec3 u_EmissionColor = vec3(0.0);
uniform float u_EmissionStrength = 0.0;
uniform vec3 u_AmbientLight = vec3(0.1, 0.1, 0.2);
uniform vec3 u_DirectionalLightDirection = vec3(0, -1, 0);
uniform vec3 u_DirectionalLightColor = vec3(1);
uniform vec3 u_CameraPosition;
uniform sampler2D u_ShadowMap;
uniform float u_Time = 0.0;

float calculateShadow(float lightIntensity, float biasStrength) {
    float depth = texture(u_ShadowMap, f_LightPosition.xy).r;
    float bias = 0.003 * biasStrength;

    if ((f_LightPosition.z - bias) > depth) {
        return 0;
    }
    return lightIntensity;
}

// Pequeno ruido procedural para criar manchas/pocas no asfalto sem precisar de textura extra.
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);

    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

void main() {
    vec4 color = texture(u_Texture, f_Uv) * u_Color;
    vec3 normal = normalize(f_Normal);

    float lightIntensity = max(dot(u_DirectionalLightDirection, -normal), 0.0);
    lightIntensity = calculateShadow(lightIntensity, max(1.0 - dot(-normal, u_DirectionalLightDirection), 0.001));
    lightIntensity *= sqrt(max(-u_DirectionalLightDirection.y, 0.0));
    vec3 diffuse = u_DirectionalLightColor * lightIntensity;

    vec3 toCameraDirection = normalize(u_CameraPosition - f_Position);
    vec3 reflectDirection = reflect(u_DirectionalLightDirection, normal);

    // O brilho molhado aparece quase so em superficies horizontais, como rua e calcada.
    float groundMask = smoothstep(0.55, 0.95, normal.y);
    float wetGround = clamp(u_Wetness * groundMask, 0.0, 1.0);

    // Mascara irregular de poca: evita o chao ficar brilhante igual plastico.
    float puddleNoise = noise(f_Position.xz * 0.18) * 0.55 + noise(f_Position.xz * 0.55) * 0.45;
    float puddleMask = smoothstep(0.35, 0.82, puddleNoise) * wetGround;

    // Ondinhas suaves da chuva no chao.
    float rippleA = sin(length(fract(f_Position.xz * 0.55) - 0.5) * 30.0 - u_Time * 5.0);
    float rippleB = sin((f_Position.x + f_Position.z) * 2.2 + u_Time * 3.0);
    float rainRipples = (rippleA * 0.5 + rippleB * 0.5) * 0.035 * wetGround;

    // Reflexo depende do angulo da camera: quanto mais de lado, mais espelhado.
    float fresnel = pow(1.0 - max(dot(normal, toCameraDirection), 0.0), 3.0);
    float mirrorAmount = clamp((0.18 + fresnel * 0.85 + puddleMask * 0.55 + rainRipples), 0.0, 1.0) * wetGround;

    // Specular mais forte para as partes molhadas.
    float specularPower = mix(32.0, 220.0, clamp(u_Smoothness + wetGround, 0.0, 1.0));
    float specularIntensity = pow(max(dot(reflectDirection, toCameraDirection), 0.0), specularPower);
    vec3 specular = specularIntensity * mix(u_DirectionalLightColor, vec3(0.35, 0.85, 1.0), wetGround) * (u_Smoothness + wetGround * 1.8);

    // Reflexo neon falso: linhas coloridas alongadas no asfalto, como se as placas refletissem na chuva.
    float cyanLine = pow(max(sin(f_Position.x * 0.58 + f_Position.z * 0.06), 0.0), 14.0);
    float pinkLine = pow(max(sin(f_Position.x * 0.42 - f_Position.z * 0.05 + 1.7), 0.0), 16.0);
    float yellowLine = pow(max(sin(f_Position.z * 0.34 + 2.1), 0.0), 26.0);

    vec3 neonReflection = vec3(0.0);
    neonReflection += vec3(0.00, 0.85, 1.80) * cyanLine;
    neonReflection += vec3(1.80, 0.00, 1.05) * pinkLine;
    neonReflection += vec3(1.50, 1.05, 0.20) * yellowLine;
    neonReflection *= mirrorAmount * 0.62;

    // Escurece um pouco a base do asfalto quando molhado e mistura com o reflexo.
    vec3 wetBase = mix(color.rgb, color.rgb * 0.42, wetGround * 0.55);

    vec3 emission = u_EmissionColor * u_EmissionStrength;
    vec3 lit = wetBase * (u_AmbientLight + diffuse) + specular + neonReflection + emission;

    // Nevoa
    float distance = length(u_CameraPosition - f_Position);
    float fogDensity = 0.075;
    vec3 fogColor = vec3(0.05, 0.08, 0.10);
    float fog = 1.0 - exp(-distance * fogDensity);
    fog = clamp(fog, 0.0, 1.0);

    lit = mix(lit, fogColor, fog);

    out_Color = vec4(lit, color.a);
}
