using System.Threading.Tasks;
using System;

namespace AnimLib {
    public static class TextAnimation {
        public static async Task<Text2D> TypeText(Text2D t, Vector2 position, string text, int wordPerMinute = 150) {
            var rnd = new Random();
            t.Transform.Pos = position;
            t.Text = "";
            if(!t.created) {
                World.current.CreateInstantly(t);
            }
            string current = "";
            foreach(var c in text) {
                System.Diagnostics.Debug.Assert((BuiltinSound.Keyboard_Click1+8) == BuiltinSound.Keyboard_Click9);
                var sidx = rnd.Next(8);
                var volume = 0.2f - (float)rnd.NextDouble()*0.08f;
                World.current.PlaySound(BuiltinSound.Keyboard_Click1+sidx, volume);
                current += c;
                double average = 60.0 / ((double)wordPerMinute*5.0);
                t.Text = current;
                bool pause = c == ' ' || c == '!' || c == ',' || c == '.';
                var waitTime = (1.0-(rnd.NextDouble()*0.25-0.125)) * (pause ? 1.5 : 1.0) * average;
                await Time.WaitSeconds(waitTime);
            }
            return t;
        }

        public static async Task<Text2D> TypeText(Vector2 position, string text, int wordPerMinute = 80) {
            return await TypeText(new Text2D(), position, text, wordPerMinute);
        }

    }
}
