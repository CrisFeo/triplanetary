using System;

using static Mathf;

public struct HexLayout {
  public Orientation orientation;
  public Point size;
  public Point origin;
}

static class HexLayoutImpl {

  // Public methods
  ///////////////////////////

  public static Point HexToPixel(this HexLayout layout, Hex h) {
    var o = layout.orientation;
    var x = (o.f0 * h.q + o.f1 * h.r) * layout.size.x;
    var y = (o.f2 * h.q + o.f3 * h.r) * layout.size.y;
    return new Point(x + layout.origin.x, y + layout.origin.y);
  }

  public static Hex PixelToHex(this HexLayout layout, Point p) {
    var o = layout.orientation;
    var pt = new Point(
      (p.x - layout.origin.x) / layout.size.x,
      (p.y - layout.origin.y) / layout.size.y
    );
    var q = o.b0 * pt.x + o.b1 * pt.y;
    var r = o.b2 * pt.x + o.b3 * pt.y;
    return RoundHex(q, r);
  }

  // Internal methods
  ///////////////////////////

  static Hex RoundHex(float q, float r) {
    var s = -q - r;
    var qi = (int)Round(q);
    var ri = (int)Round(r);
    var si = (int)Round(s);
    var qd = Abs(qi - q);
    var rd = Abs(ri - r);
    var sd = Abs(si - s);
    if (qd > rd && qd > sd) {
      qi = -ri - si;
    } else if (rd > sd) {
      ri = -qi - si;
    } else {
      si = -qi - ri;
    }
    return new Hex(qi, ri, si);
  }

}
