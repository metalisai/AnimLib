using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimLib;

/// <summary>
/// Animated color that can be set and returns to a default color
/// </summary>
public class ElasticColor {
    /// <summary>
    /// The mode of the elastic color.
    /// </summary>
    public enum Mode {
        /// <summary> Immediate change on set, returns elasticly </summary>
        EaseOut,
        /// <summary> Elasticly change on set, returns elasticly </summary>
        EaseInOut,
    }

    bool setting = false;

    Vector4 returnColor;
    Vector4 currentColor;
    Vector4 setColor;

    float speed;

    Mode mode;

    CancellationTokenSource src = new CancellationTokenSource();
    Task task;
    Action<Color> setAction = (x) => {
    };

    /// <summary>
    /// The action to be called when the color is changed.
    /// </summary>
    public Action<Color> Action {
        set {
            this.setAction = value;
        }
    }

    private Vector4 moveTowards(Vector4 a, Vector4 b, float speed) {
        var diff = b-a;
        Vector4 ret;
        ret.x = MathF.Abs(diff.x) > speed ? a.x + MathF.Sign(diff.x) * speed : b.x;
        ret.y = MathF.Abs(diff.y) > speed ? a.y + MathF.Sign(diff.y) * speed : b.y;
        ret.z = MathF.Abs(diff.z) > speed ? a.z + MathF.Sign(diff.z) * speed : b.z;
        ret.w = MathF.Abs(diff.w) > speed ? a.w + MathF.Sign(diff.w) * speed : b.w;
        return ret;
    }

    /// <summary>
    /// Create a new elastic color.
    /// </summary>
    /// <param name="setAction"> The action to set the color </param>
    /// <param name="returnColor"> The default color </param>
    /// <param name="returnSpeed"> The speed at which the color returns to the default color </param>
    /// <param name="mode"> The mode of the elastic color </param>
    public ElasticColor(Action<Color> setAction, Color returnColor, float returnSpeed, Mode mode = Mode.EaseOut) {
        this.mode = mode;
        this.setAction = setAction;
        this.returnColor = returnColor.ToVector4();
        this.currentColor = returnColor.ToVector4();
        this.speed = returnSpeed;
        task = Animate.Update(() => {
                switch(mode) {
                    case Mode.EaseOut:
                        if(this.currentColor != this.returnColor) {
                            currentColor = moveTowards(this.currentColor, this.returnColor, speed*(float)Time.deltaT);
                            this.setAction(new Color(currentColor));
                        }
                        break;
                    case Mode.EaseInOut:
                        if(setting) {
                            if(this.currentColor != this.setColor) {
                                currentColor = moveTowards(this.currentColor, this.setColor, speed*(float)Time.deltaT);
                                this.setAction(new Color(currentColor));
                            } else {
                                setting = false;
                            }
                        } else {
                            if(this.currentColor != this.returnColor) {
                                currentColor = moveTowards(this.currentColor, this.returnColor, speed*(float)Time.deltaT);
                                this.setAction(new Color(currentColor));
                            }
                        }
                    break;
                }
            }, src.Token);
    }

    /// <summary>
    /// The current color value.
    /// </summary>
    public Color Color {
        get {
            return new Color(currentColor);
        } set {
            if(mode == Mode.EaseOut) {
                currentColor = value.ToVector4();
            } else if(mode == Mode.EaseInOut) {
                setting = true;
                setColor = value.ToVector4();
            } else {
                throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// The speed at which the color returns to the default color.
    /// </summary>
    public float ReturnSpeed {
        set {
            this.speed = value;
        }
    }

    /// <summary>
    /// Stop being elastic.
    /// </summary>
    public void Stop() {
        src.Cancel();
    }
}
