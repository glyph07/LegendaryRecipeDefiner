using System;
using System.IO;
using System.Collections.Generic;

using BepInEx;
using HarmonyLib;

using UnityEngine;

using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.ManagersSystem.Ingredient;
using PotionCraft.QuestSystem.DesiredItems;
using PotionCraft.ObjectBased;
using PotionCraft.ObjectBased.AlchemyMachine;
using PotionCraft.ObjectBased.AlchemyMachineProduct;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.FinishLegendarySubstanceMenu;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


namespace LegendaryRecipeDefiner
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class LegendaryRecipeDefiner : BaseUnityPlugin
    {
        public const string pluginGuid = "fgvb.potioncraft.legendaryrecipedefiner";
        public const string pluginName = "LegendaryRecipeDefiner";
        public const string pluginVersion = "1.0.3.0";

        
        protected static string current_directory = "";
        protected static string currentLoadedFile = "";
        protected static string currentSavedFile = "";
        private static string stringToBeSaved = "";

        private static IDeserializer deserializer;
        private static ISerializer serializer;

        private static int lastMinimum = -1;
        private static bool hasSubstanceLoadedExternally = false;

        private static LRDSettings settings = null;
        public static LRDSettings Settings
        {
            get => settings;
        }
        public static LegendaryRecipeDefiner Instance { get; set; } = null;


        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                Harmony.CreateAndPatchAll(typeof(LegendaryRecipeDefiner));
                deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                current_directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                CreateStructure();
                VerifyDataIntegrity();
            }
        }

        private static void CreateStructure()
        {
            string target = current_directory + "/settings.yml";
            if (!File.Exists(target))
            {
                File.WriteAllText(target, serializer.Serialize(new LRDSettings()));
            }
            string config = File.ReadAllText(target);
            settings = ParseConfig(config);

            target = current_directory + "/Recipe Blueprints";
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
                string[] jsonFiles = Directory.GetFiles(current_directory, "*.yml", SearchOption.TopDirectoryOnly);
                foreach (string fileName in jsonFiles)
                {
                    string name = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                    if (name != "settings.yml")
                    {
                        File.Move(fileName, target + "/" + name);
                    }
                }
            }

            target = current_directory + "/Saved Recipes";
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }
        }

        private static void VerifyDataIntegrity()
        {
            string target = current_directory + "/Recipe Blueprints";
            string[] jsonFiles = Directory.GetFiles(target, "*.json", SearchOption.AllDirectories);
            foreach (string fileName in jsonFiles)
            {
                SerializedRecipeList deser = deserializer.Deserialize<SerializedRecipeList>(File.ReadAllText(fileName));
                string newFileName = fileName.Substring(0, fileName.LastIndexOf(".")) + ".yml";
                File.WriteAllText(newFileName, serializer.Serialize(deser));
                File.Delete(fileName);
            }

            target = current_directory + "/Saved Recipes";
            jsonFiles = Directory.GetFiles(target, "*.json", SearchOption.AllDirectories);
            foreach (string fileName in jsonFiles)
            {
                SerializedState deser = deserializer.Deserialize<SerializedState>(File.ReadAllText(fileName));
                string newFileName = fileName.Substring(0, fileName.LastIndexOf(".")) + ".yml";
                File.WriteAllText(newFileName, serializer.Serialize(deser));
                File.Delete(fileName);
            }
        }

        public static LRDSettings ParseConfig(string config)
        {
            return deserializer.Deserialize<LRDSettings>(config);
        }


        [Serializable]
        public enum MachinePart
        {
            Unknown = -1, RightRetort, RightDripper, RhombusVessel, TriangularVessel, RightFurnace, DoubleVessel,
            FloorVessel, LeftDripper, LeftRetort, SpiralVessel, LeftFurnace, TripletVesselLeft, TripletVesselCenter, TripletVesselRight,
            Last
        }

        [Serializable]
        public class LRDSettings
        {
            public string entry = "recipes_1.0";
            public bool error_off_entry = false;
            public bool allow_less_ingredients = true;
            public bool allow_more_ingredients = true;
            public bool allow_no_ingredients = true;
        }


        [Serializable]
        public class SerializedUpdate
        {
            public MachinePart part;

            public List<string> components;

            public SerializedUpdate()
            {
                part = MachinePart.Unknown;
                components = new List<string>();
            }

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

            public SerializedSafeBookmark()
            {
                positionX = -1;
                positionY = -1;
                prefabIndex = -1;
                isMirrored = false;
                index = -1;
            }

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
            public PotionCraft.ObjectBased.UIElements.Books.RecipeBook.SerializedRecipe content = null;
            public SerializedSafeBookmark mark = new SerializedSafeBookmark();
        }


        [Serializable]
        public class SerializedSafeLedgeTargetData
        {
            public bool isItemOnLedge = false;
            public float ledgePositionX = 0, ledgePositionY = 0;
            public int itemIndexOnLedge = -1;

            public SerializedSafeLedgeTargetData() { }
            public SerializedSafeLedgeTargetData(bool _isItemOnLedge, Vector2 ledgePosition, int _itemIndexOnLedge)
            {
                isItemOnLedge = _isItemOnLedge;
                ledgePositionX = ledgePosition.x;
                ledgePositionY = ledgePosition.y;
                itemIndexOnLedge = _itemIndexOnLedge;
            }

            public static SerializedLedgeTargetData SafeToNormal(SerializedSafeLedgeTargetData value)
            {
                SerializedLedgeTargetData result = new SerializedLedgeTargetData();
                result.isItemOnLedge = value.isItemOnLedge;
                result.ledgePosition = new Vector2(value.ledgePositionX, value.ledgePositionY);
                result.itemIndexOnLedge = value.itemIndexOnLedge;
                return result;
            }
        }

        [Serializable]
        public class SerializedSafeItemFromInventory
        {
            public string typeName = "";
            public float positionX = -1, positionY = 0;
            public float eulerAnglesX = 0, eulerAnglesY = 0, eulerAnglesZ = 0;
            public string inventoryItemName = "";
            public List<SerializedSafeLedgeTargetData> ledgeDataList = null;
            public string data = "";

            public SerializedSafeItemFromInventory() { }
            public SerializedSafeItemFromInventory(string _typeName, Vector2 position, Vector3 eulerAngles, string _inventoryItemName, List<SerializedLedgeTargetData> _ledgeDataList, string _data)
            {
                typeName = _typeName;
                positionX = position.x;
                positionY = position.y;
                eulerAnglesX = eulerAngles.x;
                eulerAnglesY = eulerAngles.y;
                eulerAnglesZ = eulerAngles.z;
                inventoryItemName = _inventoryItemName;

                if (_ledgeDataList != null)
                {
                    ledgeDataList = new List<SerializedSafeLedgeTargetData>();
                    foreach (SerializedLedgeTargetData value in _ledgeDataList)
                    {
                        ledgeDataList.Add(new SerializedSafeLedgeTargetData(value.isItemOnLedge, value.ledgePosition, value.itemIndexOnLedge));
                    }
                }

                data = _data;
            }

                public static SerializedItemFromInventory SafeToNormal(SerializedSafeItemFromInventory value)
            {
                SerializedItemFromInventory result = new SerializedItemFromInventory();
                result.typeName = value.typeName;
                result.position = new Vector2(value.positionX, value.positionY);
                result.eulerAngles = new Vector3(value.eulerAnglesX, value.eulerAnglesY, value.eulerAnglesZ);
                result.inventoryItemName = value.inventoryItemName;
                result.ledgeDataList = new List<SerializedLedgeTargetData>();

                if (value.ledgeDataList != null)
                {
                    foreach (SerializedSafeLedgeTargetData data in value.ledgeDataList)
                    {
                        result.ledgeDataList.Add(SerializedSafeLedgeTargetData.SafeToNormal(data));
                    }
                }
                result.data = value.data;
                return result;
            }
        }


        [Serializable]
        public class SafeColor
        {
            public float r = 0, g = 0, b = 0, a = 1;

            public SafeColor() { }
            public SafeColor(Color color)
            {
                r = color.r;
                g = color.g;
                b = color.b;
                a = color.a;
            }

            public Color ToColor()
            {
                return new Color(r, g, b, a);
            }
        }

        [Serializable]
        public class SafeColorList
        {
            public List<SafeColor> colorsList = new List<SafeColor>();
        }

        [Serializable]
        public class SafeSkinSettings
        {
            public string currentIconName = string.Empty;
            public SafeColor currentIconColor1;
            public SafeColor currentIconColor2;
            public SafeColor currentIconColor3;
            public SafeColor currentIconColor4;
            public string currentCustomTitle = string.Empty;
            public bool isCurrentTitleCustom;
            public string currentDescription = string.Empty;
            public bool isCurrentIconCustom;
            public bool isCurrentIconColor1Custom;
            public bool isCurrentIconColor2Custom;
            public bool isCurrentIconColor3Custom;
            public bool isCurrentIconColor4Custom;
            public int colorsCount;

            public SafeSkinSettings() { }
            public SafeSkinSettings(SerializedAlchemyMachineProductSkinSettings settings)
            {
                currentIconName = settings.currentIconName;
                currentIconColor1 = new SafeColor(settings.currentIconColor1);
                currentIconColor2 = new SafeColor(settings.currentIconColor2);
                currentIconColor3 = new SafeColor(settings.currentIconColor3);
                currentIconColor4 = new SafeColor(settings.currentIconColor4);
                currentCustomTitle = settings.currentCustomTitle;
                isCurrentTitleCustom = settings.isCurrentTitleCustom;
                currentDescription = settings.currentDescription;
                isCurrentIconCustom = settings.isCurrentIconCustom;
                isCurrentIconColor1Custom = settings.isCurrentIconColor1Custom;
                isCurrentIconColor2Custom = settings.isCurrentIconColor2Custom;
                isCurrentIconColor3Custom = settings.isCurrentIconColor3Custom;
                isCurrentIconColor4Custom = settings.isCurrentIconColor4Custom;
                colorsCount = settings.colorsCount;
            }

            public SerializedAlchemyMachineProductSkinSettings ToSettings()
            {
                SerializedAlchemyMachineProductSkinSettings result = new SerializedAlchemyMachineProductSkinSettings();
                result.currentIconName = currentIconName;
                result.currentIconColor1 = currentIconColor1.ToColor();
                result.currentIconColor2 = currentIconColor2.ToColor();
                result.currentIconColor3 = currentIconColor3.ToColor();
                result.currentIconColor4 = currentIconColor4.ToColor();
                result.currentCustomTitle = currentCustomTitle;
                result.isCurrentTitleCustom = isCurrentTitleCustom;
                result.currentDescription = currentDescription;
                result.isCurrentIconCustom = isCurrentIconCustom;
                result.isCurrentIconColor1Custom = isCurrentIconColor1Custom;
                result.isCurrentIconColor2Custom = isCurrentIconColor2Custom;
                result.isCurrentIconColor3Custom = isCurrentIconColor3Custom;
                result.isCurrentIconColor4Custom = isCurrentIconColor4Custom;
                result.colorsCount = colorsCount;

                return result;
            }
        }

        [Serializable]
        public class SafeComponentList
        {
            public List<SerializedRecipeReagent> components = new List<SerializedRecipeReagent>();
            public List<AlchemyMachineSlot> componentsSlots = new List<AlchemyMachineSlot>();
        }

        [Serializable]
        public class SerializedSafeRecipeData
        {
            public string name = "";
            public List<SafeColorList> colorsList = null;
            public SafeSkinSettings skinSettings = null;
            public SafeComponentList usedComponents = null;
            public RecipeBookPageContentType type;

            public SerializedSafeRecipeData() { }
            public SerializedSafeRecipeData(SerializedAlchemyMachineProductRecipeData data)
            {
                name = data.name;
                colorsList = new List<SafeColorList>();
                foreach (PotionCraft.Utils.CustomAnimator.ColorAnimator.ColorsList list in data.colorsList)
                {
                    SafeColorList colors = new SafeColorList();
                    foreach (Color color in list.colors)
                        colors.colorsList.Add(new SafeColor(color));
                    colorsList.Add(colors);
                }
                skinSettings = new SafeSkinSettings(data.skinSettings);
                usedComponents = new SafeComponentList();
                usedComponents.components = data.usedComponents.GetComponents();
                usedComponents.componentsSlots = data.usedComponents.GetComponentsSlots();
                type = data.GetRecipeBookPageContentType();
            }

            public SerializedAlchemyMachineProductRecipeData ToData()
            {
                SerializedAlchemyMachineProductRecipeData result = new SerializedAlchemyMachineProductRecipeData(type);
                result.name = name;
                result.colorsList = new List<PotionCraft.Utils.CustomAnimator.ColorAnimator.ColorsList>();
                foreach(SafeColorList list in colorsList)
                {
                    PotionCraft.Utils.CustomAnimator.ColorAnimator.ColorsList colors = new PotionCraft.Utils.CustomAnimator.ColorAnimator.ColorsList();
                    foreach(SafeColor color in list.colorsList)
                    {
                        colors.colors.Add(color.ToColor());
                    }
                    result.colorsList.Add(colors);
                }
                result.skinSettings = skinSettings.ToSettings();
                result.usedComponents = new PotionCraft.ManagersSystem.Potion.Entities.SerializedCompositeAlchemySubstanceComponents(usedComponents.components, usedComponents.componentsSlots);

                return result;
            }
        }


        [Serializable]
        public class SerializedState
        {
            public List<SerializedSavedRecipe> recipes = new List<SerializedSavedRecipe>();
            public SerializedSafeItemFromInventory currentMachineContent = null;
            public SerializedSafeRecipeData currentMachineRecipe = null;
            public PotionCraft.ObjectBased.UIElements.Books.RecipeBook.SerializedRecipe previousRecipe = null;
            public bool productChanged = false;
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


        public static InventoryItem GetMachinePart(LegendaryRecipe recipe, MachinePart partID)
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

        public static void SetMachinePart(LegendaryRecipe recipe, MachinePart partID, InventoryItem value)
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

        public static List<string> ProductToList(InventoryItem product)
        {
            if (product == null)
                return new List<string>();
            return new List<string> { (product as AlchemyMachineProduct).name };
        }

        public static List<string> PotionToElementsList(InventoryItem item)
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


        public static SerializedRecipe SerializeRecipe(string name)
        {
            LegendaryRecipe recipe = LegendaryRecipe.GetByName(name);
            if (recipe == null || name == "")
                return null;

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


        public static void CreateNewLegendaryRecipe(string name, PartUpdate[] updates)
        {

        }

        public static void ChangeLegendaryRecipeSelect(string name, PartUpdate[] updates)
        {
            LegendaryRecipe recipe = LegendaryRecipe.GetByName(name);
            if(recipe == null)
            {
                if (name != "")
                    CreateNewLegendaryRecipe(name, updates);
                return;
            }    

            foreach (PartUpdate update in updates)
            {
                if (update.partID == MachinePart.RightFurnace || update.partID == MachinePart.LeftFurnace)
                {
                    SetMachinePart(recipe, update.partID, update.product);
                }
                else if(update.effects == null || update.effects.Length == 0)
                {
                    SetMachinePart(recipe, update.partID, null);
                }
                else
                {
                    PotionEffect[] effects;
                    if (update.effects.Length > 5)
                    {
                        effects = new PotionEffect[] { update.effects[0], update.effects[1], update.effects[2], update.effects[3], update.effects[4] };
                    }
                    else
                    {
                        effects = update.effects;
                    }

                    if ((GetMachinePart(recipe, update.partID) as DesiredItemPotionEffectsScriptableObject))
                    {
                        (GetMachinePart(recipe, update.partID) as DesiredItemPotionEffectsScriptableObject).effects = effects;
                    }
                    else if(settings.allow_more_ingredients)
                    {
                        DesiredItemPotionEffectsScriptableObject dipeso = (DesiredItemPotionEffectsScriptableObject)ScriptableObject.CreateInstance(typeof(DesiredItemPotionEffectsScriptableObject));
                        dipeso.effects = effects;
                        dipeso.noMoreEffects = true;
                        SetMachinePart(recipe, update.partID, dipeso);
                    }
                }
            }
        }

        public static void DeserializeRecipes(string yamlLocation)
        {
            string target = yamlLocation;
            if (!File.Exists(target))
            {
                if (settings.error_off_entry)
                    Debug.LogError("No alternative recipe YAML could be found at " + target + ".");
                return;
            }

            SerializedRecipeList json = deserializer.Deserialize<SerializedRecipeList>(File.ReadAllText(target));
            List<PartUpdate> update = new List<PartUpdate>();
            List<PotionEffect> effects = new List<PotionEffect>();

            foreach (SerializedRecipe recipe in json.recipes)
            {
                bool[] partUpdates = new bool[(int)MachinePart.Last];
                if(recipe.recipe != null)
                foreach (SerializedUpdate section in recipe.recipe)
                {
                    if (section.part == MachinePart.Unknown)
                        continue;

                    // add alchemy ingredient requirement
                    if (section.part == MachinePart.RightFurnace || section.part == MachinePart.LeftFurnace)
                    {
                        string nameToAsk = section.components.Count > 0 ? section.components[0] : null;
                        update.Add(new PartUpdate(section.part, null, (nameToAsk == null || nameToAsk == "") ? null:AlchemyMachineProduct.GetByName(nameToAsk)));
                    }
                    // add potion requirement
                    else
                    {
                        foreach (string effectString in section.components)
                        {
                            effects.Add(PotionEffect.GetByName(effectString, true));
                        }
                        update.Add(new PartUpdate(section.part, effects.ToArray()));
                        effects.Clear();
                    }
                    partUpdates[(int)section.part] = true;
                }

                for(int i=0; i<(int)MachinePart.Last; i++)
                {
                    if (!partUpdates[i])
                    {
                        update.Add(new PartUpdate((MachinePart)i, null));
                    }
                }

                // only update recipe when given valid updates
                if (update.Count > 0)
                {
                    ChangeLegendaryRecipeSelect(recipe.name, update.ToArray());
                    update.Clear();
                }
            }
        }


        [HarmonyPatch(typeof(AlchemyMachineObject), nameof(AlchemyMachineObject.GetSuitableLegendaryRecipe))]
        [HarmonyPrefix]
        private static bool PreLegendaryCheck()
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

            // allows for recipes with less ingredients than usual (mysterious substances still generate normally)
            if(result && machine.tooFewIngredients != 0 && settings.allow_less_ingredients)
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
        private static void PostBrew()
        {
            AlchemyMachineObject machine = Managers.Ingredient.alchemyMachineSubManager.alchemyMachine;
            if(machine.tooFewIngredients == 0)
                machine.tooFewIngredients = lastMinimum;
        }


        public static void AddSavedRecipes(SerializedState state)
        {
            foreach (SerializedSavedRecipe rec in state.recipes)
            {
                int index = -1;
                rec.mark.ToDefault(ref index);
                PotionCraft.ObjectBased.UIElements.Books.RecipeBook.SerializedRecipe recipe = rec.content;

                RecipeBook book = RecipeBook.Instance;
                IRecipeBookPageContent newRecipe = PotionCraft.ObjectBased.UIElements.Books.RecipeBook.SerializedRecipe.DeserializeRecipe(recipe);
                book.savedRecipes[index] = newRecipe;
                book.UpdateBookmarkIcon(index);
            }
        }

        public static void RemoveSavedRecipes(List<IRecipeBookPageContent> removedRecipes)
        {
            foreach (IRecipeBookPageContent targetRecipe in removedRecipes)
            {
                RecipeBook.Instance.EraseRecipe(targetRecipe);
            }
        }


        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadFile))]
        [HarmonyPrefix]
        private static bool BeforeFileLoad(PotionCraft.SaveFileSystem.File saveFile)
        {
            DeserializeRecipes(current_directory + "/Recipe Blueprints/" + settings.entry + ".yml");

            string tempURL = saveFile.url.Substring(saveFile.url.LastIndexOf('/')+1);
            currentLoadedFile = tempURL.Substring(0, tempURL.Length - 7);
            return true;
        }


        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.SaveProgressToPool))]
        [HarmonyPrefix]
        private static void BeforeFileSave()
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
            if(Managers.Ingredient.alchemyMachineSubManager.alchemyMachine != null)
            {
                state.previousRecipe = (FinishLegendarySubstanceWindow.Instance.previousAlchemyMachineProductRecipe == null) ? null : FinishLegendarySubstanceWindow.Instance.previousAlchemyMachineProductRecipe.GetSerializedRecipe();
                state.productChanged = FinishLegendarySubstanceWindow.Instance.alchemyMachineProductChangedAfterSavingRecipe;

                if (Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedItem != null)
                {
                    SerializedItemFromInventory save = Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedItem.GetSerializedItemFromInventory();
                    state.currentMachineContent = new SerializedSafeItemFromInventory(save.typeName, save.position, save.eulerAngles, save.inventoryItemName, save.ledgeDataList, save.data);
                    state.currentMachineRecipe = new SerializedSafeRecipeData((SerializedAlchemyMachineProductRecipeData)Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedInventoryItem.GetSerializedRecipeData().Clone());

                    AlchemyMachineProductItem item = Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedItem;
                    item.DestroyItem();
                    Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.SpawnedInventoryItem = null;
                }
                FinishLegendarySubstanceWindow.Instance.previousAlchemyMachineProductRecipe = null;
                FinishLegendarySubstanceWindow.Instance.alchemyMachineProductChangedAfterSavingRecipe = true;
                FinishLegendarySubstanceWindow.Instance.Show(false, true);
            }

            string serialized = serializer.Serialize(state);
            stringToBeSaved = serialized;

            RemoveSavedRecipes(removedRecipes);
        }

        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.SaveProgressToPool))]
        [HarmonyPostfix]
        private static void OnFileSave(PotionCraft.SaveFileSystem.File __result)
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

            string target = current_directory + "/Saved Recipes/" + settings.entry + " - " + currentSavedFile + ".yml";
            File.WriteAllText(target, stringToBeSaved);

            SerializedState state = deserializer.Deserialize<SerializedState>(stringToBeSaved);
            AddSavedRecipes(state);
            if (Managers.Ingredient.alchemyMachineSubManager.alchemyMachine != null)
            {
                Managers.SaveLoad.SelectedProgressState.previousAlchemyMachineProductRecipe = state.previousRecipe;
                Managers.SaveLoad.SelectedProgressState.alchemyMachineProductChangedAfterSavingRecipe = state.productChanged;

                if (state.currentMachineContent != null)
                {
                    Managers.SaveLoad.SelectedProgressState.serializedCurrentAlchemyMachineProduct = state.currentMachineRecipe.ToData();
                    Managers.SaveLoad.SelectedProgressState.withSpawnedProduct = true;

                    SerializedItemFromInventory item = SerializedSafeItemFromInventory.SafeToNormal(state.currentMachineContent);
                    AlchemyMachineProductItem.SpawnFromSerializedData(item, RoomIndex.Basement);

                    Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.brewAnimationMainController.StartAnimation();
                    Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.brewAnimationMainController.progressTime = PotionCraft.Settings.Settings<PotionCraft.ObjectBased.AlchemyMachine.Settings.BrewAnimationControllerSettings>.Asset.totalAnimationTime - 0.1f;
                    hasSubstanceLoadedExternally = true;
                    Managers.Ingredient.alchemyMachineSubManager.alchemyMachine.alchemyMachineBox.resultItemSpawner.OnLoad();
                    hasSubstanceLoadedExternally = false;
                }

                FinishLegendarySubstanceWindow.Instance.OnLoad();
            }
        }

        [HarmonyPatch(typeof(RecipeBook), nameof(RecipeBook.OnLoad))]
        [HarmonyPostfix]
        private static void PostRecipeInit()
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

            string target = current_directory + "/Saved Recipes/"+ settings.entry + " - " + currentLoadedFile + ".yml";
            if (!File.Exists(target))
                return;

            SerializedState deletedRecipes = deserializer.Deserialize<SerializedState>(File.ReadAllText(target));
            AddSavedRecipes(deletedRecipes);
        }

        [HarmonyPatch(typeof(ResultItemSpawner), nameof(ResultItemSpawner.OnLoad))]
        [HarmonyPrefix]
        private static void PreLegendarySpawn()
        {
            if (hasSubstanceLoadedExternally)
                return;

            string target = current_directory + "/Saved Recipes/" + settings.entry + " - " + currentLoadedFile + ".yml";
            if (!File.Exists(target))
                return;
            SerializedState deletedRecipes = deserializer.Deserialize<SerializedState>(File.ReadAllText(target));
            if (Managers.Ingredient.alchemyMachineSubManager.alchemyMachine != null)
            {
                Managers.SaveLoad.SelectedProgressState.previousAlchemyMachineProductRecipe = deletedRecipes.previousRecipe;
                Managers.SaveLoad.SelectedProgressState.alchemyMachineProductChangedAfterSavingRecipe = deletedRecipes.productChanged;

                if (deletedRecipes.currentMachineContent != null)
                {
                    Managers.SaveLoad.SelectedProgressState.withSpawnedProduct = true;
                    Managers.SaveLoad.SelectedProgressState.serializedCurrentAlchemyMachineProduct = deletedRecipes.currentMachineRecipe.ToData();

                    SerializedItemFromInventory item = SerializedSafeItemFromInventory.SafeToNormal(deletedRecipes.currentMachineContent);
                    AlchemyMachineProductItem.SpawnFromSerializedData(item, RoomIndex.Basement);
                }
            }
        }

        [HarmonyPatch(typeof(PotionCraft.SaveFileSystem.File), nameof(PotionCraft.SaveFileSystem.File.Remove))]
        [HarmonyPostfix]
        private static void PostSaveDelete(PotionCraft.SaveFileSystem.File __instance)
        {
            string tempURL = __instance.url.Substring(__instance.url.LastIndexOf('/') + 1);
            string toBeDeleted = tempURL.Substring(0, tempURL.Length - 7);

            string target = current_directory + "/Saved Recipes/" + settings.entry + " - " + toBeDeleted + ".yml";
            if(File.Exists(target))
            {
                File.Delete(target);
            }
        }


        [HarmonyPatch(typeof(AlchemyMachineObject), nameof(AlchemyMachineObject.GetIngredientsCount))]
        [HarmonyPostfix]
        private static void PostIngredientCheck(ref int __result)
        {
            if (__result == 0 && settings.allow_no_ingredients)
                __result = 1;
        }
    }
}
