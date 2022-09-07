using AnimLib;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AnimLib.Tests;

public class M3x3_Test
{
    ITestOutputHelper output;

    public M3x3_Test(ITestOutputHelper output)
    {
        this.output = output;
    }

    void CompareV3(Vector3 v, Vector3 exp) {
        Assert.Equal(exp.x, v.x, 3);
        Assert.Equal(exp.y, v.y, 3);
        Assert.Equal(exp.z, v.z, 3);
    }

    [Fact]
    public void TRS2DIsCorrect()
    {
        var mat = M3x3.TRS_2D(new Vector2(11.0f, 22.0f), 0.0f, new Vector2(2.0f, 2.0f));
        var v1 = new Vector3(0.0f, 7.0f, 1.0f);
        var res1 = mat*v1;
        CompareV3(res1, new Vector3(11.0f, 36.0f, 1.0f));
        // scale by 2, rotate 90 degrees (x,y -> y,-x), move 11,22
        mat = M3x3.TRS_2D(new Vector2(11.0f, 22.0f), MathF.PI/2.0f, new Vector2(2.0f, 2.0f));
        var res2 = mat*v1;
        CompareV3(res2, new Vector3(-14.0f+11.0f, 22.0f, 1.0f));
    }
}
