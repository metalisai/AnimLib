
using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(OrthoCamera))]
internal class OrthoCameraState : CameraState
{
    [Dyn]
    public float width;
    [Dyn]
    public float height;

    public OrthoCameraState() { }

    public OrthoCameraState(float w, float h)
    {
        this.width = w;
        this.height = h;
    }

    public OrthoCameraState(OrthoCameraState ocs) : base(ocs)
    {
        this.width = ocs.width;
        this.height = ocs.height;
    }

    public override object Clone()
    {
        return new OrthoCameraState(this);
    }

    public override M4x4 CreateWorldToClipMatrix(float aspect)
    {
        float hw = width / 2.0f;
        float hh = height / 2.0f;
        return M4x4.Ortho(-hw, hw, hh, -hh, 1.0f, -1.0f);
    }

    public M4x4 CreateClipToWorldMatrix(float aspect)
    {
        float hw = width / 2.0f;
        float hh = height / 2.0f;
        return M4x4.InvOrtho(-hw, hw, hh, -hh, 1.0f, -1.0f);
    }

    public override Ray RayFromClip(Vector2 clipPos, float aspect)
    {
        M4x4 mat = CreateClipToWorldMatrix(aspect);
        Vector4 dirw = mat * new Vector4(clipPos.x, clipPos.y, 1.0f, 1.0f);
        Vector3 pos = new Vector3(dirw.x / dirw.w, dirw.y / dirw.w, dirw.z / dirw.w);
        var rayOrigin = pos;
        rayOrigin.z = 0.0f;
        return new Ray()
        {
            o = rayOrigin,
            d = (pos - rayOrigin).Normalized,
        };
    }
}

/// <summary>
/// An orthographic camera.
/// </summary>
public partial class OrthoCamera : Camera {
    /// <summary>
    /// Create a new orthographic camera.
    /// </summary>
    public OrthoCamera() : base() {
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new OrthoCameraState();
        GetState(state, evaluator);
        return state;
    }
}
