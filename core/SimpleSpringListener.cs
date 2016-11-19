using System;

namespace xam.rebound.core
{
    public class SimpleSpringListener : SpringListener
    {
        public Action<Spring> SpringUpdate { get; set; }
        public Action<Spring> SpringAtRest { get; set; }
        public Action<Spring> SpringEndStateChange { get; set; }
        public Action<Spring> SpringActivate { get; set; }
        public void onSpringUpdate(Spring spring)
        {
            SpringUpdate?.Invoke(spring);
        }
        public void onSpringAtRest(Spring spring)
        {
            SpringAtRest?.Invoke(spring);
        }

        public void onSpringActivate(Spring spring)
        {
            SpringActivate?.Invoke(spring);
        }

        public void onSpringEndStateChange(Spring spring)
        {
            SpringEndStateChange?.Invoke(spring);
        }
    }
}