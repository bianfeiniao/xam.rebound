namespace xam.rebound.core
{

    public class SteppingLooper : SpringLooper
    {
        private bool mStarted;
        private long mLastTime;

        //////@Override
        public override void start()
        {
            mStarted = true;
            mLastTime = 0;
        }

        public bool step(long interval)
        {
            if (mSpringSystem == null || !mStarted)
            {
                return false;
            }
            long currentTime = mLastTime + interval;
            mSpringSystem.loop(currentTime);
            mLastTime = currentTime;
            return mSpringSystem.getIsIdle();
        }

        //////@Override
        public override void stop()
        {
            mStarted = false;
        }
    }

}