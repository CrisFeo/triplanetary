using System;
using System.Collections.Generic;
using Rendering.App;

public record HexMap<V> {

  // Constants
  ////////////////////

  public static readonly HexMap<V> Empty = new HexMap<V>(Map<(int, int), V>.Empty);

  // Instance vars
  ///////////////////////////

  Map<(int, int), V> data { get; init; }

  // Constructors
  ///////////////////////////

  HexMap(Map<(int, int), V> initial) => data = initial;

  // Public properties
  ///////////////////////////

  public V this[Hex h] { get => data[Key(h)]; }

  // Public methods
  ///////////////////////////

  public HexMap<V> Set(Hex h, V v) => new HexMap<V>(data.Set(Key(h), v));

  public bool Has(Hex h) => data.Has(Key(h));

  // Internal methods
  ///////////////////////////

  (int, int) Key(Hex h) => (h.q, h.r);

}
