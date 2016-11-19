using Android.OS;
using Android.Views;
using Java.Lang;
using xam.rebound.core;

namespace xam.rebound.android
{
    /**
     * Android version of the spring looper that uses the most appropriate frame callback mechanism
     * available. It uses Android's {@link Choreographer} when available, otherwise it uses a
     * {@link Handler}.
     */
    abstract class AndroidSpringLooperFactory
    {

        /**
         * Create an Android {@link com.facebook.rebound.SpringLooper} for the detected Android platform.
         * @return a SpringLooper
         */
        public static SpringLooper createSpringLooper()
        {
            if (Build.VERSION.SdkInt >= Build.VERSION_CODES.JellyBean)
            {
                return ChoreographerAndroidSpringLooper.create();
            }
            else
            {
                return LegacyAndroidSpringLooper.create();
            }
        }

        /**
         * The base implementation of the Android spring looper, using a {@link Handler} for the
         * frame callbacks.
         */
        public class LegacyAndroidSpringLooper : SpringLooper
        {

            private Handler mHandler;
            private Runnable mLooperRunnable;
            private bool mStarted;
            private long mLastTime;

            /**
             * @return an Android spring looper using a new {@link Handler} instance
             */
            public static SpringLooper create()
            {
                return new LegacyAndroidSpringLooper(new Handler());
            }

            public LegacyAndroidSpringLooper(Handler handler)
            {
                mHandler = handler;
                mLooperRunnable = new Runnable(() =>
                {
                    if (!mStarted || mSpringSystem == null)
                    {
                        return;
                    }
                    long currentTime = SystemClock.UptimeMillis();
                    mSpringSystem.loop(currentTime - mLastTime);
                    mLastTime = currentTime;
                    mHandler.Post(mLooperRunnable);
                });
            }

            ////@Override
            public override void start()
            {
                if (mStarted)
                {
                    return;
                }
                mStarted = true;
                mLastTime = SystemClock.UptimeMillis();
                mHandler.RemoveCallbacks(mLooperRunnable);
                mHandler.Post(mLooperRunnable);
            }

            ////@Override
            public override void stop()
            {
                mStarted = false;
                mHandler.RemoveCallbacks(mLooperRunnable);
            }
        }

        /**
         * The Jelly Bean and up implementation of the spring looper that uses Android's
         * {@link Choreographer} instead of a {@link Handler}
         */
        // // @TargetApi(Build.VERSION_CODES.JELLY_BEAN)
        public class ChoreographerAndroidSpringLooper : SpringLooper
        {

            private Choreographer mChoreographer;
            private Choreographer.IFrameCallback mFrameCallback;
            private bool mStarted;
            private long mLastTime;

            /**
             * @return an Android spring choreographer using the system {@link Choreographer}
             */
            public static ChoreographerAndroidSpringLooper create()
            {
                return new ChoreographerAndroidSpringLooper(Choreographer.Instance);
            }

            public ChoreographerAndroidSpringLooper(Choreographer choreographer)
            {
                mChoreographer = choreographer;
                mFrameCallback = new XFrameCallBack()
                {
                    doFrame = (frameTimeNanos) =>
                    {
                        if (!mStarted || mSpringSystem == null)
                        {
                            return;
                        }
                        long currentTime = SystemClock.UptimeMillis();
                        mSpringSystem.loop(currentTime - mLastTime);
                        mLastTime = currentTime;
                        mChoreographer.PostFrameCallback(mFrameCallback);
                    }
                };
            }

            ////@Override
            public override void start()
            {
                if (mStarted)
                {
                    return;
                }
                mStarted = true;
                mLastTime = SystemClock.UptimeMillis();
                mChoreographer.RemoveFrameCallback(mFrameCallback);
                mChoreographer.PostFrameCallback(mFrameCallback);
            }

            ////@Override
            public override void stop()
            {
                mStarted = false;
                mChoreographer.RemoveFrameCallback(mFrameCallback);
            }
        }
    }
}