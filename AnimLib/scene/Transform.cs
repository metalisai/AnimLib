using System;

namespace AnimLib;

[Serializable]
internal class SceneTransform2D {
    public Vector2 Pos{get; set;}
    public float Rot {get; set;}
    public SceneTransform2D() {
    }
    public SceneTransform2D(Vector2 p, float r) {
        this.Pos = p;
        this.Rot = r;
    }
}

[Serializable]
internal class SceneTransform3D {
    public Vector3 Pos {get; set; }
    public Quaternion Rot {get; set; }
    public SceneTransform3D() {
    }
    public SceneTransform3D(Vector3 p, Quaternion r) {
        this.Pos = p;
        this.Rot = r;
    }
}
