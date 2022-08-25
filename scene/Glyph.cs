using System;

namespace AnimLib {
    public class Glyph : Visual2DEntity, IColored {
        public char Character {
            get {
                return ((GlyphState)state).glyph;
            }
            set {
                World.current.SetProperty(this, "Character", value, ((GlyphState)state).glyph);
                ((GlyphState)state).glyph = value;
            }
        }
        public Color Color
        {
            get {
                return ((GlyphState)state).color;
            }
            set {
                World.current.SetProperty(this, "Color", value, ((GlyphState)state).color);
                ((GlyphState)state).color = value;
            }
        }
        public float Size
        {
            get {
                return ((GlyphState)state).size;
            }
            set {
                World.current.SetProperty(this, "Size", value, ((GlyphState)state).size);
                ((GlyphState)state).size = value;
            }
        }
        public Glyph() : base(new GlyphState()) {
        }
        public Glyph(Glyph g) : base(g) {
        }
        public override object Clone() {
            return new Glyph(this);
        }
    }

    public class GlyphState : EntityState2D
    {
        public char glyph;
        public float size;
        public Color color;

        public GlyphState() {}

        public GlyphState(GlyphState g) : base(g) {
            this.glyph = g.glyph;
            this.size = g.size;
            this.color = g.color;
        }

        public override object Clone()
        {
            return new GlyphState(this);
        }

        public override Vector2 AABB {
            get {
                throw new NotImplementedException();
            }
        }
    }
}
