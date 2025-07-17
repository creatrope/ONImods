public static class STRINGS
{
    public static class DUPLICANTS
    {
        public static class DISEASES
        {
            public static class FLATULENCESICKNESS
            {
                public static LocString NAME = "Flatulence (Disease)";
                public static LocString DESCRIPTION = "This duplicant suffers from excessive flatulence.";
            }
        }
    }

    public static class TRAITS
    {
        public static class FLATULENT
        {
            public static LocString NAME = "Flatulent Trait";
            public static LocString DESC = "This duplicant suffers from excessive flatulence.";
        }
    }

    public static class EFFECTS
    {
        public static class FLATULENCEEFFECT
        {
            public static LocString NAME = "Flatulence Effect";
            public static LocString DESC = "This duplicant is experiencing excessive flatulence.";
        }
        public static class NOFLATULENCEEFFECT
        {
            public static LocString NAME = "No Flatulence Effect";
            public static LocString DESC = "This duplicant is temporarily immune to flatulence.";
        }
    }

    public static class ITEMS
    {
        public static class MEDICINE
        {
            public static class NOFLATULENCEPILL
            {
                public static LocString NAME = "No Flatulence Pill";
                public static LocString DESC = "A pill that temporarily prevents flatulence in duplicants.";
            }
        }
    }

    public static class OPTIONS
    {
        public const string ENABLE_CUSTOM_OUTPUT_LOG = "Enable Custom Output Log";
        public const string ENABLE_CUSTOM_OUTPUT_LOG_DESC = "Enable or disable custom output logging.";
        public const string NO_FLATULENCE_PILL_RECIPE_TIME = "No Flatulence Pill Recipe Time";
        public const string NO_FLATULENCE_PILL_RECIPE_TIME_DESC = "Time required to craft No Flatulence Pill.";
        public const string FLATULENCE_REINFECT_INTERVAL = "Flatulence Reinfect Interval";
        public const string FLATULENCE_REINFECT_INTERVAL_DESC = "Interval for reinfecting minions with Flatulence.";
        public const string FLATULENCE_SICKNESS_STRESS_PER_CYCLE = "Flatulence Sickness Stress Per Cycle";
        public const string FLATULENCE_SICKNESS_STRESS_PER_CYCLE_DESC = "Stress per cycle from Flatulence Sickness.";
        public const string NO_FLATULENCE_EFFECT_DURATION = "No Flatulence Effect Duration";
        public const string NO_FLATULENCE_EFFECT_DURATION_DESC = "Duration of No Flatulence Effect.";
        public const string FLATULENCE_CUSTOM_EMIT_INTERVAL = "Flatulence Custom Emit Interval";
        public const string FLATULENCE_CUSTOM_EMIT_INTERVAL_DESC = "Custom interval for Flatulence emission.";
    }
}