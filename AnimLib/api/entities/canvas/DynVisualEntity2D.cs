using System;

namespace AnimLib;

/// <summary>
/// A visual 2D entity that can be animated.
/// Must be placed in a <see cref="Canvas"/>.
/// </summary>

public abstract class DynVisualEntity2D : DynVisualEntity {
    /// <summary>
    /// The position of this entity relative to its parent.
    /// </summary>
    public DynProperty<Vector2> Position = DynProperty<Vector2>.CreateEmpty(Vector2.ZERO);
    /// <summary>
    /// The rotation of this entity in radians. Relative to its parent.
    /// </summary>
    public DynProperty<float> Rotation = DynProperty<float>.CreateEmpty(0.0f);
    /// <summary>
    /// The scale of this entity relative to its parent.
    /// </summary>
    public DynProperty<Vector2> Scale = DynProperty<Vector2>.CreateEmpty(Vector2.ONE);
    /// <summary>
    /// The anchor point on the parent entity in normalized coordinates.
    /// </summary>
    public DynProperty<Vector2> Anchor = DynProperty<Vector2>.CreateEmpty(Vector2.ZERO);
    /// <summary>
    /// The 'origin' of this entity. Entity will be placed relative to this point.
    /// </summary>
    public DynProperty<Vector2> Pivot = DynProperty<Vector2>.CreateEmpty(Vector2.ZERO);
    /// <summary>
    /// The homography matrix of this entity.
    /// Useful for (fake) perspective transforms.
    /// </summary>
    public DynProperty<M3x3?> Homography = DynProperty<M3x3?>.CreateEmpty(null);

    internal DynVisualEntity2D() {
    }

    internal DynVisualEntity2D(DynVisualEntity2D other) : base(other) {
        Debug.Log("Copy pos " + other.Position.Value);
        this.Position.Value = other.Position.Value;
        this.Rotation.Value = other.Rotation.Value;
        this.Scale.Value = other.Scale.Value;
        this.Anchor.Value = other.Anchor.Value;
        this.Pivot.Value = other.Pivot.Value;
        this.Homography.Value = other.Homography.Value;
    }

    internal override void OnCreated() {
        base.OnCreated();
        Position = new DynProperty<Vector2>("position", Position.Value);
        Rotation = new DynProperty<float>("rotation", Rotation.Value);
        Scale = new DynProperty<Vector2>("scale", Scale.Value);
        Anchor = new DynProperty<Vector2>("anchor", Anchor.Value);
        Pivot = new DynProperty<Vector2>("pivot", Pivot.Value);
        Homography = new DynProperty<M3x3?>("homography", Homography.Value);
    }

    private protected void GetState(EntityState2D dest, Func<DynPropertyId, object?> evaluator) {
        base.GetState(dest, evaluator);
        dest.position = evaluator(Position.Id) as Vector2? ?? default(Vector2);
        dest.rot = evaluator(Rotation.Id) as float? ?? default(float);
        dest.scale = evaluator(Scale.Id) as Vector2? ?? Vector2.ONE;
        dest.anchor = evaluator(Anchor.Id) as Vector2? ?? default(Vector2);
        dest.pivot = evaluator(Pivot.Id) as Vector2? ?? default(Vector2);
        dest.homography = evaluator(Homography.Id) as M3x3? ?? null;
    }
}
