using System.Text;

namespace AnimLib;

internal partial class ResourceManager {
    protected string CreateCsProj() {
        var sb = new StringBuilder();
        var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
        sb.Append(
$@"<Project Sdk=""Microsoft.NET.Sdk"">
<PropertyGroup>
<TargetFramework>net8.0</TargetFramework>
</PropertyGroup>

<PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"">
<OutputPath Condition=""'$(OutputPath)'=='' "">..\bin</OutputPath>
</PropertyGroup>
<PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Release|AnyCPU'"">
<OutputPath Condition=""'$(OutputPath)'=='' "">..\bin</OutputPath>
</PropertyGroup>
<PropertyGroup>
<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>

<ItemGroup>
<Reference Include=""AnimLib"">
    <HintPath>{location}</HintPath>
</Reference>
</ItemGroup>

</Project>");
        return sb.ToString();
    }

    protected string CreateMain(string classname) {
        var sb = new StringBuilder();
        sb.Append(
@"using System;
using AnimLib;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ");
        sb.Append(classname);
        sb.Append(
@" : AnimationBehaviour
{
public void Init(AnimationSettings settings) {
    settings.Name = ""My animation"";
    // animation length must be bounded
    // (it gets ""baked"" to allow seeking whole animation in editor)
    settings.MaxLength = 60.0f; 
}

public async Task Animation(World world, Animator animator) {
    var hw = new Text2D(""Hello, world!"", size: 22.0f, color: Color.RED);
    hw.Position = new Vector2(100.0f, 100.0f);
    hw.Anchor = new Vector2(-0.5f, 0.2f);
    hw.HAlign = TextHorizontalAlignment.Center;
    hw.VAlign = TextVerticalAlignment.Center;

    var hw2 = world.Clone(hw);
    hw2.Position = new Vector2(100.0f, 200.0f);
    hw2.Text = ""Already here"";
    world.CreateInstantly(hw2);

    await world.CreateFadeIn(hw, 1.0f);
    // create sine animation and change text color on every update (2hz sine black->red)
    await Animate.Sine(x => {
            x = (x+1.0f)*0.5f;
            hw.Color = new Color(x, 0.0f, 0.0f, 1.0f);
        }, 2.0);
}
}");
        return sb.ToString();
    }
}
