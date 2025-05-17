using System;
using System.IO;
using System.Collections.Generic;

using BepInEx;
using HarmonyLib;

using UnityEngine;
using UnityEngine.SceneManagement;

using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.QuestSystem.DesiredItems;
using PotionCraft.ObjectBased.AlchemyMachine;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.AlchemyMachineProduct;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;


namespace LegendaryRecipeDefiner
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class LegendaryRecipeDefiner : BaseUnityPlugin
    {
        public const string pluginGuid = "fgvb.potioncraft.legendaryrecipedefiner";
        public const string pluginName = "LegendaryRecipeDefiner";
        public const string pluginVersion = "1.0.0.0";

        static string target_recipes = "";
        static bool error_op = true;

        static string currentLoadedFile = "";
        static string currentSavedFile = "";
        static string stringToBeSaved = "";

        public static int lastMinimum = -1;


        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(LegendaryRecipeDefiner));
            string target = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/settings.config";
            if (!File.Exists(target))
            {
                if (error_op)
                    Debug.LogError("Config file for `LegendaryRecipeDefiner` could not be found.");
                else
                    Debug.Log("Config file for `LegendaryRecipeDefiner` could not be found.");
                return;
            }
            string config = File.ReadAllText(target);
            ParseConfig(config);
        }


        private static bool settingsInit = false;

        [Serializable]
        public enum MachinePart
        {
            Unknown = -1, RightRetort, RightDripper, RhombusVessel, TriangularVessel, RightFurnace, DoubleVessel,
            FloorVessel, LeftDripper, LeftRetort, SpiralVessel, LeftFurnace, TripletVesselLeft, TripletVesselCenter, TripletVesselRight
        }


        [Serializable]
        public class SerializedUpdate
        {
            public MachinePart part;

            public List<string> components;

            public SerializedUpdate(MachinePart part__, List<string> components__)
            {
                part = part__;
                components = components__;
            }
        }

        [Serializable]
        public class SerializedRecipe
        {
            public string name = "";
            public List<SerializedUpdate> recipe = new List<SerializedUpdate>();
        }

        [Serializable]
        public class SerializedRecipeList
        {
            public List<SerializedRecipe> recipes = new List<SerializedRecipe>();
        }

        [Serializable]
        public class SerializedSafeBookmark
        {
            public float positionX;
            public float positionY;
            public int prefabIndex;
            public bool isMirrored;
            public int index;

            public SerializedSafeBookmark(float x, float y, int prefab, bool mirrored, int rail)
            {
                positionX = x;
                positionY = y;
                prefabIndex = prefab;
                isMirrored = mirrored;
                index = rail;
            }

            public Bookmark.SerializedBookmark ToDefault(ref int rail)
            {
                Bookmark.SerializedBookmark result = new Bookmark.SerializedBookmark();
                result.position = new Vector2(positionX, positionY);
                result.prefabIndex = prefabIndex;
                result.isMirrored = isMirrored;
                rail = index;

                return result;
            }
        }

        [Serializable]
        public class SerializedSavedRecipe
        {
            public PotionCraft.ObjectBased.UIElements.Books.RecipeBook.SerializedRecipe content;
            public SerializedSafeBookmark mark;
        }

        [Serializable]
        public class SerializedState
        {
            public List<SerializedSavedRecipe> recipes = new List<SerializedSavedRecipe>();
            public SerializedAlchemyMachineProduct currentMachineContent = null;
        }

        public class PartUpdate
        {
            public MachinePart partID = MachinePart.Unknown;
            public PotionEffect[] effects = { };
            public AlchemyMachineProduct product = null;

            public PartUpdate() { }
            public PartUpdate(MachinePart partID__, PotionEffect[] effects__, AlchemyMachineProduct product__ = null)
            {
                partID = partID__;
                effects = effects__;
                product = product__;
            }
        }


        [HarmonyPatch(typeof(PotionCraft.SceneLoader.ProgressBarFiller), "OnBarFilled")]
        [HarmonyPostfix]
        public static void PostSettingInit()
        {
            if (settingsInit)
                return;

            DeserializeRecipes();
            settingsInit = true;
        }


        static InventoryItem GetMachinePart(LegendaryRecipe recipe, MachinePart partID)
        {
            switch (partID)
            {
                case MachinePart.Unknown:
                    return null;
                case MachinePart.RightRetort:
                    return recipe.rightRetort;
                case MachinePart.RightDripper:
                    return recipe.rightDripper;
                case MachinePart.RhombusVessel:
                    return recipe.rhombusVessel;
                case MachinePart.TriangularVessel:
                    return recipe.triangularVessel;
                case MachinePart.RightFurnace:
                    return recipe.rightFurnace;
                case MachinePart.DoubleVessel:
                    return recipe.doubleVessel;
                case MachinePart.FloorVessel:
                    return recipe.floorVessel;
                case MachinePart.LeftDripper:
                    return recipe.leftDripper;
                case MachinePart.LeftRetort:
                    return recipe.leftRetort;
                case MachinePart.SpiralVessel:
                    return recipe.spiralVessel;
                case MachinePart.LeftFurnace:
                    return recipe.leftFurnace;
                case MachinePart.TripletVesselLeft:
                    return recipe.tripletVesselLeft;
                case MachinePart.TripletVesselCenter:
                    return recipe.tripletVesselCenter;
                case MachinePart.TripletVesselRight:
                    return recipe.tripletVesselRight;
            }
            return null;
        }

        static void SetMachinePart(LegendaryRecipe recipe, MachinePart partID, InventoryItem value)
        {
            switch (partID)
            {
                case MachinePart.Unknown:
                    return;
                case MachinePart.RightRetort:
                    recipe.rightRetort = value;
                    break;
                case MachinePart.RightDripper:
                    recipe.rightDripper = value;
                    break;
                case MachinePart.RhombusVessel:
                    recipe.rhombusVessel = value;
                    break;
                case MachinePart.TriangularVessel:
                    recipe.triangularVessel = value;
                    break;
                case MachinePart.RightFurnace:
                    recipe.rightFurnace = value;
                    break;
                case MachinePart.DoubleVessel:
                    recipe.doubleVessel = value;
                    break;
                case MachinePart.FloorVessel:
                    recipe.floorVessel = value;
                    break;
                case MachinePart.LeftDripper:
                    recipe.leftDripper = value;
                    break;
                case MachinePart.LeftRetort:
                    recipe.leftRetort = value;
                    break;
                case MachinePart.SpiralVessel:
                    recipe.spiralVessel = value;
                    break;
                case MachinePart.LeftFurnace:
                    recipe.leftFurnace = value;
                    break;
                case MachinePart.TripletVesselLeft:
                    recipe.tripletVesselLeft = value;
                    break;
                case MachinePart.TripletVesselCenter:
                    recipe.tripletVesselCenter = value;
                    break;
                case MachinePart.TripletVesselRight:
                    recipe.tripletVesselRight = value;
                    break;
            }
        }

        static List<string> ProductToList(InventoryItem product)
        {
            if (product == null)
                return new List<string>();
            return new List<string> { (product as PotionCraft.ScriptableObjects.AlchemyMachineProducts.AlchemyMachineProduct).name };
        }

        static List<string> PotionToElementsList(InventoryItem item)
        {
            if (item == null)
                return new List<string>();
            List<string> result = new List<string>();
            foreach (PotionEffect effect in (item as DesiredItemPotionEffectsScriptableObject).effects)
            {
                result.Add(effect.name);
            }
            return result;
        }

        static SerializedRecipe SerializeRecipe(string name)
        {
            LegendaryRecipe recipe = LegendaryRecipe.GetByName(name);
            SerializedRecipe section = new SerializedRecipe();
            section.name = name;
            section.recipe.Add(new SerializedUpdate(MachinePart.RightRetort, PotionToElementsList(recipe.rightRetort)));
            section.recipe.Add(new SerializedUpdate(MachinePart.RightDripper, PotionToElementsList(recipe.rightDripper)));
            section.recipe.Add(new SerializedUpdate(MachinePart.RhombusVessel, PotionToElementsList(recipe.rhombusVessel)));
            section.recipe.Add(new SerializedUpdate(MachinePart.TriangularVessel, PotionToElementsList(recipe.triangularVessel)));
            section.recipe.Add(new SerializedUpdate(MachinePart.RightFurnace, ProductToList(recipe.rightFurnace)));
            section.recipe.Add(new SerializedUpdate(MachinePart.DoubleVessel, PotionToElementsList(recipe.doubleVessel)));
            section.recipe.Add(new SerializedUpdate(MachinePart.FloorVessel, PotionToElementsList(recipe.floorVessel)));
            section.recipe.Add(new SerializedUpdate(MachinePart.LeftDripper, PotionToElementsList(recipe.leftDripper)));
            section.recipe.Add(new SerializedUpdate(MachinePart.LeftRetort, PotionToElementsList(recipe.leftRetort)));
            section.recipe.Add(new SerializedUpdate(MachinePart.SpiralVessel, PotionToElementsList(recipe.spiralVessel)));
            section.recipe.Add(new SerializedUpdate(MachinePart.LeftFurnace, ProductToList(recipe.leftFurnace)));
            section.recipe.Add(new SerializedUpdate(MachinePart.TripletVesselLeft, PotionToElementsList(recipe.tripletVesselLeft)));
            section.recipe.Add(new SerializedUpdate(MachinePart.TripletVesselCenter, PotionToElementsList(recipe.tripletVesselCenter)));
            section.recipe.Add(new SerializedUpdate(MachinePart.TripletVesselRight, PotionToElementsList(recipe.tripletVesselRight)));
            return section;
        }


        public static void ChangeLegendaryRecipeSelect(string name, PartUpdate[] updates)
        {
            LegendaryRecipe recipe = LegendaryRecipe.GetByName(name);
            foreach (PartUpdate update in updates)
            {
                if (update.partID == MachinePart.RightFurnace || update.partID == MachinePart.LeftFurnace)
                {
                    SetMachinePart(recipe, update.partID, update.product);
                }
                else
                {
                    PotionEffect[] effects;
                    if (update.effects.Length > 5)
                    {
                        effects = new PotionEffect[] { update.effects[0], update.effects[1], update.effects[2], update.effects[3], update.effects[4] };
                    }
                    else if (update.effects.Length == 0)
                    {
                        SetMachinePart(recipe, update.partID, null);
                        continue;
                    }
                    else
                    {
                        effects = update.effects;
                    }
                    (GetMachinePart(recipe, update.partID) as DesiredItemPotionEffectsScriptableObject).effects = effects;
                }
            }
        }

        static void ParseConfig(string config)
        {
            config = config.Replace("\r", string.Empty);
            string[] split = config.Split('\n');
            foreach (string option in split)
            {
                string[] div = option.Split(':');
                string head = div[0];
                string context = div[1].Substring(1);
                switch (head)
                {
                    case "entry":
                        {
                            target_recipes = context;
                        }
                        break;
                    case "error_off_entry":
                        {
                            bool.TryParse(context, out error_op);
                        }
                        break;
                }
            }
        }

        static void DeserializeRecipes()
        {
            string target = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Recipe Blueprints/" + target_recipes + ".json";
            if (!File.Exists(target))
            {
                if (error_op)
                    Debug.LogError("No alternative recipe JSON could be found at " + target + ".");
                return;
            }
            SerializedRecipeList json = Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedRecipeList>(File.ReadAllText(target));
            List<PartUpdate> update = new List<PartUpdate>();
            List<PotionEffect> effects = new List<PotionEffect>();

            foreach (SerializedRecipe recipe in json.recipes)
            {
                foreach (SerializedUpdate section in recipe.recipe)
                {
                    if (section.part == MachinePart.RightFurnace || section.part == MachinePart.LeftFurnace)
                    {
                        update.Add(new PartUpdate(section.part, null, AlchemyMachineProduct.GetByName(section.components.Count > 0 ? section.components[0] : null)));
                    }
                    else
                    {
                        foreach (string effectString in section.components)
                        {
                            effects.Add(PotionEffect.GetByName(effectString));
                        }
                        update.Add(new PartUpdate(section.part, effects.ToArray()));
                        effects.Clear();
                    }
                }

                if (update.Count > 0)
                {
                    ChangeLegendaryRecipeSelect(recipe.name, update.ToArray());
                    update.Clear();
                }
            }
        }


        [HarmonyPatch(typeof(AlchemyMachineObject), nameof(AlchemyMachineObject.GetSuitableLegendaryRecipe))]
        [HarmonyPrefix]
        public static bool PreLegendaryCheck(LegendaryRecipe __result)
        {
            AlchemyMachineObject machine = Managers.Ingredient.alchemyMachineSubManager.alchemyMachine;
            LegendaryRecipe result = null;
            foreach (LegendaryRecipe legendaryRecipe in LegendaryRecipe.allLegendaryRecipes)
            {
                bool flag = false;
                for (int i = 0; i < machine.slots.Length; i++)
                {
                    AlchemyMachineSlot slot = (AlchemyMachineSlot)i;
                    if (!machine.IsSuitableItemInSlot(legendaryRecipe.DesiredItemPerSlot(slot), slot))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    result = legendaryRecipe;
                    break;
                }
            }

            if(result && machine.tooFewIngredients != 0)
            {
                lastMinimum = machine.tooFewIngredients;
                machine.tooFewIngredients = 0;
            }
            else if(!result && machine.tooFewIngredients == 0)
            {
                machine.tooFewIngredients = lastMinimum;
            }

            return result;
        }

        [HarmonyPatch(typeof(AlchemyMachineObject), nameof(AlchemyMachineObject.OnBrewAnimationDone))]
        [HarmonyPostfix]
        public static void PostBrew()
        {
            AlchemyMachineObject machine = Managers.Ingredient.alchemyMachineSubManager.alchemyMachine;
            if(machine.tooFewIngredients == 0)
                machine.tooFewIngredients = lastMinimum;
        }


        static void AddSavedRecipes(string savedRecipes)
        {
            SerializedState deletedRecipes = Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedState>(savedRecipes);
            foreach (SerializedSavedRecipe rec in deletedRecipes.recipes)
            {
                int index = -1;
                Bookmark.SerializedBookmark bookmark = rec.mark.ToDefault(ref index);
                PotionCraft.ObjectBased.UIElements.Books.RecipeBook.SerializedRecipe recipe = rec.content;

                RecipeBook book = RecipeBook.Instance;
                IRecipeBookPageContent newRecipe = PotionCraft.ObjectBased.UIElements.Books.RecipeBook.SerializedRecipe.DeserializeRecipe(recipe);
                book.savedRecipes[index] = newRecipe;
                book.UpdateBookmarkIcon(index);
            }

            if (deletedRecipes.currentMachineContent != null && Managers.Ingredient.alchemyMachineSubManager.alchemyMachine != null)
            {
                Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnResultItem(AlchemyMachineProduct.GetFromSerializedObject(deletedRecipes.currentMachineContent));
                Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.brewAnimationMainController.StartAnimation();
                Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.brewAnimationMainController.progressTime = PotionCraft.Settings.Settings<PotionCraft.ObjectBased.AlchemyMachine.Settings.BrewAnimationControllerSettings>.Asset.totalAnimationTime - 0.1f;
            }
        }

        static void RemoveSavedRecipes(List<IRecipeBookPageContent> removedRecipes)
        {
            foreach (IRecipeBookPageContent targetRecipe in removedRecipes)
            {
                RecipeBook.Instance.EraseRecipe(targetRecipe);
            }
        }


        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadFile))]
        [HarmonyPrefix]
        public static bool OnFileLoad(PotionCraft.SaveFileSystem.File saveFile)
        {
            string tempURL = saveFile.url.Substring(saveFile.url.LastIndexOf('/')+1);
            currentLoadedFile = tempURL.Substring(0, tempURL.Length - 7);
            return true;
        }


        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.SaveProgressToPool))]
        [HarmonyPrefix]
        public static void BeforeFileSave()
        {
            // SAVE CUSTOM RECIPES
            int count = -1;
            List<IRecipeBookPageContent> savedRecipes = RecipeBook.Instance.savedRecipes;
            List<IRecipeBookPageContent> removedRecipes = new List<IRecipeBookPageContent>();

            List<SerializedSavedRecipe> recipesToSave = new List<SerializedSavedRecipe>();
            foreach (IRecipeBookPageContent savedRecipe in savedRecipes)
            {
                count++;
                if (savedRecipe == null)
                {
                    continue;
                }

                switch (savedRecipe.GetRecipeBookPageContentType())
                {
                    case RecipeBookPageContentType.AlchemyMachineProduct:
                    case RecipeBookPageContentType.Salt:
                        BookmarkControllersGroupController controller = RecipeBook.Instance.bookmarkControllersGroupController;
                        Bookmark bookmarkByIndex = controller.GetBookmarkByIndex(count);

                        if (bookmarkByIndex.gameObject.activeSelf)
                        {
                            SerializedSavedRecipe recipe = new SerializedSavedRecipe();
                            var serializedBookmark = bookmarkByIndex.GetSerialized();
                            recipe.content = savedRecipe.GetSerializedRecipe();
                            recipe.mark = new SerializedSafeBookmark(serializedBookmark.position.x, serializedBookmark.position.y, serializedBookmark.prefabIndex, serializedBookmark.isMirrored, count);

                            removedRecipes.Add(savedRecipe);
                            recipesToSave.Add(recipe);
                        }
                        else
                        {
                            bookmarkByIndex.gameObject.SetActive(true);
                        }
                        break;
                }
            }

            SerializedState state = new SerializedState();
            state.recipes = recipesToSave;
            if (Managers.Ingredient.alchemyMachineSubManager.alchemyMachine != null && Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedInventoryItem != null)
            {
                state.currentMachineContent = Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedInventoryItem.GetSerializedAlchemyMachineProduct();

                Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedItem.DestroyItem();
                Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedInventoryItem = null;
                Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.finishLegendarySubstanceWindow.previousAlchemyMachineProductRecipe = null;
                Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.finishLegendarySubstanceWindow.Show(false, true);
            }

            string serialized = Newtonsoft.Json.JsonConvert.SerializeObject(state, Newtonsoft.Json.Formatting.Indented);
            stringToBeSaved = serialized;

            RemoveSavedRecipes(removedRecipes);
        }

        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.SaveProgressToPool))]
        [HarmonyPostfix]
        public static void OnFileSave(PotionCraft.SaveFileSystem.File __result)
        {
            int count = -1;
            List<IRecipeBookPageContent> savedRecipes = RecipeBook.Instance.savedRecipes;
            foreach (IRecipeBookPageContent savedRecipe in savedRecipes)
            {
                count++;
                if (savedRecipe == null)
                {
                    continue;
                }

                switch (savedRecipe.GetRecipeBookPageContentType())
                {
                    case RecipeBookPageContentType.AlchemyMachineProduct:
                    case RecipeBookPageContentType.Salt:
                        RecipeBook.Instance.bookmarkControllersGroupController.GetBookmarkByIndex(count).gameObject.SetActive(false);
                        break;
                }
            }


            string tempURL = __result.url.Substring(__result.url.LastIndexOf('/') + 1);
            currentSavedFile = tempURL.Substring(0, tempURL.Length - 7);

            string target = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Saved Recipes/" + currentSavedFile + ".json";
            File.WriteAllText(target, stringToBeSaved);

            AddSavedRecipes(stringToBeSaved);
        }

        [HarmonyPatch(typeof(RecipeBook), nameof(RecipeBook.OnLoad))]
        [HarmonyPostfix]
        public static void PostRecipeInit()
        {
            int count = -1;
            List<IRecipeBookPageContent> savedRecipes = RecipeBook.Instance.savedRecipes;
            foreach (IRecipeBookPageContent savedRecipe in savedRecipes)
            {
                count++;
                if (savedRecipe == null)
                {
                    continue;
                }

                switch (savedRecipe.GetRecipeBookPageContentType())
                {
                    case RecipeBookPageContentType.AlchemyMachineProduct:
                    case RecipeBookPageContentType.Salt:
                        RecipeBook.Instance.bookmarkControllersGroupController.GetBookmarkByIndex(count).gameObject.SetActive(false);
                        break;
                }
            }

            string target = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Saved Recipes/"+currentLoadedFile+".json";
            if (!File.Exists(target))
                return;
            
            AddSavedRecipes(File.ReadAllText(target));
        }

        [HarmonyPatch(typeof(PotionCraft.SaveFileSystem.File), nameof(PotionCraft.SaveFileSystem.File.Remove))]
        [HarmonyPostfix]
        public static void PostSaveDelete(PotionCraft.SaveFileSystem.File __instance)
        {
            string tempURL = __instance.url.Substring(__instance.url.LastIndexOf('/') + 1);
            string toBeDeleted = tempURL.Substring(0, tempURL.Length - 7);

            string target = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Saved Recipes/" + toBeDeleted + ".json";
            if(File.Exists(target))
            {
                File.Delete(target);
            }
        }
    }
}
