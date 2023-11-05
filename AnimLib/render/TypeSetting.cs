using System.Collections.Generic;

namespace AnimLib {

    public struct FontGlyph {
        public int w;
        public int h;
        public float bearingX;
        public float bearingY;
        public float hAdvance;
    };

    public struct PlacedGlyph {
        public Vector2 position;
        public Vector2 size;
        public char character;
    }

    public struct GlyphKey {
        public char c;
        public float size;

        public GlyphKey(char c, float size) {
            this.c = c;
            this.size = size;
        }

        public override bool Equals(object obj) {
            if(obj is GlyphKey) {
                var other = (GlyphKey)obj;
                return other.c == this.c && other.size == this.size;
            } else {
                return false;
            }
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 31 + c;
            hash = hash * 31 + (int)(size*10f);
            return hash;
        }
    }

    public interface ITypeSetter {
        void RenderGlyph(char c, float size, byte[,] cache, int offsetX, int offsetY);
        FontGlyph GetGlyph(char c, float size);
        Vector2 GetSize(string s, float size);
        public List<PlacedGlyph> TypesetString(Vector2 pos, string s, float size);
        float GetKerning(char c1, char c2);
    }

}
