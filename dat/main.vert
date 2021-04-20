#version 330 core

layout (location = 0) in vec2 inpos;
layout (location = 1) in vec2 inuv;
layout (location = 2) in vec3 infg;
layout (location = 3) in vec3 inbg;

out vec2 uv;
out vec3 fg;
out vec3 bg;

uniform mat4 projection;

void main() {
  gl_Position = projection * vec4(inpos, 0.0, 1.0);
  uv = inuv;
  fg = infg;
  bg = inbg;
}
