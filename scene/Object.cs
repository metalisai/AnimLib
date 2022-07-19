using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace AnimLib {
    [Serializable]
    public abstract class SceneObject {
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
        [ShowInEditor]
        public double startTime {get; set; }
        [ShowInEditor]
        public double endTime {get; set; }
        [ShowInEditor]
        public string name {get; set; } = Guid.NewGuid().ToString();
        [JsonIgnore]
        public abstract bool Is2D {get;}
        [JsonIgnore]
        public (double,double) timeslice { 
            get { return (startTime, endTime); }
            set { startTime = value.Item1; endTime = value.Item2; }
        }
        // get 2D handles that can be dragged on a surface in the editor
        // (the handles are in world space)
        public abstract Vector3[] GetHandles2D();
        public abstract void SetHandle(int id, Vector3 wpos);
        public abstract object Clone();
        // get surface if 2D object or null if not
        public abstract Plane? GetSurface();
        public SceneTransform transform {get; set; }
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
        }
    }

}
