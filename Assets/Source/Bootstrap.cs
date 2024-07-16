using Animation2D;
using Rogue;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Wargon.Ecsape;
using Wargon.UI;

[DefaultExecutionOrder(ExecutionOrder.Bootstrap)]
public class Bootstrap : MonoBehaviour {
    [SerializeField] private int targetFps = 60;
    [SerializeField] private UIRoot _root;
    [SerializeField] private AnimationsHolder AnimationsHolder;
    [SerializeField] private AbilitiesSO AbilityList;
    [SerializeField] private ItemsCollectionsConfig ItemsCollectionsConfig;
    [SerializeField] private Camera MainCamera;
    [SerializeField] private GameCore core;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private FloatingTextRenderService _textRenderService;
    private void Awake() {
        Application.targetFrameRate = targetFps;
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
        di.Register<ITextService>(_textRenderService);
    }
}

public static class SceneAPI {
    public static void OnUnLoad(UnityAction<Scene> action) {
        SceneManager.sceneUnloaded += action;
    }

    public static void OnLoad(UnityAction<Scene, LoadSceneMode> action) {
        SceneManager.sceneLoaded += action;
    }
    
}
public static class ExecutionOrder {
    public const int Bootstrap = -35;
    public const int EcsMain = -34;
}

public struct Sprite2D {
    public Vector4 Color;
    public Vector4 UV;
    public int Index;
}
public class SpriteRenderManager : MonoBehaviour {
    
}