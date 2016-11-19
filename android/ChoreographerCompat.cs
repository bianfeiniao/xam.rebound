using System;
using Android.OS;
using Android.Views;
using Java.Lang;

namespace xam.rebound.android
{
    /**
  * Wrapper class for abstracting away availability of the JellyBean Choreographer. If Choreographer
  * is unavailable we fallback to using a normal Handler.
  */
    public class ChoreographerCompat
    {

        private static long ONE_FRAME_MILLIS = 17;
        private static bool IS_JELLYBEAN_OR_HIGHER = Build.VERSION.SdkInt >= Build.VERSION_CODES.JellyBean;
        private static ChoreographerCompat INSTANCE = new ChoreographerCompat();

        private Handler mHandler;
        private Choreographer mChoreographer;

        public static ChoreographerCompat getInstance()
        {
            return INSTANCE;
        }

        private ChoreographerCompat()
        {
            if (IS_JELLYBEAN_OR_HIGHER)
            {
                mChoreographer = getChoreographer();
            }
            else
            {
                mHandler = new Handler(Looper.MainLooper);
            }
        }

        public void PostFrameCallback(FrameCallback callbackWrapper)
        {
            if (IS_JELLYBEAN_OR_HIGHER)
            {
                choreographerPostFrameCallback(callbackWrapper.getFrameCallback());
            }
            else
            {
                mHandler.PostDelayed(callbackWrapper.getRunnable(), 0);
            }
        }

        public void postFrameCallbackDelayed(FrameCallback callbackWrapper, long delayMillis)
        {
            if (IS_JELLYBEAN_OR_HIGHER)
            {
                choreographerPostFrameCallbackDelayed(callbackWrapper.getFrameCallback(), delayMillis);
            }
            else
            {
                mHandler.PostDelayed(callbackWrapper.getRunnable(), delayMillis + ONE_FRAME_MILLIS);
            }
        }

        public void removeFrameCallback(FrameCallback callbackWrapper)
        {
            if (IS_JELLYBEAN_OR_HIGHER)
            {
                choreographerRemoveFrameCallback(callbackWrapper.getFrameCallback());
            }
            else
            {
                mHandler.RemoveCallbacks(callbackWrapper.getRunnable());
            }
        }

        // // @TargetApi(Build.VERSION_CODES.JELLY_BEAN)
        private Choreographer getChoreographer()
        {
            return Choreographer.Instance;
        }

        // // @TargetApi(Build.VERSION_CODES.JELLY_BEAN)
        private void choreographerPostFrameCallback(Choreographer.IFrameCallback frameCallback)
        {
            mChoreographer.PostFrameCallback(frameCallback);
        }

        // // @TargetApi(Build.VERSION_CODES.JELLY_BEAN)
        private void choreographerPostFrameCallbackDelayed(
        Choreographer.IFrameCallback frameCallback,
        long delayMillis)
        {
            mChoreographer.PostFrameCallbackDelayed(frameCallback, delayMillis);
        }

        // // @TargetApi(Build.VERSION_CODES.JELLY_BEAN)
        private void choreographerRemoveFrameCallback(Choreographer.IFrameCallback frameCallback)
        {
            mChoreographer.RemoveFrameCallback(frameCallback);
        }

        /**
         * This class provides a compatibility wrapper around the JellyBean FrameCallback with methods
         * to access cached wrappers for submitting a real FrameCallback to a Choreographer or a Runnable
         * to a Handler.
         */
        public class FrameCallback
        {
            private Runnable mRunnable;
            private Choreographer.IFrameCallback mFrameCallback;

            public Action<long> doFrame { get; set; }

            public Choreographer.IFrameCallback getFrameCallback()
            {
                if (mFrameCallback == null)
                {
                    mFrameCallback = new XFrameCallBack()
                    {
                        doFrame = (frameTimeNanos) =>
                           {
                               doFrame?.Invoke(frameTimeNanos);
                           }
                    };
                }
                return mFrameCallback;
            }

            public Runnable getRunnable()
            {
                if (mRunnable == null)
                {
                    mRunnable = new Runnable(() =>
                    {
                        doFrame(DateTime.Now.Ticks);
                    });
                }
                return mRunnable;
            }
        }
    }

}