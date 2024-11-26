using Stunlock.Core;
using Stunlock.Localization;
using System.Reflection;
using System.Text.Json;

namespace Eclipse;
internal class Localization
{
    struct Code
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }
    struct Node
    {
        public string Guid { get; set; }
        public string Text { get; set; }
    }
    struct Words
    {
        public string Original { get; set; }
        public string Translation { get; set; }
    }
    struct LocalizationFile
    {
        public Code[] Codes { get; set; }
        public Node[] Nodes { get; set; }
        public Words[] Words { get; set; }
    }
    internal class LocalKey
    {
        public string Key { get; set; }
    }

    //static readonly Dictionary<PrefabGUID, LocalizationKey> LocalizationKeyMap = [];
    static readonly Dictionary<string, string> GuidStringsToLocalizedNames = []; // 1 and 2, need 2 to be keys in other dictionary back to the guid string 1 then back to the prefab int
    static readonly Dictionary<int, string> PrefabHashesToGuidStrings = [];

    static readonly Dictionary<string, string> LocalizedNamesToGuidStrings = [];
    static readonly Dictionary<string, int> GuidStringsToPrefabHashes = [];
    public Localization()
    {
        LoadLocalizations();
        LoadPrefabNames();
    }
    static void LoadLocalizations()
    {
        var resourceName = "Eclipse.Localization.English.json";

        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader localizationReader = new(stream);
        string jsonContent = localizationReader.ReadToEnd();
        var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent);

        localizationFile.Nodes
            .ToDictionary(x => x.Guid, x => x.Text)
            .ForEach(kvp => GuidStringsToLocalizedNames[kvp.Key] = kvp.Value);

        localizationFile.Nodes
            .ToDictionary(x => x.Text, x => x.Guid)
            .ForEach(kvp => LocalizedNamesToGuidStrings[kvp.Key] = kvp.Value);
    }
    static void LoadPrefabNames()
    {
        var resourceName = "Eclipse.Localization.Prefabs.json";

        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader reader = new(stream);
        string jsonContent = reader.ReadToEnd();
        var prefabNames = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonContent);

        prefabNames.ForEach(kvp => PrefabHashesToGuidStrings[kvp.Key] = kvp.Value);
        prefabNames.ForEach(kvp => GuidStringsToPrefabHashes[kvp.Value] = kvp.Key);
    }
    static string GetLocalizationFromKey(LocalizationKey key)
    {
        var guid = key.Key.ToGuid().ToString();

        return GetLocalization(guid);
    }
    public static PrefabGUID GetPrefabGUIDFromLocalizedName(string name)
    {
        if (LocalizedNamesToGuidStrings.TryGetValue(name, out string guidString) && GuidStringsToPrefabHashes.TryGetValue(guidString, out int prefabHash))
        {
            return new(prefabHash);
        }

        return PrefabGUID.Empty;
    }
    public static string GetLocalizedPrefabName(PrefabGUID prefabGUID)
    {
        if (PrefabHashesToGuidStrings.TryGetValue(prefabGUID.GuidHash, out var itemLocalizationHash))
        {
            return GetLocalization(itemLocalizationHash);
        }

        return prefabGUID.LookupName();
    }
    public static string GetLocalization(string Guid)
    {
        if (GuidStringsToLocalizedNames.TryGetValue(Guid, out var Text))
        {
            return Text;
        }

        return "Couldn't find localizationKey!";
    }
    public static LocalizationKey GetLocalizationKeyFromPrefabGUID(PrefabGUID prefabGUID)
    {
        if (PrefabHashesToGuidStrings.TryGetValue(prefabGUID.GuidHash, out var itemLocalizationHash))
        {
            return new LocalizationKey { Key = AssetGuid.FromString(itemLocalizationHash)};
        }

        return LocalizationKey.Empty; // Or handle appropriately
    }
}
