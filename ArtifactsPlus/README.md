# ArtifactsPlus 1.0.0

ArtifactsPlus is an ONI mod that allows artifacts to a be "activated" with user-customizable effects.  An artifact is activated when it meets certain criteria: on a pedestal, decor minimum, room size, etc.  When an artifact is activated it can have a variety of "actions". Actions can include a positive (or negative) modifier to attributes.

An activated artifact "glows"(*).  If you click on a dupe, you can see the active actions its in bio tab. You can also click on an activated artifact to see it's actions.

The ArtifactsPlus release comes with a simple ArtifactsConfig.json. By editing this file, you can customize the actions & defaults (I would love to hear your suggestions!).

The file has some global settings at the top that apply (by default) to all artifacts.

The artifact will only "activate" in a room between these sizes: 
  "RoomSizeMinimum": 32, "RoomSizeMaximum": 96,

An artifact will only "activate" in a room with this minimum amount of decor:
  "DecorMinimum": 128,

The "Scope" defines the set of dupes the artifact affects when activated.  "InWorld" only affects dupes in the current world/asteriod.  "InRoom" only affects dupes that are in the current room with the artifact when it become active.  "All" means it affects dupes in the entire game (all asteroids/worlds).
  "Scope": "InWorld",

Neighbors defines how many other activated artifacts are allowed to be in the room.  If the number is exceeded, all active artifacts become inactive.
  "Neighbors": 2

Note that JSON syntax allows all of these to be specified at the per artifact level as well, and if so specified it overrides the global value for that artifact. So you can have some artifacts with different requirements for neighbors, decor, scope, etc.

The main content is an array called "Artifacts". Each entry in "Artifacts" represents a single artifact.

 "ArtifactId": The unique string ID for the artifact (e.g., "artifact_sink", note use of the internal name).
 "Attributes":  Maps attribute names (like "Cooking", "Learning", etc.) to numeric value modifiers (which can be negative)

      "Attributes": { "Cooking": 2, "ToiletEfficiency": 1 }

Supported Attributes are:

AirConsumptionRate, Art, Athletics, BionicBatteryCountCapacity, BionicBoosterSlots, Botanist, Caring, CarryAmount, Construction, Cooking, Decor, DecorExpectation, Digging, DiseaseCureSpeed, DoctoredLevel, FarmTinker, FoodExpectation, FoodQuality, GeneratorOutput, GermResistance, Immunity, Insulation, Learning, LifeSupport, Luminescence, Machinery, MachinerySpeed, MaxUnderwaterTravelCost, PowerTinker, QualityOfLife, QualityOfLifeExpectation, RadiationRecovery, RadiationResistance, Ranching, RoomTemperaturePreference, ScaldingThreshold, ScoldingThreshold, Sneezyness, SpaceNavigation, Strength, ThermalConductivityBarrier, ToiletEfficiency, Toggle, TransitTubeTravelSpeed

Important notes:

Artifact modifiers are reapplied when an artifact becomes "active" or a dupe enters into the appropriate scope. E.g. if a dupe teleports into a world with an "inWorld" scoped artifact, the actions will be reapplied.

Internal names for attributes and effects are used. Note that many of these names are modified for the user facing UI we see while playing the game, via in interface called "AttributeConverter".  The exact interactions are weighted and can be unintuitive.

# Options

There is an option to adjust how often the artifact and minion status is updated. 
If you believe an artifact should be active, but it is not, wait a few seconds!
Note there may be performance implications for setting this too low.

# 1.0.0 Release

Status Effects have been removed. I will add them back in in a future release.

Modifiers can stack! Go ahead and collect and activate as many coffee mugs as you like!

(*) There is a newly introduced bug the Prehistoric Release that is prevening the glowing, hopefully will be fixed soon!