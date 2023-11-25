using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace AnimLib;

/// <summary>
/// A 3x3 matrix.
/// </summary>
public struct M3x3 {
    /// <summary>
    /// A component of the matrix.
    /// </summary>
    public float m11, m21, m31,
    m12, m22, m32,
    m13, m23, m33;

    /// <summary>
    /// Inverse of the matrix. Assumes the matrix is invertible.
    /// </summary>
    public M3x3 Inverted {
        // TODO: unit test?
        get {
            float det = m11*(m22*m33 - m32*m23) - m12*(m21*m33 - m23*m31) + m13*(m21*m32 - m22*m31);
            float invDet = 1.0f / det;
            M3x3 ret;
            ret.m11 = (m22*m33 - m32*m23) * invDet;
            ret.m12 = (m13*m32 - m12*m33) * invDet;
            ret.m13 = (m12*m23 - m13*m22) * invDet;
            ret.m21 = (m23*m31 - m21*m33) * invDet;
            ret.m22 = (m11*m33 - m13*m31) * invDet;
            ret.m23 = (m21*m13 - m11*m23) * invDet;
            ret.m31 = (m21*m32 - m31*m22) * invDet;
            ret.m32 = (m31*m12 - m11*m32) * invDet;
            ret.m33 = (m11*m22 - m21*m12) * invDet;
            return ret;
        }
    }

    /// <summary>
    /// Construct a matrix from column vectors.
    /// </summary>
    /// <param name="c1">The first column.</param>
    /// <param name="c2">The second column.</param>
    /// <param name="c3">The third column.</param>
    public M3x3 FromColumns(Vector3 c1, Vector3 c2, Vector3 c3) {
        M3x3 ret;
        ret.m11 = c1.x; ret.m21 = c1.y; ret.m31 = c1.z;
        ret.m12 = c2.x; ret.m22 = c2.y; ret.m32 = c2.z;
        ret.m13 = c3.x; ret.m23 = c3.y; ret.m33 = c3.z;
        return ret;
    }

    /// <summary>
    /// Convert the matrix to a string representation.
    /// </summary>
    public override string ToString() {
        return $"[\n{m11:N3} {m12:N3} {m13:N3}\n{m21:N3} {m22:N3} {m23:N3}\n{m31:N3} {m32:N3} {m33:N3}\n]";
    }

    /// <summary>
    /// Create a matrix that represents a 2D translation.
    /// </summary>
    /// <param name="t">The translation vector.</param>
    public static M3x3 Translate_2D(Vector2 t) {
        M3x3 tr;
        tr.m11 = 1.0f; tr.m12 = 0.0f; tr.m13 = t.x;
        tr.m21 = 0.0f; tr.m22 = 1.0f; tr.m23 = t.y;
        tr.m31 = 0.0f; tr.m32 = 0.0f; tr.m33 = 1.0f;
        return tr;
    }

    /// <summary>
    /// Create a matrix that represents a 3D rotation.
    /// </summary>
    /// <param name="q">The quaternion to rotate by.</param>
    public static M3x3 Rotate(Quaternion q) {
        M3x3 ret = new() {
            m11 = 1.0f - 2.0f*q.y*q.y - 2.0f*q.z*q.z,
            m21 = 2.0f*q.x*q.y + 2.0f*q.w*q.z,
            m31 = 2.0f*q.x*q.z - 2.0f*q.w*q.y,

            m12 = 2.0f*q.x*q.y - 2.0f*q.w*q.z,
            m22 = 1.0f - 2.0f*q.x*q.x - 2.0f*q.z*q.z,
            m32 = 2.0f*q.y*q.z + 2.0f*q.w*q.x,
            
            m13 = 2.0f*q.x*q.z + 2.0f*q.w*q.y,
            m23 = 2.0f*q.y*q.z - 2.0f*q.w*q.x,
            m33 = 1.0f - 2.0f*q.x*q.x - 2.0f*q.y*q.y,
        };
        return ret;
    }

    /// <summary>
    /// Create a matrix that represents a 2D rotation.
    /// </summary>
    /// <param name="r">The rotation in radians.</param>
    public static M3x3 Rotate_2D(float r) {
        M3x3 rot;
        rot.m11 = MathF.Cos(r); rot.m12 = -MathF.Sin(r); rot.m13 = 0.0f;
        rot.m21 = MathF.Sin(r); rot.m22 = MathF.Cos(r); rot.m23 = 0.0f;
        rot.m31 = 0.0f; rot.m32 = 0.0f; rot.m33 = 1.0f;
        return rot;
    }

    /// <summary>
    /// Create a matrix that represents a 2D homography transformation. Can be used to map a quad to another quad.
    /// </summary>
    /// <param name="src">The source points. At least 4 points are required.</param>
    /// <param name="dst">The destination points. Equal length with src is assumed.</param>
    /// <returns>A matrix that represents a transformation that maps the source points to the destination points.</returns>
    public static M3x3 Homography(Vector2[] src, Vector2[] dst) {
        int n = src.Length;
        if (n < 4) {
            throw new ArgumentException("Need at least 4 points to compute homography");
        }
        var A = Matrix<float>.Build.Dense(2*n,2*n);
        for (int i = 0; i < n; i++)
        {
            A[2*i, 0] = src[i].x;
            A[2*i, 1] = src[i].y;
            A[2*i, 2] = 1.0f;
            A[2*i, 6] = -src[i].x*dst[i].x;
            A[2*i, 7] = -src[i].y*dst[i].x;
            A[2*i+1, 3] = src[i].x;
            A[2*i+1, 4] = src[i].y;
            A[2*i+1, 5] = 1.0f;
            A[2*i+1, 6] = -src[i].x*dst[i].y;
            A[2*i+1, 7] = -src[i].y*dst[i].y;
        }
        var b = Vector<float>.Build.DenseOfArray(dst.SelectMany(v => new float[] {v.x, v.y}).ToArray());
        var x = A.PseudoInverse() * b;
        M3x3 ret;
        ret.m11 = x[0]; ret.m12 = x[1]; ret.m13 = x[2];
        ret.m21 = x[3]; ret.m22 = x[4]; ret.m23 = x[5];
        ret.m31 = x[6]; ret.m32 = x[7]; ret.m33 = 1.0f;
        return ret;
    }

    private static M4x4 RotXOp(float perspective, float angle)
    {
        var rot = Quaternion.AngleAxis(angle, Vector3.RIGHT);
        return M4x4.Rotate(rot);
    }

    private static M4x4 RotYOp(float perspective, float angle)
    {
        var rot = Quaternion.AngleAxis(angle, Vector3.UP);
        return M4x4.Rotate(rot);
    }

    private static M4x4 RotZOp(float perspective, float angle)
    {
        var rot = Quaternion.AngleAxis(angle, Vector3.FORWARD);
        return M4x4.Rotate(rot);
    }

    /// <summary>
    /// Create a homography matrix that (fakes) a 3D rotation around the X axis.
    /// Similar to CSS perspective.
    /// </summary>
    /// <param name="perspective">The perspective factor.</param>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>A homography matrix that represents a 3D rotation around the X axis.</returns>
    public static M3x3 RotateX_2D(float perspective, float angle) {
        return Rotate_2D(perspective, angle, RotXOp(perspective, angle));
    }

    /// <summary>
    /// Create a homography matrix that (fakes) a 3D rotation around the Y axis.
    /// Similar to CSS perspective.
    /// </summary>
    /// <param name="perspective">The perspective factor.</param>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>A homography matrix that represents a 3D rotation around the Y axis.</returns>
    public static M3x3 RotateY_2D(float perspective, float angle) {
        return Rotate_2D(perspective, angle, RotYOp(perspective, angle));
    }

    /// <summary>
    /// Create a homography matrix that (fakes) a 3D rotation around the Z axis.
    /// Similar to CSS perspective.
    /// </summary>
    /// <param name="perspective">The perspective factor.</param>
    /// <param name="angle">The angle in radians.</param>
    /// <returns>A homography matrix that represents a 3D rotation around the Z axis.</returns>
    public static M3x3 RotateZ_2D(float perspective, float angle) {
        return Rotate_2D(perspective, angle, RotZOp(perspective, angle));
    }

    private static M3x3 Rotate_2D(float perspective, float angle, in M4x4 op) {
        Vector4 point1 = new(0.0f, 0.0f, 0.0f, 1.0f);
        Vector4 point2 = new(0.0f, 1.0f, 0.0f, 1.0f);
        Vector4 point3 = new(1.0f, 0.0f, 0.0f, 1.0f);
        Vector4 point4 = new(1.0f, 1.0f, 0.0f, 1.0f);
        var transformed1 = op * point1;
        var transformed2 = op * point2;
        var transformed3 = op * point3;
        var transformed4 = op * point4;
        transformed1 /= transformed1.w;
        transformed2 /= transformed2.w;
        transformed3 /= transformed3.w;
        transformed4 /= transformed4.w;

        M3x3 ret = Homography([point1.xy, point2.xy, point3.xy, point4.xy], [transformed1.xy, transformed2.xy, transformed3.xy, transformed4.xy]);
        return ret;
    }

    /// <summary>
    /// Create a matrix that represents 2D translation, rotation and scaling.
    /// </summary>
    /// <param name="t">The translation vector.</param>
    /// <param name="r">The rotation in radians.</param>
    /// <param name="s">The scaling vector.</param>
    /// <returns>A matrix that represents 2D translation, rotation and scaling.</returns>
    public static M3x3 TRS_2D(Vector2 t, float r, Vector2 s) {
        M3x3 tr, rot, sc;

        // TODO: simplify
        
        tr.m11 = 1.0f; tr.m12 = 0.0f; tr.m13 = t.x;
        tr.m21 = 0.0f; tr.m22 = 1.0f; tr.m23 = t.y;
        tr.m31 = 0.0f; tr.m32 = 0.0f; tr.m33 = 1.0f;

        rot.m11 = MathF.Cos(r); rot.m12 = -MathF.Sin(r); rot.m13 = 0.0f;
        rot.m21 = MathF.Sin(r); rot.m22 = MathF.Cos(r); rot.m23 = 0.0f;
        rot.m31 = 0.0f; rot.m32 = 0.0f; rot.m33 = 1.0f;

        sc.m11 = s.x;  sc.m12 = 0.0f; sc.m13 = 0.0f;
        sc.m21 = 0.0f; sc.m22 = s.y;  sc.m23 = 0.0f;
        sc.m31 = 0.0f; sc.m32 = 0.0f; sc.m33 = 1.0f;

        return tr*rot*sc;
    }

    /// <summary>
    /// Multiply two matrices. Represents the transformation of the first matrix followed by the transformation of the second matrix.
    /// </summary>
    /// <param name="l">The first matrix.</param>
    /// <param name="r">The second matrix.</param>
    /// <returns>A matrix that represents the transformation of the first matrix followed by the transformation of the second matrix.</returns>
    public static M3x3 operator* (M3x3 l, M3x3 r) {
        return new M3x3() {
            m11 = l.m11 * r.m11 + l.m12 * r.m21 + l.m13 * r.m31,
            m21 = l.m21 * r.m11 + l.m22 * r.m21 + l.m23 * r.m31,
            m31 = l.m31 * r.m11 + l.m32 * r.m21 + l.m33 * r.m31,
            m12 = l.m11 * r.m12 + l.m12 * r.m22 + l.m13 * r.m32,
            m22 = l.m21 * r.m12 + l.m22 * r.m22 + l.m23 * r.m32,
            m32 = l.m31 * r.m12 + l.m32 * r.m22 + l.m33 * r.m32,
            m13 = l.m11 * r.m13 + l.m12 * r.m23 + l.m13 * r.m33,
            m23 = l.m21 * r.m13 + l.m22 * r.m23 + l.m23 * r.m33,
            m33 = l.m31 * r.m13 + l.m32 * r.m23 + l.m33 * r.m33,
        };
    }

    /// <summary>
    /// Multiply a matrix by a vector. Represents the transformation of the matrix on the vector.
    /// </summary>
    /// <param name="m">The matrix.</param>
    /// <param name="v">The vector.</param>
    /// <returns>A vector transformed by the specified matrix.</returns>
    public static Vector3 operator* (M3x3 m, Vector3 v) {
        var ret = new Vector3() {
            x = v.x*m.m11 + v.y*m.m12 + v.z*m.m13,
            y = v.x*m.m21 + v.y*m.m22 + v.z*m.m23,
            z = v.x*m.m31 + v.y*m.m32 + v.z*m.m33,
        };
        return ret;
    }

    /// <summary>
    /// Multiply a matrix by a scalar. Component-wise multiplication.
    /// </summary>
    /// <param name="l">The matrix.</param>
    /// <param name="r">The scalar.</param>
    /// <returns>A matrix with each component multiplied by the scalar.</returns>
    public static M3x3 operator* (M3x3 l, float r) {
        return new M3x3() {
            m11 = l.m11*r,
            m21 = l.m21*r,
            m31 = l.m31*r,
            m12 = l.m12*r,
            m22 = l.m22*r,
            m32 = l.m32*r,
            m13 = l.m13*r,
            m23 = l.m23*r,
            m33 = l.m33*r,
        };
    }

    /// <summary>
    /// Unary negation of a matrix. Component-wise negation. Equivalent to multiplying the matrix by -1.
    /// </summary>
    public static M3x3 operator- (M3x3 l) {
        return new M3x3() {
            m11 = -l.m11,
            m21 = -l.m21,
            m31 = -l.m31,
            m12 = -l.m12,
            m22 = -l.m22,
            m32 = -l.m32,
            m13 = -l.m13,
            m23 = -l.m23,
            m33 = -l.m33,
        };
    }
    
    /// <summary>
    /// Identity matrix.
    /// </summary>
    public static readonly M3x3 IDENTITY = new M3x3() {
        m11 = 1.0f, m12 = 0.0f, m13 = 0.0f,
        m21 = 0.0f, m22 = 1.0f, m23 = 0.0f,
        m31 = 0.0f, m32 = 0.0f, m33 = 1.0f,
    };
}
