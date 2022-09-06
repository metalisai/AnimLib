using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace AnimLib {
    [Serializable]
    public abstract class SceneObject2D : SceneObject {
        [ShowInEditor]
        [JsonIgnore]
        public Vector2 position {
            get {
                return transform.Pos;
            }
            set {
                transform.Pos = value;
            }
        }

        [ShowInEditor]
        public string CanvasName {
            get; set;
        }

        public SceneTransform2D transform {get; set; }

        public SceneObject2D() : base() {
            CanvasName = CanvasState.DEFAULTNAME;
            transform = new SceneTransform2D(Vector2.ZERO, 0.0f);
        }
        public SceneObject2D(SceneObject2D obj) : base(obj) {
            this.transform = new SceneTransform2D(obj.transform.Pos, obj.transform.Rot);
            this.CanvasName = obj.CanvasName;
        }

        public abstract bool Intersects(Vector2 point);

        // get surface if 2D object or null if not
        public Plane? GetSurface() {
            // TODO
#warning SceneObject2D.GetSurface() not implemented
            return new Plane(new Vector3(0.0f, 0.0f, -1.0f), Vector3.ZERO);
        }

        public abstract VisualEntity2D InitializeEntity();

        // get 2D handles that can be dragged on a surface in the editor
        // (the handles are in world space)
        public abstract Vector2[] GetHandles2D();
        public abstract void SetHandle(int id, Vector2 wpos);

    }
    public abstract class SceneObject3D : SceneObject {
        [ShowInEditor]
        [JsonIgnore]
        public Vector3 position {
            get {
                return transform.Pos;
            }
            set {
                transform.Pos = value;
            }
        }
        public SceneTransform3D transform {get; set;}
    }
    [Serializable]
    public abstract class SceneObject {
        [ShowInEditor]
        public double startTime {get; set; }
        [ShowInEditor]
        public double endTime {get; set; }
        [ShowInEditor]
        public string name {get; set; } = Guid.NewGuid().ToString();
        [JsonIgnore]
        public (double,double) timeslice { 
            get { return (startTime, endTime); }
            set { startTime = value.Item1; endTime = value.Item2; }
        }
        public abstract object Clone();
        [JsonIgnore]
        public (string, Func<object>, Action<object>)[] Properties { get; protected set;} = new (string, Func<object>, Action<object>)[]{};

        public SceneObject() {
            var props = this.GetType().GetProperties();
            var showProps = props.Where(x => Attribute.IsDefined(x, typeof(ShowInEditorAttribute))).ToArray();
            var propsArr = new (string, Func<object>, Action<object>)[showProps.Length];
            int i = 0;
            foreach(var prop in showProps) {
                (string, Func<object>, Action<object>) cprop = (prop.Name, () => {
                    return prop.GetGetMethod().Invoke(this, new object[]{});
                }, (newvalue) => {
                    prop.GetSetMethod().Invoke(this, new object[] {newvalue});
                });
                propsArr[i] = cprop;
                i++;
            }
            Properties = propsArr;
            timeslice = (0.0f, 99999.0f);
        }

        public SceneObject(SceneObject obj) {
            this.startTime = obj.startTime;
            this.endTime = obj.endTime;
            this.name = obj.name;
        }
    }

}
