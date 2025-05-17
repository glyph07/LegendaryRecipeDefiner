# Legendary Recipe Definer
The **Legendary Recipe Definer** project is a mod built for [Potion Craft](https://store.steampowered.com/app/1210320/Potion_Craft_Alchemist_Simulator/) (v2.0). Using this mod, players can edit in-game legendary recipes! **Legendary Recipe Definer** should maintain a base-game save file if it is later removed, but always create a backup in case of corruption.

## Installation
### Bepinex
This mod was made using Bepinex, a modding framework commonly used with Unity games. After following the given [installation guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html), you should be able to easily set **Legendary Recipe Definer** up!

If you run into any issues, ensure you have done the following:
* Downloaded the [latest version](https://github.com/BepInEx/BepInEx/releases) of Bepinex.
* Extracted its contents into the same folder as the Potion Craft executable.
* Run the game at least once to generate the configuration files.

### Legendary Recipe Definer
At this point, download the [latest release](https://github.com/glyph07/LegendaryRecipeDefiner/releases) of **Legendary Recipe Definer**. Extract the release directly into the `Potion Craft/Bepinex/plugins` directory. Everything should be fully installed at this point!

## Editing Recipes
### Format
Custom recipes are stored in JSON format within the `LegendaryRecipeDefiner/Recipe Blueprint` directory. The default and v1.0 legendary recipes have already been created as an example of the expected format. A blueprint does not need to contain every kind of legendary recipe; missing recipes will just use their default values.

Individual recipes consist of a name and fourteen components. A recipe name *must* match the name of an in-game recipe (e.g. "Nigredo", "SunSalt", "PhilosophersStone"). Components are made up of a part ID and an expected ingredient. A part ID will be a number between 0 and 13, corresponding to particular part of the alchemy machine:

0 - Right Retort
<br>1 - Right Dripper
<br>2 - Rhombus Vessel
<br>3 - Triangular Vessel
<br>4 - Right Furnace
<br>5 - Double Vessel

6 - Floor Vessel
<br>7 - Left Dripper
<br>8 - Left Retort
<br>9 - Spiral Vessel

10 - Left Furnace
<br>11 - Triplet Vessel Left
<br>12 - Triplet Vessel Center
<br>13 - Triplet Vessel Right

The value of the corresponding ingredient depends on the part value. For the two furnaces (4 and 10), an alchemical component name is expected (meaning *non-salt* legendary items). For the other parts, a list of 0-5 effect names is expected. These names all must match their respective names in-game to work.

It is recommended to just duplicate and edit one of the example JSON files for ease of use. All relevant names may also be found in these examples.

### Configuration
By default `recipes_1.0.json` is used in-game as the legendary recipe blueprint. This value can changed in the `settings.config` file in the **Legendary Recipe Definer** directory. Change the 'entry' variable to a file of your choice within the blueprints folder (***do not*** append '.json' to the end of this variable).

The 'error_off_entry' variable determines whether an error is thrown if the given file cannot be found. No issues should occur if different blueprints are used at different points in the same save file.

### Usage
At this point, the game should be able to be played as a normal Potion Craft game. No special consideration should be needed when saving recipes and/or saving the game. Only recipes matching the current loaded blueprint can be used in-game. For example, a saved legendary recipe from the default game will be disabled when using a modded blueprint.

If all blueprints are removed from the game (or even the mod itself), every modded recipe will be replaced with a blank page. These blueprints are saved in the `LegendaryRecipeDefiner/Saved Recipes` directory, however, so they will persist on the same save file they were stored in if returned.
