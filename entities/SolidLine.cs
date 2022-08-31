using System;
using System.Linq;

namespace AnimLib {
    public class SolidLineState : MeshBackedGeometry {
        public Vector3[] points;
        public Color color;
        public float width;
        // this is used to animate lines (from 0 to 1, how much of the line is visible)
        public float progression = 1.0f;

        public override object Clone()
        {
            return new SolidLineState(this);
        }

        public SolidLineState(string owner) : base(owner) {

        }

        public SolidLineState(RendererHandle h, string owner) : base(h, owner) {

        }

        public SolidLineState(SolidLineState sls) : base(sls) {
            this.color = sls.color;
            this.points = sls.points.ToArray();
            this.width = sls.width;
        }

        public override void UpdateMesh(ColoredTriangleMeshGeometry mesh)
        {
            if(points.Length == 2) {
                var ps = points.ToArray();
                ps[1] = points[0] + progression*(points[1]-points[0]);
                LineRenderer.UpdateLineMesh(mesh, ps, width, color, ownerGuid);
            } else {
                LineRenderer.UpdateLineMesh(mesh, points, width, color, ownerGuid);
            }
        }
    }

    public class SolidLine : VisualEntity3D, Labelable {
        public Vector3[] Points { 
            get {
                return ((SolidLineState)state).points;
            }
            set {
                World.current.SetProperty(this, "Points", value, ((SolidLineState)state).points);
                ((SolidLineState)state).points = value;
            }
        }
        public Color Color { 
            get {
                return ((SolidLineState)state).color;
            } set {
                World.current.SetProperty(this, "Color", value, ((SolidLineState)state).color);
                ((SolidLineState)state).color = value;
            }
        }
        public float Width {
            get {
                return ((SolidLineState)state).width;
            } set {
                World.current.SetProperty(this, "Width", value, ((SolidLineState)state).width);
                ((SolidLineState)state).width = value;
            }
        }

        public float Progression {
            get {
                return ((SolidLineState)state).progression;
            } set {
                World.current.SetProperty(this, "Progression", value, ((SolidLineState)state).progression);
                ((SolidLineState)state).progression = value;
            }
        }

        public SolidLine(string owner) : base(new SolidLineState(owner)) {
        }

        public SolidLine() : this(World.current.Resources.GetGuid()) {
        }

        public SolidLine(SolidLine sl) : base(sl) {
        }

        public Vector2? GetLabelOffset(CameraState cam, Rect label, LabelStyle style, EntityState state, Vector2 screenSize)
        {
            var mstate = (SolidLineState)state;
            if(cam is PerspectiveCameraState) {
                var pcam  = cam as PerspectiveCameraState;
                var startS = pcam.WorldToScreenPos(mstate.points[0], screenSize);
                var endS = pcam.WorldToScreenPos(mstate.points[1], screenSize);
                var dirS = (endS-startS).Normalized;
                var dotH = Vector2.Dot(dirS, Vector2.RIGHT);
                var dotV = Vector2.Dot(dirS, Vector2.UP);
                var alphaH = MathF.Acos(dotH);
                var linewhh = 0.5f*mstate.width/(MathF.Abs(MathF.Sin(0.5f*MathF.PI-alphaH)));
                var alphaV = MathF.Acos(dotV);
                var linewhv = 0.5f*mstate.width/(MathF.Abs(MathF.Sin(0.5f*MathF.PI-alphaV)));
                var centerS = 0.5f*(startS+endS);
            
                float x, y;
                if(MathF.Abs(dotH) >= MathF.Abs(dotV)) { // place up
                    var centerH = 0.5f*(mstate.points[0]+mstate.points[1]) + new Vector3(0.0f, linewhh, 0.0f);
                    var centerSH = pcam.WorldToScreenPos(centerH, screenSize);
                    var dif = centerSH-centerS;
                    x = 0.0f;
                    y = MathF.Abs(0.5f*label.width*MathF.Tan(alphaH))+0.5f*label.height;
                    return new Vector2(-x, -y);
                } else { // place left
                    var centerV = 0.5f*(mstate.points[0]+mstate.points[1]) + new Vector3(-linewhv, 0.0f, 0.0f);
                    var centerSV = pcam.WorldToScreenPos(centerV, screenSize);
                    var dif = centerSV-centerS;
                    x = MathF.Abs(0.5f*label.height*MathF.Tan(alphaV))+0.5f*label.width;
                    y = 0.0f;
                    return new Vector2(-x+dif.x, -y+dif.y);
                }
            } else {
                throw new NotImplementedException();
            }
        }

        public Vector3? GetLabelWorldCoordinate(LabelStyle style, EntityState state)
        {
            var ps = ((SolidLineState)state).points;
            return 0.5f*(ps[0]+ps[1]);
        }

        public override object Clone() {
            return new SolidLine(this);
        }
    }

}
