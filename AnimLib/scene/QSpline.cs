using System.Linq;

namespace AnimLib;

internal class PlayerQSpline : SceneObject2D {
    [ShowInEditor]
    public float width { get; set; }
    [ShowInEditor]
    public Color color { get; set; }
    public Vector2[] points { get; set; }
    public override Vector2[] GetHandles2D() {
        return points.Select(x => x + transform.Pos).ToArray();
    }
    public override void SetHandle(int id, Vector2 wpos) {
        if(id >= points.Length || id < 0)
            return;
        points[id] = wpos - transform.Pos;
    }

    public override object Clone() {
        return new PlayerQSpline() {
            timeslice = timeslice,
            color = color,
            points = points,
            width = width,
            transform = new SceneTransform2D(transform.Pos, transform.Rot)
        };
    }

    public override bool Intersects(Vector2 point) {
#warning PlayerQSpline.Intersects is not implemented
        return false;
    }

    public override VisualEntity2D InitializeEntity() {
        return null;
    }
}
