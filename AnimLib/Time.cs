using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace AnimLib;

public class Time 
{
    private class WaitTask {
        public TaskCompletionSource<bool> TaskCompletion;
        public double EndTime;
    }

    private class FrameTask {
        public TaskCompletionSource<bool> TCS;
        public long StartFrame;
        public string GUID = Guid.NewGuid().ToString();
    }

    internal static void NewFrame(double dt) {
        _currentTime += dt;
        _dt = dt;

        // NOTE: items can be added to the list when SetResult is called!
        for(int i = _waitFrameTasks.Count-1; i >= 0; i--) {
            var t = _waitFrameTasks[i];
            if(_currentFrame > t.StartFrame) {
                t.TCS.SetResult(true);
                //Console.WriteLine($"FrameTask completed {t.GUID}, thread {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                _waitFrameTasks.RemoveAt(i);
            }
        }

        for(int i = _waitTasks.Count-1; i >= 0; i--) {
            var t = _waitTasks[i];
            if(_currentTime >= t.EndTime) {
                t.TaskCompletion.SetResult(true);
                _waitTasks.RemoveAt(i);
            }
        }

        _dt = double.NaN;
        _currentFrame++;
    }

    /// <summary>
    /// Wait for the next frame to start.
    /// </summary>
    public static Task WaitFrame() {
        var tcs = new TaskCompletionSource<bool>();
        var task = new FrameTask() { TCS = tcs, StartFrame = _currentFrame};
        //Console.WriteLine($"Created new FrameTask {task.GUID}, thread {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        _waitFrameTasks.Add(task);
        return tcs.Task;
    }

    /// <summary>
    /// Wait for specified number of seconds.
    /// </summary>
    public static Task WaitSeconds(double seconds) {
        var tcs = new TaskCompletionSource<bool>();
        var task = new WaitTask() {
            TaskCompletion = tcs,
            EndTime = _currentTime + seconds,
        };
        if(_currentTime >= task.EndTime) {
            tcs.SetResult(true);
        }
        if(!task.TaskCompletion.Task.IsCompleted) {
            _waitTasks.Add(task);
        }
        return tcs.Task;
    }

    internal static void Reset() {
        _currentFrame = 0;
        _currentTime = 0.0;
        _waitFrameTasks.Clear();
        _waitTasks.Clear();
    }

    static double _dt;
    static double _currentTime;
    static long _currentFrame;

    /// <summary>
    /// Current time in seconds.
    /// </summary>
    public static double T {
        get {
            return _currentTime;
        }
    }

    /// <summary>
    /// Delta time in seconds. Used within animation tasks, invalid outside.
    /// </summary>
    public static double deltaT {
        get {
            if(double.IsNaN(_dt)) {
                Debug.Error("Time.deltaT is not valid outside animation tasks");
            }
            return _dt;
        }
    }

    static List<FrameTask> _waitFrameTasks = new List<FrameTask>();
    static List<WaitTask> _waitTasks = new List<WaitTask>();
}
