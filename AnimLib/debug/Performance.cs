using System;
using System.Collections.Generic;

namespace AnimLib;

internal static class Performance {
    internal class Call : IDisposable {

        public string Name;
        public long Time;

        internal Call firstChild;
        internal Call nextSibling;

        public Call(string name) {
#if DEBUG
            Name = name;
            Time = System.Diagnostics.Stopwatch.GetTimestamp();
            Performance.BeginBlock(this);
#endif
        }

        public void Dispose()
        {
#if DEBUG
            Performance.EndBlock();
#endif
        }
    }

    public static double TimeToRenderCanvases = 0.0;
    public static int views = 0;
    public static double TimeToRenderViews = 0.0;
    public static double TimeToProcessFrame = 0.0;
    public static double TimeToWaitSync = 0.0;
    public static double TimeToBake = 0.0;
    public static int CommandCount = 0;
    public static int CachedGlyphPaths = 0;

    static Call root = null;

    static Stack<Call> CallStack = new();

    public static Call lastRoot = null;

    internal static void BeginBlock(Call call) {
        if (CallStack.Count > 0) {
            var parent = CallStack.Peek();
            if (parent.firstChild == null) {
                parent.firstChild = call;
            } else {
                var sibling = parent.firstChild;
                while (sibling.nextSibling != null) {
                    sibling = sibling.nextSibling;
                }
                sibling.nextSibling = call;
            }
        }
        CallStack.Push(call);
    }

    internal static void EndBlock() {
        var call = CallStack.Pop();
        var time = System.Diagnostics.Stopwatch.GetTimestamp() - call.Time;
        call.Time = time;
    }

    public static void BeginFrame() {
        CallStack.Clear();
        var root = new Call("Root");
        CallStack.Push(root);
        Performance.root = root;
    }

    public static void EndFrame() {
        EndBlock();
        lastRoot = root;
    }

}
