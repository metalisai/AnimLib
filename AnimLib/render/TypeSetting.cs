using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// The entry point of the application.
/// </summary>
public struct FontGlyph {
    /// <summary> width of the glyph in pixels </summary>
    public int w;
    /// <summary> height of the glyph in pixels </summary>
    public int h;
    /// <summary> x offset of the glyph in pixels </summary>
    public float bearingX;
    /// <summary> y offset of the glyph in pixels </summary>
    public float bearingY;
    /// <summary> how far to move for the next glyph </summary>
    public float hAdvance;
};

/// <summary>
/// A glyph that's gone through the typesetter
/// </summary>
public struct PlacedGlyph {
    /// <summary> position of the glyph</summary>
    public Vector2 position;
    /// <summary> size of the glyph</summary>
    public Vector2 size;
    /// <summary> The character this glyph represents</summary>
    public char character;
}

/// <summary>
/// A key for a glyph in the cache
/// </summary>
public struct GlyphKey {
    /// <summary> The character this glyph represents</summary>
    public char c;
    /// <summary> The size of the glyph. Different sizes need to be cached separately.</summary>
    public float size;

    /// <summary>
    /// Create a new glyph key
    /// </summary>
    public GlyphKey(char c, float size) {
        this.c = c;
        this.size = size;
    }

    /// <summary>
    /// Equality check override for hashing
    /// </summary>
    public override bool Equals(object? obj) {
        if(obj is GlyphKey) {
            var other = (GlyphKey)obj;
            return other.c == this.c && other.size == this.size;
        } else {
            return false;
        }
    }

    /// <summary>
    /// Hashing override
    /// </summary>
    public override int GetHashCode() {
        int hash = 17;
        hash = hash * 31 + c;
        hash = hash * 31 + (int)(size*10f);
        return hash;
    }
}
