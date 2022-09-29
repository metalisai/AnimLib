using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimLib;
public class ElasticFloat {
    public enum Mode {
        EaseOut,
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

    private float moveTowards(float a, float b, float speed) {
        var diff = b-a;
        float ret = MathF.Abs(diff) > speed ? a + MathF.Sign(diff) * speed : b;
        return ret;
    }

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
                            setAction(currentValue);
                        }
                        break;
                    case Mode.EaseInOut:
                        if(setting) {
                            if(this.currentValue != this.setValue) {
                                currentValue = moveTowards(this.currentValue, this.setValue, speed*(float)Time.deltaT);
                                setAction(currentValue);
                            } else {
                                setting = false;
                            }
                        } else {
                            if(this.currentValue != this.returnValue) {
                                currentValue = moveTowards(this.currentValue, this.returnValue, speed*(float)Time.deltaT);
                                setAction(currentValue);
                            }
                        }
                    break;
                }
            }, src.Token);
    }

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

    public float ReturnSpeed {
        set {
            this.speed = value;
        }
    }

    public void Stop() {
        src.Cancel();
    }
}
