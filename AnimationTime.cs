using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace AnimLib
{
    public class AnimationTime 
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
        public static void NewFrame(double dt) {
            _currentTime += dt;

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

            _currentFrame++;
        }
        public static Task WaitFrame() {
            var tcs = new TaskCompletionSource<bool>();
            var task = new FrameTask() { TCS = tcs, StartFrame = _currentFrame};
            //Console.WriteLine($"Created new FrameTask {task.GUID}, thread {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            _waitFrameTasks.Add(task);
            return tcs.Task;
        }
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
        public static void Reset() {
            _currentFrame = 0;
            _currentTime = 0.0;
        }
        static double _currentTime;
        static long _currentFrame;

        public static double Time {
            get {
                return _currentTime;
            }
        }

        static List<FrameTask> _waitFrameTasks = new List<FrameTask>();
        static List<WaitTask> _waitTasks = new List<WaitTask>();
    }
}
