# AnimLib

![build](https://github.com/metalisai/AnimLib/actions/workflows/dotnet.yml/badge.svg)

<a href="url"><img src="/img/AnimLib.png" width="400" ></a> <br/>
  

## What is this?
This is a WIP application to programmatically create animations. 
It's not very user friendly right now, so I wouldn't recommend using it yet.

The main use case is creating visualizations with 2D vector graphics, but it's intended to support 3D as well.
I started working on this years ago and occasionally come back to it every now and then. It's still very incomplete/buggy and probably needs a major
refactor. The initial version was very different from what it is now. I've lost interest in the project and I didn't see any reason to keep it private, so I
decided to put it up on github.

## How does it work?

You'd open up the application, create a new project and then compile a .dll plugin that the application can load.
Check the following example code and the rendering below.

```cs
using AnimLib;
using System.Threading.Tasks;

public class Yo : AnimationBehaviour
{
    public void Init(AnimationSettings settings) {
        settings.Name = "My animation";
        // animation length must be bounded
        // (it gets "baked" to allow seeking whole animation in editor)
        settings.MaxLength = 60.0f; 
    }

    public async Task Animation(World world, Animator animator) {
        // create text "Hello, world!"
        var hw = new Text2D("Hello, world!", size: 22.0f, color: Color.RED);
        hw.Transform.Pos = new Vector2(100.0f, 100.0f);
        hw.Anchor = new Vector2(-0.5f, 0.0f); // screen is -0.5 ... 0.5 with origin at center

        // find an existing rectangle that has been created in the editor application
        var rect = animator.Scene.GetSceneEntityByName("rect") as Rectangle;
        if (rect != null)
            _ = Animate.Move(rect.Transform, rect.Transform.Pos + new Vector2(100.0f, 100.0f), 1.0f);

        // create another text
        var hw2 = world.Clone(hw);
        hw2.Transform.Pos = new Vector2(100.0f, 200.0f);
        hw2.Text = "Already here! Yes, it's true!";
        // place 'hw2' text into world with no fade
        world.CreateInstantly(hw2);

        // interpolate hw2 text color to green, wait until it finishes
        await Animate.Color(hw2, Color.GREEN, 1.0f);
        // move hw2 text down 100 pixels without waiting for it to complete
        _ = Animate.Move(hw2.Transform, hw2.Transform.Pos + new Vector2(0.0f, 100.0f), 1.0f);

        // create a single circle with an alpha fade in
        float size = 30.0f;
        var circle = new Circle(size);
        await world.CreateFadeIn(circle, 1.0f);
        // clone the circle into 5x5 grid that expands from the circle
        for (int i = 0; i < 5; i++)
        for (int j = 0; j < 5; j++)
        {
            var c2 = world.CreateClone(circle);
            // equivalent to the above Animate.Move
            _ = Animate.Offset(c2.Transform, new Vector2(i*2.0f*size, j*2.0f*size), 1.0f);
        }

        // fade in the hello world text, didn't await the expanding circles so both happen at the same time
        await world.CreateFadeIn(hw, 1.0f);
        // create sine animation and change text color on every update (2hz sine black->red)
        await Animate.Sine(x => {
                x = (x+1.0f)*0.5f;
                hw.Color = new Color(x, 0.0f, 0.0f, 1.0f);
            }, 2.0);
    }
}
```

[Here's this program rendered.](https://youtu.be/JRv98Lcgkew)

[Here's the most complicated animation I've created so far.](https://youtu.be/_LwfBfO-Tao) [Source](https://github.com/metalisai/dump/blob/master/QuickAlgos/radixsort/src/dunno.cs)

## What's special about it?

This application is by now means completely unique.
The most similar library that I know is probably [3Blue1Brown](https://www.3blue1brown.com/)'s manim and the community fork that sprouted from it. 
AnimLib's usage code certainly isn't quite as elegant yet and is not really geared towards math animations like manim.
I've also seen something almost identical created in javascript and HTML5 canvas, but I can't find it right now. 

I'd consider the more unique features to be use of C#'s async features and real-time seekable playback. Unlike most other similar applications there is an intermediate frame state format, which means you can seek and render individual frames without rendering the whole thing. 
Also the animations are automatically hot reloaded when you recompile the animation .dll. This means you can instantly see the changes of entire animation as soon as you recompile.  
The editor application is very primitive right now, but there is potential to hit a sweetspot between things like Adobe's After Effects and manim. Being able to use GUI
for some things like placing objects and programming for animating program behaviour or whatever else. For example you can drag and drop .png or .svg file into the editor
and load it from the C# code. Or create editable color presets in editor and use them in your code by name. Much more convenient than manually loading files or recompiling for every color change.

## What's missing

The UI doesn't look good, there are unfixed bugs and I wouldn't expect the application to work you of the box. There are some things I haven't quite figured out yet. 
Sound support is very primitive and can get out of sync for complex animations that struggle to run real-time. I'd also add more elegant helper framework so that the code would be less verbose. Lots of work to do.
