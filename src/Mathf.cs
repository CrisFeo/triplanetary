using System;

static class Mathf {

  public const float PI = (float)Math.PI;

  public const float EPSILON = 1E-6f;

  public static float Sqrt(float a) {
    return (float)Math.Sqrt(a);
  }

  public static float Abs(float a) {
    return (float)Math.Abs(a);
  }

  public static float Round(float a) {
    return (float)Math.Round(a, MidpointRounding.AwayFromZero);
  }

  public static float Lerp(float a, float b, float t) {
    return a * (1 - t) + b * t;
  }

  public static float Min(float a, float b) {
    return a < b ? a : b;
  }

  public static float Max(float a, float b) {
    return a > b ? a : b;
  }

  public static float DegToRad(float degrees) {
    return degrees * PI / 180f;
  }

  public static float Sin(float r) {
    return (float)Math.Sin(r);
  }

  public static float Cos(float r) {
    return (float)Math.Cos(r);
  }

  public static float Tan(float r) {
    return (float)Math.Tan(r);
  }

}
