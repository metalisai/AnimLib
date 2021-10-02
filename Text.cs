using System.Collections.Generic;
using System.Text;
using System.Linq;

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
        public Glyph() {
            state = new GlyphState();
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
    }

    public class Text2D : Visual2DEntity, IColored {
        public List<Glyph> Glyphs = new List<Glyph>();
        public TypeSetting Ts;

        protected void CreateGlyphs() {
            foreach(var g in Glyphs) {
                g.Transform.parent = Transform;
                World.current.CreateInstantly(g);
            }
        }

        protected override void OnCreated() {
            CreateGlyphs();
        }

        public string Text {
            get {
                var sb = new StringBuilder();
                foreach(var c in Glyphs) {
                    sb.Append(c.Character);
                }
                return sb.ToString();
            }
            set {
                // TODO: reuse old glyphs
                foreach(var g in Glyphs) {
                    World.current.Destroy(g);
                }
                Glyphs.Clear();
                var pcs = World.current.ts.TypesetString(Vector3.ZERO, value, Size);
                foreach(var pc in pcs) {
                    var g = new Glyph() {
                        Color = Color, 
                        Size = Size,
                        Character = pc.character,
                    };
                    g.Transform.Pos = (Vector3)pc.position;
                    ((EntityState2D)g.state).sizeRect = pc.size;
                    g.state.selectable = false;

                    g.Transform.parent = this.Transform;
                    g.EntityId = World.current.GetUniqueId();

                    Glyphs.Add(g);
                }
                if(this.created) {
                    CreateGlyphs();
                }
            }
        }
        public float Size {
            get {
                return ((Text2DState)state).size;
            }
            set {
                // TODO: retypeset glyphs
                foreach(var g in Glyphs) {
                    g.Size = value;
                }
                World.current.SetProperty(this, "Size", value, ((Text2DState)state).size);
                ((Text2DState)state).size = value;
            }
        }
        public Color Color
        {
            get {
                return ((Text2DState)state).color;
            }
            set {
                foreach(var g in Glyphs) {
                    g.Color = value;
                }
                World.current.SetProperty(this, "Color", value, ((Text2DState)state).color);
                ((Text2DState)state).color = value;
            }
        }
        public TextHorizontalAlignment HAlign
        {
            get {
                return ((Text2DState)state).halign;
            }
            set {
                // TODO: retypeset glyphs
                World.current.SetProperty(this, "HAlign", value, ((Text2DState)state).halign);
                ((Text2DState)state).halign = value;
            }
        }
        public TextVerticalAlignment VAlign
        {
            get {
                return ((Text2DState)state).valign;
            }
            set {
                // TODO: retypeset glyphs
                World.current.SetProperty(this, "VAlign", value, ((Text2DState)state).valign);
                ((Text2DState)state).valign = value;
            }
        }

        public Text2D() {
            var txts = new Text2DState();
            state = txts;
        }

        public Text2D(Text2D t) : base(t) {
            Glyphs = t.Glyphs.Select(x => (Glyph)x.Clone()).ToList();
        }

        public override object Clone() {
            return new Text2D(this);
        }
    }

    public class Text2DState : EntityState2D
    {
        public TextHorizontalAlignment halign = TextHorizontalAlignment.Left;
        public TextVerticalAlignment valign = TextVerticalAlignment.Up;
        public bool is3d = false;
        public float size = 22.0f;
        public Color color = Color.BLACK;

        public Text2DState() {}

        public Text2DState(Text2DState ts) : base(ts) {
            this.halign = ts.halign;
            this.valign = ts.valign;
            this.is3d = ts.is3d;
            this.size = ts.size;
            this.color = ts.color;
        }

        public override object Clone() 
        {
            return new Text2DState(this);
        }
    }
}
