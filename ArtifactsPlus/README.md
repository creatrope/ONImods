# ArtifactsPlus 1.0.0

ArtifactsPlus is an ONI mod that allows artifacts to a be "activated" with user-customizable effects.  An artifact is activated when it meets certain criteria: on a pedestal, decor minimum, room size, etc.  When an artifact is activated it can have a variety of "actions". Actions can include a positive (or negative) modifier to attributes.

An activated artifact "glows"(*).  If you click on a dupe, you can see the active actions its in bio tab. You can also click on a artifact to see it's modifiers.

The ArtifactsPlus release comes with a simple ArtifactsConfig.json. By editing this file, you can customize the actions & defaults (I would love to hear your suggestions!).

The file has some global settings at the top that apply (by default) to all artifacts.

The artifact will only "activate" in a room between these sizes: 
  "RoomSizeMinimum": 32, "RoomSizeMaximum": 96,

An artifact will only "activate" in a room with this minimum amount of decor:
  "DecorMinimum": 128,

The "Scope" defines the set of dupes the artifact affects when activated. The only currently supported Scope is "inWorld" which means the modifier applies to all dupes in the same world as the activated artifact.

Neighbors defines how many other activated artifacts are allowed to be in the room.  If the number is exceeded, all active artifacts become inactive.
  "Neighbors": 2

Note that JSON syntax allows all of these to be specified at the per artifact level as well, and if so specified it overrides the global value for that artifact. So you can have some artifacts with different requirements for neighbors, decor, scope, etc.

The main content is an array called "Artifacts". Each entry in "Artifacts" represents a single artifact.

 "ArtifactId": The unique string ID for the artifact (e.g., "artifact_sink", note use of the internal name).
 "Attributes":  Maps attribute names (like "Cooking", "Learning", etc.) to numeric value modifiers (which can be negative)

      "Attributes": { "Cooking": 2, "ToiletEfficiency": 1 }

Supported Attributes are:

AirConsumptionRate, Art, Athletics, BionicBatteryCountCapacity, BionicBoosterSlots, Botanist, Caring, CarryAmount, Construction, Cooking, Decor, DecorExpectation, Digging, DiseaseCureSpeed, DoctoredLevel, FarmTinker, FoodExpectation, FoodQuality, GeneratorOutput, GermResistance, Immunity, Insulation, Learning, LifeSupport, Luminescence, Machinery, MachinerySpeed, MaxUnderwaterTravelCost, PowerTinker, QualityOfLife, QualityOfLifeExpectation, RadiationRecovery, RadiationResistance, Ranching, RoomTemperaturePreference, ScaldingThreshold, ScoldingThreshold, Sneezyness, SpaceNavigation, Strength, ThermalConductivityBarrier, ToiletEfficiency, Toggle, TransitTubeTravelSpeed

# Important notes:
# On Customizing ArtifactsConfig.json in 1.0.0

1. Copy ArtifactsConfig.json to a new custom file (e.g MyCfg.json) in the mod directory (e.g. Mods/ArtifactsPlus/).
   - If you are using the Steam Workshop, you can find the mod directory at: `Steam\steamapps\workshop\content\457140\` followed by the mod ID.
   - If you are using a manual install, it will be in the `Mods/ArtifactsPlus/` directory of your ONI installation.
2. Make changes to the custom file.
3. When you load the game, change the mod settings to point to your new file.
4. Updating the mod WILL erase your custom file, so my suggestion is that you make a backup, and put a shortcut to it in mod directory (or just remember to copy it back after the mod updates)

# 1.0.0 Release

Artifact modifiers are reapplied when an artifact becomes "active" or a dupe enters into the appropriate scope. E.g. if a dupe teleports into a world with an "inWorld" scoped artifact, the modifiers will be reapplied.

Internal names for attributes are used in the JSON. Note that many of these names are modified for the user facing UI we see while playing the game, via in interface called "AttributeConverter".  The exact interactions are weighted and can be unintuitive.


The only currently supported scope is "inWorld". This means that the artifact will affect all dupes in the same world as the activated artifact. If you want to have an artifact that only affects dupes in a specific room, you can use the "RoomSizeMinimum" and "RoomSizeMaximum" settings to limit the scope.

The polling interval (settable in the mod options) for checking if an artifact is active, and updating the minion status  is set to 30 seconds by default.
If you believe an artifact should be active, but the dupe is not showing the effects, wait a few seconds!
Note there may be performance implications/stuttering for setting polling interval  too low.

Status Effects have been removed. I will add them back in in a future release.

Modifiers can stack! Go ahead and collect and activate as many coffee mugs as you like!

(*) There is a newly introduced bug the Prehistoric Release that is prevening the glowing, hopefully will be fixed soon!