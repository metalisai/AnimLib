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
}
