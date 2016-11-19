using Java.Lang;
using System.Collections.Generic;

namespace xam.rebound.core
{
    /**
  * BaseSpringSystem maintains the set of springs within an Application context. It is responsible for
  * Running the spring integration loop and maintains a registry of all the Springs it solves for.
  * In addition to listening to physics events on the individual Springs in the system, listeners
  * can be added to the BaseSpringSystem itself to provide pre and post integration setup.
  */
    public class BaseSpringSystem
    {

        private Dictionary<string, Spring> mSpringRegistry = new Dictionary<string, Spring>();
       // private Set<Spring> mActiveSprings = new HashSet<Spring>();

        private HashSet<Spring> mActiveSprings = new HashSet<Spring>();

        private SpringLooper mSpringLooper;
        private HashSet<SpringSystemListener> mListeners = new HashSet<SpringSystemListener>();
        private bool mIdle = true;

        /**
         * create a new BaseSpringSystem
         * @param springLooper parameterized springLooper to allow testability of the
         *        physics loop
         */
        public BaseSpringSystem(SpringLooper springLooper)
        {
            if (springLooper == null)
            {
                throw new IllegalArgumentException("springLooper is required");
            }
            mSpringLooper = springLooper;
            mSpringLooper.setSpringSystem(this);
        }

        /**
         * check if the system is idle
         * @return is the system idle
         */
        public bool getIsIdle()
        {
            return mIdle;
        }

        /**
         * create a spring with a random uuid for its name.
         * @return the spring
         */
        public Spring createSpring()
        {
            Spring spring = new Spring(this);
            registerSpring(spring);
            return spring;
        }

        /**
         * get a spring by name
         * @param id id of the spring to retrieve
         * @return Spring with the specified key
         */
        public Spring getSpringById(string id)
        {
            if (id == null)
            {
                throw new IllegalArgumentException("id is required");
            }
            return mSpringRegistry[id];
        }

        /**
         * return all the springs in the simulator
         * @return all the springs
         */
        public List<Spring> getAllSprings()
        {
            var collection = mSpringRegistry.Values;
            List<Spring> list = new List<Spring>();
            foreach (var v in collection) {
                list.Add(v);
            }
            return list;
        }

        /**
         * Registers a Spring to this BaseSpringSystem so it can be iterated if active.
         * @param spring the Spring to register
         */
       public void registerSpring(Spring spring)
        {
            if (spring == null)
            {
                throw new IllegalArgumentException("spring is required");
            }
            if (mSpringRegistry.ContainsKey(spring.getId()))
            {
                throw new IllegalArgumentException("spring is already registered");
            }
            mSpringRegistry.Add(spring.getId(), spring);
        }

        /**
         * Deregisters a Spring from this BaseSpringSystem, so it won't be iterated anymore. The Spring should
         * not be used anymore after doing this.
         *
         * @param spring the Spring to deregister
         */
       public void deregisterSpring(Spring spring)
        {
            if (spring == null)
            {
                throw new IllegalArgumentException("spring is required");
            }
            mActiveSprings.Remove(spring);
            mSpringRegistry.Remove(spring.getId());
        }

        /**
         * update the springs in the system
         * @param deltaTime delta since last update in millis
         */
       public void advance(double deltaTime)
        {
            foreach (Spring spring in mActiveSprings)
            {
                // advance time in seconds
                if (spring.systemShouldAdvance())
                {
                    spring.advance(deltaTime / 1000.0);
                }
                else
                {
                    mActiveSprings.Remove(spring);
                }
            }
        }

        /**
         * loop the system until idle
         * @param elapsedMillis elapsed milliseconds
         */
        public void loop(double elapsedMillis)
        {
            foreach (SpringSystemListener listener in mListeners)
            {
                listener.onBeforeIntegrate(this);
            }
            advance(elapsedMillis);
            if (mActiveSprings.Count == 0)
            {
                mIdle = true;
            }
            foreach (SpringSystemListener listener in mListeners)
            {
                listener.onAfterIntegrate(this);
            }
            if (mIdle)
            {
                mSpringLooper.stop();
            }
        }

        /**
         * This is used internally by the {@link Spring}s created by this {@link BaseSpringSystem} to notify
         * it has reached a state where it needs to be iterated. This will add the spring to the list of
         * active springs on this system and start the iteration if the system was idle before this call.
         * @param springId the id of the Spring to be activated
         */
        public void activateSpring(string springId)
        {
            Spring spring = mSpringRegistry[springId];
            if (spring == null)
            {
                throw new IllegalArgumentException("springId " + springId + " does not reference a registered spring");
            }
            mActiveSprings.Add(spring);
            if (getIsIdle())
            {
                mIdle = false;
                mSpringLooper.start();
            }
        }

        /** listeners **/

        /**
         * Add new listener object.
         * @param newListener listener
         */
        public void addListener(SpringSystemListener newListener)
        {
            if (newListener == null)
            {
                throw new IllegalArgumentException("newListener is required");
            }
            mListeners.Add(newListener);
        }

        /**
         * Remove listener object.
         * @param listenerToRemove listener
         */
        public void removeListener(SpringSystemListener listenerToRemove)
        {
            if (listenerToRemove == null)
            {
                throw new IllegalArgumentException("listenerToRemove is required");
            }
            mListeners.Remove(listenerToRemove);
        }

        /**
         * Remove all listeners.
         */
        public void removeAllListeners()
        {
            mListeners.Clear();
        }
    }


}