namespace AnimLib;

internal class LabelState : EntityState3D
{
    public string text;
    public float size;
    public LabelStyle style = LabelStyle.Free;
    //public Labelable target;
    public Color color;

    public LabelState() {}

    public LabelState(LabelState ls) : base(ls) {
        this.text = ls.text;
        this.size = ls.size;
        this.style = ls.style;
        //this.target = ls.target;
        this.color = ls.color;
    }

    public override object Clone()
    {
        return new LabelState(this);
    }
}

public class Label : VisualEntity3D {
    public string Text {
        get {
            return ((LabelState)state).text;
        } set {
            World.current.SetProperty(this, "Text", value, ((LabelState)state).text);
            ((LabelState)state).text = value;
        }
    }
    public float Size {
        get {
            return ((LabelState)state).size;
        } set {
            World.current.SetProperty(this, "Size", value, ((LabelState)state).size);
            ((LabelState)state).size = value;
        }
    }

    public Color Color {
        get {
            return ((LabelState)state).color;
        } set {
            World.current.SetProperty(this, "Color", value, ((LabelState)state).color);
            ((LabelState)state).color = value;
        }
    }

    public Label() : base(new LabelState()) {
    }

    public Label(Label l) : base(l) {
    }

    public override object Clone() {
        return new Label(this);
    }
}
