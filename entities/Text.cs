using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimLib {

    public class Text2D : VisualEntity2D, IColored
    {
        List<(Shape s, char c)> Glyphs = new List<(Shape s, char c)>();

        public Text2D(string text = "") : base(new Text2DState(text)) {
        }

        public Text2D(Text2D state) : base(state) {
        }

        protected void ShapeText() {
            var placedShapes = Animator.Current.ShapeText(Text, Vector2.ZERO, (int)Size, Font);
            foreach(var g in placedShapes) {
                g.s.Transform.parent = Transform;
            }
            this.Glyphs = placedShapes;
        }

        protected void CreateText() {
            foreach(var g in Glyphs) {
                g.s.state.selectable = false;
                g.s.Transform.parent = Transform;
                g.s.Color = Color;
                World.current.CreateInstantly(g.s);
            }
        }

        protected override void OnCreated() {
            ShapeText();
            CreateText();
            base.OnCreated();
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
                World.current.SetProperty(this, "Font", value, ((Text2DState)state).font);
                ((Text2DState)state).font = value;
            }
        }

        public float Size {
            get {
                return ((Text2DState)state).size;
            }
            set {
                World.current.SetProperty(this, "Size", value, ((Text2DState)state).size);
                ((Text2DState)state).size = value;
            }
        }

        public string Text {
            get {
                return ((Text2DState)state).text;
            }
            set {
                World.current.SetProperty(this, "Text", value, ((Text2DState)state).text);
                ((Text2DState)state).text = value;

                if (this.created) {
                    ShapeText();
                    CreateText();
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
        public bool is3d = false;
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
            this.is3d = ts.is3d;
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
