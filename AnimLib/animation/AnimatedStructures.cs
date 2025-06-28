using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AnimLib;

/// <summary>
/// A list of text items that can be animated.
/// </summary>
public class AnimatedTextList {
    /// <summary> An item in a AnimatedTextList </summary>
    public class ListItem {
        /// <summary> The text item </summary>
        public required Text2D text;
        /// <summary> The position of the text item </summary>
        public Vector2 position;
    }
    Vector2 origin;
    float heightOffset = 1.0f;

    List<ListItem> items = new List<ListItem>();
    
    /// <summary> Create a new AnimatedTextList </summary>
    public AnimatedTextList(Vector2 topleft, float heightOffset) {
        this.origin = topleft;
        this.heightOffset = heightOffset;
    }

    /// <summary> Add a text entity to the list. The addtion is animated. </summary>
    /// <param name="text">The text to add.</param>
    public async Task Add(Text2D text) {
        var lastitem = items.LastOrDefault();
        Vector2 newpos = lastitem == null ? origin : lastitem.position+new Vector2(0.0f, heightOffset);
        var startpos = text.Position;
        items.Add(new ListItem() {
            text = text,
            position = newpos,
        });
        await Animate.InterpF((x) => {
            text.Position = Vector2.Lerp(startpos, newpos, x);
        }, 0.0f, 1.0f, 1.0);
    }

    Vector2 PosForIndex(int idx) {
        return origin + new Vector2(0.0f, idx * heightOffset);
    }

    /// <summary> Add a text entity to the list at a specific index. The addtion is animated. </summary>
    /// <param name="text">The text to add.</param>
    /// <param name="idx">The index to add the text at.</param>
    public async Task Add(Text2D text, int idx) {
        var lastitem = items.LastOrDefault();
        Vector2 newpos = PosForIndex(idx);
        var startpos = text.Position;
        var newitem = new ListItem() {
            text = text,
            position = newpos,
        };
        items.Insert(idx, newitem);
        await Animate.InterpF((x) => {
            text.Position = Vector2.Lerp(startpos, newpos, x);
            for(int i = idx+1; i < items.Count; i++) {
                items[i].text.Position = Vector2.Lerp(items[i].position, items[i].position+new Vector2(0.0f, heightOffset),x);
            }
        }, 0.0f, 1.0f, 1.0);
        int i = 0;
        foreach(var item in items) {
            item.position = PosForIndex(i);
            i++;
        }
    }

    /// <summary> Get the text entity at a specific index. </summary>
    /// <param name="idx">The index to get the text entity at.</param>
    public Text2D GetItem(int idx) {
        return items[idx].text;
    }

    /// <summary> Remove the text entity at a specific index. The removal is animated. </summary>
    public async Task RemoveItem(int idx) {
        int count = items.Count;
        await Animate.InterpF((x) => {
            for(int i = idx+1; i < count; i++) {
                items[i].text.Position = Vector2.Lerp(items[i].position, items[i].position-new Vector2(0.0f, heightOffset),x);
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

    /// <summary> Get all the text entities in the list. </summary>
    public Text2D[] GetItems() {
        return items.Select(x => x.text).ToArray();
    }
}
