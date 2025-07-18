using System;
using System.Text.Json.Serialization;

namespace AnimLib;

[Serializable]
internal class PlayerCircle : SceneObject2D {
    [ShowInEditor]
    public float radius { get; set; }
    [ShowInEditor]
    public Color color { get; set; } = Color.RED;
    [ShowInEditor]
    public Color outline { get; set; } = Color.BLACK;
    [ShowInEditor]
    public float outlineWidth { get; set; } = 0.0f;
    [ShowInEditor]
    public ShapeMode mode { get; set; } = ShapeMode.FilledContour;

    protected PlayerCircle(PlayerCircle c) : base(c) {
        this.radius = c.radius;
        this.color = c.color;
    }

    [JsonConstructor]
    public PlayerCircle(float radius, string canvasName) : base() {
        this.radius = radius;
        this.CanvasName = canvasName;
    }

    public override Vector2[] GetHandles2D() {
        var handle = (radius*Vector2.RIGHT).Rotated(transform.Rot);
        return new Vector2[] {handle};
    }

    public override void SetHandle(int id, Vector2 wpos) {
        System.Diagnostics.Debug.Assert(id == 0); // circle only has one handle for radius
        float r = wpos.Length;
        this.radius = r;
    }

    public override object Clone() {
        return new PlayerCircle(this);
    }

    public override bool Intersects(Vector2 point) {
        return (transform.Pos-point).Length < radius;
    }

    public override VisualEntity2D InitializeEntity()
    {
        var ent = new Circle(this.radius);
        ent.Color = this.color;
        ent.ContourColor = this.outline;
        ent.ContourSize = this.outlineWidth;
        ent.Mode = this.mode;
        ent.Position = transform.Pos;
        ent.Rotation = transform.Rot;
        return ent;
    }
}
