namespace AnimLib;

internal class CircleState : ShapeState {
    public float radius;

    public CircleState(ShapePath path) : base(path) {
    }

    public CircleState(CircleState cs) : base(cs) {
        this.radius = cs.radius;
    }

    public override object Clone() {
        return new CircleState(this);
    }
}

/// <summary>
/// A circle shaped entity.
/// </summary>
public class Circle : Shape, IColored {

    private static ShapePath CreateCirclePath(float radius) {
        var pb = new PathBuilder();
        pb.Circle(Vector2.ZERO, radius);
        return pb;
    }

    public Circle(float radius) : base(new CircleState(CreateCirclePath(radius))) {
    }

    public Circle(Circle c) : base(c) {
    }

    public float Radius { 
        get {
            return ((CircleState)state).radius;
        }
        set {
            World.current.SetProperty(this, "Radius", value, ((CircleState)state).radius);
            ((CircleState)state).radius = value;
        }
    }

    public Circle Pos(Vector3 p) {
        this.Transform.Pos = p;
        return this;
    }

    public Circle R(float r){
        this.Radius = r;
        return this;
    }

    public Circle C(Color color){ 
        this.Color = color;
        return this;
    }
    public override object Clone() {
        return new Circle(this);
    }
}
