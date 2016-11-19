using System;
using Android.Views;

namespace xam.rebound.android
{
    public class XFrameCallBack : Java.Lang.Object, Choreographer.IFrameCallback
    {
        public Action<long> doFrame { get; set; }
        public void DoFrame(long frameTimeNanos)
        {
            doFrame?.Invoke(frameTimeNanos);
        }
    }
}