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
using Java.Lang;

namespace xam.rebound.core
{

    /**
     * Classical spring implementing Hooke's law with configurable friction and tension.
     */
    public class Spring
    {
        // unique incrementer id for springs
        private static int ID = 0;

        // maximum amount of time to simulate per physics iteration in seconds (4 frames at 60 FPS)
        private static double MAX_DELTA_TIME_SEC = 0.064;
        // fixed timestep to use in the physics solver in seconds
        private static double SOLVER_TIMESTEP_SEC = 0.001;
        private SpringConfig mSpringConfig;
        private bool mOvershootClampingEnabled;


        // unique id for the spring in the system
        private string mId;
  // all physics simulation objects are and reused in each processing pass
  private PhysicsState mCurrentState = new PhysicsState();
        private PhysicsState mPreviousState = new PhysicsState();
        private PhysicsState mTempState = new PhysicsState();
        private double mStartValue;
        private double mEndValue;
        private bool mWasAtRest = true;
        // thresholds for determining when the spring is at rest
        private double mRestSpeedThreshold = 0.005;
        private double mDisplacementFromRestThreshold = 0.005;
        private double mTimeAccumulator = 0;
        
        private HashSet<SpringListener> mListeners =new HashSet<SpringListener>();

        private BaseSpringSystem mSpringSystem;

  /**
   * create a new spring
   */
  public Spring(BaseSpringSystem springSystem)
        {
            if (springSystem == null)
            {
                throw new IllegalArgumentException("Spring cannot be created outside of a BaseSpringSystem");
            }
            mSpringSystem = springSystem;
            mId = "spring:" + ID++;
            setSpringConfig(SpringConfig.defaultConfig);
        }

        /**
         * Destroys this Spring, meaning that it will be deregistered from its BaseSpringSystem so it won't be
         * iterated anymore and will clear its set of listeners. Do not use the Spring after calling this,
         * doing so may just cause an exception to be thrown.
         */
        public void destroy()
        {
            mListeners.Clear();
            mSpringSystem.deregisterSpring(this);
        }

        /**
         * get the unique id for this spring
         * @return the unique id
         */
        public string getId()
        {
            return mId;
        }

        /**
         * set the config class
         * @param springConfig config class for the spring
         * @return this Spring instance for chaining
         */
        public Spring setSpringConfig(SpringConfig springConfig)
        {
            if (springConfig == null)
            {
                throw new IllegalArgumentException("springConfig is required");
            }
            mSpringConfig = springConfig;
            return this;
        }

        /**
         * retrieve the spring config for this spring
         * @return the SpringConfig applied to this spring
         */
        public SpringConfig getSpringConfig()
        {
            return mSpringConfig;
        }

        /**
         * Set the displaced value to determine the displacement for the spring from the rest value.
         * This value is retained and used to calculate the displacement ratio.
         * The default signature also sets the Spring at rest to facilitate the common behavior of moving
         * a spring to a new position.
         * @param currentValue the new start and current value for the spring
         * @return the spring for chaining
         */
        public Spring setCurrentValue(double currentValue)
        {
            return setCurrentValue(currentValue, true);
        }

        /**
         * The full signature for setCurrentValue includes the option of not setting the spring at rest
         * after updating its currentValue. Passing setAtRest false means that if the endValue of the
         * spring is not equal to the currentValue, the physics system will start iterating to resolve
         * the spring to the end value. This is almost never the behavior that you want, so the default
         * setCurrentValue signature passes true.
         * @param currentValue the new start and current value for the spring
         * @param setAtRest optionally set the spring at rest after updating its current value.
         *                  see {@link com.facebook.rebound.Spring#set_AtRest()}
         * @return the spring for chaining
         */
        public Spring setCurrentValue(double currentValue, bool setAtRest)
        {
            mStartValue = currentValue;
            mCurrentState.position = currentValue;
            mSpringSystem.activateSpring(this.getId());
            foreach (SpringListener listener in mListeners)
            {
                listener.onSpringUpdate(this);
            }
            if (setAtRest)
            {
                set_AtRest();
            }
            return this;
        }

        /**
         * Get the displacement value from the last time setCurrentValue was called.
         * @return displacement value
         */
        public double getStartValue()
        {
            return mStartValue;
        }

        /**
         * Get the current
         * @return current value
         */
        public double getCurrentValue()
        {
            return mCurrentState.position;
        }

        /**
         * get the displacement of the springs current value from its rest value.
         * @return the distance displaced by
         */
        public double getCurrentDisplacementDistance()
        {
            return getDisplacementDistanceForState(mCurrentState);
        }

        /**
         * get the displacement from rest for a given physics state
         * @param state the state to measure from
         * @return the distance displaced by
         */
        private double getDisplacementDistanceForState(PhysicsState state)
        {
            return Java.Lang.Math.Abs(mEndValue - state.position);
        }

        /**
         * set the rest value to determine the displacement for the spring
         * @param endValue the endValue for the spring
         * @return the spring for chaining
         */
        public Spring setEndValue(double endValue)
        {
            if (mEndValue == endValue && is_AtRest())
            {
                return this;
            }
            mStartValue = getCurrentValue();
            mEndValue = endValue;
            mSpringSystem.activateSpring(this.getId());
            foreach (SpringListener listener in mListeners)
            {
                listener.onSpringEndStateChange(this);
            }
            return this;
        }

        /**
         * get the rest value used for determining the displacement of the spring
         * @return the rest value for the spring
         */
        public double getEndValue()
        {
            return mEndValue;
        }

        /**
         * set the velocity on the spring in pixels per second
         * @param velocity velocity value
         * @return the spring for chaining
         */
        public Spring setVelocity(double velocity)
        {
            if (velocity == mCurrentState.velocity)
            {
                return this;
            }
            mCurrentState.velocity = velocity;
            mSpringSystem.activateSpring(this.getId());
            return this;
        }

        /**
         * get the velocity of the spring
         * @return the current velocity
         */
        public double getVelocity()
        {
            return mCurrentState.velocity;
        }

        /**
         * Sets the speed at which the spring should be considered at rest.
         * @param restSpeedThreshold speed pixels per second
         * @return the spring for chaining
         */
        public Spring setRestSpeedThreshold(double restSpeedThreshold)
        {
            mRestSpeedThreshold = restSpeedThreshold;
            return this;
        }

        /**
         * Returns the speed at which the spring should be considered at rest in pixels per second
         * @return speed in pixels per second
         */
        public double getRestSpeedThreshold()
        {
            return mRestSpeedThreshold;
        }

        /**
         * set the threshold of displacement from rest below which the spring should be considered at rest
         * @param displacementFromRestThreshold displacement to consider resting below
         * @return the spring for chaining
         */
        public Spring setRestDisplacementThreshold(double displacementFromRestThreshold)
        {
            mDisplacementFromRestThreshold = displacementFromRestThreshold;
            return this;
        }

        /**
         * get the threshold of displacement from rest below which the spring should be considered at rest
         * @return displacement to consider resting below
         */
        public double getRestDisplacementThreshold()
        {
            return mDisplacementFromRestThreshold;
        }

        /**
         * Force the spring to clamp at its end value to avoid overshooting the target value.
         * @param overshootClampingEnabled whether or not to enable overshoot clamping
         * @return the spring for chaining
         */
        public Spring setOvershootClampingEnabled(bool overshootClampingEnabled)
        {
            mOvershootClampingEnabled = overshootClampingEnabled;
            return this;
        }

        /**
         * Check if overshoot clamping is enabled.
         * @return is overshoot clamping enabled
         */
        public bool isOvershootClampingEnabled()
        {
            return mOvershootClampingEnabled;
        }

        /**
         * Check if the spring is overshooting beyond its target.
         * @return true if the spring is overshooting its target
         */
        public bool isOvershooting()
        {
            return mSpringConfig.tension > 0 &&
                   ((mStartValue < mEndValue && getCurrentValue() > mEndValue) ||
                   (mStartValue > mEndValue && getCurrentValue() < mEndValue));
        }

        /**
         * advance the physics simulation in SOLVER_TIMESTEP_SEC sized chunks to fulfill the required
         * realTimeDelta.
         * The math is inlined inside the loop since it made a huge performance impact when there are
         * several springs being advanced.
         * @param realDeltaTime clock drift
         */
       public void advance(double realDeltaTime)
        {

            bool isAtRest = is_AtRest();

            if (isAtRest && mWasAtRest)
            {
                /* begin debug
                Log.d(TAG, "bailing out because we are at rest:" + getName());
                end debug */
                return;
            }

            // clamp the amount of realTime to simulate to avoid stuttering in the UI. We should be able
            // to catch up in a subsequent advance if necessary.
            double adjustedDeltaTime = realDeltaTime;
            if (realDeltaTime > MAX_DELTA_TIME_SEC)
            {
                adjustedDeltaTime = MAX_DELTA_TIME_SEC;
            }

            /* begin debug
            long startTime = System.currentTimeMillis();
            int iterations = 0;
            end debug */

            mTimeAccumulator += adjustedDeltaTime;

            double tension = mSpringConfig.tension;
            double friction = mSpringConfig.friction;

            double position = mCurrentState.position;
            double velocity = mCurrentState.velocity;
            double tempPosition = mTempState.position;
            double tempVelocity = mTempState.velocity;

            double aVelocity, aAcceleration;
            double bVelocity, bAcceleration;
            double cVelocity, cAcceleration;
            double dVelocity, dAcceleration;

            double dxdt, dvdt;

            // iterate over the true time
            while (mTimeAccumulator >= SOLVER_TIMESTEP_SEC)
            {
                /* begin debug
                iterations++;
                end debug */
                mTimeAccumulator -= SOLVER_TIMESTEP_SEC;

                if (mTimeAccumulator < SOLVER_TIMESTEP_SEC)
                {
                    // This will be the last iteration. Remember the previous state in case we need to
                    // interpolate
                    mPreviousState.position = position;
                    mPreviousState.velocity = velocity;
                }

                // Perform an RK4 integration to provide better detection of the acceleration curve via
                // sampling of Euler integrations at 4 intervals feeding each derivative into the calculation
                // of the next and taking a weighted sum of the 4 derivatives as the output.

                // This math was inlined since it made for big performance improvements when advancing several
                // springs in one pass of the BaseSpringSystem.

                // The initial derivative is based on the current velocity and the calculated acceleration
                aVelocity = velocity;
                aAcceleration = (tension * (mEndValue - tempPosition)) - friction * velocity;

                // Calculate the next derivatives starting with the last derivative and integrating over the
                // timestep
                tempPosition = position + aVelocity * SOLVER_TIMESTEP_SEC * 0.5;
                tempVelocity = velocity + aAcceleration * SOLVER_TIMESTEP_SEC * 0.5;
                bVelocity = tempVelocity;
                bAcceleration = (tension * (mEndValue - tempPosition)) - friction * tempVelocity;

                tempPosition = position + bVelocity * SOLVER_TIMESTEP_SEC * 0.5;
                tempVelocity = velocity + bAcceleration * SOLVER_TIMESTEP_SEC * 0.5;
                cVelocity = tempVelocity;
                cAcceleration = (tension * (mEndValue - tempPosition)) - friction * tempVelocity;

                tempPosition = position + cVelocity * SOLVER_TIMESTEP_SEC;
                tempVelocity = velocity + cAcceleration * SOLVER_TIMESTEP_SEC;
                dVelocity = tempVelocity;
                dAcceleration = (tension * (mEndValue - tempPosition)) - friction * tempVelocity;

                // Take the weighted sum of the 4 derivatives as the output.
                dxdt = 1.0 / 6.0 * (aVelocity + 2.0 * (bVelocity + cVelocity) + dVelocity);
                dvdt = 1.0 / 6.0 * (aAcceleration + 2.0 * (bAcceleration + cAcceleration) + dAcceleration);

                position += dxdt * SOLVER_TIMESTEP_SEC;
                velocity += dvdt * SOLVER_TIMESTEP_SEC;
            }

            mTempState.position = tempPosition;
            mTempState.velocity = tempVelocity;

            mCurrentState.position = position;
            mCurrentState.velocity = velocity;

            if (mTimeAccumulator > 0)
            {
                interpolate(mTimeAccumulator / SOLVER_TIMESTEP_SEC);
            }

            // End the spring immediately if it is overshooting and overshoot clamping is enabled.
            // Also make sure that if the spring was considered within a resting threshold that it's now
            // snapped to its end value.
            if (is_AtRest() || (mOvershootClampingEnabled && isOvershooting()))
            {
                // Don't call setCurrentValue because that forces a call to onSpringUpdate
                if (tension > 0)
                {
                    mStartValue = mEndValue;
                    mCurrentState.position = mEndValue;
                }
                else
                {
                    mEndValue = mCurrentState.position;
                    mStartValue = mEndValue;
                }
                setVelocity(0);
                isAtRest = true;
            }
            bool notifyActivate = false;
            if (mWasAtRest)
            {
                mWasAtRest = false;
                notifyActivate = true;
            }
            bool notifyAtRest = false;
            if (isAtRest)
            {
                mWasAtRest = true;
                notifyAtRest = true;
            }
            foreach (SpringListener listener in mListeners)
            {
                // starting to move
                if (notifyActivate)
                {
                    listener.onSpringActivate(this);
                }

                // updated
                listener.onSpringUpdate(this);

                // coming to rest
                if (notifyAtRest)
                {
                    listener.onSpringAtRest(this);
                }
            }
        }

        /**
         * Check if this spring should be advanced by the system.  * The rule is if the spring is
         * currently at rest and it was at rest in the previous advance, the system can skip this spring
         * @return should the system process this spring
         */
        public bool systemShouldAdvance()
        {
            return !is_AtRest() || !wasAtRest();
        }

        /**
         * Check if the spring was at rest in the prior iteration. This is used for ensuring the ending
         * callbacks are fired as the spring comes to a rest.
         * @return true if the spring was at rest in the prior iteration
         */
        public bool wasAtRest()
        {
            return mWasAtRest;
        }

        /**
         * check if the current state is at rest
         * @return is the spring at rest
         */
        public bool is_AtRest()
        {
            return Java.Lang.Math.Abs(mCurrentState.velocity) <= mRestSpeedThreshold &&
                (getDisplacementDistanceForState(mCurrentState) <= mDisplacementFromRestThreshold ||
                 mSpringConfig.tension == 0);
        }

        /**
         * Set the spring to be at rest by making its end value equal to its current value and setting
         * velocity to 0.
         * @return this object
         */
        public Spring set_AtRest()
        {
            mEndValue = mCurrentState.position;
            mTempState.position = mCurrentState.position;
            mCurrentState.velocity = 0;
            return this;
        }

        /**
         * linear interpolation between the previous and current physics state based on the amount of
         * timestep remaining after processing the rendering delta time in timestep sized chunks.
         * @param alpha from 0 to 1, where 0 is the previous state, 1 is the current state
         */
        private void interpolate(double alpha)
        {
            mCurrentState.position = mCurrentState.position * alpha + mPreviousState.position * (1 - alpha);
            mCurrentState.velocity = mCurrentState.velocity * alpha + mPreviousState.velocity * (1 - alpha);
        }

        /** listeners **/

        /**
         * add a listener
         * @param newListener to add
         * @return the spring for chaining
         */
        public Spring addListener(SpringListener newListener)
        {
            if (newListener == null)
            {
                throw new IllegalArgumentException("newListener is required");
            }
            mListeners.Add(newListener);
            return this;
        }

        /**
         * remove a listener
         * @param listenerToRemove to remove
         * @return the spring for chaining
         */
        public Spring removeListener(SpringListener listenerToRemove)
        {
            if (listenerToRemove == null)
            {
                throw new IllegalArgumentException("listenerToRemove is required");
            }
            mListeners.Remove(listenerToRemove);
            return this;
        }

        /**
         * remove all of the listeners
         * @return the spring for chaining
         */
        public Spring removeAllListeners()
        {
            mListeners.Clear();
            return this;
        }

        /**
         * This method checks to see that the current spring displacement value is equal to the input,
         * accounting for the spring's rest displacement threshold.
         * @param value The value to compare the spring value to
         * @return Whether the displacement value from the spring is within the bounds of the compare
         * value, accounting for threshold
         */
        public bool currentValueIsApproximately(double value)
        {
            return Java.Lang.Math.Abs(getCurrentValue() - value) <= getRestDisplacementThreshold();
        }

    }


    // storage for the current and prior physics state while integration is occurring
    public class PhysicsState
    {
       public double position { get; set; }
       public double velocity { get; set; }
    }
}