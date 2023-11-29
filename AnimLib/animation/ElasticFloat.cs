using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimLib;

/// <summary>
/// An implementation of an animated float value that returns to a set value at a set speed.
/// </summary>
public class ElasticFloat {
    /// <summary>
    /// Operating mode of the ElasticFloat.
    /// </summary>
    public enum Mode {
        /// <summary> Only ease when returning to target. </summary>
        EaseOut,
        /// <summary> Ease when setting and when returning. </summary>
        EaseInOut,
    }

    bool setting = false;

    float returnValue;
    float currentValue;
    float setValue;

    float speed;

    Mode mode;

    CancellationTokenSource src = new CancellationTokenSource();
    Task task;
    Action<float> setAction = (x) => {
    };

    /// <summary>
    /// The action called when the value is updated.
    /// </summary>
    public Action<float> Action {
        set {
            this.setAction = value;
        }
    }

    private float moveTowards(float a, float b, float speed) {
        var diff = b-a;
        float ret = MathF.Abs(diff) > speed ? a + MathF.Sign(diff) * speed : b;
        return ret;
    }

    /// <summary>
    /// Create a new ElasticFloat.
    /// </summary>
    /// <param name="setAction">The action called when the value is updated.</param>
    /// <param name="returnValue">The value to return to.</param>
    /// <param name="returnSpeed">The speed at which to return to the value.</param>
    /// <param name="mode">The mode of the ElasticFloat.</param>
    /// <returns></returns>
    public ElasticFloat(Action<float> setAction, float returnValue, float returnSpeed, Mode mode = Mode.EaseOut) {
        this.mode = mode;
        this.setAction = setAction;
        this.returnValue = returnValue;
        this.currentValue = returnValue;
        this.speed = returnSpeed;
        task = Animate.Update(() => {
                switch(mode) {
                    case Mode.EaseOut:
                        if(this.currentValue != this.returnValue) {
                            currentValue = moveTowards(this.currentValue, this.returnValue, speed*(float)Time.deltaT);
                            this.setAction(currentValue);
                        }
                        break;
                    case Mode.EaseInOut:
                        if(setting) {
                            if(this.currentValue != this.setValue) {
                                currentValue = moveTowards(this.currentValue, this.setValue, speed*(float)Time.deltaT);
                                this.setAction(currentValue);
                            } else {
                                setting = false;
                            }
                        } else {
                            if(this.currentValue != this.returnValue) {
                                currentValue = moveTowards(this.currentValue, this.returnValue, speed*(float)Time.deltaT);
                                this.setAction(currentValue);
                            }
                        }
                    break;
                }
            }, src.Token);
    }

    /// <summary>
    /// The current value of the ElasticFloat.
    /// </summary>
    public float Value {
        get {
            return currentValue;
        } set {
            if(mode == Mode.EaseOut) {
                currentValue = value;
            } else if(mode == Mode.EaseInOut) {
                setting = true;
                setValue = value;
            } else {
                throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// Setter to set the return speed of the ElasticFloat.
    /// </summary>
    public float ReturnSpeed {
        set {
            this.speed = value;
        }
    }

    /// <summary>
    /// Stop updating this ElasticFloat.
    /// </summary>
    public void Stop() {
        src.Cancel();
    }
}
