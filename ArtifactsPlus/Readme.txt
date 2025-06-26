Alpha Release of ArtifactsPlus 0.1

ArtifactsPlus is an ONI mod that allows artifacts to a be "activated" with user-customizable effects.  An artifact is activated when it meets certain criteria: on a pedestal, decor minimum, room size, etc.  When an artifact is activated it can have a variety of "actions". Actions can include a positive (or negative) change to attributes, or a status "effect".

An activated artifact "glows".  If you click on a dupe, you can see the active actions its in bio tab. You can also click on an activated artifact to see it's actions.

The ArtifactsPlus alpha release comes with a simple ArtifactsConfig.json. By editing this file, you can customize the actions & defaults (I would love to hear your suggestions!).

The file has some global settings at the top that apply (by default) to all artifacts.

The artifact will only "activate" in a room between these sizes: 
  "RoomSizeMinimum": 32, "RoomSizeMaximum": 96,

The artifact will only "activate" in a room with this minimum amount of decor:
  "DecorMinimum": 128,

The "Scope" defines the set of dupes the artifact affects when activated.  "InWorld" only affects dupes in the current world/asteriod.  "InRoom" only affects dupes that are in the current room with the artifact when it become active.  "All" means it affects dupes in the entire game (all asteroids/worlds).
  "Scope": "InWorld",

Neighbors defines how many other activated artifacts are allowed to be in the room.  If the number is exceeded, all active artifacts become inactive.
  "Neighbors": 1

Note that JSON syntax allows all of these to be specified at the per artifact level as well, and if so specified it overrides the global value for that artifact. So you can have some artifacts with different requirements for neighbors, decor, scope, etc.

The main content is an array called "Artifacts". Each entry in "Artifacts" represents a single artifact.

 "ArtifactId": The unique string ID for the artifact (e.g., "artifact_sink").
 "Attributes":  Maps attribute names (like "Cooking", "Learning", etc.) to numeric values (which can be negative)

      "Attributes": { "Cooking": 2, "ToiletEfficiency": 1 }

Supported Attributes are:

AirConsumptionRate, Art, Athletics, BionicBatteryCountCapacity, BionicBoosterSlots, Botanist, Caring, CarryAmount, Construction, Cooking, Decor, DecorExpectation, Digging, DiseaseCureSpeed, DoctoredLevel, FarmTinker, FoodExpectation, FoodQuality, GeneratorOutput, GermResistance, Immunity, Insulation, Learning, LifeSupport, Luminescence, Machinery, MachinerySpeed, MaxUnderwaterTravelCost, PowerTinker, QualityOfLife, QualityOfLifeExpectation, RadiationRecovery, RadiationResistance, Ranching, RoomTemperaturePreference, ScaldingThreshold, ScoldingThreshold, Sneezyness, SpaceNavigation, Strength, ThermalConductivityBarrier, ToiletEfficiency, Toggle, TransitTubeTravelSpeed

The format also supports status "effects" with durations. 
{ "Id": "Sleep", "Duration": 0.0 }

AnewHope, ArcadePlaying, AteFrozenFood, AteFromFeeder, BadSleep, BadSleepAfraidOfDark, BadSleepCold, BadSleepMovement, BarracksStamina, BeachChairLit, BeachChairRelaxing, BeachChairUnlit, BedHealth, BedStamina, BedroomStamina, Break1, Break2, Break3, Break4, Break5, BionicBedTimeEffect, BionicBatteryCountCapacity, BionicBoosterSlots, BionicOffline, BionicRadiationExposureExtreme, BionicRadiationExposureMajor, BionicRadiationExposureMinor, BionicWaterStress, BotMopping, BotSweeping, CarpetFeet, CarryAmount, CenterOfAttention, Charging, Claustrophobic, ColdAir, ContaminatedLungs, Cooking, CookingDown, CookingUp, Construction, ConstructionDown, ConstructionUp, CryFace, CryoFriend, Danced, Dancing, Decor, Decor0, Decor1, Decor2, Decor3, Decor4, Decor5, DecorDown, DecorExpectation, DecorMinus1, DecorUp, DeeperDiversLungs, Diarrhea, Digging, DiggingDown, DiggingUp, DiseaseCureSpeed, DivergentCropTended, DivergentCropTendedWorm, DoctoredLevel, DoctoredOffCotEffect, DoctoredOffRejuvenatorEffect, DuplicantGotMilk, Edible0, Edible1, Edible2, Edible3, EdibleMinus1, EdibleMinus2, EdibleMinus3, EggHug, EggSong, Espress, ExpellingGunk, FarmTinker, Flatulence, FoodExpectation, FoodQuality, FoodSicknessRecovery, FreshOil_CrudeOil, FreshOil_PhytoOil, FullBladder, GeneratorOutput, GermResistance, GoodConversation, GoodEats, Greasemonkey, Greeting, GreenThumb, GunkHungover, GunkSick, HadMilk, HasBalloon, HeardJoySinger, HistamineSuppression, HotStuff, HotTub, HotTubRelaxing, Hugged, HuggingFrenzy, Hyperthermia, Hypothermia, IceBellyWellFed, ImmuneSystemOverwhelmed, Immunity, Insulation, InteriorDecorator, InteractedWithAirborneCondo, InteractedWithCritterCondo, InteractedWithUnderwaterCondo, IrritableBowel, IsJoySinger, IsRoboDancer, IsSparkleStreaker, Juicer, LadderBedStamina, Learning, LearningDown, LearningUp, LifeSupport, LightWounds, LightWoundsCritter, Loner, LowOxygen, Luminescence, LuxuryBedStamina, MACHINERY_SPICE, Machinery, MachineryDown, MachinerySpeed, MachineryUp, MaxUnderwaterTravelCost, MedicalCot, MedicalCotDoctored, MegaBrainTankRelax, MegaBrainTankStress, Medicine_BasicBooster, Medicine_BasicRadPill, Medicine_GenericPill, Medicine_IntermediateBooster, Medicine_IntermediateRadPill, Medicine_VitaminSupplement, MentalBreak, MessTableSalt, ModerateWounds, ModerateWoundsCritter, MoleHands, MooWellFed, Mourning, NarcolepticSleep, Narcolepsy, NewCrewArrival, NightOwl, NoFunAllowed, NoLubricationMajor, NoLubricationMinor, NoiseMajor, NoiseMinor, NoisePeaceful, NoOxygen, NoodleArms, PassedOutSleep, PeacefulSleep, PeopleTooCloseWhileSleeping, PILOTING_SPICE, PlayedArcade, Pleasant, PoppedEarDrums, PostDiseaseRecovery, PowerTinker, QualityOfLife, QualityOfLifeExpectation, RadiationExposureExtreme, RadiationExposureMajor, RadiationExposureMinor, RadiationRecovery, RadiationResistance, Ranching, RanchingDown, RanchingUp, Ranched, RecentlyBeachChair, RecentlyDanced, RecentlyHotTub, RecentlyMechanicalSurfboard, RecentlyPartied, RecentlyPlayedArcade, RecentlyPlayedSinglePlayerArcade, RecentlyRecDrink, RecentlySauna, RecentlySawRoboDancer, RecentlySlippedTracker, RecentlySocialized, RecentlyTelephoned, RecentlyVerticalWindTunnel, RefreshingTouch, Regeneration, RestfulSleep, Restless, Rejuvenator, RejuvenatorDoctored, RoomBarracks, RoomBathroom, RoomBedroom, RoomGreatHall, RoomLatrine, RoomMessHall, RoomNatureReserve, RoomPark, RoomPrivateBedroom, RoomTemperaturePreference, SawRoboDancer, SawSparkleStreaker, ScaldingThreshold, ScoldingThreshold, SeaFoodRadiationResistance, SevereWounds, SevereWoundsCritter, Showered, SimpleTastes, Sleep, SleepClinic, SlowLearner, SmelledFlowers, SmelledPutridOdour, SmelledStinky, Sneezyness, SodaFountain, SoiledSuit, SoakingWet, SoreBack, SpaceNavigation, SpaceTourist, StarryEyed, StaleFood, SteppedInContaminatedWater, STRENGTH_SPICE, Strength, StrengthDown, StrengthUp, StrongArm, SuddenMoraleHelper, Sunlight_Burning, Sunlight_Pleasant, Thriver, Toggle, ToiletEfficiency, Twinkletoes, UnderWater, UncomfortableFeet, Uncultured, UnfashionableClothing, Vertigo, Vomiting, WarmAir, WarmTouch, WarmTouchFood, WasAttacked, WellFed, WetFeet, WoodDeerWellFed, WorkEncouraged, ZombieSicknessRecovery

Important notes:

Artifact actions are reapplied when an artifact becomes "active" or a dupe enters into the appropriate scope. E.g. if a dupe teleports into a world with an "inWorld" scoped artifact, the actions will be reapplied.

Internal names for attributes and effects are used. Note that many of these names are modified for the user facing UI we see while playing the game, via in interface called "AttributeConverter".  The exact interactions are weighted and can be unintuitive.

Alpha 0.1 release issues

Very few of the combinations have been tested ! Although the syntax will work, the results may be exciting and unpredictable!

The status effects in this alpha release do not survive a game reload, but the attributes effects do.  Status effect durations are not working yet.