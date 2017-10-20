using JetBlast.Config;

namespace JetBlast
{
    public struct UserConfig
    {
        /// <summary>
        /// Enable debugging features (Developer only).
        /// </summary>
        public static bool DebugMode { get; }

        /// <summary>
        /// Multiplier of the thrust output for each engine.
        /// </summary>
        public static float ThrustMultiplier { get; }

        /// <summary>
        /// Multiplier of the reverse thrust output for each engine.
        /// </summary>
        public static float ReverseThrustMultiplier { get; }

        /// <summary>
        /// Radius of thrust forces.
        /// </summary>
        public static float ThrustRadius { get; }

        /// <summary>
        /// Strength of heathaze effect.
        /// </summary>
        public static float HeatHazeStrength { get; }

        /// <summary>
        /// Enable engine exhaust effect when throttling.
        /// </summary>
        public static bool UseThrottleExhaust { get; }
      
        static UserConfig()
        {
            DebugMode = IniHelper.GetConfigSetting("General", "DebugMode", false);
            ThrustMultiplier = IniHelper.GetConfigSetting("General", "ThrustMultiplier", 1.0f);
            ReverseThrustMultiplier = IniHelper.GetConfigSetting("General", "ReverseThrustMultiplier", 1.0f);
            ThrustRadius = IniHelper.GetConfigSetting("General", "ThrustRadius", 1.0f);
            HeatHazeStrength = IniHelper.GetConfigSetting("General", "HeatHazeStrength", 1.0f);
            UseThrottleExhaust = IniHelper.GetConfigSetting("General", "UseThrottleExhaust", true);
        }
    }
}
