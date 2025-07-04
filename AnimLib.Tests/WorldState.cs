using Xunit.Abstractions;
using Xunit.Sdk;
using AnimLib;

namespace AnimLib.Tests;

public class WorldState
{
    AnimationSettings settings = new AnimationSettings()
    {
        FPS = 60,
        Width = 800,
        Height = 600,
        MaxLength = 60.0f
    };

    [Fact]
    public void CreateEntity_CreateDestroyeCircle_Matches()
    {
        // Arrange
        var world = new World(settings);
        world.StartEditing(this);

        var circle = new Circle(50.0f);
        world.CreateDynInstantly(circle);

        Assert.True(circle.Created);

        world.DestroyDyn(circle);

        Assert.False(circle.Created);

        world.EndEditing();
    }

    [Fact]
    public void CreateEntity_RogueCircle_NotCreated()
    {
        // Arrange
        var world = new World(settings);
        world.StartEditing(this);

        var circle = new Circle(50.0f);

        Assert.False(circle.Created);

        world.EndEditing();
    }

    (World, Animator) SetupWorldWithAnimator()
    {
        // Arrange
        var world = new World(settings);
        world.StartEditing(this);

        // need an animator and a font to create text
        var rm = new ResourceManager();
        var props = new AnimationPlayer.PlayerProperties();
        TextPlacement tp = new TextPlacement(TextPlacement.DefaultFontPath, "Default");
        var animator = new Animator(rm, world, null, settings, props, tp);
        animator.BeginAnimate();
        return (world, animator);
    }

    [Fact]
    public void CreateText_Word_ShapesBehave()
    {
        var (world, animator) = SetupWorldWithAnimator();

        var text = new Text2D("Hello World");
        var shapes = text.GetSubstring("Hello");
        // has any shapes (letters)
        Assert.NotEmpty(shapes);

        // haven't created anything yet, so nothing should be created
        Assert.False(text.Created);
        Assert.False(shapes[0].Created);

        world.CreateDynInstantly(text);

        // check that the text and shapes are created
        Assert.True(text.Created);
        Assert.True(shapes[0].Created);

        // change the text
        text.Text = "Goodbye World";
        // check that the old shapes are destroyed
        Assert.False(shapes[0].Created);
        shapes = text.GetSubstring("Goodbye");
        Assert.NotEmpty(shapes);

        world.DestroyDyn(text);

        // check that both the text and its shapes are destroyed
        Assert.False(text.Created);
        Assert.False(shapes[0].Created);

        animator.EndAnimate();
        world.EndEditing();
    }

    [Fact]
    public void CreateText_Word_DisbandSeparates()
    {
        var (world, animator) = SetupWorldWithAnimator();

        var text = new Text2D("Disbanding");
        var shapes = text.GetSubstring("Disbanding");

        Assert.NotEmpty(shapes);

        world.CreateDynInstantly(text);
        Assert.True(text.Created);
        text.Disband();
        Assert.False(text.Created);
        Assert.True(shapes[0].Created);

        animator.EndAnimate();
        world.EndEditing();
    }
}
