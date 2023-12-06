using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// A typesetter is responsible for placing glyphs to form text
/// </summary>
public interface ITypeSetter {
    /// <summary>
    /// Render a glyph to the cache
    /// </summary>
    void RenderGlyph(char c, float size, byte[,] cache, int offsetX, int offsetY);
    /// <summary>
    /// Get a glyph from the cache
    /// </summary>
    FontGlyph GetGlyph(char c, float size);
    /// <summary>
    /// Get the placed size of a string
    /// </summary>
    Vector2 GetSize(string s, float size);
    /// <summary>
    /// Typeset a string
    /// </summary>
    public List<PlacedGlyph> TypesetString(Vector2 pos, string s, float size);
    /// <summary>
    /// Get the kerning between a pair of characters
    /// </summary>
    float GetKerning(char c1, char c2);
}
