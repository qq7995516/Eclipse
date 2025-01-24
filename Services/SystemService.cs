using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Scripting;
using Unity.Entities;

namespace Eclipse.Services;
public class SystemService(World world)
{
    readonly World _world = world ?? throw new ArgumentNullException(nameof(world));

    ClientScriptMapper _clientScriptMapper;
    public ClientScriptMapper ClientScriptMapper => _clientScriptMapper ??= GetSystem<ClientScriptMapper>();

    PrefabCollectionSystem _prefabCollectionSystem;
    public PrefabCollectionSystem PrefabCollectionSystem => _prefabCollectionSystem ??= GetSystem<PrefabCollectionSystem>();

    GameDataSystem _gameDataSystem;
    public GameDataSystem GameDataSystem => _gameDataSystem ??= GetSystem<GameDataSystem>();

    ManagedDataSystem _managedDataSystem;
    public ManagedDataSystem ManagedDataSystem => _managedDataSystem ??= GetSystem<ManagedDataSystem>();
    T GetSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetExistingSystemManaged<T>() ?? throw new InvalidOperationException($"Failed to get {Il2CppType.Of<T>().FullName} from the Server...");
    }
}