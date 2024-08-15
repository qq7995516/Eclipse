using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
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

    static GameObject ExperienceBarGameObject;
    static Image ExperienceFill;
    static LocalizedText PrestigeFronter;

    static GameObject LegacyBarGameObject;
    static Image LegacyFill;
    static LocalizedText LegacyText;

    static GameObject ExpertiseBarGameObject;
    static Image ExpertiseFill;
    static LocalizedText ExpertiseText;
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

    public static Entity LocalCharacter = Entity.Null;
    public CanvasService(UICanvasBase canvas)
    {
        // Instantiate the ExperienceBar from the PlayerEntryPrefab and find the BotomBarCanvas
        UICanvasBase = canvas;

        if (ExperienceBar) InitializeExperience(canvas);
        if (LegacyBar) InitializeLegacy(ExperienceBarGameObject);
        if (ExpertiseBar) InitializeExpertise(LegacyBarGameObject);
    }
    public static IEnumerator CanvasUpdateLoop() // need to find another component, can abstract data to whatever just need something relatively unused
    {
        while (true)
        {
            NetworkedEntity player = NetworkedEntity.ServerEntity(LocalCharacter);
            Entity syncedPlayer = player.GetSyncedEntityOrNull();

            int toUnpack = syncedPlayer.Read<Team>().FactionIndex;

            if (toUnpack >= 33 && ExperienceBar)
            {
                UnpackValues(toUnpack, ShowPrestige, LegacyBar, ExpertiseBar, out float level, out float? prestige, out float? legacy, out float? expertise);

                Core.Log.LogInfo($"Level: {level} | Prestige: {prestige} | Legacy: {legacy} | Expertise: {expertise}");

                if (level > 0 && level != ExperienceFill.fillAmount)
                {
                    ExperienceFill.fillAmount = level;
                }

                if (prestige.HasValue && prestige.Value != int.Parse(PrestigeFronter.Text.ToString()))
                {
                    PrestigeFronter.ForceSet(prestige.ToString());
                }

                if (legacy.HasValue && legacy.Value != LegacyFill.fillAmount)
                {
                    LegacyFill.fillAmount = legacy.Value;
                    if (LegacyText.GetText() == "Legacy") // try to set to players current blood type
                    {
                        PrefabGUID bloodPrefab = syncedPlayer.Has<Blood>() ? syncedPlayer.Read<Blood>().BloodType : PrefabGUID.Empty;
                        if (!bloodPrefab.IsEmpty())
                        {
                            BloodType bloodType = GetBloodTypeFromPrefab(bloodPrefab);
                            string bloodTypeString = bloodType.ToString();
                            if (LegacyText.GetText() != bloodTypeString && bloodType != BloodType.None)
                            {
                                LegacyText.ForceSet(bloodTypeString);
                            }
                        }
                    }
                }

                if (expertise.HasValue && expertise.Value != ExpertiseFill.fillAmount)
                {
                    ExpertiseFill.fillAmount = expertise.Value;
                    if (ExpertiseText.GetText() == "Expertise") // try to set to players current weapon type
                    {
                        if (syncedPlayer.Has<Equipment>())
                        {
                            string weaponType = GetWeaponTypeFromEntity(syncedPlayer.Read<Equipment>().WeaponSlot.SlotEntity.GetEntityOnServer()).ToString();
                            if (ExpertiseText.GetText() != weaponType)
                            {
                                ExpertiseText.ForceSet(weaponType);
                            }
                        }
                    }
                }
            }

            yield return Delay;
        }
    }
    static void InitializeExperience(UICanvasBase canvas)
    {
        GameObject CanvasObject = FindTargetUIObject(canvas.transform.root, "BottomBarCanvas");
        Canvas bottomBarCanvas = CanvasObject.GetComponent<Canvas>();
        Canvas = bottomBarCanvas;
        GameObject ExperienceBarObject = GameObject.Instantiate(canvas.ProximityPlayerListOverlay.PlayerEntryPrefab.gameObject);
        ExperienceBarGameObject = ExperienceBarObject;

        // Mark for DontDestroyOnLoad, move to VRisingWorld scene
        RectTransform ExperienceBarRectTransform = ExperienceBarObject.GetComponent<RectTransform>();
        GameObject.DontDestroyOnLoad(ExperienceBarObject);
        SceneManager.MoveGameObjectToScene(ExperienceBarObject, SceneManager.GetSceneByName("VRisingWorld"));

        // Set BotomBarCanvas as the parent of the ExperienceBar to ensure rendering
        ExperienceBarRectTransform.SetParent(bottomBarCanvas.transform, false);

        // Get MiniMap south icon on the compass to set location for now
        GameObject MiniMapSouthObject = FindTargetUIObject(canvas.transform.root, "S");
        RectTransform MiniMapSouthRectTransform = MiniMapSouthObject.GetComponent<RectTransform>();

        // Configure ExperienceBar location, size, layer etc
        float sizeMultiplier = 1.5f;
        float rectWidth = ExperienceBarRectTransform.rect.width;
        float sizeOffsetX = ((rectWidth * sizeMultiplier) - rectWidth) * (1 - ExperienceBarRectTransform.pivot.x);
        ExperienceBarRectTransform.localScale *= sizeMultiplier;
        ExperienceBarRectTransform.position = new Vector3(MiniMapSouthObject.transform.position.x - sizeOffsetX, MiniMapSouthObject.transform.position.y - (MiniMapSouthRectTransform.rect.height * sizeMultiplier), MiniMapSouthObject.transform.position.z); // don't forget to make system for universal teams!   
        ExperienceBarRectTransform.gameObject.layer = CanvasObject.layer;

        // Set initial fill, color and text for bar header
        GameObject FillImageObject = FindTargetUIObject(ExperienceBarRectTransform.transform, "Fill");
        Image FillImage = FillImageObject.GetComponent<Image>();
        ExperienceFill = FillImage;
        FillImage.fillAmount = 0f;
        FillImage.color = Color.green;

        // Get object for ExperienceBar header text and clone for prestige if needed
        GameObject TextObject = FindTargetUIObject(ExperienceBarRectTransform.transform, "Text_Player_VampireName");
        LocalizedText BarHeaderText = TextObject.GetComponent<LocalizedText>();

        // Deactivate unwanted objects
        GameObject iconObject = FindTargetUIObject(ExperienceBarRectTransform.transform, "Icon_VoiceChatStatus");
        //FindGameObjectComponents(iconObject);

        /*
        if (ShowPrestige)
        {
            Core.Log.LogInfo("ShowPrestige is true...");
            //Image iconImage = iconObject.GetComponent<Image>();
            Core.Log.LogInfo("IconImage retrieved...");
            GameObject PrestigeTextObject = GameObject.Instantiate(TextObject); // note: this starts behind the bar, get localPosition from Image
            Core.Log.LogInfo("PrestigeTextObject instantiated...");
            PrestigeTextObject.name = "Text_Player_Prestige";
            PrestigeTextObject.transform.SetParent(ExperienceBarRectTransform.transform, false);
            Core.Log.LogInfo("PrestigeTextObject parented...");
            LocalizedText PrestigeText = PrestigeTextObject.GetComponent<LocalizedText>();
            PrestigeFronter = PrestigeText;
            PrestigeText.ForceSet("0");
            Core.Log.LogInfo("PrestigeText configured...");
            PrestigeText.transform.localPosition = iconObject.transform.localPosition;
            PrestigeText.Text.fontSize *= sizeMultiplier;
            PrestigeText.Text.color = Color.cyan;
            PrestigeText.SetRectSizeToTextSize = true;
            Core.Log.LogInfo("PrestigeText positioned/colored...");
            //iconImage.sprite = CreateSpriteFromTexture(CreateFrameBorder(PrestigeText.Rect.sizeDelta, (int)PrestigeText.Text.fontSize, Color.black));
            PrestigeTextObject.transform.SetAsLastSibling();
            iconObject.SetActive(false);
            Core.Log.LogInfo("PrestigeTextObject set to last sibling, sprite created...");
        }
        else
        {
            iconObject = FindTargetUIObject(ExperienceBarRectTransform.transform, "Icon_container");
            iconObject.SetActive(false); // can disable if not showing prestige
        }
        */

        // Finish configuring BarHeader after cloning for Prestige
        BarHeaderText.Text.fontSize *= sizeMultiplier;
        BarHeaderText.ForceSet("Experience");
        Vector3 textHeight = BarHeaderText.transform.localPosition;
        textHeight.y = ExperienceBarRectTransform.rect.height * 2 * sizeMultiplier; // 2 happened to work well first try so will keep it for now for further scaling

        // Activate the ExperienceBarObject
        ExperienceBarObject.SetActive(true);

        // Deactivate DamageTakenFill
        FindTargetUIObject(ExperienceBarRectTransform.transform, "DamageTakenFill").SetActive(false);
        iconObject.SetActive(false);

    }
    static void InitializeLegacy(GameObject gameObject)
    {
        // Instantiate the LegacyBar from the ExperienceBar
        GameObject LegacyBarObject = GameObject.Instantiate(gameObject);
        LegacyBarGameObject = LegacyBarObject;

        // Mark for DontDestroyOnLoad, move to VRisingWorld scene
        RectTransform LegacyBarRectTransform = LegacyBarObject.GetComponent<RectTransform>();
        GameObject.DontDestroyOnLoad(LegacyBarObject);
        SceneManager.MoveGameObjectToScene(LegacyBarObject, SceneManager.GetSceneByName("VRisingWorld"));

        // Set BotomBarCanvas as the parent of the LegacyBar to ensure rendering
        LegacyBarRectTransform.SetParent(Canvas.transform, false);

        // Configure LegacyBar position, most of what was configured for ExperienceBar can be reused
        LegacyBarRectTransform.position = new Vector3(gameObject.GetComponent<RectTransform>().position.x, gameObject.transform.position.y - LegacyBarRectTransform.rect.height, gameObject.transform.position.z);

        // Configure LegacyBar fill, color
        GameObject FillImageObject = FindTargetUIObject(LegacyBarRectTransform.transform, "Fill");
        Image FillImage = FillImageObject.GetComponent<Image>();
        LegacyFill = FillImage;
        FillImage.fillAmount = 0f;
        FillImage.color = Color.red;

        // Set LegacyBar header text
        GameObject TextObject = FindTargetUIObject(LegacyBarRectTransform.transform, "Text_Player_VampireName");
        LocalizedText LegacyBarHeader = TextObject.GetComponent<LocalizedText>();
        LegacyBarHeader.ForceSet("Legacy");
        LegacyText = LegacyBarHeader;

        // Activate the ExperienceBarObject
        LegacyBarObject.SetActive(true);

        // Deactivate prestige object
        /*
        if (ShowPrestige)
        {
            GameObject PrestigeObject = FindTargetUIObject(LegacyBarRectTransform.transform, "Text_Player_Prestige");
            PrestigeObject.SetActive(false);
        }
        */

        // Deactivate DamageTakenFill
        FindTargetUIObject(LegacyBarRectTransform.transform, "Icon_container").SetActive(false);
        FindTargetUIObject(LegacyBarRectTransform.transform, "DamageTakenFill").SetActive(false);
    }
    static void InitializeExpertise(GameObject gameObject)
    {
        // Instantiate the ExpertiseBar from the LegacyBar
        GameObject ExpertiseBarObject = GameObject.Instantiate(gameObject);

        // Mark for DontDestroyOnLoad, move to VRisingWorld scene
        RectTransform ExpertiseBarRectTransform = ExpertiseBarObject.GetComponent<RectTransform>();
        GameObject.DontDestroyOnLoad(ExpertiseBarObject);
        SceneManager.MoveGameObjectToScene(ExpertiseBarObject, SceneManager.GetSceneByName("VRisingWorld"));

        // Set parent to canvas
        ExpertiseBarRectTransform.SetParent(Canvas.transform, false);

        // Configure ExpertiseBar position, most of what was configured for LegacyBar can be reused
        ExpertiseBarRectTransform.position = new Vector3(gameObject.GetComponent<RectTransform>().position.x, gameObject.transform.position.y - ExpertiseBarRectTransform.rect.height, gameObject.transform.position.z);

        // Configure LegacyBar fill, color
        GameObject FillImageObject = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Fill");
        Image FillImage = FillImageObject.GetComponent<Image>();
        ExpertiseFill = FillImage;
        FillImage.fillAmount = 0f;
        FillImage.color = Color.gray;

        // Set LegacyBar header text
        GameObject TextObject = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Text_Player_VampireName");
        LocalizedText ExpertiseBarHeader = TextObject.GetComponent<LocalizedText>();
        ExpertiseBarHeader.ForceSet("Expertise");
        ExpertiseText = ExpertiseBarHeader;

        // Activate the ExperienceBarObject
        ExpertiseBarObject.SetActive(true);

        // Deactivate prestige object
        /*
        if (ShowPrestige)
        {
            GameObject PrestigeObject = FindTargetUIObject(ExpertiseBarObject.transform, "Text_Player_Prestige");
            PrestigeObject.SetActive(false);
        }
        */

        // Deactivate DamageTakenFill
        FindTargetUIObject(ExpertiseBarRectTransform.transform, "Icon_container").SetActive(false);
        FindTargetUIObject(ExpertiseBarRectTransform.transform, "DamageTakenFill").SetActive(false);
    }
    static void UnpackValues(int packed, bool Prestige, bool Legacies, bool Expertise,
                             out float levelProgress, out float? prestigeLevel, out float? legacyProgress, out float? expertiseProgress)
    {
        // Subtract offset
        packed -= 33;

        // Initialize outputs
        levelProgress = 0;
        prestigeLevel = null;
        legacyProgress = null;
        expertiseProgress = null;

        // Unpacker variable, starting at 0
        int unpacker = 0;

        // Always start by unpacking level progress
        levelProgress = (packed >> unpacker & 0x7F) / 100f;
        unpacker += 7;

        if (Prestige)
        {
            prestigeLevel = packed >> unpacker & 0x7F;
            unpacker += 7;
        }

        if (Legacies)
        {
            legacyProgress = (packed >> unpacker & 0x7F) / 100f;
            unpacker += 7;
        }

        if (Expertise)
        {
            expertiseProgress = (packed >> unpacker & 0x7F) / 100f;
        }
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
        static Texture2D CreateGreenTexture()
        {
            // Create a 1x1 texture
            Texture2D texture = new(1, 1);

            // Set the texture color to green
            texture.SetPixel(0, 0, Color.green);

            // Apply changes to the texture
            texture.Apply();

            return texture;
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

                //FindGameObjectComponents(current.gameObject);
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
    static WeaponType GetWeaponTypeFromEntity(Entity weaponEntity)
    {
        if (weaponEntity == Entity.Null) return WeaponType.Unarmed;

        string weaponCheck = weaponEntity.Read<PrefabGUID>().LookupName();

        return Enum.GetValues(typeof(WeaponType))
                   .Cast<WeaponType>()
                   .FirstOrDefault(type =>
                    weaponCheck.Contains(type.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    !(type == WeaponType.Sword && weaponCheck.Contains("GreatSword", StringComparison.OrdinalIgnoreCase))
                   );
    }
    static BloodType GetBloodTypeFromPrefab(PrefabGUID bloodPrefab)
    {
        string bloodCheck = bloodPrefab.LookupName().ToString();

        return Enum.GetValues(typeof(BloodType))
                   .Cast<BloodType>()
                   .FirstOrDefault(type =>
                       bloodCheck.Contains(type.ToString(), StringComparison.OrdinalIgnoreCase));
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