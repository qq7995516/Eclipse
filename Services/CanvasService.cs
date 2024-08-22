using Eclipse.Patches;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using ProjectM.Network;
using ProjectM.UI;
using Stunlock.Core;
using Stunlock.Localization;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static Eclipse.Services.CanvasService.UIObjectUtils;
using Image = UnityEngine.UI.Image;
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

    static readonly WaitForSeconds Delay = new(2.5f);

    static UICanvasBase UICanvasBase;
    static Canvas Canvas;
    public static List<int> PlayerData;

    static GameObject ExperienceBarGameObject;
    static LocalizedText ExperienceHeader;
    static LocalizedText ExperienceText;
    static Image ExperienceIcon;
    static Image ExperienceFill;
    static float ExperienceProgress = 0f;
    static int ExperienceLevel = 0;
    static int ExperiencePrestige = 0;

    static GameObject LegacyBarGameObject;
    static LocalizedText LegacyHeader;
    static LocalizedText LegacyText;
    static Image LegacyIcon;
    static Image LegacyFill;
    static float LegacyProgress = 0f;
    static int LegacyLevel = 0;
    static int LegacyPrestige = 0;

    static GameObject ExpertiseBarGameObject;
    static LocalizedText ExpertiseHeader;
    static LocalizedText ExpertiseText;
    static Image ExpertiseIcon;
    static Image ExpertiseFill;
    static float ExpertiseProgress = 0f;
    static int ExpertiseLevel = 0;
    static int ExpertisePrestige = 0;

    static GameObject QuestObject;
    static int DailyProgress = 0;
    static int DailyGoal = 0;
    static int DailyTarget = 0;
    static int WeeklyProgress = 0;
    static int WeeklyGoal = 0;
    static int WeeklyTarget = 0;
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
        { BloodType.Worker, "BloodIcon_Small_Worker" },
        { BloodType.Warrior, "BloodIcon_Small_Warrior" },
        { BloodType.Scholar, "BloodIcon_Small_Scholar" },
        { BloodType.Rogue, "BloodType_Rogue_Small" },
        { BloodType.Mutant, "BloodType_Putrid_Small" },
        { BloodType.Draculin, "BloodType_Draculin_Small" },
        { BloodType.Immortal, "BloodType_Dracula_Small" },
        { BloodType.Creature, "BloodType_Beast_Small" },
        { BloodType.Brute, "BloodIcon_Small_Brute" }
    };

    static readonly Dictionary<BloodType, Sprite> BloodSprites = [];
    static void InitializeSprites()
    {
        List<string> spriteNames = [..BloodIcons.Values];
        Il2CppArrayBase<Sprite> allSprites = Resources.FindObjectsOfTypeAll<Sprite>();

        if (!File.Exists(Plugin.FilePaths[1])) File.Create(Plugin.FilePaths[1]).Dispose();

        using StreamWriter writer = new(Plugin.FilePaths[1], false);
        foreach (Sprite sprite in allSprites)
        {
            writer.WriteLine(sprite.name);
        }

        var matchedSprites = allSprites
            .Where(sprite => spriteNames.Contains(sprite.name))
            .ToDictionary(sprite => BloodIcons.First(pair => pair.Value == sprite.name).Key, sprite => sprite);

        foreach (var pair in matchedSprites)
        {
            //Core.Log.LogInfo($"BloodType: {pair.Key} | Sprite: {pair.Value.name}");
            BloodSprites[pair.Key] = pair.Value;
        }
    }

    public static bool Active = false;
    public CanvasService(UICanvasBase canvas)
    {
        // Instantiate the ExperienceBar from the PlayerEntryPrefab and find the BotomBarCanvas
        UICanvasBase = canvas;
        InitializeBars(canvas);
        try
        {
            //InitializeSprites();
            //FindGameObjects(canvas.transform, Plugin.FilePaths[0], true);
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
    public static IEnumerator CanvasUpdateLoop() // need to find another component, can abstract data to whatever just need something relatively unused that syncs. Check SyncingComponents or w/e that was called
    {
        while (true)
        {
            if (!Active) Active = true;

            ExperienceProgress = PlayerData[0] / 100f;
            ExperienceLevel = PlayerData[1];
            ExperiencePrestige = PlayerData[2];
            LegacyProgress = PlayerData[3] / 100f;
            LegacyLevel = PlayerData[4];
            LegacyPrestige = PlayerData[5];
            int legacyType = PlayerData[6];
            ExpertiseProgress = PlayerData[7] / 100f;
            ExpertiseLevel = PlayerData[8];
            ExpertisePrestige = PlayerData[9];
            int expertiseType = PlayerData[10];
            DailyProgress = PlayerData[11];
            DailyGoal = PlayerData[12];
            DailyTarget = PlayerData[13];
            WeeklyProgress = PlayerData[14];
            WeeklyGoal = PlayerData[15];
            WeeklyTarget = PlayerData[16];

            //Core.Log.LogInfo($"Experience: {ExperienceProgress}/{ExperienceLevel}/{ExperiencePrestige} | Legacy: {LegacyProgress}/{LegacyLevel}/{LegacyPrestige} {legacyType} | Expertise: {ExpertiseProgress}/{ExpertiseLevel}/{ExpertisePrestige} {expertiseType} | Daily: {DailyProgress}/{DailyGoal}/{DailyTarget} | Weekly: {WeeklyProgress}/{WeeklyGoal}/{WeeklyTarget}");

            if (ExperienceProgress != ExperienceFill.fillAmount && ExperienceProgress != 0f)
            {
                ExperienceFill.fillAmount = ExperienceProgress;

                if (ExperienceText.GetText() != ExperienceLevel.ToString())
                {
                    ExperienceText.ForceSet(ExperienceLevel.ToString());
                }
            }

            if (LegacyProgress != LegacyFill.fillAmount && LegacyProgress != 0f)
            {
                LegacyFill.fillAmount = LegacyProgress;
                string bloodType = Enum.GetName(typeof(BloodType), legacyType);
                if (LegacyHeader.GetText() != bloodType)
                {
                    LegacyHeader.ForceSet(bloodType);
                }

                if (LegacyText.GetText() != LegacyLevel.ToString())
                {
                    LegacyText.ForceSet(LegacyLevel.ToString());
                }
            }

            if (ExpertiseProgress != ExpertiseFill.fillAmount && ExpertiseProgress != 0f)
            {
                ExpertiseFill.fillAmount = ExpertiseProgress;
                string weaponType = Enum.GetName(typeof(WeaponType), expertiseType);
                if (ExpertiseHeader.GetText() != weaponType)
                {
                    ExpertiseHeader.ForceSet(weaponType);
                }

                if (ExpertiseText.GetText() != ExpertiseLevel.ToString())
                {
                    ExpertiseText.ForceSet(ExpertiseLevel.ToString());
                }
            }

            /*
            Entity localCharacter = ClientChatSystemPatch.localCharacter;
            Entity localUser = ClientChatSystemPatch.localUser;
            if (localCharacter.Exists() && localUser.Exists() && localCharacter.Has<FollowerBuffer>())
            {
                var buffer = localCharacter.ReadBuffer<FollowerBuffer>();
                foreach (FollowerBuffer followerBuffer in buffer)
                {
                    Entity following = followerBuffer.Entity.GetEntityOnServer();
                    if (following.Exists() && following.Has<FactionReference>())
                    {
                        Core.Log.LogInfo("Following exists...");
                        if (!following.Read<FactionReference>().FactionGuid.Equals(PlayerFaction)) continue;
                        Core.Log.LogInfo("Following is player faction...");
                        following.With((ref CharacterHUD characterHUD) =>
                        {
                            if (characterHUD.Name.IsEmpty)
                            {
                                characterHUD.Name = new FixedString64Bytes($"{localUser.Read<User>().CharacterName.Value}'s Familiar");
                                Core.Log.LogInfo($"Set familiar name...");
                            }
                        });
                        break;
                    }
                }
            }
            */

            yield return Delay;
        }
    }
    static void InitializeBars(UICanvasBase canvas)
    {
        GameObject CanvasObject = FindTargetUIObject(canvas.transform.root, "BottomBarCanvas");
        GameObject TargetInfoObject = FindTargetUIObject(canvas.transform.root, "TargetInfoPanelCanvas");
        Canvas bottomBarCanvas = CanvasObject.GetComponent<Canvas>();
        //Canvas targetInfoCanvas = TargetInfoObject.GetComponent<Canvas>();

        Canvas = bottomBarCanvas;
        //GameObject objectPrefab = canvas.ProximityPlayerListOverlay.PlayerEntryPrefab.gameObject;
        GameObject objectPrefab = canvas.TargetInfoParent.gameObject;
        GameObject iconContainerPrefab = FindTargetUIObject(canvas.transform.root, "Icon_container");
        //GameObject tutorialWindowPrefab = canvas.AchievementsParent.gameObject;
        //GameObject bloodOrbPrefab = canvas.BottomBarParentPrefab.BloodOrb.gameObject;
        //GameObject bloodOrbPrefab = FindTargetUIObject(canvas.transform.root, "BloodOrbParent");

        // Instantiate all bars
        GameObject ExperienceBarObject = GameObject.Instantiate(objectPrefab);
        GameObject LegacyBarObject = GameObject.Instantiate(objectPrefab);
        GameObject ExpertiseBarObject = GameObject.Instantiate(objectPrefab);
        GameObject IconContainerObject = GameObject.Instantiate(iconContainerPrefab);
        //GameObject QuestObject = GameObject.Instantiate(tutorialWindowPrefab);
        //GameObject BloodOrbObject = GameObject.Instantiate(bloodOrbPrefab);

        // Assign GameObjects to respective fields
        ExperienceBarGameObject = ExperienceBarObject;
        LegacyBarGameObject = LegacyBarObject;
        ExpertiseBarGameObject = ExpertiseBarObject;

        // Mark for DontDestroyOnLoad and move to VRisingWorld scene
        RectTransform ExperienceBarRectTransform = ExperienceBarObject.GetComponent<RectTransform>();
        RectTransform LegacyBarRectTransform = LegacyBarObject.GetComponent<RectTransform>();
        RectTransform ExpertiseBarRectTransform = ExpertiseBarObject.GetComponent<RectTransform>();
        RectTransform IconContainerRectTransform = IconContainerObject.GetComponent<RectTransform>();

        GameObject.DontDestroyOnLoad(ExperienceBarObject);
        GameObject.DontDestroyOnLoad(LegacyBarObject);
        GameObject.DontDestroyOnLoad(ExpertiseBarObject);

        SceneManager.MoveGameObjectToScene(ExperienceBarObject, SceneManager.GetSceneByName("VRisingWorld"));
        SceneManager.MoveGameObjectToScene(LegacyBarObject, SceneManager.GetSceneByName("VRisingWorld"));
        SceneManager.MoveGameObjectToScene(ExpertiseBarObject, SceneManager.GetSceneByName("VRisingWorld"));

        // Set BottomBarCanvas as the parent for all bars
        ExperienceBarRectTransform.SetParent(bottomBarCanvas.transform, false);
        LegacyBarRectTransform.SetParent(bottomBarCanvas.transform, false);
        ExpertiseBarRectTransform.SetParent(bottomBarCanvas.transform, false); // not needed for the health bars to show up?
        IconContainerRectTransform.SetParent(bottomBarCanvas.transform, false);

        // Get MiniMap south icon on the compass to set location for now
        GameObject MiniMapSouthObject = FindTargetUIObject(canvas.transform.root, "S");
        RectTransform MiniMapSouthRectTransform = MiniMapSouthObject.GetComponent<RectTransform>();

        /*
        if (BloodOrbObject == null)
        {
            Core.Log.LogError("BloodOrbObject is null...");
        }
        else
        {
            RectTransform BloodOrbRectTransform = BloodOrbObject.GetComponent<RectTransform>();
            GameObject.DontDestroyOnLoad(BloodOrbObject);
            SceneManager.MoveGameObjectToScene(BloodOrbObject, SceneManager.GetSceneByName("VRisingWorld"));
            BloodOrbRectTransform.SetParent(bottomBarCanvas.transform, false);
            //CanvasObject.GetComponent<RectTransform>().rect.height * 0.2f
            BloodOrbRectTransform.position = new Vector3(MiniMapSouthRectTransform.position.x, MiniMapSouthRectTransform.position.y / 4, MiniMapSouthRectTransform.position.z);
            BloodOrbObject.layer = CanvasObject.layer;
            GameObject BloodFill = FindTargetUIObject(BloodOrbRectTransform.transform, "BloodFill");
            Image BloodFillImage = BloodFill.GetComponent<Image>();
            BloodFillImage.fillAmount = 0.25f;
            BloodFillImage.color = Color.green;
            GameObject Blood = FindTargetUIObject(BloodOrbRectTransform.transform, "Blood");
            //EventTrigger eventTrigger = Blood.GetComponent<EventTrigger>();
            Image BloodImage = Blood.GetComponent<Image>();
            BloodImage.color = Color.green;
            BloodImage.fillAmount = 0.50f;
            GameObject BloodType = FindTargetUIObject(BloodOrbRectTransform.transform, "BloodType");
            Image BloodTypeImage = BloodType.GetComponent<Image>();
            Animator FillAnimation = BloodFill.GetComponent<Animator>();
            BloodTypeImage.fillAmount = 0.1f;
            BloodTypeImage.color = Color.green;
            BloodOrbObject.SetActive(true);
            //Blood.active = false; main blood thing

        }

        
        if (QuestObject == null)
        {
            Core.Log.LogError("QuestObject is null...");
        }
        else
        {
            RectTransform QuestRectTransform = QuestObject.GetComponent<RectTransform>();
            GameObject.DontDestroyOnLoad(QuestObject);
            SceneManager.MoveGameObjectToScene(QuestObject, SceneManager.GetSceneByName("VRisingWorld"));
            QuestRectTransform.SetParent(bottomBarCanvas.transform, false);
            QuestRectTransform.position = new Vector3(MiniMapSouthRectTransform.position.x / 2, MiniMapSouthRectTransform.position.y / 2, MiniMapSouthRectTransform.position.z);
            QuestObject.layer = CanvasObject.layer;
            QuestObject.SetActive(true);
            FindGameObjectComponents(QuestObject);
        }
        Core.Log.LogInfo("check6");
        */

        // Assign Fill Images
        ExperienceFill = FindTargetUIObject(ExperienceBarRectTransform.transform, "Fill").GetComponent<Image>();
        LegacyFill = FindTargetUIObject(LegacyBarRectTransform.transform, "Fill").GetComponent<Image>();
        ExpertiseFill = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Fill").GetComponent<Image>();

        // Assign LocalizedText for headers
        ExperienceHeader = FindTargetUIObject(ExperienceBarRectTransform.transform, "Name").GetComponent<LocalizedText>();
        LegacyHeader = FindTargetUIObject(LegacyBarRectTransform.transform, "Name").GetComponent<LocalizedText>();
        ExpertiseHeader = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Name").GetComponent<LocalizedText>();

        // Assign LocalizedText for Level
        ExperienceText = FindTargetUIObject(ExperienceBarRectTransform.transform, "LevelText").GetComponent<LocalizedText>();
        LegacyText = FindTargetUIObject(LegacyBarRectTransform.transform, "LevelText").GetComponent<LocalizedText>();
        ExpertiseText = FindTargetUIObject(ExpertiseBarRectTransform.transform, "LevelText").GetComponent<LocalizedText>();

        // Configure ExperienceBar
        ConfigureBar(ExperienceBarRectTransform, MiniMapSouthObject, MiniMapSouthRectTransform, ExperienceHeader, ExperienceText, ExperienceFill, CanvasObject.layer, 1f, "Experience", Color.green, 1);

        // Configure LegacyBar
        ConfigureBar(LegacyBarRectTransform, MiniMapSouthObject, MiniMapSouthRectTransform, LegacyHeader, LegacyText, LegacyFill, CanvasObject.layer, 1f, "Legacy", Color.red, 2);

        // Configure ExpertiseBar
        ConfigureBar(ExpertiseBarRectTransform, MiniMapSouthObject, MiniMapSouthRectTransform, ExpertiseHeader, ExpertiseText, ExpertiseFill, CanvasObject.layer, 1f, "Expertise", Color.grey, 3);

        /*
        // Assign Icon Images
        ExperienceIcon = FindTargetUIObject(ExperienceBarRectTransform.transform, "Icon_VoiceChatStatus").GetComponent<Image>();
        LegacyIcon = FindTargetUIObject(LegacyBarRectTransform.transform, "Icon_VoiceChatStatus").GetComponent<Image>();
        ExpertiseIcon = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Icon_VoiceChatStatus").GetComponent<Image>();

        // Disable Icon Images Until Needed
        ExperienceIcon.enabled = false;
        LegacyIcon.enabled = false;
        ExpertiseIcon.enabled = false;

        // Assign Text Headers
        LocalizedText ExperienceTextHeader = FindTargetUIObject(ExperienceBarRectTransform.transform, "Name").GetComponent<LocalizedText>();
        ExperienceTextHeader.ForceSet("Experience");

        LegacyText = FindTargetUIObject(LegacyBarRectTransform.transform, "Name").GetComponent<LocalizedText>();
        LegacyText.ForceSet("Legacy");

        ExpertiseText = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Name").GetComponent<LocalizedText>();
        ExpertiseText.ForceSet("Expertise");
        */

        // Activate all bars
        if (ExperienceBar) ExperienceBarObject.SetActive(true);
        if (LegacyBar)
        {
            LegacyBarObject.SetActive(true);
            IconContainerRectTransform.position = new Vector3(LegacyBarRectTransform.rect.xMax, LegacyBarRectTransform.position.y, LegacyBarRectTransform.position.z);
            IconContainerRectTransform.gameObject.layer = CanvasObject.layer;
            //IconContainerObject.SetActive(true);
            LegacyIcon = FindTargetUIObject(IconContainerRectTransform.transform, "Icon_VoiceChatStatus").GetComponent<Image>();
            LegacyIcon.enabled = false;
        }
        if (ExpertiseBar) ExpertiseBarObject.SetActive(true);

        // Deactivate unwanted objects in all bars
        DeactivateUnwantedObjects(ExperienceBarRectTransform);
        DeactivateUnwantedObjects(LegacyBarRectTransform);
        DeactivateUnwantedObjects(ExpertiseBarRectTransform);
    }
    static void ConfigureBar(RectTransform barRectTransform, GameObject referenceObject, RectTransform referenceRectTransform, LocalizedText textHeader, LocalizedText levelText, Image fillImage, int layer, float sizeMultiplier, string barHeaderText, Color fillColor, int barNumber)
    {
        //GameObject WorldEventObject = FindTargetUIObject(canvas.transform.root, "HUDAlert_WorldEvent"); use for y
        //RectTransform WorldEventRectTransform = WorldEventObject.GetComponent<RectTransform>();

        float rectWidth = barRectTransform.rect.width;
        float sizeOffsetX = ((rectWidth * sizeMultiplier) - rectWidth) * (1 - barRectTransform.pivot.x);
        barRectTransform.localScale *= 0.75f;
        barRectTransform.position = new Vector3(referenceObject.transform.position.x - sizeOffsetX * 2, (referenceObject.transform.position.y * 0.9f) - (referenceRectTransform.rect.height * 1.75f * barNumber), referenceObject.transform.position.z);
        barRectTransform.gameObject.layer = layer;

        fillImage.fillAmount = 0f;
        fillImage.color = fillColor;

        levelText.ForceSet("0");
        textHeader.ForceSet(barHeaderText);
        textHeader.Text.fontSize *= 1.5f;
    }
    static void DeactivateUnwantedObjects(RectTransform barRectTransform)
    {
        //FindTargetUIObject(barRectTransform.transform, "Icon_container").SetActive(false);
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        FindTargetUIObject(barRectTransform.transform, "Skull").SetActive(false);
        FindTargetUIObject(barRectTransform.transform, "InformationPanel").SetActive(false);
        FindTargetUIObject(barRectTransform.transform, "AbsorbFill").SetActive(false);
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
        public static void FindGameObjects(Transform root, string filePath = "", bool includeInactive = false)
        {
            // Stack to hold the transforms to be processed
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            // HashSet to keep track of visited transforms to avoid cyclic references
            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(includeInactive);
            List<Transform> transforms = [..children];

            if (string.IsNullOrEmpty(filePath))
            {
                while (transformStack.Count > 0)
                {
                    var (current, indentLevel) = transformStack.Pop();

                    if (!visited.Add(current))
                    {
                        // If we have already visited this transform, skip it
                        continue;
                    }

                    List<string> objectComponents = FindGameObjectComponents(current.gameObject);

                    // Create an indentation string based on the indent level
                    string indent = new('|', indentLevel);

                    // Write the current GameObject's name and some basic info to the file

                    // Add all children to the stack
                    foreach (Transform child in transforms)
                    {
                        if (child.parent == current)
                        {
                            transformStack.Push((child, indentLevel + 1));
                        }
                    }
                }
                return;
            }

            if (!File.Exists(filePath)) File.Create(filePath).Dispose();

            using StreamWriter writer = new(filePath, false);
            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                List<string> objectComponents = FindGameObjectComponents(current.gameObject);

                // Create an indentation string based on the indent level
                string indent = new('|', indentLevel);

                // Write the current GameObject's name and some basic info to the file
                writer.WriteLine($"{indent}{current.gameObject.name} | {string.Join(",", objectComponents)} | [{current.gameObject.scene.name}]");

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
        public static List<string> FindGameObjectComponents(GameObject parentObject)
        {
            List<string> components = [];

            int componentCount = parentObject.GetComponentCount();
            for (int i = 0; i < componentCount; i++)
            {
                components.Add($"{parentObject.GetComponentAtIndex(i).GetIl2CppType().FullName}({i})");
            }

            return components;
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
