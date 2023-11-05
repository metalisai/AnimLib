using AnimLib;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AnimLib.Tests;

public class M4x4_Test
{
    ITestOutputHelper output;

    public M4x4_Test(ITestOutputHelper output)
    {
        this.output = output;
    }
    
    void CompareM4x4(M4x4 a, M4x4 b) {
        var m1 = a.Array;
        var m2 = b.Array;
        for(int i = 0; i < 4; i++) {
            for(int j = 0; j < 4; j++) {
                try {
                    Assert.Equal(m2[i,j], m1[i,j], 3);
                }
                catch(XunitException e) {
                    output.WriteLine($"{e.Message}: Expected value was different at {i},{j}");
                    throw;
                }
            }
        }
    }

    [Fact]
    public void HomogenousInvIsInv()
    {
        var mat = M4x4.TRS(new Vector3(300.0f, -222.33412f, 999.0f), Quaternion.AngleAxis(33.0f, new Vector3(33.0f, 22.0f, 11.0f).Normalized), new Vector3(2.0f, 0.1f, 2.1f));
        var invMat = mat.InvertedHomogenous;
        CompareM4x4(mat*invMat, M4x4.IDENTITY);
    }

    [Fact]
    public void PerspInverseIsInverse() 
    {
        float fov = MathF.PI/2; // 90 degrees
        float aspect = 16.0f/9.0f;
        float near = 0.01f;
        float far = 1000.0f;
        var mat = M4x4.Perspective(fov, aspect, near, far);
        var invMat = M4x4.InvPerspective(fov, aspect, near, far);
        CompareM4x4(mat*invMat, M4x4.IDENTITY);
    }

    [Fact]
    public void OrthoInverseIsInverse()
    {
        // if invMat is inverse of mat multiplying them should result identity matrix (A*inv(A)=I)
        float l = -100, r = 300, t = 200, b = -300, f = 1.0f, n = -1.0f;
        var mat = M4x4.Ortho(l, r, t, b, f, n);
        var invMat=  M4x4.InvOrtho(l, r, t, b, f, n);
        var multiplied = mat * invMat;
        CompareM4x4(multiplied, M4x4.IDENTITY);
    }
}
