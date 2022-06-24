using System;
using System.Threading.Tasks;

namespace AnimLib {
    public class AnimationSettings {
        public string Name = Guid.NewGuid().ToString();
        public double FPS = 60.0;
        public int Width = 1920;
        public int Height = 1080;
        public double MaxLength = 600.0;
    }
    public interface AnimationBehaviour {
        Task Animation(World world, Animator animator);
        void Init(AnimationSettings settings);
    }

    public class EmptyBehaviour : AnimationBehaviour {
        public void Init(AnimationSettings settings) {
            settings.MaxLength = 1.0;
        }
        public async Task Animation(World world, Animator animator) {
            var hw = new Text2D();
            hw.Transform.Pos = new Vector2(100.0f, 100.0f);
            hw.Size = 22.0f;
            hw.Color = Color.RED;
            hw.Anchor = new Vector2(0.5f, 0.5f);
            hw.HAlign = TextHorizontalAlignment.Center;
            hw.VAlign = TextVerticalAlignment.Center;
            // TODO: this thing  is screaming for multiline text
            hw.Text = "No project assembly (.dll) found!";
            var hw2 = world.Clone(hw);
            hw2.Transform.Pos = new Vector2(100.0f, 131.0f);
            hw2.Text = "Go to project directory and build it";
            var hw3 = world.Clone(hw);
            hw3.Transform.Pos = new Vector2(100.0f, 162.0f);
            hw3.Text = "The animation will reload automatically";
            world.CreateInstantly(hw);
            world.CreateInstantly(hw2);
            world.CreateInstantly(hw3);

            await Task.Yield();
        }
    }
}
