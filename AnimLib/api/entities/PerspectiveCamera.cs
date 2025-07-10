using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(PerspectiveCamera))]
internal class PerspectiveCameraState : CameraState
{
    [Dyn]
    public float fov = 60.0f;
    [Dyn]
    public float zNear = 0.1f;
    [Dyn]
    public float zFar = 1000.0f;

    public PerspectiveCameraState() : base() { }

    public PerspectiveCameraState(PerspectiveCameraState pcs) : base(pcs)
    {
        this.fov = pcs.fov;
        this.zNear = pcs.zNear;
        this.zFar = pcs.zFar;
    }

    public M4x4 CreateWorldToViewMatrix()
    {
        var invRot = rotation;
        invRot.w *= -1.0f;
        var invPos = new Vector3(-position.x, -position.y, -position.z);
        M4x4 worldToView = M4x4.RT(invRot, invPos);
        return worldToView;
    }

    public M4x4 CreateViewToClipMatrix(float aspect)
    {
        var invRot = rotation;
        invRot.w *= -1.0f;
        var invPos = new Vector3(-position.x, -position.y, -position.z);
        M4x4 viewToClip = M4x4.Perspective(fov, aspect, this.zNear, this.zFar);
        return viewToClip;
    }

    public override M4x4 CreateWorldToClipMatrix(float aspect)
    {
        var invRot = rotation;
        invRot.w *= -1.0f;
        var invPos = new Vector3(-position.x, -position.y, -position.z);
        M4x4 worldToView = M4x4.RT(invRot, invPos);
        M4x4 worldToClip = M4x4.Perspective(fov, aspect, this.zNear, this.zFar) * worldToView;
        return worldToClip;
    }

    public Vector3 WorldToClipPos(float aspect, Vector3 pos)
    {
        var mat = CreateWorldToClipMatrix(aspect);
        Vector4 v4 = new Vector4(pos.x, pos.y, pos.z, 1.0f);
        var ret = mat * v4;
        return new Vector3(ret.x / ret.w, ret.y / ret.w, ret.z / ret.w);
    }

    public Vector2 WorldToNormScreenPos(Vector3 pos, float aspect)
    {
        var clip = WorldToClipPos(aspect, pos);
        return new Vector2((clip.x + 1.0f) * 0.5f, 1.0f - (clip.y + 1.0f) * 0.5f);
    }

    public Vector2 WorldToScreenPos(Vector3 pos, Vector2 screenSize)
    {
        var clip = WorldToClipPos(screenSize.x / screenSize.y, pos);
        return new Vector2(clip.x * 0.5f * screenSize.x, clip.y * 0.5f * screenSize.y);
    }

    public M4x4 CreateClipToWorldMatrix(float aspect)
    {
        M4x4 clipToView = M4x4.InvPerspective(fov, aspect, this.zNear, this.zFar);
        M4x4 viewToWorld = M4x4.TRS(position, rotation, Vector3.ONE);
        return viewToWorld * clipToView;
    }

    public override Ray RayFromClip(Vector2 clipPos, float aspect)
    {
        M4x4 mat = CreateClipToWorldMatrix(aspect);
        Vector4 dirw = mat * new Vector4(clipPos.x, clipPos.y, 1.0f, 1.0f);
        Vector3 pos = new Vector3(dirw.x / dirw.w, dirw.y / dirw.w, dirw.z / dirw.w);
        // TODO: technically the origin should be on the near plane not camera origin
        return new Ray()
        {
            o = position,
            d = (pos - position).Normalized,
        };
    }
}

/// <summary>
/// A 3D perspective camera.
/// </summary>
public partial class PerspectiveCamera : Camera {
    /// <summary>
    /// Creates a new PerspectiveCamera.
    /// </summary>
    public PerspectiveCamera() : base() {
    }

    private PerspectiveCameraState GetCurrentState()
    {
        return new PerspectiveCameraState
        {
            fov = this.Fov,
            zNear = this.ZNear,
            zFar = this.ZFar,
            position = this.Position,
            rotation = this.Rotation,
            scale = this.Scale,
            clearColor = this.ClearColor,
        };
    }

    /// <summary>
    /// Converts a world-space position to a normalized screen-space position.
    /// </summary>
    public Vector2 WorldToNormScreenPos(Vector3 pos, float aspect)
    {
        return GetCurrentState().WorldToNormScreenPos(pos, aspect);
    }

    /// <summary>
    /// Converts a world-space position to a screen-space position.
    /// </summary>
    /// <param name="pos">The world-space position.</param>
    /// <param name="screenSize">The size of the screen in pixels.</param>
    /// <returns>Screen-space coordinates relative to screen center.</returns>
    public Vector2 WorldToScreenPos(Vector3 pos, Vector2 screenSize) {
        return GetCurrentState().WorldToScreenPos(pos, screenSize);
    }

    /// <summary>
    /// Converts a world-space position to a normalized screen-space position.
    /// </summary>
    public Vector3 WorldToClipPos(float aspect, Vector3 pos) {
        return GetCurrentState().WorldToClipPos(aspect, pos);
    }   

    /// <summary>
    /// Creates a matrix that converts from clip-space to world-space. Note that a perspective division has to be performed after multiplying with that matrix.
    /// </summary>
    /// <param name="aspect">The aspect ratio of the screen.</param>
    public M4x4 CreateClipToWorldMatrix(float aspect) {
        return GetCurrentState().CreateClipToWorldMatrix(aspect);
    }

    /// <summary>
    /// Create a ray from a clip-space position.
    /// </summary>
    public Ray RayFromClip(Vector2 clipPos, float aspect) {
        return GetCurrentState().RayFromClip(clipPos, aspect);
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new PerspectiveCameraState();
        GetState(state, evaluator);
        return state;
    }
}
