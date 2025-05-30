# Legendary Recipe Definer
The **Legendary Recipe Definer** project is a mod built for [Potion Craft](https://store.steampowered.com/app/1210320/Potion_Craft_Alchemist_Simulator/) (v2.0). Using this mod, players can edit in-game legendary recipes! **Legendary Recipe Definer** will maintain a base-game save file if it is later removed, but always create a backup in case of corruption.

## Manual Installation
### Bepinex
This mod was made using Bepinex, a modding framework commonly used with Unity games. After following the given [installation guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html), you should be able to easily set **Legendary Recipe Definer** up!

If you run into any issues, ensure you have done the following:
* Downloaded the [latest version](https://github.com/BepInEx/BepInEx/releases) of Bepinex.
* Extracted its contents into the same folder as the Potion Craft executable.
* Run the game at least once to generate the configuration files.

### Legendary Recipe Definer
At this point, download the [latest release](https://github.com/glyph07/LegendaryRecipeDefiner/releases) of **Legendary Recipe Definer**. Extract the release directly into the `Potion Craft/Bepinex/plugins` directory. Everything should be fully installed at this point!

## Editing Recipes
Before you begin editing recipes, run the game at least once with this mod installed. This should properly set up all config files and directory structures necessary for the mod to run.
### Format
Custom recipes are stored in YAML format within the `Recipe Blueprint` directory. The v2.0 and v1.0 legendary recipes have already been created as an example of the expected format. A blueprint does not need to contain every kind of legendary recipe; missing recipes will just use their default values.

Individual recipes consist of a name and fourteen components. A recipe name *must* match the name of an in-game recipe (e.g. "Nigredo", "SunSalt", "PhilosophersStone"). Each component is made up of a part ID and an expected ingredient. The part ID is simply the name of a part of the alchemy machine.

#### Level 1
* RightRetort
* RightDripper
* RhombusVessel
* TriangularVessel
* DoubleVessel
#### Level 2
* SpiralVessel
* LeftRetort
* LeftDripper
* FloorVessel
#### Level 3
* TripletVesselRight
* TripletVesselCenter
* TripletVesselLeft
#### Furnaces
* RightFurnace
* LeftFurnace

The expected ingredient value(s) change based on the given part. For the furnace parts, an alchemical component name is expected:

**Nigredo, Albedo, Citrinitas, Rubedo, PhilosophersStone**

For the other parts, a list of 0-5 effect names is expected. These names all must match their respective names in-game to work. All effect and component names can be found in the example blueprints, and it is recommended to just use these examples as a base when creating new recipes.

Any parts not included in a recipe (or parts with zero components) will be entirely removed from the legendary recipe. It will not be replaced with its default value. Recipes can be defined with any number of usable parts (even zero). If two recipes use the exact same components, the higher tier recipe will be prioritized, essentially disabling the lower tier recipe.

### Configuration
By default `recipes_1.0.yml` is used in-game as the legendary recipe blueprint. This value can changed in the `settings.yml` file in the **Legendary Recipe Definer** directory. Change the 'entry' variable to a file of your choice within the blueprints folder (***do not*** append '.yml' to the end of this variable).

The 'errorOffEntry' variable determines whether an error is thrown if the given entry file cannot be found. No issues should occur if different blueprints are used at different points in the same save file.

The 'allowLessIngredients', 'allowMoreIngredients', and 'allowNoIngredients' settings allow for more flexible recipe creation, and they are set to `true` by default.
* 'allowLessIngredients' allows the user to create recipes with less ingredients than default (e.g. a Nigredo recipe that only needs two ingredients).
* 'allowMoreIngredients' allows the user to create recipes with more ingredients than default (e.g. a Void Salt recipe that uses six or more ingredients).
* 'allowNoIngredients' allows the user to create recipes with no required ingredients.


### Usage
At this point, the game can be played as a normal Potion Craft game. No special consideration should be needed when saving recipes to the recipe book and/or saving the game. Only recipes matching the current loaded blueprint can be used in-game. For example, a saved legendary recipe from the default game will be disabled when using a modded blueprint.

If all blueprints are removed from the game (or even the mod itself), every modded recipe will be replaced with a blank page. These blueprints are saved in the `Saved Recipes` directory, however, so they will persist on the same save file they were stored in if reloaded.
