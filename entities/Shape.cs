using System;
using System.Collections.Generic;

namespace AnimLib {
    public enum ShapeMode {
        None,
        Contour,
        FilledContour,
        Filled,
    }

    public enum PathVerb {
        Noop, // do nothing
        Move, // move without stroke
        Line, // straight line
        Cubic, // cubic curve
        Conic, // circular or conic arc
        Quad, // quadratic curve
        Close, // close contour
    }

    public struct VerbData {
        public Vector2[] points;
        public float conicWeight;
    }

    public class ShapePath {
        public (PathVerb,VerbData)[] path;

        public ShapePath Clone() {
            var cp = new (PathVerb,VerbData)[path.Length];
            path.CopyTo(cp.AsSpan());
            return new ShapePath() {
                path = cp,
            };
        }
    }

    public class PathBuilder {
        private Vector2 lastPos = Vector2.ZERO;

        public List<(PathVerb,VerbData)> path = new List<(PathVerb,VerbData)>();

        public void MoveTo(Vector2 pos) {
            var vd = new VerbData() {
                points = new Vector2[1] { pos },
            };
            path.Add((PathVerb.Move, vd));
            lastPos = pos;
        }

        public void LineTo(Vector2 pos) {
            var vd = new VerbData() {
                points = new Vector2[2] { lastPos, pos},
            };
            path.Add((PathVerb.Line, vd));
            lastPos = pos;
        }

        public void QuadTo(Vector2 p1, Vector2 p2) {
            var vd = new VerbData() {
                points = new Vector2[3] { lastPos, p1, p2 },
            };
            path.Add((PathVerb.Quad, vd));
            lastPos = p2;
        }

        public void CubicTo(Vector2 p1, Vector2 p2, Vector3 p3) {
            var vd = new VerbData() {
                points = new Vector2[4] { lastPos, p1, p2, p3 },
            };
            path.Add((PathVerb.Cubic, vd));
            lastPos = p3;
        }

        public void Close() {
            var vd = new VerbData() {
                points = new Vector2[] { lastPos },
            };
            path.Add((PathVerb.Close, vd));
        }

        public void Clear() {
            lastPos = Vector3.ZERO;
            path.Clear();
        }

        public ShapePath GetPath() {
            return this;
        }

        public static implicit operator ShapePath(PathBuilder pb) => new ShapePath() { path = pb.path.ToArray()};
    }

    public class ShapeState : EntityState2D {
        public ShapePath path;
        public Color color = Color.RED;
        public Color contourColor = Color.BLACK;
        public float contourSize = 0.0f;
        public ShapeMode mode = ShapeMode.FilledContour;

        public ShapeState(ShapePath path) {
            this.path = path;
        }

        public ShapeState(ShapeState ss) : base(ss) {
            this.path = ss.path.Clone();
            this.color = ss.color;
            this.contourColor = ss.contourColor;
            this.contourSize = ss.contourSize;
            this.mode = ss.mode;
        }

        public override Vector2 AABB {
            get {
                throw new NotImplementedException();
            }
        }

        public override object Clone() {
            return new ShapeState(this);
        }
    }

    public class Shape : Visual2DEntity, IColored {
        public Shape(ShapePath path) : base(new ShapeState(path)) {
        }

        public Shape(Shape s) : base(s) {
        }

        public ShapePath Path {
            get {
                return ((ShapeState)state).path;
            }
            set {
                World.current.SetProperty(this, "Path", value, ((ShapeState)state).path);
                ((ShapeState)state).path = value;
            }
        }
        public Color Color {
            get {
                return ((ShapeState)state).color;
            }
            set {
                World.current.SetProperty(this, "Color", value, ((ShapeState)state).color);
                ((ShapeState)state).color = value;
            }
        }
        public Color ContourColor {
            get {
                return ((ShapeState)state).contourColor;
            }
            set {
                World.current.SetProperty(this, "ContourColor", value, ((ShapeState)state).contourColor);
                ((ShapeState)state).contourColor = value;
            }
        }
        public float ContourSize {
            get {
                return ((ShapeState)state).contourSize;
            }
            set {
                World.current.SetProperty(this, "ContourSize", value, ((ShapeState)state).contourSize);
                ((ShapeState)state).contourSize = value;
            }
        }
        public ShapeMode Mode {
            get {
                return ((ShapeState)state).mode;
            }
            set {
                World.current.SetProperty(this, "Mode", value, ((ShapeState)state).mode);
                ((ShapeState)state).mode = value;
            }
        }

        public override object Clone() {
            return new Shape(this);
        }
    }
}
