using System;
using System.Collections.Generic;
using System.IO;
using Rendering;
using Rendering.App;

using static Mathf;

public static class Program {

  // Constants
  ///////////////////////////

  const string PIXEL_PATH = "pixel.png";
  const string TILE_PATH = "tiles/thin.png";
  const string TILE_FILL_PATH = "tiles/fill.png";
  const string SHIP_PATH = "ship.png";
  const string PLANET_PATH = "planet.png";
  const string GRAVITY_RIGHT_PATH = "gravity_right.png";
  const string GRAVITY_DOWN_PATH = "gravity_down.png";
  const string TORPEDO_PATH = "torpedo.png";
  const string VERTEX_SHADER_PATH = "main.vert";
  const string FRAGMENT_SHADER_PATH = "main.frag";
  const string PROJECTION_UNIFORM_NAME = "projection";

  static readonly string DATA_PATH = Path.Combine(
    Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
    "dat"
  );

  // Internal vars
  ///////////////////////////

  static GL.Program program;
  static GL.Uniform projection;
  static SpriteBatcher batcher;
  static GL.Texture pixelTexture;
  static GL.Texture tileTexture;
  static GL.Texture tileFillTexture;
  static GL.Texture shipTexture;
  static GL.Texture torpedoTexture;
  static GL.Texture planetTexture;
  static GL.Texture gravityRightTexture;
  static GL.Texture gravityDownTexture;

  // Records
  ///////////////////////////

  enum TileType {
    Empty = 0,
    Planet,
    GravityRight = 10,
    GravityRightDown,
    GravityLeftDown,
    GravityLeft,
    GravityLeftUp,
    GravityRightUp,
  }

  record Mob {
    public Lst<Hex> history  { get; init; }
    public Hex      velocity { get; init; }
    public bool     derelict { get; init; }
  }

  record Tile {
    public TileType tileType { get; init; }
    public Lst<int> contents { get; init; }
  }

  record State {
    public Point            mouse           { get; init; }
    public bool             isDragging      { get; init; }
    public Point            dragMouseStart  { get; init; }
    public Point            dragOriginStart { get; init; }
    public HexLayout        hexLayout       { get; init; }
    public HexMap<Tile>     map             { get; init; }
    public int              nextId          { get; init; }
    public Map<int, Mob>    mobs            { get; init; }
    public int              playerId        { get; init; }
    public Lst<int>         torpedos        { get; init; }
  }

  record Event {
    public record KeyUpdate(Key key, bool isDown) : Event;
    public record MouseUpdate(float x, float y)   : Event;
  }

  // Internal methods
  ///////////////////////////

  public static void Main() {
    try {
      WindowApp.Run(
        Init,
        Subs,
        Step,
        View,
        600,
        600,
        "Triplanetary"
      );
    } catch (Exception e) {
      Console.WriteLine(e);
    }
  }

  static State Init(Window w) {
    // Set up GL globals
    {
      program = GL.CreateProgram(
        Path.Combine(DATA_PATH, VERTEX_SHADER_PATH),
        Path.Combine(DATA_PATH, FRAGMENT_SHADER_PATH)
      );
      projection = GL.CreateUniform(program, PROJECTION_UNIFORM_NAME);
      pixelTexture = GL.CreateTexture(Path.Combine(DATA_PATH, PIXEL_PATH));
      tileTexture = GL.CreateTexture(Path.Combine(DATA_PATH, TILE_PATH));
      tileFillTexture = GL.CreateTexture(Path.Combine(DATA_PATH, TILE_FILL_PATH));
      shipTexture = GL.CreateTexture(Path.Combine(DATA_PATH, SHIP_PATH));
      torpedoTexture = GL.CreateTexture(Path.Combine(DATA_PATH, TORPEDO_PATH));
      planetTexture = GL.CreateTexture(Path.Combine(DATA_PATH, PLANET_PATH));
      gravityRightTexture = GL.CreateTexture(Path.Combine(DATA_PATH, GRAVITY_RIGHT_PATH));
      gravityDownTexture = GL.CreateTexture(Path.Combine(DATA_PATH, GRAVITY_DOWN_PATH));
      batcher = SpriteBatcher.New();
    }
    // Set up hex grid
    var (width, height) = w.Size;
    var hexLayout = new HexLayout {
      orientation = Orientation.POINTY,
      size = new Point(
        tileTexture.width / Sqrt(3),
        tileTexture.height / 2f
      ),
      origin = new Point(width / 2, height / 2),
    };
    // Set up the map
    var map = HexMap<Tile>.Empty;
    var radius = 50;
    for (int q = -radius; q <= radius; q++) {
      var r1 = (int)Max(-radius, -q - radius);
      var r2 = (int)Min(radius, -q + radius);
      for (var r = r1; r <= r2; r++) {
        map = map.Set(new Hex(q, r), new Tile {
          tileType = TileType.Empty,
          contents = Lst<int>.Empty,
        });
      }
    }
    var planetHex = new Hex(5, 5);
    map = map.Set(planetHex, new Tile {
      tileType = TileType.Planet,
      contents = Lst<int>.Empty,
    });
    var typeStart = (int)TileType.GravityRight;
    var typeOffset = (int)TileType.GravityLeft - typeStart;
    foreach (var neighborHex in planetHex.Neighbors()) {
      map = map.Set(neighborHex, new Tile {
        tileType = (TileType)(typeStart+typeOffset),
        contents = Lst<int>.Empty,
      });
      typeOffset++;
      typeOffset %= 6;
    }
    // Set up player
    var nextId = 1;
    var mobs = Map<int, Mob>.Empty;
    var playerId = nextId++;
    var playerHex = new Hex(0, 0);
    mobs = mobs.Set(playerId, new Mob {
      history = Lst<Hex>.Empty.Add(playerHex),
      velocity = new Hex(0, 0),
    });
    map.Set(playerHex, map[playerHex] with {
      contents = Lst<int>.Empty.Add(playerId),
    });
    return new State {
      mouse = new Point(0, 0),
      isDragging = false,
      dragMouseStart = new Point(0, 0),
      dragOriginStart = new Point(0, 0),
      hexLayout = hexLayout,
      map = map,
      nextId = nextId,
      mobs = mobs,
      playerId = playerId,
      torpedos = Lst<int>.Empty,
    };
  }

  static Sub<Event> Subs(Window w) {
    return Sub.Many(
      WindowApp.Key<Event>(w, (k, s) => new Event.KeyUpdate(k, s)),
      WindowApp.Mouse<Event>(w, (x, y) => new Event.MouseUpdate(x, y))
    );
  }

  static (State, Cmd<Event>) Step(State state, Event evt) {
    switch(evt) {
      case Event.KeyUpdate e: {
        switch (e.key) {
          case Key.Q: {
            if (!e.isDown) break;
            return (state, Cmd.Quit<Event>());
          }
          case Key.MouseLeft: {
            if (!e.isDown) break;
            var player = state.mobs[state.playerId];
            var current = player.history.Last;
            var next = current.Add(player.velocity);
            var mouse = state.hexLayout.PixelToHex(state.mouse);
            var maxThrust = 1;
            if (player.derelict) maxThrust = 0;
            if (mouse.Dist(next) <= maxThrust) {
              state = state with {
                mobs = state.mobs.Set(
                  state.playerId,
                  player = player with {
                    velocity = mouse.Sub(current),
                  }
                ),
              };
              return (SimulateMobs(state), null);
            }
            return (state, null);
          }
          case Key.MouseRight: {
            return (state with {
              isDragging = e.isDown,
              dragMouseStart = state.mouse,
              dragOriginStart = state.hexLayout.origin,
            }, null);
          }
          case Key.MouseMiddle: {
            if (!e.isDown) break;
            var player = state.mobs[state.playerId];
            if (player.derelict) break;
            var current = player.history.Last;
            var next = current.Add(player.velocity);
            var mouse = state.hexLayout.PixelToHex(state.mouse);
            if (mouse.Dist(next) > 1) return (state, null);
            var newId = state.nextId;
            var nextId = newId + 1;
            state = state with {
              nextId = nextId,
              mobs = state.mobs.Set(newId, new Mob {
                history = Lst<Hex>.Empty.Add(current),
                velocity = mouse.Sub(current),
                derelict = true,
              }),
              map = state.map.Set(current, state.map[current] with {
                contents = state.map[current].contents.Add(newId),
              }),
              torpedos = state.torpedos.Add(newId),
            };
            return (SimulateMobs(state), null);
          }
        }
        return (state, null);
      }
      case Event.MouseUpdate e: {
        var mouse = new Point(e.x, e.y);
        var hexLayout = state.hexLayout;
        if (state.isDragging) {
          var dragVector = state.mouse.Sub(state.dragMouseStart);
          hexLayout.origin = state.dragOriginStart.Add(dragVector);
        }
        return (state with {
          mouse = mouse,
          hexLayout = hexLayout,
        }, null);
      }
    }
    return (state, null);
  }

  static State SimulateMobs(State state) {
    var mobs = state.mobs.MapValues((k, v) => SimulateMob(state.map, state.mobs, k));
    var map = state.map;
    foreach (var (id, mob) in mobs) {
      var prev = state.mobs[id].history.Last;
      var newMob = SimulateMob(state.map, state.mobs, id);
      mobs = mobs.Set(id, newMob);
      var next = newMob.history.Last;
      map = map.Set(prev, map[prev] with {
        contents = map[prev].contents.Remove(id),
      });
      map = map.Set(next, map[next] with {
        contents = map[next].contents.Add(id),
      });
    }
    return state with {
      map = map,
      mobs = mobs,
    };
  }

  static Mob SimulateMob(HexMap<Tile> map, Map<int, Mob> mobs, int id) {
    var mob = mobs[id];
    var start = mob.history.Last;
    var end = start.Add(mob.velocity);
    var derelict = mob.derelict;
    var gravity = new Hex(0, 0);
    foreach (var (hex, alt) in start.LineWithAlternatives(end)) {
      var chosenTile = map[hex];
      if (chosenTile.tileType == TileType.Planet) {
        // We can "skirt" around a planetary collision if we are straddling
        // the line between an empty and a planetary hex
        if (!alt.HasValue || map[alt.Value].tileType == TileType.Planet) {
          end = hex;
          derelict = true;
          gravity = mob.velocity.Mul(-1);
          break;
        }
        chosenTile = map[alt.Value];
      }
      // Check if we collided with anything, ignoring ourselves and anything
      // that was created this tick on our initial tile (cause its some sort
      // of ordnance)
      foreach (var otherId in chosenTile.contents) {
        if (otherId == id) continue;
        var other = mobs[otherId];
        if (hex.Same(start) && other.history.Count == 1) continue;
        derelict = true;
        break;
      }
      // Apply gravity for all hexes after the first (unless we aren't moving)
      if (!hex.Same(start) || start.Same(end)) {
        gravity = ApplyGravity(map[hex], gravity);
        if (alt.HasValue) {
          gravity = ApplyGravity(map[alt.Value], gravity);
        }
      }
    }
    return mob with {
      history = mob.history.Add(end),
      velocity = mob.velocity.Add(gravity),
      derelict = derelict,
    };
  }

  static Hex ApplyGravity(Tile tile, Hex gravity) {
    switch (tile.tileType) {
      case TileType.GravityRight:     return gravity.Neighbor(0);
      case TileType.GravityRightDown: return gravity.Neighbor(1);
      case TileType.GravityLeftDown:  return gravity.Neighbor(2);
      case TileType.GravityLeft:      return gravity.Neighbor(3);
      case TileType.GravityLeftUp:    return gravity.Neighbor(4);
      case TileType.GravityRightUp:   return gravity.Neighbor(5);
    }
    return gravity;
  }

  static void View(Window w, State state) {
    var (screenWidth, screenHeight) = w.Size;
    GL.Use(program);
    GL.Set(projection, GL.Ortho(0, screenWidth, 0, screenHeight, -1, 1));
    batcher.Begin();
    var mouseHex = state.hexLayout.PixelToHex(state.mouse);
    var screenHex = state.hexLayout.PixelToHex(new Point(0, 0));
    var player = state.mobs[state.playerId];
    var playerHex = player.history.Last;
    // Draw map
    {
      var hexHeight = (int)(screenHeight / (state.hexLayout.size.y - 2));
      var hexWidth = (int)(screenWidth / (state.hexLayout.size.x - 2));
      for (var r = 0; r < hexHeight; r++) {
        var rOffset = (int)(r / 2);
        for (int q = -rOffset; q < hexWidth - rOffset; q++) {
          var hex = screenHex.Add(new Hex(q, r)).Sub(new Hex(1, 1));
          if (!state.map.Has(hex)) continue;
          var c = Colors.Green;
          if (hex.Dist(playerHex.Add(player.velocity)) <= 1) {
            c = Colors.Yellow;
          }
          var pos = state.hexLayout.HexToPixel(hex);
          DrawSprite(tileTexture, pos, c);
          switch (state.map[hex].tileType) {
            case TileType.Planet:           DrawSprite(planetTexture, pos, Colors.White);      break;
            case TileType.GravityRight:     DrawSprite(gravityRightTexture, pos, Colors.White,  1,  1); break;
            case TileType.GravityRightDown: DrawSprite(gravityDownTexture,  pos, Colors.White,  1,  1); break;
            case TileType.GravityLeftDown:  DrawSprite(gravityDownTexture,  pos, Colors.White, -1,  1); break;
            case TileType.GravityLeft:      DrawSprite(gravityRightTexture, pos, Colors.White, -1,  1); break;
            case TileType.GravityLeftUp:    DrawSprite(gravityDownTexture,  pos, Colors.White, -1, -1); break;
            case TileType.GravityRightUp:   DrawSprite(gravityDownTexture,  pos, Colors.White,  1, -1); break;
          }
        }
      }
    }
    // Draw travel line
    {
      foreach (var (hex, alt) in playerHex.LineWithAlternatives(mouseHex)) {
        var col = Colors.White;
        if (alt.HasValue) {
          col = Colors.Yellow;
          DrawSprite(tileFillTexture, state.hexLayout.HexToPixel(alt.Value), col);
        }
        DrawSprite(tileFillTexture, state.hexLayout.HexToPixel(hex), col);
      }
    }
    // Draw torpedos
    {
      for (var  i = 0; i < state.torpedos.Count; i++) {
        var torpedo = state.mobs[state.torpedos[i]];
        for (var j = 1; j < torpedo.history.Count; j++) {
          var p = state.hexLayout.HexToPixel(torpedo.history[j - 1]);
          var n = state.hexLayout.HexToPixel(torpedo.history[j]);
          DrawLine(p, n, 2, Colors.Blue);
        }
        var currentHex = torpedo.history.Last;
        var currentPos = state.hexLayout.HexToPixel(currentHex);
        var nextPos = state.hexLayout.HexToPixel(currentHex.Add(torpedo.velocity));
        DrawLine(currentPos, nextPos, 2, Colors.Cyan);
        DrawSprite(torpedoTexture, currentPos, Colors.Blue);
      }
    }
    // Draw player
    {
      for (var i = 1; i < player.history.Count; i++) {
        var p = state.hexLayout.HexToPixel(player.history[i - 1]);
        var n = state.hexLayout.HexToPixel(player.history[i]);
        DrawLine(p, n, 3, Colors.Red);
      }
      var currentPos = state.hexLayout.HexToPixel(playerHex);
      var nextPos = state.hexLayout.HexToPixel(playerHex.Add(player.velocity));
      DrawLine(currentPos, nextPos, 2, Colors.Yellow);
      var col = Colors.Red;
      if (player.derelict) col = Colors.Gray;
      DrawSprite(shipTexture, currentPos, col);
    }
    batcher.End(false);
    batcher.Render();
  }

  static void DrawSprite(
    GL.Texture texture,
    Point position,
    Color color,
    float uvx = 1,
    float uvy = 1
  ) {
    var w = texture.width;
    var h = texture.height;
    var x = (int)(position.x - w / 2);
    var y = (int)(position.y - h / 2);
    var bg = Colors.Black;
    batcher.Draw(new Sprite {
      texture = texture,
      topLeft =     GL.V(x,     y + h, uvx * 0, uvy * 1, color.color, bg.color),
      topRight =    GL.V(x + w, y + h, uvx * 1, uvy * 1, color.color, bg.color),
      bottomRight = GL.V(x + w, y,     uvx * 1, uvy * 0, color.color, bg.color),
      bottomLeft =  GL.V(x,     y,     uvx * 0, uvy * 0, color.color, bg.color),
    });
  }

  static void DrawLine(Point start, Point end, float thickness, Color color) {
    var forward = end.Sub(start);
    var right = forward.Rotate(90).Norm().Mul(thickness / 2);
    var tl = end.Sub(right);
    var tr = end.Add(right);
    var br = start.Add(right);
    var bl = start.Sub(right);
    var bg = Colors.Black;
    batcher.Draw(new Sprite {
      texture = pixelTexture,
      topLeft =     GL.V(tl.x, tl.y, 0, 1, color.color, bg.color),
      topRight =    GL.V(tr.x, tr.y, 1, 1, color.color, bg.color),
      bottomRight = GL.V(br.x, br.y, 1, 0, color.color, bg.color),
      bottomLeft =  GL.V(bl.x, bl.y, 0, 0, color.color, bg.color),
    });
  }

}
