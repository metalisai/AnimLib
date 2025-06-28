using System;

namespace AnimLib;

/// <summary>
/// A visual 2D entity that can be animated.
/// Must be placed in a <see cref="Canvas"/>.
/// </summary>

public abstract partial class DynVisualEntity2D : DynVisualEntity {

    internal DynVisualEntity2D()
    {
        if (World.current.ActiveCanvas.created)
        {
            // this is here to make the compiler happy
            _canvasIdP.Value = Canvas.Default?.EntityId ?? throw new Exception("Can't find default canvas");
            //Canvas = World.current.ActiveCanvas;
        }
        else
        {
            Debug.Error("World.ActiveCanvas is set to a canvas entity that isn't created. Using default canvas.");
            // this is here to make the compiler happy
            _canvasIdP.Value = Canvas.Default?.EntityId ?? throw new Exception("Can't find default canvas");
            //Canvas = Canvas.Default;
        }
    }

    public DynVisualEntity2D? Parent { get; set; }

    internal DynVisualEntity2D(DynVisualEntity2D other) : base(other)
    {
        _canvasIdP.Value = other._canvasIdP.Value;
        _positionP.Value = other._positionP.Value;
        _rotationP.Value = other._rotationP.Value;
        _scaleP.Value = other._scaleP.Value;
        _anchorP.Value = other._anchorP.Value;
        _pivotP.Value = other._pivotP.Value;
        _homographyP.Value = other._homographyP.Value;
    }

    internal override void OnCreated()
    {
        base.OnCreated();
        _canvasIdP = new DynProperty<int>("canvasId", CanvasId);
        _positionP = new DynProperty<Vector2>("position", Position);
        _rotationP = new DynProperty<float>("rotation", Rotation);
        _scaleP = new DynProperty<Vector2>("scale", Scale);
        _anchorP = new DynProperty<Vector2>("anchor", Anchor);
        _pivotP = new DynProperty<Vector2>("pivot", Pivot);
        _homographyP = new DynProperty<M3x3?>("homography", Homography);

        // entities dont have id before creation, so the id must be resolved after creation
        if (Parent != null)
        {
            if (Parent.Created)
            {
                ParentId.Value = Parent.Id;
            }
            else
            {
                Debug.Error("2D entity parented to an entity that hasn't been created yet.");
                Parent = null;
            }
        }
    }

    private protected void GetState(EntityState2D dest, Func<DynPropertyId, object?> evaluator) {
        base.GetState(dest, evaluator);
        dest.canvasId = evaluator(_canvasIdP.Id) as int? ?? default;
        dest.position = evaluator(_positionP.Id) as Vector2? ?? default;
        dest.rotation = evaluator(_rotationP.Id) as float? ?? default;
        dest.scale = evaluator(_scaleP.Id) as Vector2? ?? Vector2.ONE;
        dest.anchor = evaluator(_anchorP.Id) as Vector2? ?? default;
        dest.pivot = evaluator(_pivotP.Id) as Vector2? ?? default;
        dest.homography = evaluator(_homographyP.Id) as M3x3?;
    }
}
