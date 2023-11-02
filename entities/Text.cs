using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimLib {

    public class Text2D : EntityCollection2D, IColored
    {
        List<(Shape s, char c)> Glyphs = new List<(Shape s, char c)>();

        public Text2D(string text = "", 
            float size = 22.0f, 
            string font = null, 
            Color? color = null
        ) : base(new Text2DState(text)) {
            var state = (Text2DState)this.state;
            state.size = size;
            state.font = font;
            state.color = color ?? Color.BLACK;
            ShapeText();
        }

        public Text2D(Text2D state) : base(state) {
            ShapeText();
        }

        protected void ShapeText() {
            foreach(var g in Glyphs) {
                DestroyChild(g.s);
            }
            var placedShapes = Animator.Current.ShapeText(Text, Vector2.ZERO, (int)Size, Font);
            foreach(var g in placedShapes) {
                Attach(g.s);
            }
            this.Glyphs = placedShapes;
        }

        protected void CreateText() {
            foreach(var g in Glyphs) {
                g.s.state.selectable = false;
                g.s.Color = Color;
            }
        }

        protected override void OnCreated() {
            CreateText();
            base.OnCreated();
        }

        public (Shape s, char c)[] CurrentShapes {
            get {
                return Glyphs.ToArray();
            }
        }

        public Shape[] GetSubstring(string str) {
            var mystr = new string(this.Glyphs.Select(x => x.c).ToArray());
            var idx = mystr.IndexOf(str);
            if(idx >= 0) {
                var range = Glyphs.GetRange(idx, str.Length).Select(x => x.s).ToArray();
                return range;
            }
            return null;
        }

        public string Font {
            get {
                return ((Text2DState)state).font;
            }
            set {
                var oldFont = ((Text2DState)state).font;
                World.current.SetProperty(this, "Font", value, ((Text2DState)state).font);
                ((Text2DState)state).font = value;
                if (oldFont != value) {
                    ShapeText();
                }
            }
        }

        public float Size {
            get {
                return ((Text2DState)state).size;
            }
            set {
                var oldSize = ((Text2DState)state).size;
                World.current.SetProperty(this, "Size", value, ((Text2DState)state).size);
                ((Text2DState)state).size = value;
                if (oldSize != value) {
                    ShapeText();
                }
            }
        }

        public string Text {
            get {
                return ((Text2DState)state).text;
            }
            set {
                var oldText = ((Text2DState)state).text;
                World.current.SetProperty(this, "Text", value, ((Text2DState)state).text);
                ((Text2DState)state).text = value;
                if (oldText != value) {
                    ShapeText();
                    if (this.created) {
                        CreateText();
                    }
                }
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

        public Color Color { 
            get
            {
                return ((Text2DState)state).color;
            }
            set
            {
                World.current.SetProperty(this, "Color", value, ((Text2DState)state).color);
                ((Text2DState)state).color = value;
                foreach(var g in Glyphs) {
                    g.s.Color = value;
                }
            }
        }

        public override object Clone() {
            return new Text2D(this);
        }
    }

    public class Text2DState : EntityState2D
    {
        public TextHorizontalAlignment halign = TextHorizontalAlignment.Left;
        public TextVerticalAlignment valign = TextVerticalAlignment.Up;
        public float size = 22.0f;
        public Color color = Color.BLACK;
        public string text;
        public string font = null;

        public Text2DState(string text = "") {
            this.text = text;
        }

        public Text2DState(Text2DState ts) : base(ts) {
            this.halign = ts.halign;
            this.valign = ts.valign;
            this.size = ts.size;
            this.color = ts.color;
            this.text = ts.text;
        }

        public override object Clone() 
        {
            return new Text2DState(this);
        }

        public override Vector2 AABB {
            get {
                throw new NotImplementedException();
            }
        }
    }
}
