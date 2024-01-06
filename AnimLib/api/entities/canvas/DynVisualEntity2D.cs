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

    internal override void OnCreated() {
        base.OnCreated();
        Position = new DynProperty<Vector2>("position", Vector2.ZERO);
        Rotation = new DynProperty<float>("rotation", 0.0f);
        Scale = new DynProperty<Vector2>("scale", Vector2.ONE);
        Anchor = new DynProperty<Vector2>("anchor", Vector2.ZERO);
        Pivot = new DynProperty<Vector2>("pivot", Vector2.ZERO);
        Homography = new DynProperty<M3x3?>("homography", null);
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
