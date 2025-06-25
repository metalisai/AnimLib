using System;

namespace AnimLib;

/// <summary>
/// A visual 2D entity that can be animated.
/// Must be placed in a <see cref="Canvas"/>.
/// </summary>

public abstract partial class DynVisualEntity2D : DynVisualEntity {

    internal DynVisualEntity2D() {
    }

    internal DynVisualEntity2D(DynVisualEntity2D other) : base(other) {
        _positionP.Value = other._positionP.Value;
        _rotationP.Value = other._rotationP.Value;
        _scaleP.Value = other._scaleP.Value;
        _anchorP.Value = other._anchorP.Value;
        _pivotP.Value = other._pivotP.Value;
        _homographyP.Value = other._homographyP.Value;
    }

    internal override void OnCreated() {
        base.OnCreated();
        _positionP = new DynProperty<Vector2>("position", Position);
        _rotationP = new DynProperty<float>("rotation", Rotation);
        _scaleP = new DynProperty<Vector2>("scale", Scale);
        _anchorP = new DynProperty<Vector2>("anchor", Anchor);
        _pivotP = new DynProperty<Vector2>("pivot", Pivot);
        _homographyP = new DynProperty<M3x3?>("homography", Homography);
    }

    private protected void GetState(EntityState2D dest, Func<DynPropertyId, object?> evaluator) {
        base.GetState(dest, evaluator);
        dest.position = evaluator(_positionP.Id) as Vector2? ?? default;
        dest.rotation = evaluator(_rotationP.Id) as float? ?? default;
        dest.scale = evaluator(_scaleP.Id) as Vector2? ?? Vector2.ONE;
        dest.anchor = evaluator(_anchorP.Id) as Vector2? ?? default;
        dest.pivot = evaluator(_pivotP.Id) as Vector2? ?? default;
        dest.homography = evaluator(_homographyP.Id) as M3x3?;
    }
}
