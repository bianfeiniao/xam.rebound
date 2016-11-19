namespace xam.rebound.core
{
    public class SynchronousLooper : SpringLooper
    {

        public static double SIXTY_FPS = 16.6667;
        private double mTimeStep;
        private bool mRunning;

        public SynchronousLooper()
        {
            mTimeStep = SIXTY_FPS;
        }

        public double getTimeStep()
        {
            return mTimeStep;
        }

        public void setTimeStep(double timeStep)
        {
            mTimeStep = timeStep;
        }

        //////@Override
        public override void start()
        {
            mRunning = true;
            while (!mSpringSystem.getIsIdle())
            {
                if (mRunning == false)
                {
                    break;
                }
                mSpringSystem.loop(mTimeStep);
            }
        }

        //////@Override
        public override void stop()
        {
            mRunning = false;
        }
    }
}