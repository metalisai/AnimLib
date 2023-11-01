using AnimLib;
using Xunit;

namespace AnimLib.Tests;

public class Plane_Test
{
    void CompareV3(Vector3 v, Vector3 exp) {
        Assert.Equal(exp.x, v.x, 3);
        Assert.Equal(exp.y, v.y, 3);
        Assert.Equal(exp.z, v.z, 3);
    }

    [Fact]
    public void NormalPointConstructor()
    {
        // create plane at (10,20,0) with normal towards +x
        // cast ray from (20,20,0) straight towards the plane
        // should intersect at (10,20,0)
        // TODO: this needs better coverage
        var plane = new Plane(new Vector3(1.0f, 0.0f, 0.0f).Normalized, new Vector3(10.0f, 20.0f, 0.0f));
        var ray = new Ray();
        ray.d = new Vector3(-1.0f, 0.0f, 0.0f);
        ray.o = new Vector3(20.0f, 20.0f, 0.0f);
        var result = ray.Intersect(plane);
        Assert.True(result != null);
        if(result != null) {
            var val = result.Value;
            CompareV3(val, new Vector3(10.0f, 20.0f, 0.0f));
        }
    }
}
