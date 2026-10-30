shader_type spatial;

// Texturas
uniform sampler2D albedo_texture : hint_albedo;
uniform sampler2D normal_texture : hint_normal;
uniform sampler2D data_texture; // Roughness (R), Displace (G), AO (B), Translucency (A)

// Parámetros ajustables
uniform float roughness_scale : hint_range(0, 1) = 1.0;
uniform float displace_scale : hint_range(0, 1) = 1.0;
uniform float ao_scale : hint_range(0, 1) = 1.0;
uniform float translucency_scale : hint_range(0, 1) = 1.0;

void fragment() {
    // Get values from textures
    vec4 albedo_color = texture(albedo_texture, UV);
    vec3 normal_map = texture(normal_texture, UV).rgb;
    vec4 data = texture(data_texture, UV);

    // Split channels from data texture
    float roughness = data.r * roughness_scale;
    float displace = data.g * displace_scale;
    float ao = data.b * ao_scale;
    float translucency = data.a * translucency_scale;

    // Apply values to PBR
    ALBEDO = albedo_color.rgb;
    NORMAL_MAP = normal_map;
    ROUGHNESS = roughness;
    METALLIC = 0.0; // Set the metalness
    AO = ao;
    TRANSLUCENCY = translucency;

    // Opcional: Aplicar el desplazamiento (displace)
    // Esto depende de cómo quieras usar el desplazamiento en tu shader
    // Por ejemplo, podrías modificar la posición del vértice en el vertex shader.
}