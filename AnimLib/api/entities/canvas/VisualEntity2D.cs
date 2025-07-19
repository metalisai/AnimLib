using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(VisualEntity2D), onlyProperties: true)]
internal abstract class EntityState2D : EntityState
{
    [Dyn]
    public int canvasId = -1; // entity Id of canvas
    [Dyn]
    public Vector2 position = Vector2.ZERO;
    [Dyn]
    public float rotation = 0.0f;
    [Dyn]
    public Vector2 anchor = Vector2.ZERO;
    [Dyn]
    public Vector2 pivot = Vector2.ZERO;
    [Dyn]
    public Vector2 scale = Vector2.ONE;
    [Dyn]
    public M3x3? homography = null; // optional homography matrix (relative to canvas)
    // NOTE: pivot and anchor always use CanvasNormalized coordinates
    public Entity2DCoordinateSystem csystem = Entity2DCoordinateSystem.CanvasOrientedWorld;

    public EntityState2D() { }

    public EntityState2D(EntityState2D e2d) : base(e2d)
    {
        this.canvasId = e2d.canvasId;
        this.position = e2d.position;
        this.rotation = e2d.rotation;
        this.anchor = e2d.anchor;
        this.pivot = e2d.pivot;
        this.scale = e2d.scale;
        this.csystem = e2d.csystem;
        this.homography = e2d.homography;
    }

    // normalized coordinates -0.5..0.5
    internal M4x4 NormalizedCanvasToWorld(CanvasState canvas)
    {
        var anchorWorld = canvas.NormalizedCanvasToWorld * new Vector4(anchor.x, anchor.y, 0.0f, 1.0f);
        var c1 = new Vector4(canvas.width * Vector3.Cross(canvas.normal, canvas.up), 0.0f);
        var c2 = new Vector4(canvas.height * canvas.up, 0.0f);
        var c3 = new Vector4(-canvas.normal, 0.0f);
        var mat = M4x4.FromColumns(c1, c2, c3, anchorWorld);
        return mat;
    }

    // oriented world coordinates (x - left, y - up, z - forward)
    internal M4x4 CanvasToWorld(CanvasState canvas)
    {
        var anchorWorld = canvas.NormalizedCanvasToWorld * new Vector4(anchor.x, anchor.y, 0.0f, 1.0f);
        var c1 = new Vector4(Vector3.Cross(canvas.normal, canvas.up), 0.0f);
        var c2 = new Vector4(canvas.up, 0.0f);
        var c3 = new Vector4(-canvas.normal, 0.0f);
        return M4x4.FromColumns(c1, c2, c3, anchorWorld);
    }

    // TODO: this doesn't belong here
    public M4x4 ModelToWorld(EntityStateResolver resolver)
    {
        if (parentId == 0)
        {
            return M4x4.TRS(position, Quaternion.IDENTITY, scale);
        }
        else
        {
            var parent = (EntityState2D?)resolver.GetEntityState(parentId);
            if (parent == null)
            {
                Debug.Error($"Entity {this} did not find parent {parentId}");
                return M4x4.IDENTITY;
            }
            return parent.ModelToWorld(resolver) * M4x4.TRS(position, Quaternion.IDENTITY, scale);
        }
    }

    // axis aligned bounding box required for normalized coordinates
    public abstract Vector2 AABB { get; }
}

/// <summary>
/// A visual 2D entity that can be animated.
/// Must be placed in a <see cref="Canvas"/>.
/// </summary>
public abstract partial class VisualEntity2D : VisualEntity
{

    internal VisualEntity2D()
    {
        if (World.current?.ActiveCanvas.Created ?? false)
        {
            // this is here to make the compiler happy
            _canvasIdP.Value = Canvas.Default?.Id ?? throw new Exception("Can't find default canvas");
            Canvas = World.current.ActiveCanvas;
        }
        else
        {
            Debug.Error("World.ActiveCanvas is set to a canvas entity that isn't created. Using default canvas.");
            // this is here to make the compiler happy
            _canvasIdP.Value = Canvas.Default?.Id ?? throw new Exception("Can't find default canvas");
            Canvas = Canvas.Default;
        }
    }

    VisualEntity2D? _parent;
    public VisualEntity2D? Parent
    {
        get => _parent;
        set
        {
            _parent = value;
            if (Created.Value)
            {
                ParentId.Value = value?.Id ?? -1;
            }
        }
    }

    Canvas? _canvas;
    public Canvas? Canvas
    {
        get => _canvas;
        set
        {
            _canvas = value;
            _canvasIdP.Value = value?.Id ?? Canvas.Default?.Id ?? -1;
        }
    }

    internal VisualEntity2D(VisualEntity2D other) : base(other)
    {
        _canvas = other.Canvas;
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
        _canvasIdP = new DynProperty<int>("canvasId", CanvasId, _canvasIdP);
        _positionP = new DynProperty<Vector2>("position", Position, _positionP);
        _rotationP = new DynProperty<float>("rotation", Rotation, _rotationP);
        _scaleP = new DynProperty<Vector2>("scale", Scale, _scaleP);
        _anchorP = new DynProperty<Vector2>("anchor", Anchor, _anchorP);
        _pivotP = new DynProperty<Vector2>("pivot", Pivot, _pivotP);
        _homographyP = new DynProperty<M3x3?>("homography", Homography, _homographyP);

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

        if (Canvas != null && Canvas != Canvas.Default)
        {
            _canvasIdP.Value = Canvas.Id;
        }
    }

    private protected void GetState(EntityState2D dest, Func<DynPropertyId, object?> evaluator)
    {
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

/// <summary>
/// I have no idea what this is. ^^
/// </summary>
public enum Entity2DCoordinateSystem {
    /// <summary>
    /// Don't know 1
    /// </summary>
    CanvasOrientedWorld,
    /// <summary>
    /// Don't know 2
    /// </summary>
    CanvasNormalized,
};