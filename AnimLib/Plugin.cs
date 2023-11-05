using System;
using System.Threading.Tasks;

namespace AnimLib;

/// <summary>
/// Settings which are used to configure the animation process.
/// </summary>
public class AnimationSettings {
    public string Name = Guid.NewGuid().ToString();
    public double FPS = 60.0;
    public int Width = 1920;
    public int Height = 1080;
    public double MaxLength = 600.0;

    public AnimationSettings Clone() {
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
    Task Animation(World world, Animator animator);
    void Init(AnimationSettings settings);
}

/// <summary>
/// Builtin behaviour in case no behaviour is specified.
/// </summary>
public class NoProjectBehaviour : AnimationBehaviour {
    public void Init(AnimationSettings settings) {
        settings.MaxLength = 1.0;
    }
    public async Task Animation(World world, Animator animator) {
        var hw = new Text2D("No project loaded!");
        hw.Transform.Pos = new Vector2(100.0f, -200.0f);
        hw.Size = 22.0f;
        hw.Color = Color.RED;
        hw.Anchor = new Vector2(-0.5f, 0.5f); // top left
        //hw.HAlign = TextHorizontalAlignment.Center;
        //hw.VAlign = TextVerticalAlignment.Center;
        // TODO: this thing  is screaming for multiline text
        var hw2 = world.Clone(hw);
        hw2.Transform.Pos = new Vector2(100.0f, -200.0f+31.0f);
        hw2.Text = "File->New project... or File->Open project... to continue";
        world.CreateInstantly(hw);
        world.CreateInstantly(hw2);
        await Task.Yield();
    }
}


/// <summary>
/// Builtin behaviour in case the specified behaviour has an error.
/// </summary>
public class ErrorBehaviour : AnimationBehaviour {
    public void Init(AnimationSettings settings) {
        settings.MaxLength = 1.0;
    }
    public async Task Animation(World world, Animator animator) {
        var hw = new Text2D();
        hw.Transform.Pos = new Vector2(100.0f, -200.0f);
        hw.Size = 22.0f;
        hw.Color = Color.RED;
        hw.Anchor = new Vector2(-0.5f, 0.5f); // top left
        hw.HAlign = TextHorizontalAlignment.Center;
        hw.VAlign = TextVerticalAlignment.Center;
        // TODO: this thing  is screaming for multiline text
        hw.Text = "Error occurred during animation";
        var hw2 = world.Clone(hw);
        hw2.Transform.Pos = new Vector2(100.0f, -200.0f+31.0f);
        hw2.Text = "Fix your animation and try again!";
        world.CreateInstantly(hw);
        world.CreateInstantly(hw2);
        await Task.Yield();
    }
}


/// <summary>
/// Builtin behaviour in case the project is loaded but no assembly containing the behaviour is found.
/// </summary>
public class EmptyBehaviour : AnimationBehaviour {
    public void Init(AnimationSettings settings) {
        settings.MaxLength = 1.0;
    }
    public async Task Animation(World world, Animator animator) {
        var hw = new Text2D();
        hw.Transform.Pos = new Vector2(100.0f, -200.0f);
        hw.Size = 22.0f;
        hw.Color = Color.RED;
        hw.Anchor = new Vector2(-0.5f, 0.5f); // top left
        hw.HAlign = TextHorizontalAlignment.Center;
        hw.VAlign = TextVerticalAlignment.Center;
        // TODO: this thing  is screaming for multiline text
        hw.Text = "No project assembly (.dll) found!";
        var hw2 = world.Clone(hw);
        hw2.Transform.Pos = new Vector2(100.0f, -200.0f+31.0f);
        hw2.Text = "Go to project directory and build it";
        var hw3 = world.Clone(hw);
        hw3.Transform.Pos = new Vector2(100.0f, -200.0f+62.0f);
        hw3.Text = "The animation will reload automatically";
        world.CreateInstantly(hw);
        world.CreateInstantly(hw2);
        world.CreateInstantly(hw3);

        await Task.Yield();
    }
}