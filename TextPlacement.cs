using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using SkiaSharp;

namespace AnimLib;

internal class TextPlacement : System.IDisposable {

    struct LoadedFont {
        public string name;
        public Font font;
        public SKTypeface typeface;
        public SKFont skFont;
    }

    struct TPGlyphKey {
        public int codepoint;
        public int size;
        public string font;

        public TPGlyphKey(char c, int size, string font) {
            this.codepoint = c;
            this.size = size;
            this.font = font;
        }

        public override bool Equals(object obj) {
            if(obj is GlyphKey) {
                var other = (TPGlyphKey)obj;
                return other.codepoint == this.codepoint && other.size == this.size && other.font == this.font;
            } else return false;
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 31 + codepoint;
            hash = hash * 31 + (int)(size*10);
            hash = hash ^ font.GetHashCode();
            return hash;
        }
    }

    string activeFont;

    Dictionary<string, LoadedFont> LoadedFonts = new Dictionary<string, LoadedFont>();
    Dictionary<TPGlyphKey, SKPath> CachedGlyphPaths = new Dictionary<TPGlyphKey, SKPath>();

    SKMatrix mirrorMat = new SKMatrix(1.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f);

    List<(PlacedGlyph, SKPath)> PlaceText(Buffer buf, Vector2 origin, int size, ref LoadedFont lf) {
        var ret = new List<(PlacedGlyph, SKPath)>();
        var len = buf.Length;
        if(len <= 0) {
            return ret;
        }
        var info = buf.GetGlyphInfoSpan();
        var pos = buf.GetGlyphPositionSpan();

        lf.skFont.Size = size;

        float x = origin.x;
        float y = origin.y;
        for (int i = 0; i < len; i++) {
            var g = new PlacedGlyph();
            g.character = (char)info[i].Codepoint;
            g.position.x = x + pos[i].XOffset;
            g.position.y = y + pos[i].YOffset;
            x += pos[i].XAdvance;
            y += pos[i].YAdvance;
            SKPath path;
            var key = new TPGlyphKey(g.character, size, lf.name);
            if(!CachedGlyphPaths.TryGetValue(key, out path)) {
                path = lf.skFont.GetGlyphPath((ushort)info[i].Codepoint);
                path.Transform(mirrorMat);
                CachedGlyphPaths.Add(key, path);
                Performance.CachedGlyphPaths = CachedGlyphPaths.Count;
            }
            ret.Add((g, path));
        }
        return ret;
    }

    List<Shape> PlaceTextAsShapes(Buffer buf, Vector2 origin, int size, ref LoadedFont lf) {
        var ret = new List<Shape>();
        var len = buf.Length;
        if(len <= 0) {
            return ret;
        }
        var info = buf.GetGlyphInfoSpan();
        var pos = buf.GetGlyphPositionSpan();

        lf.skFont.Size = size;

        float x = origin.x;
        float y = origin.y;
        for (int i = 0; i < len; i++) {
            float posx = x + pos[i].XOffset;
            float posy = y + pos[i].YOffset;
            x += pos[i].XAdvance;
            y += pos[i].YAdvance;
            SKPath path;
            var key = new TPGlyphKey((char)info[i].Codepoint, size, lf.name);
            if(!CachedGlyphPaths.TryGetValue(key, out path)) {
                path = lf.skFont.GetGlyphPath((ushort)info[i].Codepoint);
                path.Transform(mirrorMat);
                CachedGlyphPaths.Add(key, path);
                Performance.CachedGlyphPaths = CachedGlyphPaths.Count;
            }
            var sp = path.ToShapePath();
            var shape = new Shape(sp);
            shape.Transform.Pos = new Vector2(posx, posy);
            shape.Mode = ShapeMode.Filled;
            ret.Add(shape);
        }
        return ret;
    }

    // https://github.com/mono/SkiaSharp/blob/2ad29861d5a40d3bf78c28ab0a9cb02a8f0fe437/source/SkiaSharp.HarfBuzz/SkiaSharp.HarfBuzz.Shared/BlobExtensions.cs
    public static Blob ToHarfBuzzBlob(SKStreamAsset asset)
    {
        Blob blob;
        if (asset == null)
        {
            throw new System.ArgumentNullException(nameof(asset));
        }

        var size = asset.Length;

        var memoryBase = asset.GetMemoryBase();
        if (memoryBase != System.IntPtr.Zero)
        {
            blob = new Blob(memoryBase, size, MemoryMode.ReadOnly, () => asset.Dispose());
        }
        else
        {
            var ptr = Marshal.AllocCoTaskMem(size);
            asset.Read(ptr, size);
            blob = new Blob(ptr, size, MemoryMode.ReadOnly, () => Marshal.FreeCoTaskMem(ptr));
        }

        blob.MakeImmutable();

        return blob;
    }

    public void Dispose()
    {
        foreach(var lf in LoadedFonts.Values) {
            lf.font.Dispose();
            lf.typeface.Dispose();
            lf.skFont.Dispose();
        }
        foreach(var val in CachedGlyphPaths.Values) {
            val.Dispose();
            CachedGlyphPaths.Clear();
        }
    }

    int textSize = 64;

    public void LoadFont(string fontfile, string fontname) {
        LoadedFont lf;
        fontname = fontname.ToLower();
        if(LoadedFonts.TryGetValue(fontname, out lf)) {
            Debug.Warning($"Font {fontname} already loaded, ignoring");
            return;
        }
        using (var fs = new FileStream(fontfile, FileMode.Open, FileAccess.Read)) {
            var typeface = SKTypeface.FromStream(fs);
            Debug.TLog($"Font {fontfile} units per em {typeface.UnitsPerEm}");
            int index;
            using(var blob = ToHarfBuzzBlob(typeface.OpenStream(out index))) 
            using(var face = new Face(blob, index))
            {
                face.Index = index;
                face.UnitsPerEm = typeface.UnitsPerEm;
                var font = new Font(face);
                font.SetScale((int)textSize, (int)textSize); // dpi?
                var skFont = new SKFont(typeface, (float)textSize);

                lf.font = font;
                lf.typeface = typeface;
                lf.skFont = skFont;
                lf.name = fontname;
                LoadedFonts.Add(fontname, lf);
            }
        }
    }

    public TextPlacement(string defaultFontFile, string fontName) {
        LoadFont(defaultFontFile, fontName);
        activeFont = fontName;
    }

    public List<(PlacedGlyph,SKPath)> PlaceText(string text, Vector2 origin, int size, string font = null) {
        // write line
        var buf = new Buffer();
        buf.AddUtf8(text);
        buf.GuessSegmentProperties();
        size = size * 96 / 72; // 96 DPI ? idk
        string fontname = font ?? activeFont;
        fontname = fontname.ToLower();
        LoadedFont lf;
        if(LoadedFonts.TryGetValue(fontname, out lf)) {
            lf.font.SetScale(size, size);
            lf.font.Shape(buf);
            var textb = PlaceText(buf, origin, size, ref lf);
            buf.Dispose();
            return textb;
        } else { 
            Debug.Error($"No font named {fontname} loaded");
            buf.Dispose();
            return new List<(PlacedGlyph, SKPath)>();
        }
    }

    public List<Shape> PlaceTextAsShapes(string text, Vector2 origin, int size, string font = null) {
        var buf = new Buffer();
        buf.AddUtf8(text);
        buf.GuessSegmentProperties();
        size = size * 96 / 72; // 96 DPI ? idk
        string fontname = font ?? activeFont;
        fontname = fontname.ToLower();
        LoadedFont lf;
        if(LoadedFonts.TryGetValue(fontname, out lf)) {
            lf.font.SetScale(size, size);
            lf.font.Shape(buf);
            var textb = PlaceTextAsShapes(buf, origin, size, ref lf);
            buf.Dispose();
            return textb;
        } else {
            Debug.Error($"No font named {fontname} loaded");
            buf.Dispose();
            return new List<Shape>();
        }
    }
}
