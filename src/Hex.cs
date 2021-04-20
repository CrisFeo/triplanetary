using System;
using System.Collections.Generic;

using static Mathf;

public struct Hex {
  public int q;
  public int r;
  public int s;
  public Hex(int q, int r, int s) {
    this.q = q;
    this.r = r;
    this.s = s;
  }
  public Hex(int q, int r) {
    this.q = q;
    this.r = r;
    this.s = -q - r;
  }
  public override string ToString() =>
    $"Hex {q},{r},{s}";
}

public static class HexImpl {

  public static bool Same(this Hex a, Hex b) {
    return (a.q, a.r, a.s) == (b.q, b.r, b.s);
  }

  public static Hex Add(this Hex a, Hex b) {
    return new Hex {
      q = a.q + b.q,
      r = a.r + b.r,
      s = a.s + b.s
    };
  }

  public static Hex Sub(this Hex a, Hex b) {
    return new Hex {
      q = a.q - b.q,
      r = a.r - b.r,
      s = a.s - b.s
    };
  }

  public static Hex Mul(this Hex a, Hex b) {
    return new Hex {
      q = a.q * b.q,
      r = a.r * b.r,
      s = a.s * b.s
    };
  }

  public static Hex Mul(this Hex a, int b) {
    return new Hex {
      q = a.q * b,
      r = a.r * b,
      s = a.s * b
    };
  }

  public static int Len(this Hex a) {
    return (int)((Math.Abs(a.q) + Math.Abs(a.r) + Math.Abs(a.s)) / 2);
  }

  public static int Dist(this Hex a, Hex b) {
    return a.Sub(b).Len();
  }

  static readonly Hex[] DIRECTIONS = new Hex[] {
   new Hex( 1,  0),
   new Hex( 1, -1),
   new Hex( 0, -1),
   new Hex(-1,  0),
   new Hex(-1,  1),
   new Hex( 0,  1)
  };

  public static Hex Dir(int d) {
    return DIRECTIONS[d];
  }

  public static Hex Neighbor(this Hex a, int d) {
    return a.Add(Dir(d));
  }

  public static IEnumerable<Hex> Neighbors(this Hex a) {
    for (var i = 0; i < 6; i++) {
      yield return Neighbor(a, i);
    }
  }

  public static IEnumerable<Hex> Line(this Hex a, Hex b) {
    var n = a.Dist(b);
    var step = 1f / Max(n, 1);
    for (var i = 0; i <= n; i++) {
      var t = step * i;
      var qu = Lerp(a.q + EPSILON, b.q + EPSILON, t);
      var ru = Lerp(a.r - EPSILON, b.r - EPSILON, t);
      var u = new Hex((int)Round(qu), (int)Round(ru));
      var qv = Lerp(a.q - EPSILON, b.q - EPSILON, t);
      var rv = Lerp(a.r + EPSILON, b.r + EPSILON, t);
      var v = new Hex((int)Round(qv), (int)Round(rv));
      yield return u;
      if (!u.Same(v)) yield return v;
    }
  }

  public static IEnumerable<(Hex, Hex?)> LineWithAlternatives(this Hex a, Hex b) {
    var n = a.Dist(b);
    var step = 1f / Max(n, 1);
    for (var i = 0; i <= n; i++) {
      var t = step * i;
      var qu = Lerp(a.q + EPSILON, b.q + EPSILON, t);
      var ru = Lerp(a.r - EPSILON, b.r - EPSILON, t);
      var u = new Hex((int)Round(qu), (int)Round(ru));
      var qv = Lerp(a.q - EPSILON, b.q - EPSILON, t);
      var rv = Lerp(a.r + EPSILON, b.r + EPSILON, t);
      var v = new Hex((int)Round(qv), (int)Round(rv));
      if (u.Same(v)) {
        yield return (u, null);
      } else {
        yield return (u, v);
      }
    }
  }
}
