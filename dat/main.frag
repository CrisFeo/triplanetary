#version 330 core

out vec4 FragColor;

in vec3 fg;
in vec3 bg;
in vec2 uv;

uniform sampler2D tex;

void main() {
  vec4 v = texture(tex, uv);
  //FragColor = v;
  FragColor = vec4(clamp(v.a * fg, 0, 1), v.a);
  //FragColor = vec4(mix(bg, fg, v.r), 1);
}
