using Animation2D;
using Rogue;
using UnityEngine;
using Wargon.Ecsape;
using Wargon.UI;

[DefaultExecutionOrder(ExecutionOrder.Bootstrap)]
public class Bootstrap : MonoBehaviour {
    [SerializeField] private UIRoot _root;
    [SerializeField] private AnimationsHolder AnimationsHolder;
    [SerializeField] private AbilitiesSO AbilityList;
    [SerializeField] private ItemsCollectionsConfig ItemsCollectionsConfig;
    [SerializeField] private Camera MainCamera;
    [SerializeField] private GameCore core;
    [SerializeField] private EnemySpawner enemySpawner;
    private void Awake() {
        World.ENTITIES_CACHE = 32;
        var uiService = new UIService(_root);
        var di = DI.GetOrCreateContainer<DependencyContainer>();
        di.Register(core);
        di.Register<Camera>().From(MainCamera);
        di.Register<IUIService>().From(uiService);
        di.Register<AnimationsHolder>().From(AnimationsHolder);
        di.Register<IObjectPool>(new GameObjectPool());
        di.Register<AbilitiesSO>().From(AbilityList);
        di.Register<RunTimeData>().From<RunTimeData>();
        di.Register<ItemsCollectionsConfig>().From(ItemsCollectionsConfig);
        di.Register(new InventoryService());
        di.Register(new LootServise());
        di.Register(new SaveService());
        di.Register<IPauseSerivce>(new PauseService());
        di.Register(enemySpawner);
    }
}

public static class ExecutionOrder {
    public const int Bootstrap = -35;
    public const int EcsMain = -34;
}