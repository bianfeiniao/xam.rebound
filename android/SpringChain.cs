using System.Collections.Generic;
using System.Linq;
using xam.rebound.core;

namespace xam.rebound.android
{
    /**
  * SpringChain is a helper class for creating spring animations with multiple springs in a chain.
  * Chains of springs can be used to create cascading animations that maintain individual physics
  * state for each member of the chain. One spring in the chain is chosen to be the control spring.
  * Springs before and after the control spring in the chain are pulled along by their predecessor.
  * You can change which spring is the control spring at any point by calling
  * {@link SpringChain#setControlSpringIndex(int)}.
  */
    public class SpringChain : SpringListener
    {

        /**
         * Add these spring configs to the registry to support live tuning through the
         * {@link com.facebook.rebound.ui.SpringConfiguratorView}
         */
        private static SpringConfigRegistry registry = SpringConfigRegistry.getInstance();
        private static int DEFAULT_MAIN_TENSION = 40;
        private static int DEFAULT_MAIN_FRICTION = 6;
        private static int DEFAULT_ATTACHMENT_TENSION = 70;
        private static int DEFAULT_ATTACHMENT_FRICTION = 10;
        private static int id = 0;


        /**
         * Factory method for creating a new SpringChain with default SpringConfig.
         * @return the newly created SpringChain
         */
        public static SpringChain create()
        {
            return new SpringChain();
        }

        /**
         * Factory method for creating a new SpringChain with the provided SpringConfig.
         * @param mainTension tension for the main spring
         * @param mainFriction friction for the main spring
         * @param attachmentTension tension for the attachment spring
         * @param attachmentFriction friction for the attachment spring
         * @return the newly created SpringChain
         */
        public static SpringChain create(
            int mainTension,
            int mainFriction,
            int attachmentTension,
            int attachmentFriction)
        {
            return new SpringChain(mainTension, mainFriction, attachmentTension, attachmentFriction);
        }

        private SpringSystem mSpringSystem = SpringSystem.create();
        private HashSet<SpringListener> mListeners =
            new HashSet<SpringListener>();
        private HashSet<Spring> mSprings = new HashSet<Spring>();
        private int mControlSpringIndex = -1;

        // The main spring config defines the tension and friction for the control spring. Keeping these
        // values separate allows the behavior of the trailing springs to be different than that of the
        // control point.
        private SpringConfig mMainSpringConfig;

        // The attachment spring config defines the tension and friction for the rest of the springs in
        // the chain.
        private SpringConfig mAttachmentSpringConfig;

        public SpringChain()
        {
            //this(
            //    DEFAULT_MAIN_TENSION,
            //    DEFAULT_MAIN_FRICTION,
            //    DEFAULT_ATTACHMENT_TENSION,
            //    DEFAULT_ATTACHMENT_FRICTION);
        }

        private SpringChain(
            int mainTension,
            int mainFriction,
            int attachmentTension,
            int attachmentFriction)
        {
            mMainSpringConfig = SpringConfig.fromOrigamiTensionAndFriction(mainTension, mainFriction);
            mAttachmentSpringConfig =
                SpringConfig.fromOrigamiTensionAndFriction(attachmentTension, attachmentFriction);
            registry.addSpringConfig(mMainSpringConfig, "main spring " + id++);
            registry.addSpringConfig(mAttachmentSpringConfig, "attachment spring " + id++);
        }

        public SpringConfig getMainSpringConfig()
        {
            return mMainSpringConfig;
        }

        public SpringConfig getAttachmentSpringConfig()
        {
            return mAttachmentSpringConfig;
        }

        /**
         * Add a spring to the chain that will callback to the provided listener.
         * @param listener the listener to notify for this Spring in the chain
         * @return this SpringChain for chaining
         */
        public SpringChain addSpring(SpringListener listener)
        {
            // We listen to each spring added to the SpringChain and dynamically chain the springs together
            // whenever the control spring state is modified.
            Spring spring = mSpringSystem
                .createSpring()
                .addListener(this)
                .setSpringConfig(mAttachmentSpringConfig);
            mSprings.Add(spring);
            mListeners.Add(listener);
            return this;
        }

        /**
         * Set the index of the control spring. This spring will drive the positions of all the springs
         * before and after it in the list when moved.
         * @param i the index to use for the control spring
         * @return this SpringChain
         */
        public SpringChain setControlSpringIndex(int i)
        {
            mControlSpringIndex = i;
            Spring controlSpring = mSprings.ElementAtOrDefault(mControlSpringIndex);
            if (controlSpring == null)
            {
                return null;
            }
            foreach (Spring spring in mSpringSystem.getAllSprings())
            {
                spring.setSpringConfig(mAttachmentSpringConfig);
            }
            getControlSpring().setSpringConfig(mMainSpringConfig);
            return this;
        }

        /**
         * Retrieve the control spring so you can manipulate it to drive the positions of the other
         * springs.
         * @return the control spring.
         */
        public Spring getControlSpring()
        {
            return mSprings.ElementAtOrDefault(mControlSpringIndex);
        }

        /**
         * Retrieve the list of springs in the chain.
         * @return the list of springs
         */
        public List<Spring> getAllSprings()
        {
            List<Spring> list = mSprings.ToList<Spring>();
            return list;
        }

        ////@Override
        public void onSpringUpdate(Spring spring)
        {
            // Get the control spring index and update the endValue of each spring above and below it in the
            // spring collection triggering a cascading effect.
            int idx = mSprings.ToList<Spring>().IndexOf(spring);
            SpringListener listener = mListeners.ElementAt<SpringListener>(idx);
            int above = -1;
            int below = -1;
            if (idx == mControlSpringIndex)
            {
                below = idx - 1;
                above = idx + 1;
            }
            else if (idx < mControlSpringIndex)
            {
                below = idx - 1;
            }
            else if (idx > mControlSpringIndex)
            {
                above = idx + 1;
            }
            if (above > -1 && above < mSprings.Count)
            {
                mSprings.ElementAtOrDefault(above).setEndValue(spring.getCurrentValue());
            }
            if (below > -1 && below < mSprings.Count)
            {
                mSprings.ElementAtOrDefault(below).setEndValue(spring.getCurrentValue());
            }
            listener.onSpringUpdate(spring);
        }

        ////@Override
        public void onSpringAtRest(Spring spring)
        {
            int idx = mSprings.ToList<Spring>().IndexOf(spring);
            mListeners.ElementAt<SpringListener>(idx).onSpringAtRest(spring);
        }

        ////@Override
        public void onSpringActivate(Spring spring)
        {
            int idx = mSprings.ToList<Spring>().IndexOf(spring);
            mListeners.ElementAt<SpringListener>(idx).onSpringActivate(spring);
        }

        ////@Override
        public void onSpringEndStateChange(Spring spring)
        {
            int idx = mSprings.ToList<Spring>().IndexOf(spring);
            mListeners.ElementAt<SpringListener>(idx).onSpringEndStateChange(spring);
        }
    }
}