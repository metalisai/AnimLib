namespace AnimLib;

internal class Gizmo3DObj : SceneObject3D {
    public override object Clone() {
        return new Gizmo3DObj() {
            transform = new SceneTransform3D(transform.Pos, transform.Rot),
            timeslice = timeslice,
        };
    }
}

