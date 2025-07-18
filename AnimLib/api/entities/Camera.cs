
using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Camera))]
internal abstract class CameraState : EntityState3D
{
    [Dyn]
    public Color clearColor = new Color(255, 255, 255, 255);

    public CameraState() { }

    public CameraState(CameraState cs) : base(cs)
    {
        this.clearColor = cs.clearColor;
    }

    public abstract M4x4 CreateWorldToClipMatrix(float aspect);
    public abstract Ray RayFromClip(Vector2 clipPos, float aspect);
}

public abstract partial class Camera : VisualEntity3D
{
    internal Camera() : base() { }
}
