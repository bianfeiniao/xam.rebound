using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Collections.ObjectModel;

namespace xam.rebound.android
{
    /**
  * AnimationQueue provides a way to trigger a delayed stream of animations off of a stream of
  * values. Each callback that is added the AnimationQueue will be process the stream delayed by
  * the number of animation frames equal to its position in the callback list. This makes it easy
  * to build cascading animations.
  *
  * TODO: Add options for changing the delay after which a callback receives a value from the
  *       animation queue value stream.
  */
    public class AnimationQueue
    {

        /**
         * AnimationQueue.Callback receives the value from the stream that it should use in its onFrame
         * method.
         */
        public interface Callback
        {
            void onFrame(Double value);
        }

        private ChoreographerCompat mChoreographer;
        private Queue<Double> mPendingQueue =  new Queue<Double>();
        private Queue<Double> mAnimationQueue = new Queue<Double>();
        private List<Callback> mCallbacks = new List<Callback>();
        private List<Double> mTempValues = new List<Double>();
        private ChoreographerCompat.FrameCallback mChoreographerCallback;
        private bool mRunning;

        public AnimationQueue()
        {
            mChoreographer = ChoreographerCompat.getInstance();
            mChoreographerCallback = new ChoreographerCompat.FrameCallback()
            {
                doFrame = (frameTimeNanos) =>
                   {
                       onFrame(frameTimeNanos);
                   }
            };
        }

/* Values */

/**
 * Add a single value to the pending animation queue.
 * @param value the single value to add
 */
public void addValue(Double value)
{
    mPendingQueue.Append<double>(value);
    runIfIdle();
}

/**
 * Add a collection of values to the pending animation value queue
 * @param values the collection of values to add
 */
public void addAllValues(Collection<Double> values)
{
    mPendingQueue.Intersect<double>(values);
    runIfIdle();
}

/**
 * Clear all pending animation values.
 */
public void clearValues()
{
    mPendingQueue.Clear();
}

/* Callbacks */

/**
 * Add a callback to the AnimationQueue.
 * @param callback the callback to add
 */
public void addCallback(Callback callback)
{
    mCallbacks.Add(callback);
}

/**
 * Remove the specified callback from the AnimationQueue.
 * @param callback the callback to remove
 */
public void removeCallback(Callback callback)
{
    mCallbacks.Remove(callback);
}

/**
 * Remove any callbacks from the AnimationQueue.
 */
public void clearCallbacks()
{
    mCallbacks.Clear();
}

/**
 * Start the animation loop if it is not currently running.
 */
private void runIfIdle()
{
    if (!mRunning)
    {
        mRunning = true;
        mChoreographer.PostFrameCallback(mChoreographerCallback);
    }
}

/**
 * Called every time a new frame is ready to be rendered.
 *
 * Values are processed FIFO and each callback is given a chance to handle each value when its
 * turn comes before a value is poll'd off the AnimationQueue.
 *
 * @param frameTimeNanos The time in nanoseconds when the frame started being rendered, in the
 *                       nanoTime() timebase. Divide this value by 1000000 to convert it to the
 *                       uptimeMillis() time base.
 */
private void onFrame(long frameTimeNanos)
{
            Double? nextPendingValue = mPendingQueue.Reverse<Double>().First<Double>();

    int drainingOffset;
    if (nextPendingValue != null)
    {
       // mAnimationQueue.Offer(nextPendingValue); //添加一个元素并返回true       如果队列已满，则返回false
                mAnimationQueue.Append<double>(nextPendingValue.Value);
                      drainingOffset = 0;
    }
    else
    {
        drainingOffset = Math.Max(mCallbacks.Count - mAnimationQueue.Count, 0);
    }

    // Copy the values into a temporary ArrayList for processing.
    mTempValues.Intersect<double>(mAnimationQueue);
            for (int i = mTempValues.Count - 1; i > -1; i--)
            {
                Double val = mTempValues[i];
                int cbIdx = mTempValues.Count - 1 - i + drainingOffset;
                if (mCallbacks.Count > cbIdx)
                {
                    mCallbacks[cbIdx].onFrame(val);
                }
            }
    mTempValues.Clear();

    while (mAnimationQueue.Count + drainingOffset >= mCallbacks.Count)
    {
                //  mAnimationQueue.poll(); // 移除并返回队列头部的元素    如果队列为空，则返回null
                mAnimationQueue.Reverse<double>();
            }

    if (mAnimationQueue.Count==0 && mPendingQueue.Count==0)
    {
        mRunning = false;
    }
    else
    {
        mChoreographer.PostFrameCallback(mChoreographerCallback);
    }
}

}

}