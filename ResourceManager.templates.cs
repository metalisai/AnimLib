using System.Text;

namespace AnimLib {
    public partial class ResourceManager {
        protected string CreateCsProj() {
            var sb = new StringBuilder();
            sb.Append(
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
  <TargetFramework>netcoreapp3.1</TargetFramework>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include=""\home\ttammear\Projects\animlib\animlib.csproj"" />
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

public async Task Animation(World world, AnimationPlayer player) {
    var hw = world.Create2DText(
            new Vector2(0.0f, 0.0f), // position
            22.0f, // text size
            0.0f, // rotation
            Color.BLACK, // color
            ""Hello, world!"", // text
            new Vector2(0.5f, 0.5f), // anchor (middle) 
            TextHorizontalAlignment.Center, // hor alignment inside textbox
            TextVerticalAlignment.Center // ver alignment inside textbox
        );
    // create sine animation and change text color on every update (2hz sine black->red)
    await AnimationTransform.Sine(x => {
            x = (x+1.0f)*0.5f;
            hw.Color = new Color(x, 0.0f, 0.0f, 1.0f);
        }, 2.0);
}
}");
            return sb.ToString();
        }
    }
}
