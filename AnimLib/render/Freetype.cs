using SharpFont;

using System.Collections.Generic;

namespace AnimLib {
    public class FreetypeSetting : ITypeSetter {

        Library library;
        Face font;
        //Face symbola;

        Dictionary<GlyphKey, FontGlyph> _cachedGlyphs = new Dictionary<GlyphKey, FontGlyph>();

        public FreetypeSetting() {
            library = new Library();
            font = new Face(library, TextPlacement.DefaultFontPath);
#if !Linux
            font.SelectCharmap(Encoding.Unicode);
            font.SetPixelSizes(0, 96);
#endif
            //symbola = new Face(library, "/usr/share/fonts/truetype/ancient-scripts/Symbola_hint.ttf");
        }

        private void CacheGlyph(char c, float size) {
            uint gi = font.GetCharIndex(c);
            font.SetCharSize(new Fixed26Dot6(0), size, 0, 96);
            font.LoadGlyph(gi, LoadFlags.Default, LoadTarget.Normal);
            PushGlyph(c, size, font.Glyph);
        }

        public void RenderGlyph(char c, float size, byte[,] cache, int offsetX, int offsetY) {
            uint gi = font.GetCharIndex(c);
            font.SetCharSize(new Fixed26Dot6(0), size, 0, 96);
            font.LoadGlyph(gi, LoadFlags.Default, LoadTarget.Normal);
            var glyph = font.Glyph;
            glyph.RenderGlyph(RenderMode.Normal);
            bool notexture = glyph.Bitmap.Width == 0 && glyph.Bitmap.Rows == 0;
            if(!notexture) {
                var data = font.Glyph.Bitmap.BufferData;
                for(int i = 0; i < glyph.Bitmap.Rows; i++) {
                    for(int j = 0; j < glyph.Bitmap.Width; j++) {
                        cache[offsetY+i, offsetX+j] = data[i*glyph.Bitmap.Width + j];
                    }
                }
            }
        }

        public FontGlyph GetGlyph(char c, float size) {
            var key = new GlyphKey {
                c = c,
                size = size    
            };
            FontGlyph g;
            if(!_cachedGlyphs.TryGetValue(key, out g)) {
                CacheGlyph((char)c, size);
                g = _cachedGlyphs[key];
            }
            return g;
        }

        public Vector2 GetSize(string s, float size) {
            float x = 0.0f;
            for(int i = 0; i < s.Length; i++) {
                var c = s[i];
                FontGlyph g = GetGlyph(c, size);
                float kernX = 0.0f;
                if(i > 0) {
                    var kx = font.GetKerning(s[i-1], c, KerningMode.Default);
                    kernX = (float)kx.X;
                }
                float baseY = 0.0f + size;

                float yd = g.h - g.bearingY;

                float lowX = x + g.bearingX + kernX;
                float highX = lowX+g.w;
                float lowY = baseY+yd;
                float highY = baseY-g.h+yd;

                // TODO: use lookup table!
                x = x + g.hAdvance + kernX;
            }
            return new Vector2(x, 2.0f*size);
        }

        public void PushGlyph(char c, float size, GlyphSlot glyph) {
            var key = new GlyphKey() {
                c = c,
                size = size
            };
            var cg = new FontGlyph() {
                w = glyph.Bitmap.Width,
                h = glyph.Bitmap.Rows,
                bearingX = glyph.Metrics.HorizontalBearingX.ToSingle(),
                bearingY = glyph.Metrics.HorizontalBearingY.ToSingle(),
                hAdvance = glyph.Metrics.HorizontalAdvance.ToSingle(),
            };
            _cachedGlyphs.Add(key, cg);
        }

        public float GetKerning(char c1, char c2) {
            var kx = font.GetKerning(c1, c2, KerningMode.Default);
            return (float)kx.X;
        }

        public List<PlacedGlyph> TypesetString(Vector2 pos, string s, float size) {
            var ret = new List<PlacedGlyph>();

            float x = pos.x;
            float y = pos.y;

            for(int i = 0; i < s.Length; i++) {

                var c = s[i];
                var fg = GetGlyph(c, size);
                float kernX = 0.0f;
                if(i > 0) {
                    kernX = GetKerning(s[i-1], c);
                }
                float baseY = y + size;
                float yd = fg.h - fg.bearingY;
                float lowX = x + fg.bearingX + kernX;
                float lowY = baseY-fg.h+yd;

                var pc = new PlacedGlyph {
                    position = new Vector2(lowX, lowY),
                    size  = new Vector2(fg.w, fg.h),
                    character = s[i],
                };

                x = x + fg.hAdvance + kernX;

                ret.Add(pc);
            }
            return ret;
        }
    }

}
