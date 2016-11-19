using Java.Lang;
using Java.Util;
using System.Collections.Generic;

namespace xam.rebound.core
{
    /**
   * class for maintaining a registry of all spring configs
   */
    public class SpringConfigRegistry
    {

        private static SpringConfigRegistry INSTANCE = new SpringConfigRegistry(true);

        public static SpringConfigRegistry getInstance()
        {
            return INSTANCE;
        }

        private Dictionary<SpringConfig,string> mSpringConfigMap;

        /**
         * constructor for the SpringConfigRegistry
         */
        SpringConfigRegistry(bool includeDefaultEntry)
        {

            mSpringConfigMap = new Dictionary<SpringConfig, string>();
            if (includeDefaultEntry)
            {
                addSpringConfig(SpringConfig.defaultConfig, "default config");
            }
        }

        /**
         * add a SpringConfig to the registry
         *
         * @param springConfig SpringConfig to add to the registry
         * @param configName name to give the SpringConfig in the registry
         * @return true if the SpringConfig was added, false if a config with that name is already
         *    present.
         */
        public bool addSpringConfig(SpringConfig springConfig, string configName)
        {
            if (springConfig == null)
            {
                throw new IllegalArgumentException("springConfig is required");
            }
            if (configName == null)
            {
                throw new IllegalArgumentException("configName is required");
            }
            if (mSpringConfigMap.ContainsKey(springConfig))
            {
                return false;
            }
            mSpringConfigMap.Add(springConfig, configName);
            return true;
        }

        /**
         * remove a specific SpringConfig from the registry
         * @param springConfig the of the SpringConfig to remove
         * @return true if the SpringConfig was removed, false if it was not present.
         */
        public bool removeSpringConfig(SpringConfig springConfig)
        {
            if (springConfig == null)
            {
                throw new IllegalArgumentException("springConfig is required");
            }
            return mSpringConfigMap.Remove(springConfig) != null;
        }

        /**
         * retrieve all SpringConfig in the registry
         * @return a list of all SpringConfig
         */
        public Dictionary<SpringConfig, string> getAllSpringConfig()
        {
            var ds = Collections.UnmodifiableMap(mSpringConfigMap);
            return ds as Dictionary<SpringConfig, string>;
        }

        /**
         * clear all SpringConfig in the registry
         */
        public void removeAllSpringConfig()
        {
            mSpringConfigMap.Clear();
        }
    }

}