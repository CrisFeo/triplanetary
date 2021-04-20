using System;

using static Mathf;

public struct Orientation {

  public float f0;
  public float f1;
  public float f2;
  public float f3;
  public float b0;
  public float b1;
  public float b2;
  public float b3;
  public float angle;

  public static readonly Orientation POINTY = new Orientation {
    f0 = Sqrt(3f),
    f1 = Sqrt(3f) / 2f,
    f2 = 0f,
    f3 = 3f / 2f,
    b0 = Sqrt(3f) / 3f,
    b1 = -1f / 3f,
    b2 = 0f,
    b3 = 2f / 3f,
    angle = 0.5f,
  };

  public static readonly Orientation FLAT = new Orientation {
    f0 = 3f / 2f,
    f1 = 0f,
    f2 = Sqrt(3f) / 2f,
    f3 = Sqrt(3f),
    b0 = 2f / 3f,
    b1 = 0f,
    b2 = -1f / 3f,
    b3 = Sqrt(3f) / 3f,
    angle = 0f
  };

}
