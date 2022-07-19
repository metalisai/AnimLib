using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AnimLib {
    public class AnimatedTextList {
        public class ListItem {
            public Text2D text;
            public Vector2 position;
        }
        Vector2 origin;
        float heightOffset = 1.0f;

        List<ListItem> items = new List<ListItem>();
        
        public AnimatedTextList(Vector2 topleft, float heightOffset) {
            this.origin = topleft;
            this.heightOffset = heightOffset;
        }

        public async Task Add(Text2D text) {
            var lastitem = items.LastOrDefault();
            Vector2 newpos = lastitem == null ? origin : lastitem.position+new Vector2(0.0f, heightOffset);
            var startpos = text.state.position;
            items.Add(new ListItem() {
                text = text,
                position = newpos,
            });
            await Animate.InterpF((x) => {
                text.Transform.Pos = new Vector3(Vector2.Lerp(startpos, newpos, x), text.state.position.z);
            }, 0.0f, 1.0f, 1.0);
        }

        Vector2 PosForIndex(int idx) {
            return origin + new Vector2(0.0f, idx * heightOffset);
        }

        public async Task Add(Text2D text, int idx) {
            var lastitem = items.LastOrDefault();
            Vector2 newpos = PosForIndex(idx);
            var startpos = text.state.position;
            var newitem = new ListItem() {
                text = text,
                position = newpos,
            };
            items.Insert(idx, newitem);
            await Animate.InterpF((x) => {
                text.Transform.Pos = new Vector3(Vector2.Lerp(startpos, newpos, x), text.state.position.z);
                for(int i = idx+1; i < items.Count; i++) {
                    items[i].text.Transform.Pos = new Vector3(Vector2.Lerp(items[i].position, items[i].position+new Vector2(0.0f, heightOffset),x), items[i].text.state.position.z);
                }
            }, 0.0f, 1.0f, 1.0);
            int i = 0;
            foreach(var item in items) {
                item.position = PosForIndex(i);
                i++;
            }
        }

        public Text2D GetItem(int idx) {
            return items[idx].text;
        }

        public async Task RemoveItem(int idx) {
            int count = items.Count;
            await Animate.InterpF((x) => {
                for(int i = idx+1; i < count; i++) {
                    items[i].text.Transform.Pos = new Vector3(Vector2.Lerp(items[i].position, items[i].position-new Vector2(0.0f, heightOffset),x), items[i].text.state.position.z);
                    //text.Transform.Pos = new Vector3(Vector2.Lerp(startpos, newpos, x), text.state.position.z);
                }
            }, 0.0f, 1.0f, 1.0);
            for(int i = idx; i < count-1; i++) {
                items[i] = items[i+1];
            }
            for(int i = idx; i < count-1; i++) {
                items[i].position -= new Vector2(0.0f, heightOffset);
            }
            items.RemoveAt(items.Count-1);
        }

        public Text2D[] GetItems() {
            return items.Select(x => x.text).ToArray();
        }
    }
}
