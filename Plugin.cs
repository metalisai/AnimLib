using System;
using System.Threading.Tasks;

namespace AnimLib {
    public class AnimationSettings {
        public string Name = Guid.NewGuid().ToString();
        public double FPS = 60.0;
        public double MaxLength = 600.0;
    }
    public interface AnimationBehaviour {
        Task Animation(World world, Animator animator);
        void Init(AnimationSettings settings);
    }
}
