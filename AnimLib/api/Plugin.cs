using System;
using System.Threading.Tasks;

namespace AnimLib;

/// <summary>
/// Settings which are used to configure the animation process.
/// </summary>
public class AnimationSettings {
    /// <summary>
    /// The name of the animation.
    /// </summary>
    public string Name = Guid.NewGuid().ToString();

    /// <summary>
    /// The target FPS of the animation.
    /// </summary>
    public double FPS = 60.0;

    /// <summary>
    /// The target frame width.
    /// </summary>
    public int Width = 1920;

    /// <summary>
    /// The target frame height.
    /// </summary>
    public int Height = 1080;

    /// <summary>
    /// The maximum length of the animation in seconds.
    /// </summary>
    public double MaxLength = 600.0;

    /// <summary>
    /// Creates a copy of the settings.
    /// </summary>
    internal AnimationSettings Clone() {
        return new AnimationSettings() {
            Name = this.Name,
            FPS = this.FPS,
            Width = this.Width,
            Height = this.Height,
            MaxLength = this.MaxLength
        };
    }
}

/// <summary>
/// The animation behaviour is the main entry point for the animation process.
/// </summary>
public interface AnimationBehaviour {
    /// <summary>
    /// The animation procedure.
    /// </summary>
    Task Animation(World world, Animator animator);

    /// <summary>
    /// The initialization procedure.
    /// </summary>
    void Init(AnimationSettings settings);
}

/// <summary>
/// Builtin behaviour in case no behaviour is specified.
/// </summary>
internal class NoProjectBehaviour : AnimationBehaviour {
    public void Init(AnimationSettings settings) {
        settings.MaxLength = 1.0;
    }

    public async Task Animation(World world, Animator animator) {
        var hw = new Text2D("No project loaded!");
        hw.Position = new Vector2(100.0f, -200.0f);
        hw.Size = 22.0f;
        hw.Color = Color.RED;
        hw.Anchor = new Vector2(-0.5f, 0.5f); // top left
        //hw.HAlign = TextHorizontalAlignment.Center;
        //hw.VAlign = TextVerticalAlignment.Center;
        // TODO: this thing  is screaming for multiline text
        var hw2 = world.CloneDyn(hw);
        hw2.Position = new Vector2(100.0f, -200.0f+31.0f);
        hw2.Text = "File->New project... or File->Open project... to continue";
        world.CreateDynInstantly(hw);
        world.CreateDynInstantly(hw2);
        await Task.Yield();
    }
}


/// <summary>
/// Builtin behaviour in case the specified behaviour has an error.
/// </summary>
internal class ErrorBehaviour : AnimationBehaviour {
    public void Init(AnimationSettings settings) {
        settings.MaxLength = 1.0;
    }

    public async Task Animation(World world, Animator animator) {
        var hw = new Text2D();
        hw.Position = new Vector2(100.0f, -200.0f);
        hw.Size = 22.0f;
        hw.Color = Color.RED;
        hw.Anchor = new Vector2(-0.5f, 0.5f); // top left
        hw.HAlign = TextHorizontalAlignment.Center;
        hw.VAlign = TextVerticalAlignment.Center;
        // TODO: this thing  is screaming for multiline text
        hw.Text = "Error occurred during animation";
        var hw2 = world.CloneDyn(hw);
        hw2.Position = new Vector2(100.0f, -200.0f+31.0f);
        hw2.Text = "Fix your animation and try again!";
        world.CreateDynInstantly(hw);
        world.CreateDynInstantly(hw2);
        await Task.Yield();
    }
}


/// <summary>
/// Builtin behaviour in case the project is loaded but no assembly containing the behaviour is found.
/// </summary>
internal class EmptyBehaviour : AnimationBehaviour {
    public void Init(AnimationSettings settings) {
        settings.MaxLength = 1.0;
    }

    public async Task Animation(World world, Animator animator) {
        var hw = new Text2D();
        hw.Position = new Vector2(100.0f, -200.0f);
        hw.Size = 22.0f;
        hw.Color = Color.RED;
        hw.Anchor = new Vector2(-0.5f, 0.5f); // top left
        hw.HAlign = TextHorizontalAlignment.Center;
        hw.VAlign = TextVerticalAlignment.Center;
        // TODO: this thing  is screaming for multiline text
        hw.Text = "No project assembly (.dll) found!";
        var hw2 = world.CloneDyn(hw);
        hw2.Position = new Vector2(100.0f, -200.0f+31.0f);
        hw2.Text = "Go to project directory and build it";
        var hw3 = world.CloneDyn(hw);
        hw3.Position = new Vector2(100.0f, -200.0f+62.0f);
        hw3.Text = "The animation will reload automatically";
        world.CreateDynInstantly(hw);
        world.CreateDynInstantly(hw2);
        world.CreateDynInstantly(hw3);

        await Task.Yield();
    }
}
