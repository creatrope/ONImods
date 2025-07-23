__ArtifactsPlus 1.0.7__

ArtifactsPlus is an ONI mod that allows artifacts to a be "activated" with user-customizable effects.  An artifact is activated when it meets certain criteria: on a pedestal, decor minimum, room size, etc.  When an artifact is activated it can have a variety of "actions". Actions can include a positive (or negative) modifier to attributes.

An activated artifact "glows".  If you click on a dupe, you can see the active actions its in bio tab. You can also click on a artifact to see it's modifiers.

The ArtifactsPlus release comes with an internal non-user-visible ArtifactsConfig.json. See instructions below to make a persistent customized local version.

The file has some global settings at the top that apply (by default) to all artifacts.

The artifact will only "activate" in a room between these sizes: 
  "RoomSizeMinimum": 32, "RoomSizeMaximum": 96,

An artifact will only "activate" in a room with this minimum amount of decor:
  "DecorMinimum": 100,

The only currently supported artifact scope is "inWorld" which means the modifier applies to all dupes in the same world as the activated artifact.

Neighbors defines how many other activated artifacts are allowed to be in the room.  If the number is exceeded, all active artifacts become inactive.
  "Neighbors": 4

The main content is an array called "Artifacts". Each entry in "Artifacts" represents a single artifact.

 "ArtifactId": The unique string ID for the artifact (e.g., "artifact_sink", note use of the internal name).
 "Attributes":  Maps attribute names (like "Cooking", "Learning", etc.) to numeric value modifiers (which can be negative)

      "Attributes": { "Cooking": 2, "ToiletEfficiency": 1 }

__Important notes__

ArtifactsPlus now supports persistent user configurations. There is a non-functional example sample file Sample.ArtifactsConfig.json. 
Copy this file to User.ArtifactsConfig.json in the same directory as the mod, and edit it to your liking. You can use the attributes below, with any modifiers (which can be negative).

Artifact modifiers are reapplied when an artifact becomes "active" or a dupe enters a world with activated artifacts.

Internal names for attributes are used in the JSON. Note that many of these names are modified for the user facing UI we see while playing the game, via in interface called "AttributeConverter".  The exact interactions are weighted and can be unintuitive.

The polling interval (settable in the mod options) for checking if an artifact is active, and updating the minion status  is set to 30 seconds by default.
If you believe an artifact should be active, but the dupe is not showing the effects, wait a few seconds!
Note there may be performance implications/stuttering for setting polling interval  too low.

Modifiers can stack! Go ahead and collect and activate as many coffee mugs as you like!

__List of Current InGame Artifacts (as of July 2025)__

artifact_sink
artifact_rubikscube
artifact_obelisk
artifact_blender
artifact_reactormodel
artifact_sandstone
artifact_officemug
artifact_okayxray
artifact_moldavite
artifact_vhs
artifact_saxophone
artifact_modernart
artifact_honeyjar
artifact_ameliaswatch
artifact_teapot
artifact_brickphone
artifact_robotarm
artifact_shieldgenerator
artifact_bioluminescentrock
artifact_grubstatue
artifact_stethoscope
artifact_eggrock
artifact_hatchfossil
artifact_rocktornado
artifact_pacupercolator
artifact_magmalamp
artifact_oracle
artifact_dnamodel
artifact_rainboweggrock
artifact_plasmalamp
artifact_moodring
artifact_solarsystem
artifact_moonmoonmoon

__List of Attributes That Can Be Modified:__

AirConsumptionRate: Air Consumption Rate (Air Consumption determines how much Oxygen a Duplicant requires per minute to live.)
Art: Creativity (Determines how quickly a Duplicant produces Artwork.)
Athletics: Athletics (Determines a Duplicant's default runspeed.)
BionicBatteryCountCapacity: Power Banks (The number of power banks this Bionic Duplicant can store)
BionicBoosterSlots: Booster Slots (The number of boosters this Bionic Duplicant can install at once)
Botanist: Agriculture (Determines how quickly and efficiently a Duplicant cultivates Plants.)
Caring: Medicine (Determines a Duplicant's ability to care for sick peers.)
CarryAmount: Carrying Capacity (Determines the maximum weight that a Duplicant can carry.)
Construction: Construction (Determines a Duplicant's building Speed.)
Cooking: Cuisine (Determines how quickly a Duplicant prepares Food.)
Decor: Decor (Affects a Duplicant's Morale and their opinion of their surroundings.)
DecorExpectation: Decor Morale Bonus (A Decor Morale Bonus allows Duplicants to receive Morale boosts from lower Decor values.)
Digging: Excavation (Determines a Duplicant's mining speed.)
DiseaseCureSpeed: Disease Recovery Speed Bonus (Recovery speed bonus is increased when another Duplicant provides medical care to the patient)
DoctoredLevel: Treatment Received Effect
FoodExpectation: Food Morale Bonus (A Food Morale Bonus allows Duplicants to receive Morale boosts from lower quality Food)
GeneratorOutput: Power Output
GermResistance: Germ Resistance (Duplicants with a higher Germ Resistance rating are less likely to contract germ-based Diseases.)
Immunity: Immunity (Determines a Duplicant's Disease susceptibility and recovery time.)
Insulation: Insulation (Highly Insulated Duplicants retain body heat easily, while low Insulation Duplicants are easier to keep cool.)
Learning: Science (Determines how quickly a Duplicant conducts Research and gains Skill Points.)
LifeSupport: Life Support (Determines how efficiently a Duplicant maintains Algae Terrariums, Deodorizers, and Water Sieves)
Luminescence: Luminescence (Determines how much light a Duplicant emits.)
Machinery: Machinery (Determines how quickly a Duplicant uses machines.)
MachinerySpeed: Machinery Speed (Speed Bonus)
MaxUnderwaterTravelCost: Underwater Movement (Determines a Duplicant's runspeed when submerged in Liquid)
QualityOfLife: Morale (A Duplicant's Morale must exceed their Morale Need, or they'll begin to accumulate Stress.)
QualityOfLifeExpectation: Morale Need (Dictates how high a Duplicant's Morale must be kept to prevent them from gaining Stress)
RadiationRecovery: Radiation Absorption (The rate at which Radiation is neutralized within a Duplicant body.)
RadiationResistance: Radiation Resistance (Determines how easily a Duplicant repels Radiation Sickness.)
Ranching: Husbandry (Determines how efficiently a Duplicant tends Critters.)
RoomTemperaturePreference: Temperature Preference (Determines the minimum body Temperature a Duplicant prefers to maintain.)
ScaldingThreshold: Scalding Threshold (Determines the Temperature at which a Duplicant will get burned.)
ScoldingThreshold: Frostbite Threshold (Determines the Temperature at which a Duplicant will get frostbitten.)
Sneezyness: Sneeziness (Determines how frequently a Duplicant sneezes.)
SpaceNavigation: Piloting (Determines how long it takes a Duplicant to complete a space mission.)
Strength: Strength (Determines a Duplicant's Carrying Capacity and cleaning speed.)
ThermalConductivityBarrier: Insulation Thickness (Determines how quickly a Duplicant retains or loses body Heat in any given area.)
Toggle: Toggle (Determines how efficiently a Duplicant tunes machinery, flips switches, and sets sensors.)
ToiletEfficiency: Bathroom Use Speed (Determines how long a Duplicant needs to do their "business".)
TransitTubeTravelSpeed: Transit Speed (Determines a Duplicant's default Transit Tube travel speed.)