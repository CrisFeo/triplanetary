using System;

using static Mathf;

public struct Point {
  public float x;
  public float y;
  public Point(float x, float y) {
    this.x = x;
    this.y = y;
  }
  public override string ToString() =>
    $"Point {x},{y}";
}

public static class PointImpl {

  public static bool Same(this Point a, Point b) {
    return (a.x, a.y) == (b.x, b.y);
  }

  public static Point Add(this Point a, Point b) {
    return new Point {
      x = a.x + b.x,
      y = a.y + b.y,
    };
  }

  public static Point Sub(this Point a, Point b) {
    return new Point {
      x = a.x - b.x,
      y = a.y - b.y,
    };
  }

  public static Point Mul(this Point a, Point b) {
    return new Point {
      x = a.x * b.x,
      y = a.y * b.y,
    };
  }

  public static Point Mul(this Point a, float b) {
    return new Point {
      x = a.x * b,
      y = a.y * b,
    };
  }

  public static Point Div(this Point a, float b) {
    return new Point {
      x = a.x / b,
      y = a.y / b,
    };
  }

  public static Point Norm(this Point a) {
    return a.Div(a.Len());
  }

  public static float Len(this Point a) {
    return Sqrt(a.x * a.x + a.y * a.y);
  }

  public static float Dist(this Point a, Point b) {
    return a.Sub(b).Len();
  }

  public static Point Rotate(this Point a, float degrees) {
    var r = DegToRad(degrees);
    var sin = Sin(r);
    var cos = Cos(r);
    var tx = a.x;
    var ty = a.y;
    return new Point(
      cos * tx - sin * ty,
      sin * tx + cos * ty
    );
  }

}
