using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimLib;

internal class Text2DState : EntityState2D
{
    public TextHorizontalAlignment halign = TextHorizontalAlignment.Left;
    public TextVerticalAlignment valign = TextVerticalAlignment.Up;
    public float size = 22.0f;
    public Color color = Color.BLACK;
    public string text;
    public string? font = null;

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

    public override Vector2 AABB {
        get {
            throw new NotImplementedException();
        }
    }
}

/// <summary>
/// A 2D text entity, consisting of a collection of vector shapes
/// </summary>
public class Text2D : EntityCollection2D, IColored
{
    List<(DynShape s, char c)> Glyphs = new ();
    internal float size;
    internal string? font;
    internal Color color;
    internal string text;

    /// <summary>
    /// Creates a new text entity with the given text, size, font and color.
    /// </summary>
    public Text2D(string text = "",
        float size = 22.0f,
        string? font = null,
        Color? color = null
    ) : base()
    {
        this.text = text;
        this.size = size;
        this.font = font;
        this.color = color ?? Color.BLACK;
        ShapeText();
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Text2D(Text2D state) : base(state) {
        this.text = state.text;
        this.size = state.size;
        this.font = state.font;
        this.color = state.color;
        ShapeText();
    }

    /// <summary>
    /// Create the shapes that make up the text.
    /// </summary>
    protected void ShapeText() {
        foreach(var g in Glyphs) {
            DestroyChild(g.s);
        }
        var placedShapes = Animator.Current?.ShapeText(Text, Vector2.ZERO, (int)Size, Font) ?? new List<(DynShape s, char c)>();
        foreach(var g in placedShapes) {
            g.s.Color = Color;
            g.s.SortKey.Value = base.SortKey.Value;
            Attach(g.s);
        }
        this.Glyphs = placedShapes;
    }

    /// <summary>
    /// Create the underlying shapes.
    /// </summary>
    protected void CreateText() {
        /*foreach(var g in Glyphs) {
            g.s.state.selectable = false;
        }*/
    }

    /// <summary>
    /// When this entity is created, create the underlying shapes.
    /// </summary>
    internal override void OnCreated() {
        CreateText();
        base.OnCreated();
    }

    /// <summary>
    /// Property to get the underlying shapes.
    /// </summary>
    public (DynShape s, char c)[] CurrentShapes {
        get {
            return Glyphs.ToArray();
        }
    }

    /// <summary>
    /// Property to get the underlying shapes given a string.
    /// </summary>
    public DynShape[] GetSubstring(string str) {
        var mystr = new string(this.Glyphs.Select(x => x.c).ToArray());
        var idx = mystr.IndexOf(str);
        if(idx >= 0) {
            var range = Glyphs.GetRange(idx, str.Length).Select(x => x.s).ToArray();
            return range;
        }
        return Array.Empty<DynShape>();
    }

    /// <summary>
    /// Name of the font used to generate the text.
    /// </summary>
    public string? Font {
        get {
            return font;
        }
        set {
            var oldFont = font;
            font = value;
            if (oldFont != value) {
                ShapeText();
            }
        }
    }

    /// <summary>
    /// Font size in world units. (pixels for the default canvas)
    /// </summary>
    public float Size {
        get {
            return size;
        }
        set {
            var oldSize = size;
            size = value;
            if (oldSize != value) {
                ShapeText();
            }
        }
    }

    /// <summary>
    /// The text to display.
    /// </summary>
    public string Text {
        get {
            return text;
        }
        set {
            var oldText = text;
            text = value;
            if (oldText != value) {
                ShapeText();
                if (this.Created) {
                    CreateText();
                }
            }
        }
    }

    /// <summary>
    /// Horizontal alignment of the text.
    /// </summary>
    public TextHorizontalAlignment HAlign
    {
        /*get {
            return ((Text2DState)state).halign;
        }*/
        set {
            // TODO: retypeset glyphs
            /*World.current.SetProperty(this, "HAlign", value, ((Text2DState)state).halign);
            ((Text2DState)state).halign = value;*/
        }
    }
    
    /// <summary>
    /// Vertical alignment of the text.
    /// </summary>
    public TextVerticalAlignment VAlign
    {
        /*get {
            return ((Text2DState)state).valign;
        }*/
        set {
            // TODO: retypeset glyphs
            /*World.current.SetProperty(this, "VAlign", value, ((Text2DState)state).valign);
            ((Text2DState)state).valign = value;*/
        }
    }

    /// <summary>
    /// The color of the text. Controls the color of the underlying shapes.
    /// </summary>
    public Color Color { 
        get
        {
            return color;
        }
        set
        {
            color = value;
            foreach(var g in Glyphs) {
                g.s.Color = value;
            }
        }
    }

    /// <summary>
    /// The key used to sort this entity on the canvas.
    /// </summary>
    new public int SortKey {
        /*get {
            return ((Text2DState)state).sortKey;
        }*/
        set {
            base.SortKey.Value = value;
            foreach(var g in Glyphs) {
                g.s.SortKey.Value = value;
            }
        }
    }

    /// <summary>
    /// Clone this text entity.
    /// </summary>
    internal override object Clone() {
        return new Text2D(this);
    }

    private protected void GetState(Text2DState state, Func<DynPropertyId, object?> evaluator)
    {
        base.GetState(state, evaluator);
        state.text = this.text;
        state.color = this.color;
        state.font = this.font;
        state.size = this.size;
        //state.halign = this.halign;
        //state.valign = this.VAlign;
    }


    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new Text2DState();
        this.GetState(state, evaluator);
        return state;
    }
}
