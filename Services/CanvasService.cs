using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM.UI;
using Stunlock.Core;
using Stunlock.Localization;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.UIObjectUtils;
using StringComparison = System.StringComparison;

// UI ideas
// how to summon familiars, choose class spells, etc with a menu on the client? could just have the various UI elements correspond to respective familiar lists, class spells, etc.
// like if menu 1 has 4 buttons they choose the spell to request from the server if the server detects a change in a certain component which the client would do when they click a button
// display class somewhere in UI

namespace Eclipse.Services;
internal class CanvasService
{
    static readonly bool ExperienceBar = Plugin.Leveling;
    static readonly bool ShowPrestige = Plugin.Prestige;
    static readonly bool LegacyBar = Plugin.Legacies;
    static readonly bool ExpertiseBar = Plugin.Expertise;

    static readonly WaitForSeconds Delay = new(1);

    static UICanvasBase UICanvasBase;
    static Canvas Canvas;
    public static List<int> PlayerData;

    static GameObject ExperienceBarGameObject;
    static Image ExperienceFill;
    static Image ExperienceIcon;
    static float ExperienceProgress = 0f;

    static GameObject LegacyBarGameObject;
    static Image LegacyFill;
    static LocalizedText LegacyText;
    static Image LegacyIcon;
    static float LegacyProgress = 0f;

    static GameObject ExpertiseBarGameObject;
    static Image ExpertiseFill;
    static LocalizedText ExpertiseText;
    static Image ExpertiseIcon;
    static float ExpertiseProgress = 0f;
    public enum BloodType
    {
        Worker,
        Warrior,
        Scholar,
        Rogue,
        Mutant,
        VBlood,
        None,
        GateBoss,
        Draculin,
        Immortal,
        Creature,
        Brute
    }
    public enum WeaponType
    {
        Sword,
        Axe,
        Mace,
        Spear,
        Crossbow,
        GreatSword,
        Slashers,
        Pistols,
        Reaper,
        Longbow,
        Whip,
        Unarmed,
        FishingPole
    }

    static readonly Dictionary<BloodType, string> BloodIcons = new()
    {
        { BloodType.Worker, "BloodType_Worker_Small" },
        { BloodType.Warrior, "BloodType_Warrior_Small" },
        { BloodType.Scholar, "BloodType_Scholar_Small" },
        { BloodType.Rogue, "BloodType_Rogue_Small" },
        { BloodType.Mutant, "BloodType_Mutant_Small" },
        { BloodType.Draculin, "BloodType_Draculin_Small" },
        { BloodType.Immortal, "BloodType_Immortal_Small" },
        { BloodType.Creature, "BloodType_Creature_Small" },
        { BloodType.Brute, "BloodType_Brute_Small" }
    };

    static Sprite simpleFill;

    static readonly Dictionary<BloodType, Sprite> BloodSprites = [];
    static void InitializeSprites()
    {
        List<string> spriteNames = [..BloodIcons.Values];
        Il2CppArrayBase<Sprite> allSprites = Resources.FindObjectsOfTypeAll<Sprite>();

        simpleFill = allSprites.First(sprite => sprite.name == "SimpleProgressBar_Fill");

        var matchedSprites = allSprites
            .Where(sprite => spriteNames.Contains(sprite.name))
            .ToDictionary(sprite => BloodIcons.First(pair => pair.Value == sprite.name).Key, sprite => sprite);

        foreach (var pair in matchedSprites)
        {
            Core.Log.LogInfo($"BloodType: {pair.Key} | Sprite: {pair.Value.name}");
            BloodSprites[pair.Key] = pair.Value;
        }
    }

    public static Entity LocalCharacter;
    public static bool Active = false;
    public CanvasService(UICanvasBase canvas)
    {
        // Instantiate the ExperienceBar from the PlayerEntryPrefab and find the BotomBarCanvas
        UICanvasBase = canvas;
        InitializeBars(canvas);
        try
        {
            InitializeSprites();
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Failed to initialize blood sprites: {ex}");
        }
    }
    public static List<int> ParseString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }
        return configString.Split(',').Select(int.Parse).ToList();
    }
    public static IEnumerator CanvasUpdateLoop() // need to find another component, can abstract data to whatever just need something relatively unused
    {
        while (true)
        {
            if (!Active) Active = true;

            ExperienceProgress = PlayerData[0] / 100f;
            LegacyProgress = PlayerData[1] / 100f;
            int legacyType = PlayerData[2];
            ExpertiseProgress = PlayerData[3] / 100f;
            int expertiseType = PlayerData[4];

            Core.Log.LogInfo($"Experience: {ExperienceProgress} | Legacy: {LegacyProgress} {legacyType} | Expertise: {ExpertiseProgress} {expertiseType}");

            if (ExperienceProgress != ExperienceFill.fillAmount && ExperienceProgress != 0f)
            {
                ExperienceFill.fillAmount = ExperienceProgress;
            }

            if (LegacyProgress != LegacyFill.fillAmount && LegacyProgress != 0f)
            {
                LegacyFill.fillAmount = LegacyProgress;
                string bloodType = Enum.GetName(typeof(BloodType), legacyType);
                if (LegacyText.GetText() != bloodType)
                {
                    LegacyText.ForceSet(bloodType);
                    if (BloodSprites.TryGetValue((BloodType)legacyType, out Sprite sprite))
                    {
                        LegacyIcon.sprite = sprite;
                        if (!LegacyIcon.enabled) LegacyIcon.enabled = true;
                    }
                }
            }

            if (ExpertiseProgress != ExpertiseFill.fillAmount && ExpertiseProgress != 0f)
            {
                ExpertiseFill.fillAmount = ExpertiseProgress;
                string weaponType = Enum.GetName(typeof(WeaponType), expertiseType);
                if (ExpertiseText.GetText() != weaponType)
                {
                    ExpertiseText.ForceSet(weaponType);
                }
            }

            yield return Delay;
        }
    }
    static void InitializeBars(UICanvasBase canvas)
    {
        GameObject CanvasObject = FindTargetUIObject(canvas.transform.root, "BottomBarCanvas");
        Canvas bottomBarCanvas = CanvasObject.GetComponent<Canvas>();
        Canvas = bottomBarCanvas;
        GameObject objectPrefab = canvas.ProximityPlayerListOverlay.PlayerEntryPrefab.gameObject;

        // Instantiate all bars
        GameObject ExperienceBarObject = GameObject.Instantiate(objectPrefab);
        GameObject LegacyBarObject = GameObject.Instantiate(objectPrefab);
        GameObject ExpertiseBarObject = GameObject.Instantiate(objectPrefab);

        // Mark for DontDestroyOnLoad and move to VRisingWorld scene
        RectTransform ExperienceBarRectTransform = ExperienceBarObject.GetComponent<RectTransform>();
        RectTransform LegacyBarRectTransform = LegacyBarObject.GetComponent<RectTransform>();
        RectTransform ExpertiseBarRectTransform = ExpertiseBarObject.GetComponent<RectTransform>();

        GameObject.DontDestroyOnLoad(ExperienceBarObject);
        GameObject.DontDestroyOnLoad(LegacyBarObject);
        GameObject.DontDestroyOnLoad(ExpertiseBarObject);

        SceneManager.MoveGameObjectToScene(ExperienceBarObject, SceneManager.GetSceneByName("VRisingWorld"));
        SceneManager.MoveGameObjectToScene(LegacyBarObject, SceneManager.GetSceneByName("VRisingWorld"));
        SceneManager.MoveGameObjectToScene(ExpertiseBarObject, SceneManager.GetSceneByName("VRisingWorld"));

        // Set BottomBarCanvas as the parent for all bars
        ExperienceBarRectTransform.SetParent(bottomBarCanvas.transform, false);
        LegacyBarRectTransform.SetParent(bottomBarCanvas.transform, false);
        ExpertiseBarRectTransform.SetParent(bottomBarCanvas.transform, false);

        // Get MiniMap south icon on the compass to set location for now
        GameObject MiniMapSouthObject = FindTargetUIObject(canvas.transform.root, "S");
        RectTransform MiniMapSouthRectTransform = MiniMapSouthObject.GetComponent<RectTransform>();

        // Configure ExperienceBar
        ConfigureBar(ExperienceBarRectTransform, MiniMapSouthObject, MiniMapSouthRectTransform, CanvasObject.layer, 1.5f, "Experience", Color.green, 1);

        // Configure LegacyBar
        ConfigureBar(LegacyBarRectTransform, MiniMapSouthObject, MiniMapSouthRectTransform, CanvasObject.layer, 1.5f, "Legacy", new Color(1, 0, 0, 1), 2);

        // Configure ExpertiseBar
        ConfigureBar(ExpertiseBarRectTransform, MiniMapSouthObject, MiniMapSouthRectTransform, CanvasObject.layer, 1.5f, "Expertise", new Color(0.5f, 0.5f, 0.5f, 1), 3);

        // Assign GameObjects to respective fields
        ExperienceBarGameObject = ExperienceBarObject;
        LegacyBarGameObject = LegacyBarObject;
        ExpertiseBarGameObject = ExpertiseBarObject;

        // Assign Fill Images
        ExperienceFill = FindTargetUIObject(ExperienceBarRectTransform.transform, "Fill").GetComponent<Image>();
        LegacyFill = FindTargetUIObject(LegacyBarRectTransform.transform, "Fill").GetComponent<Image>();
        ExpertiseFill = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Fill").GetComponent<Image>();

        // Assign Icon Images
        ExperienceIcon = FindTargetUIObject(ExperienceBarRectTransform.transform, "Icon_VoiceChatStatus").GetComponent<Image>();
        LegacyIcon = FindTargetUIObject(LegacyBarRectTransform.transform, "Icon_VoiceChatStatus").GetComponent<Image>();
        ExpertiseIcon = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Icon_VoiceChatStatus").GetComponent<Image>();

        // Disable Icon Images Until Needed
        ExperienceIcon.enabled = false;
        LegacyIcon.enabled = false;
        ExpertiseIcon.enabled = false;

        // Assign Text Headers
        LocalizedText ExperienceTextHeader = FindTargetUIObject(ExperienceBarRectTransform.transform, "Text_Player_VampireName").GetComponent<LocalizedText>();
        ExperienceTextHeader.ForceSet("Experience");

        LegacyText = FindTargetUIObject(LegacyBarRectTransform.transform, "Text_Player_VampireName").GetComponent<LocalizedText>();
        LegacyText.ForceSet("Legacy");

        ExpertiseText = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Text_Player_VampireName").GetComponent<LocalizedText>();
        ExpertiseText.ForceSet("Expertise");

        // Deactivate unwanted objects in all bars
        DeactivateUnwantedObjects(ExperienceBarRectTransform);
        DeactivateUnwantedObjects(LegacyBarRectTransform);
        DeactivateUnwantedObjects(ExpertiseBarRectTransform);

        // Activate all bars
        if (ExperienceBar) ExperienceBarObject.SetActive(true);
        if (LegacyBar) LegacyBarObject.SetActive(true);
        if (ExpertiseBar) ExpertiseBarObject.SetActive(true);
    }
    static void ConfigureBar(RectTransform barRectTransform, GameObject referenceObject, RectTransform referenceRectTransform, int layer, float sizeMultiplier, string barHeaderText, Color fillColor, int barNumber)
    {
        float rectWidth = barRectTransform.rect.width;
        float sizeOffsetX = ((rectWidth * sizeMultiplier) - rectWidth) * (1 - barRectTransform.pivot.x);
        barRectTransform.localScale *= sizeMultiplier;
        barRectTransform.position = new Vector3(referenceObject.transform.position.x - sizeOffsetX, referenceObject.transform.position.y - (referenceRectTransform.rect.height * sizeMultiplier * barNumber), referenceObject.transform.position.z);
        barRectTransform.gameObject.layer = layer;

        Image fillImage = FindTargetUIObject(barRectTransform.transform, "Fill").GetComponent<Image>();
        fillImage.fillAmount = 0f;
        if (simpleFill != null && simpleFill.name == "SimpleProgressBar_Fill") fillImage.sprite = simpleFill;
        fillImage.color = fillColor;

        LocalizedText textHeader = FindTargetUIObject(barRectTransform.transform, "Text_Player_VampireName").GetComponent<LocalizedText>();
        textHeader.ForceSet(barHeaderText);
    }
    static void DeactivateUnwantedObjects(RectTransform barRectTransform)
    {
        //FindTargetUIObject(barRectTransform.transform, "Icon_container").SetActive(false);
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").SetActive(false);
    }
    public static class UIObjectUtils
    {
        public static GameObject FindTargetUIObject(Transform root, string targetName)
        {
            // Stack to hold the transforms to be processed
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            // HashSet to keep track of visited transforms to avoid cyclic references
            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(true);

            List<Transform> transforms = [.. children];

            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                if (current.gameObject.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    // Return the transform if the name matches
                    return current.gameObject;
                }

                // Create an indentation string based on the indent level
                //string indent = new('|', indentLevel);

                // Print the current GameObject's name and some basic info
                //Core.Log.LogInfo($"{indent}{current.gameObject.name} ({current.gameObject.scene.name})");

                // Add all children to the stack
                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }
                }
            }
            return null;
        }
        static LocalizationKey? InsertValue(string value, string key, string hexString)
        {
            if (AssetGuid.TryParse(hexString, out AssetGuid asset))
            {
                LocalizedKeyValue localizedKey = LocalizedKeyValue.Create(key, value);
                LocalizedString localizedString = LocalizedString.Create(asset, localizedKey);
                return new LocalizationKey(localizedString._LocalizationGUID);
            }
            return null;
        }
        public static void FindLoadedObjects<T>() where T : UnityEngine.Object
        {
            Il2CppReferenceArray<UnityEngine.Object> resources = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
            Core.Log.LogInfo($"Found {resources.Length} {Il2CppType.Of<T>().FullName}'s!");
            foreach (UnityEngine.Object resource in resources)
            {
                Core.Log.LogInfo($"Sprite: {resource.name}");
            }
        }
        public static Texture2D CreateFrameBorder(Vector2 size, int borderWidth, Color borderColor)
        {
            // Create a new Texture2D
            Texture2D texture = new((int)size.x, (int)size.y);

            // Fill the texture with a transparent color
            Color[] fillColor = new Color[texture.width * texture.height];
            for (int i = 0; i < fillColor.Length; i++)
            {
                fillColor[i] = Color.clear;
            }
            texture.SetPixels(fillColor);

            // Draw the border, make fraction of fontsize
            borderWidth = Mathf.RoundToInt(borderWidth * 0.5f); // Border width as a fraction of the font size
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (x < borderWidth || x >= texture.width - borderWidth || y < borderWidth || y >= texture.height - borderWidth)
                    {
                        texture.SetPixel(x, y, borderColor);
                    }
                }
            }

            // Apply changes to the texture
            texture.Apply();

            return texture;
        }
        public static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            // Create a new sprite with the texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            return sprite;
        }
        public static void FindGameObjects(Transform root, bool includeInactive = false)
        {
            // Stack to hold the transforms to be processed
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            // HashSet to keep track of visited transforms to avoid cyclic references
            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(includeInactive);
            List<Transform> transforms = [.. children];

            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                FindGameObjectComponents(current.gameObject);
                // Create an indentation string based on the indent level
                string indent = new('|', indentLevel);

                // Print the current GameObject's name and some basic info
                Core.Log.LogInfo($"{indent}{current.gameObject.name} ({current.gameObject.scene.name})");

                // Add all children to the stack
                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }
                }
            }
        }
        public static void FindGameObjectComponents(GameObject parentObject)
        {
            //GameObject childObject = FindTargetUIObject(parentObject.transform, childName);
            int componentCount = parentObject.GetComponentCount();
            for (int i = 0; i < componentCount; i++)
            {
                Core.Log.LogInfo($"{parentObject.name} Component {i}: {parentObject.GetComponentAtIndex(i).name} | {parentObject.GetComponentAtIndex(i).GetIl2CppType().FullName}");
            }
        }
        public static void GatherInfo(GameObject gameObject)
        {
            /*
            GameObject BloodType = FindTargetUIObject(gameObject.transform, "BloodType");
            GameObject Percentage = FindTargetUIObject(gameObject.transform, "Percentage");
            GameObject Icon = FindTargetUIObject(gameObject.transform, "Icon");
            GameObject BloodOrb = FindTargetUIObject(gameObject.transform, "BloodOrb");
            GameObject OrbBorder = FindTargetUIObject(gameObject.transform, "Orb border");
            GameObject BloodFill = FindTargetUIObject(gameObject.transform, "BloodFill");
            GameObject Glass = FindTargetUIObject(gameObject.transform, "Glass");
            GameObject BlackBackground = FindTargetUIObject(gameObject.transform, "BlackBackground");
            GameObject Blood = FindTargetUIObject(gameObject.transform, "Blood");

            FindGameObjects(gameObject.transform, true);

            FindGameObjectComponents(BloodType, "BloodType");
            FindGameObjectComponents(Percentage, "Percentage");
            FindGameObjectComponents(Icon, "Icon");
            FindGameObjectComponents(BloodOrb, "BloodOrb");
            FindGameObjectComponents(OrbBorder, "Orb border");
            FindGameObjectComponents(BloodFill, "BloodFill");
            FindGameObjectComponents(Glass, "Glass");
            FindGameObjectComponents(BlackBackground, "BlackBackground");
            FindGameObjectComponents(Blood, "Blood");
            */
        }
    }

    // BloodOrbComponent stuff, colored orbs instead of bars would be amazing but can't just grab the BloodOrbComponent like everything else and was taking too much time
    /*
            // Instantiate the ExperienceBar from the PlayerEntryPrefab
        GameObject BloodOrbParentObject = FindTargetUIObject(BottomBarCanvasObject.transform, "BloodOrbParent");
        GameObject ExperienceOrbObject = GameObject.Instantiate(BloodOrbParentObject);
        GameObject.DontDestroyOnLoad(ExperienceOrbObject);
        //SceneManager.MoveGameObjectToScene(ExperienceOrbObject, SceneManager.GetSceneByName("VRisingWorld"));
        Core.Log.LogInfo($"ExperienceOrbObject instantiated and set to DontDestroyOnLoad... {ExperienceOrbObject.name}");

        // Set the parent of the ExperienceBar to the canvas
        ExperienceOrbObject.transform.SetParent(BottomBarCanvasObject.transform, false);
        Core.Log.LogInfo("ExperienceOrbObject moved to BottomBarCanvas parent...");
        
        // Retrieve BloodOrbRectTransform to set position, layout, layer etc for ExperienceOrb
        RectTransform BloodOrbRectTransform = BloodOrbParentObject.GetComponent<RectTransform>();
        Core.Log.LogInfo("BloodOrbRectTransform retrieved...");

        // Configure ExperienceOrbRectTransform
        RectTransform ExperienceOrbRectTransform = ExperienceOrbObject.GetComponent<RectTransform>();
        float offsetX = BloodOrbRectTransform.rect.width / 2;
        ExperienceOrbRectTransform.sizeDelta = BloodOrbRectTransform.sizeDelta / 3;
        float startX = canvas.BottomBarParent.rect.xMax;
        ExperienceOrbRectTransform.localPosition = new Vector3(startX + offsetX, BloodOrbRectTransform.localPosition.y, BloodOrbRectTransform.localPosition.z); // don't forget to make system for universal teams!
        ExperienceOrbObject.layer = BloodOrbRectTransform.gameObject.layer;
        Core.Log.LogInfo("ExperienceOrbRectTransform configured...");

        FindGameObjects(BloodOrbParentObject.transform, true);
        GameObject BloodOrbObject = FindTargetUIObject(ExperienceOrbObject.transform, "BloodOrb");
        BloodOrbObject.SetActive(true);

        Component BloodOrbComponent = BloodOrbObject.GetComponentAtIndex(1);
        Core.Log.LogInfo($"BloodOrbComponent: {BloodOrbComponent.name} | {BloodOrbComponent.GetIl2CppType().FullName} | {BloodOrbComponent.GetType().FullName}");
        if (BloodOrbComponent == null)
        {
            Core.Log.LogError("BloodOrbComponent is null...");
            return;
        }

        BloodOrbComponent bloodOrbComponent = BloodOrbComponent.GetComponent<BloodOrbComponent>();
     
        Image BloodFillImage = bloodOrbComponent.BloodFillImage;
        BloodFillImage.fillAmount = 0f;
        BloodFillImage.color = Color.green;

        BloodQualityParent BloodQualityParent = bloodOrbComponent.BloodQualityParent;
        LocalizedText LocalizedQuality = BloodQualityParent.BloodQuality;
        LocalizedQuality.ForceSet("0%");
        BloodQuality = LocalizedQuality; // will be used in UpdateLoop

        LocalizedText LocalizedType = bloodOrbComponent.Text_CurrentBloodType;
        LocalizedType.ForceSet("[0] Experience");

        bloodOrbComponent.LKey_BloodHeader = InsertValue("0", "{value}", "d47ca4ea-648d-487b-b9ef-79b15899278d") ?? LocalizationKey.Empty;
        LocalizedText LocalizedAmount = bloodOrbComponent.Text_BloodAmount;
        bloodOrbComponent.LKey_BloodValue = InsertValue("0", "{amount}", "553bb7dc-d583-47b1-a13a-834682e8f1ab") ?? LocalizationKey.Empty;
        LocalizedAmount.LocalizationKey = bloodOrbComponent.LKey_BloodValue;
        bloodOrbComponent.LKey_BloodDesc = InsertValue("0", "{value}", "4210316d-23d4-4274-96f5-d6f0944bd0bb") ?? LocalizationKey.Empty;
        bloodOrbComponent.LKey_BloodWarning = LocalizationKey.Empty;
        bloodOrbComponent.LKey_VBlood = LocalizationKey.Empty;
    */
}