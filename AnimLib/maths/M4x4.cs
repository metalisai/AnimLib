using System;

namespace AnimLib;

/// <summary>
/// A 4x4 matrix.
/// </summary>
public struct M4x4 {
    /// <summary>
    /// A component of the matrix.
    /// </summary>
    public float m11, m21, m31, m41,
    m12, m22, m32, m42,
    m13, m23, m33, m43,
    m14, m24, m34, m44;

    /// <summary>
    /// The top left 3x3 submatrix.
    /// </summary>
    public M3x3 M33 {
        get {
            var mat =  new M3x3() {
                m11 = this.m11, 
                m21 = this.m21,
                m31 = this.m31,
                m12 = this.m12,
                m22 = this.m22,
                m32 = this.m32,
                m13 = this.m13,
                m23 = this.m23,
                m33 = this.m33,
            };
            return mat;
        }
    }

    /// <summary>
    /// The inverse of the matrix. Assumes the source matrix represents a homogenous transform.
    /// </summary>
    public M4x4 InvertedHomogenous {
        get {
            // transpose rotation (inner 3x3)
            // multiply translation by negative transposed rotation
            var ret = new M4x4();
            var inner = this.M33.Inverted;
            ret.m11 = inner.m11;
            ret.m21 = inner.m21;
            ret.m31 = inner.m31;
            ret.m12 = inner.m12;
            ret.m22 = inner.m22;
            ret.m32 = inner.m32;
            ret.m13 = inner.m13;
            ret.m23 = inner.m23;
            ret.m33 = inner.m33;
            var t = -inner*new Vector3(this.m14, this.m24, this.m34);
            ret.m14 = t.x;
            ret.m24 = t.y;
            ret.m34 = t.z;
            ret.m44 = 1.0f;
            return ret;
        }
    }

    /// <summary>
    /// The matrix represented as a 2D array.
    /// </summary>
    public float[,] Array {
        get {
            var ret = new float[4,4];
            ret[0,0] = m11; ret[1,0] = m21; ret[2,0] = m31; ret[3,0] = m41;
            ret[0,1] = m12; ret[1,1] = m22; ret[2,1] = m32; ret[3,1] = m42;
            ret[0,2] = m13; ret[1,2] = m23; ret[2,2] = m33; ret[3,2] = m43;
            ret[0,3] = m14; ret[1,3] = m24; ret[2,3] = m34; ret[3,3] = m44;
            return ret;
        }
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public M4x4(ref M4x4 m) {
        m11 = m.m11; m12 = m.m12; m13 = m.m13; m14 = m.m14;
        m21 = m.m21; m22 = m.m22; m23 = m.m23; m24 = m.m24;
        m31 = m.m31; m32 = m.m32; m33 = m.m33; m34 = m.m34;
        m41 = m.m41; m42 = m.m42; m43 = m.m43; m44 = m.m44;
    }

    /// <summary>
    /// Identity matrix. Represents no transformation.
    /// </summary>
    public static readonly M4x4 IDENTITY = new M4x4() {
        m11 = 1.0f, m12 = 0.0f, m13 = 0.0f, m14 = 0.0f,
        m21 = 0.0f, m22 = 1.0f, m23 = 0.0f, m24 = 0.0f,
        m31 = 0.0f, m32 = 0.0f, m33 = 1.0f, m34 = 0.0f,
        m41 = 0.0f, m42 = 0.0f, m43 = 0.0f, m44 = 1.0f,
    };
    
    /// <summary>
    /// Convert the matrix to a string representation.
    /// </summary>
    public override string ToString() {
        return $"[\n{m11:N5} {m12:N5} {m13:N5} {m14:N5}\n{m21:N5} {m22:N5} {m23:N5} {m24:N5}\n{m31:N5} {m32:N5} {m33:N5} {m34:N5}\n{m41:N5} {m42:N5} {m43:N5} {m44:N5}\n]";
    }

    /// <summary>
    /// The 1-D array representation of the matrix. Column-major.
    /// </summary>
    public float[] ToArray() {
        return new float[16] {m11, m21, m31, m41, m12, m22, m32, m42, m13, m23, m33, m43, m14, m24, m34, m44};
    }

    /// <summary>
    /// Constructs a 3D perspective projection matrix.
    /// </summary>
    /// <param name="fov">The field of view in degrees.</param>
    /// <param name="aspectRatio">The aspect ratio of the viewport.</param>
    /// <param name="zNear">The near clipping plane.</param>
    /// <param name="zFar">The far clipping plane.</param>
    /// <returns>A 4x4 perspective projection matrix.</returns>
    public static M4x4 Perspective(float fov, float aspectRatio, float zNear, float zFar) {
        float tanHalfFOV = (float)Math.Tan((fov/360.0f)*Math.PI);
        float d = zNear-zFar;
        var ret = new M4x4() {
            m11 = 1.0f / (tanHalfFOV * aspectRatio),
            m22 = 1.0f / tanHalfFOV,
            m33 = (-zNear - zFar) / d,
            m43 = 1.0f,
            m34 = 2.0f * zFar * zNear / d,
        }; 
        return ret;
    }

    /// <summary>
    /// Constructs an inverse 3D perspective projection matrix.
    /// </summary>
    /// <param name="fov">The field of view in degrees.</param>
    /// <param name="aspectRatio">The aspect ratio of the viewport.</param>
    /// <param name="zNear">The near clipping plane.</param>
    /// <param name="zFar">The far clipping plane.</param>
    /// <returns>A 4x4 inverse perspective projection matrix.</returns>
    /// <seealso cref="Perspective"/>
    public static M4x4 InvPerspective(float fov, float aspectRatio, float zNear, float zFar) {
        float tanHalfFOV = (float)Math.Tan((fov/360.0f)*Math.PI);
        float d = zNear-zFar;
        var ret = new M4x4() {
            m11 = tanHalfFOV * aspectRatio,
            m22 = tanHalfFOV,
            m43 = d / (2.0f * zFar * zNear),
            m34 = 1.0f,
            m44 = ((zNear + zFar) / d) / (2.0f * zFar * zNear / d),
        }; 
        return ret;
    }

    /// <summary>
    /// Constructs an inverse 3D orthographic projection matrix.
    /// </summary>
    /// <param name="l">The left clipping plane.</param>
    /// <param name="r">The right clipping plane.</param>
    /// <param name="t">The top clipping plane.</param>
    /// <param name="b">The bottom clipping plane.</param>
    /// <param name="f">The far clipping plane.</param>
    /// <param name="n">The near clipping plane.</param>
    /// <returns>A 4x4 inverse orthographic projection matrix.</returns>
    public static M4x4 InvOrtho(float l, float r, float t, float b, float f, float n) {
        var ret = new M4x4() {
            m11 = (r-l)/2.0f,
            m21 = 0.0f,
            m31 = 0.0f,
            m41 = 0.0f,
            m12 = 0.0f,
            m22 = (t-b)/2.0f,
            m32 = 0.0f,
            m42 = 0.0f,
            m13 = 0.0f,
            m23 = 0.0f,
            m33 = -(f-n)/2.0f,
            m43 = 0.0f,
            m14 = (r+l)/2,
            m24 = (t+b)/2,
            m34 = -(f+n)/2,
            m44 = 1.0f,
        };
        return ret;
    }

    /// <summary>
    /// Constructs a 3D orthographic projection matrix.
    /// </summary>
    /// <param name="l">The left clipping plane.</param>
    /// <param name="r">The right clipping plane.</param>
    /// <param name="t">The top clipping plane.</param>
    /// <param name="b">The bottom clipping plane.</param>
    /// <param name="f">The far clipping plane.</param>
    /// <param name="n">The near clipping plane.</param>
    /// <returns>A 4x4 orthographic projection matrix.</returns>
    public static M4x4 Ortho(float l, float r, float t, float b, float f, float n) {
        var ret = new M4x4() {
            m11 = 2.0f / (r-l),
            m21 = 0.0f,
            m31 = 0.0f,
            m41 = 0.0f,
            m12 = 0.0f,
            m22 = 2.0f / (t-b),
            m32 = 0.0f,
            m42 = 0.0f,
            m13 = 0.0f,
            m23 = 0.0f,
            m33 = -2.0f / (f-n),
            m43 = 0.0f,
            m14 = -(r+l)/(r-l),
            m24 = -(t+b)/(t-b),
            m34 = -(f+n)/(f-n),
            m44 = 1.0f,
        };
        return ret;
    }

    /// <summary>
    /// Constructs a homogeneous 3D rotation matrix from a quaternion.
    /// </summary>
    /// <param name="r">The quaternion to convert.</param>
    /// <returns>A 4x4 rotation matrix.</returns>
    public static M4x4 Rotate(Quaternion r) {
        var ret = new M4x4() {
            m11 = 1.0f - 2.0f*r.y*r.y - 2.0f*r.z*r.z,
            m21 = 2.0f*r.x*r.y + 2.0f*r.z*r.w,
            m31 = 2.0f*r.x*r.z - 2.0f*r.y*r.w,
            m41 = 0.0f,
            m12 = 2.0f*r.x*r.y - 2.0f*r.z*r.w,
            m22 = 1.0f - 2.0f*r.x*r.x - 2.0f*r.z*r.z,
            m32 = 2.0f*r.y*r.z + 2.0f*r.x*r.w,
            m42 = 0.0f,
            m13 = 2.0f*r.x*r.z + 2.0f*r.y*r.w,
            m23 = 2.0f*r.y*r.z - 2.0f*r.x*r.w,
            m33 = 1.0f - 2.0f*r.x*r.x - 2.0f*r.y*r.y,
            m43 = 0.0f,
            m14 = 0.0f,
            m24 = 0.0f,
            m34 = 0.0f,
            m44 = 1.0f,
        };
        return ret;
    }

    /// <summary>
    /// Transpose this matrix.
    /// </summary>
    public void Transpose() {
        var mat = new M4x4(ref this);
        m11 = mat.m11; m12 = mat.m21; m13 = mat.m31; m14 = mat.m41;
        m21 = mat.m12; m22 = mat.m22; m23 = mat.m32; m24 = mat.m42;
        m31 = mat.m13; m32 = mat.m23; m33 = mat.m33; m34 = mat.m43;
        m41 = mat.m14; m42 = mat.m24; m43 = mat.m34; m44 = mat.m44;
    }

    /// <summary>
    /// Constructs a homogeneous 3D translation matrix.
    /// </summary>
    /// <param name="t">The translation vector.</param>
    /// <returns>A 4x4 translation matrix.</returns>
    public static M4x4 Translate(Vector3 t) {
        var ret = new M4x4() {
            m11 = 1.0f,
            m21 = 0.0f,
            m31 = 0.0f,
            m41 = 0.0f,
            m12 = 0.0f,
            m22 = 1.0f,
            m32 = 0.0f,
            m42 = 0.0f,
            m13 = 0.0f,
            m23 = 0.0f,
            m33 = 1.0f,
            m43 = 0.0f,
            m14 = t.x,
            m24 = t.y,
            m34 = t.z,
            m44 = 1.0f,
        };
        return ret;
    }

    /// <summary>
    /// Constructs a homogeneous 3D scale matrix.
    /// </summary>
    /// <param name="s">The scale vector.</param>
    /// <returns>A 4x4 scale matrix.</returns>
    public static M4x4 Scale(Vector3 s) {
        var ret = new M4x4() {
            m11 = s.x,
            m22 = s.y,
            m33 = s.z,
            m44 = 1.0f
        };
        return ret;
    }

    /// <summary>
    /// Inner product of two matrices. Represents the transformation of the first matrix followed by the transformation of the second matrix.
    /// </summary>
    /// <param name="l">The first matrix.</param>
    /// <param name="r">The second matrix.</param>
    /// <returns>A 4x4 matrix representing the transformation of the first matrix followed by the transformation of the second matrix.</returns>
    public static M4x4 operator* (M4x4 l, M4x4 r) {
        return new M4x4() {
            m11 = l.m11 * r.m11 + l.m12 * r.m21 + l.m13 * r.m31 + l.m14 * r.m41,
            m21 = l.m21 * r.m11 + l.m22 * r.m21 + l.m23 * r.m31 + l.m24 * r.m41,
            m31 = l.m31 * r.m11 + l.m32 * r.m21 + l.m33 * r.m31 + l.m34 * r.m41,
            m41 = l.m41 * r.m11 + l.m42 * r.m21 + l.m43 * r.m31 + l.m44 * r.m41,
            m12 = l.m11 * r.m12 + l.m12 * r.m22 + l.m13 * r.m32 + l.m14 * r.m42,
            m22 = l.m21 * r.m12 + l.m22 * r.m22 + l.m23 * r.m32 + l.m24 * r.m42,
            m32 = l.m31 * r.m12 + l.m32 * r.m22 + l.m33 * r.m32 + l.m34 * r.m42,
            m42 = l.m41 * r.m12 + l.m42 * r.m22 + l.m43 * r.m32 + l.m44 * r.m42,
            m13 = l.m11 * r.m13 + l.m12 * r.m23 + l.m13 * r.m33 + l.m14 * r.m43,
            m23 = l.m21 * r.m13 + l.m22 * r.m23 + l.m23 * r.m33 + l.m24 * r.m43,
            m33 = l.m31 * r.m13 + l.m32 * r.m23 + l.m33 * r.m33 + l.m34 * r.m43,
            m43 = l.m41 * r.m13 + l.m42 * r.m23 + l.m43 * r.m33 + l.m44 * r.m43,
            m14 = l.m11 * r.m14 + l.m12 * r.m24 + l.m13 * r.m34 + l.m14 * r.m44,
            m24 = l.m21 * r.m14 + l.m22 * r.m24 + l.m23 * r.m34 + l.m24 * r.m44,
            m34 = l.m31 * r.m14 + l.m32 * r.m24 + l.m33 * r.m34 + l.m34 * r.m44,
            m44 = l.m41 * r.m14 + l.m42 * r.m24 + l.m43 * r.m34 + l.m44 * r.m44,
        };
    }

    /// <summary>
    /// Constructs a homogeneous 3D transformation from a quaternion and a translation vector.
    /// </summary>
    /// <param name="r">The quaternion representing the rotation.</param>
    /// <param name="t">The translation vector.</param>
    /// <returns>A 4x4 matrix representing a rotation followed by a translation.</returns>
    public static M4x4 RT(Quaternion r, Vector3 t) {
        // TODO: optimize
        var tm = M4x4.Translate(t);
        var rm = M4x4.Rotate(r);
        return rm*tm;
    }

    /// <summary>
    /// Constructs a homogenous 3D transformation from a translation vector and a scale vector.
    /// </summary>
    /// <param name="t">The translation vector.</param>
    /// <param name="s">The scale vector.</param>
    /// <returns>A 4x4 matrix representing a translation followed by a scale.</returns>
    public static M4x4 TS(Vector3 t, Vector3 s) {
        // TODO: optimize
        var tm = M4x4.Translate(t);
        var sm = M4x4.Scale(s);
        return tm*sm;
    }

    /// <summary>
    /// Constructs a homogenous 3D transformation from a translation vector, a quaternion, and a scale vector.
    /// </summary>
    /// <param name="t">The translation vector.</param>
    /// <param name="r">The quaternion representing the rotation.</param>
    /// <param name="s">The scale vector.</param>
    /// <returns>A 4x4 matrix representing a translation followed by a rotation followed by a scale.</returns>
    public static M4x4 TRS(Vector3 t, Quaternion r, Vector3 s) {
        return new M4x4() {
            // TODO: rearrange to make compiler's life easier (write order)
            m11 = (1.0f-2.0f*(r.y*r.y+r.z*r.z))*s.x,
            m12 = (r.x*r.y-r.z*r.w)*s.y*2.0f,
            m13 = (r.x*r.z+r.y*r.w)*s.z*2.0f,
            m14 = t.x,
            m21 = (r.x*r.y+r.z*r.w)*s.x*2.0f,
            m22 = (1.0f-2.0f*(r.x*r.x+r.z*r.z))*s.y,
            m23 = (r.y*r.z-r.x*r.w)*s.z*2.0f,
            m24 = t.y,
            m31 = (r.x*r.z-r.y*r.w)*s.z*2.0f,
            m32 = (r.y*r.z+r.x*r.w)*s.y*2.0f,
            m33 = (1.0f-2.0f*(r.x*r.x+r.y*r.y))*s.z,
            m34 = t.z,
            m41 = 0.0f,
            m42 = 0.0f,
            m43 = 0.0f,
            m44 = 1.0f,
        };
    }

    /// <summary>
    /// Constructs a new 4x4 matrix from a list of column vectors.
    /// </summary>
    public static M4x4 FromColumns(Vector4 c1, Vector4 c2, Vector4 c3, Vector4 c4) {
        M4x4 ret;
        ret.m11 = c1.x; ret.m21 = c1.y; ret.m31 = c1.z; ret.m41 = c1.w;
        ret.m12 = c2.x; ret.m22 = c2.y; ret.m32 = c2.z; ret.m42 = c2.w;
        ret.m13 = c3.x; ret.m23 = c3.y; ret.m33 = c3.z; ret.m43 = c3.w;
        ret.m14 = c4.x; ret.m24 = c4.y; ret.m34 = c4.z; ret.m44 = c4.w;
        return ret;
    }

    /// <summary>
    /// Multies a vector by a matrix. Applies the transformation represented by the matrix to the vector.
    /// </summary>
    public static Vector4 operator* (M4x4 m, Vector4 v) {
        var ret = new Vector4() {
            x = v.x*m.m11 + v.y*m.m12 + v.z*m.m13 + v.w*m.m14,
            y = v.x*m.m21 + v.y*m.m22 + v.z*m.m23 + v.w*m.m24,
            z = v.x*m.m31 + v.y*m.m32 + v.z*m.m33 + v.w*m.m34,
            w = v.x*m.m41 + v.y*m.m42 + v.z*m.m43 + v.w*m.m44
        };
        return ret;
    }
}
