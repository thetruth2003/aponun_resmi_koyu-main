using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SaveCoordinator, dunyayi kaydetme/yukleme, menu metadata'si, quest ilerlemesi ve
/// autosave gibi daginik akislarin tek merkezden yonetilmesini saglar.
/// </summary>
[DefaultExecutionOrder(-500)]
public class SaveCoordinator : MonoBehaviour
{
    private const string KeySaveExists = "SaveExists";
    private const string KeyLastScene = "LastScene";
    private const string KeyDayCount = "DayCount";
    private const string KeyStateKeys = "state_keys";
    private const string DefaultGameplayScene = "aponun orjinal koyu";
    private const string DefaultCutsceneSaveFile = "cutscenes_save.json";
    private const string MetaFileName = "meta_save.json";
    private const string QuestProgressFileName = "quest_progress.json";
    private const string PlayerStateFileName = "player_state.json";

    public static SaveCoordinator Instance { get; private set; }

    private static bool _bootstrapped;

    private ActiveQuestSystem _subscribedQuestSystem;
    private bool _pendingLoad;
    private string _pendingSceneName;
    private bool _isRestoring;

    private string MetaPath => Path.Combine(Application.persistentDataPath, MetaFileName);
    private string QuestProgressPath => Path.Combine(Application.persistentDataPath, QuestProgressFileName);
    private string PlayerStatePath => Path.Combine(Application.persistentDataPath, PlayerStateFileName);
    private string DefaultCutsceneSavePath => Path.Combine(Application.persistentDataPath, DefaultCutsceneSaveFile);

    [Serializable]
    private class MetaSave
    {
        public int day;
        public int money;
        public string lastQuest;
        public string savedAt;
    }

    [Serializable]
    private class QuestProgressRec
    {
        public string assetName;
        public int currentIndex;
    }

    [Serializable]
    private class QuestProgressModel
    {
        public List<QuestProgressRec> quests = new();
    }

    [Serializable]
    private class PlayerStateModel
    {
        public bool hasState;
        public Vector3 position;
        public Vector3 eulerAngles;
        public bool wasInCar;
        public string activeCarId;
    }

    [Serializable]
    private class StateKeyListWrapper
    {
        public List<string> keys = new();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstanceExists()
    {
        if (_bootstrapped || Instance != null)
            return;

        var go = new GameObject("__SaveCoordinator");
        DontDestroyOnLoad(go);
        go.AddComponent<SaveCoordinator>();
        _bootstrapped = true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _bootstrapped = true;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        game_start.OnDayChanged += HandleDayChangedAutosave;
        RefreshRuntimeHooks();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        game_start.OnDayChanged -= HandleDayChangedAutosave;
        UnsubscribeQuestAutosave();
    }

    private void OnApplicationQuit()
    {
        if (_isRestoring)
            return;

        if (FindWorldSave() == null && FindQuestSave() == null)
            return;

        SaveGame("application quit");
    }

    public bool HasSave() => PlayerPrefs.GetInt(KeySaveExists, 0) == 1;

    public void SaveGame(string reason = "manual")
    {
        if (_isRestoring)
            return;

        SaveWorldState();
        SaveQuestState();
        SaveQuestProgress();
        SavePlayerState();
        SaveMeta(reason);
    }

    public void SaveGameNow()
    {
        SaveGame("manual");
    }

    public void LoadLastSaveFromMenu()
    {
        if (!HasSave())
            return;

        string lastScene = PlayerPrefs.GetString(KeyLastScene, DefaultGameplayScene);
        BeginLoad(lastScene);
    }

    public void LoadLastSaveNow()
    {
        LoadLastSaveFromMenu();
    }

    public void BeginLoad(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            sceneName = DefaultGameplayScene;

        _pendingLoad = true;
        _pendingSceneName = sceneName.Trim();
        Time.timeScale = 1f;
        SceneManager.LoadScene(_pendingSceneName, LoadSceneMode.Single);
    }

    public void StartNewGame(string sceneName = DefaultGameplayScene)
    {
        ClearAllSavedData();
        _pendingLoad = false;
        _pendingSceneName = null;
        Time.timeScale = 1f;
        SceneManager.LoadScene(string.IsNullOrWhiteSpace(sceneName) ? DefaultGameplayScene : sceneName, LoadSceneMode.Single);
    }

    public void ClearAllSavedData()
    {
        if (GameStateTracker.Instance != null)
            GameStateTracker.Instance.ClearAll();

        DeleteIfExists(MetaPath);
        DeleteIfExists(QuestProgressPath);
        DeleteIfExists(PlayerStatePath);
        DeleteIfExists(DefaultCutsceneSavePath);

        ClearWorldSaveFiles();
        ClearQuestStatePrefs();

        PlayerPrefs.DeleteKey(KeySaveExists);
        PlayerPrefs.DeleteKey(KeyLastScene);
        PlayerPrefs.DeleteKey(KeyDayCount);
        PlayerPrefs.Save();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshRuntimeHooks();

        if (_pendingLoad && string.Equals(scene.name, _pendingSceneName, StringComparison.OrdinalIgnoreCase))
            StartCoroutine(RestoreAfterSceneLoad());
    }

    private IEnumerator RestoreAfterSceneLoad()
    {
        yield return null;
        yield return null;

        RestoreCurrentScene();
    }

    private void RestoreCurrentScene()
    {
        if (_isRestoring)
            return;

        _isRestoring = true;
        _pendingLoad = false;

        try
        {
            RefreshRuntimeHooks();
            LoadQuestState();
            LoadWorldState();
            RestorePlayerState();
            RestoreQuestProgress();
            RefreshRuntimeUI();
        }
        finally
        {
            _pendingSceneName = null;
            _isRestoring = false;
        }
    }

    private void RefreshRuntimeHooks()
    {
        ActiveQuestSystem activeQuestSystem = FindObjectOfType<ActiveQuestSystem>(true);
        if (_subscribedQuestSystem == activeQuestSystem)
            return;

        UnsubscribeQuestAutosave();
        _subscribedQuestSystem = activeQuestSystem;

        if (_subscribedQuestSystem != null)
            _subscribedQuestSystem.OnActiveStepChanged += HandleQuestStepChangedAutosave;
    }

    private void UnsubscribeQuestAutosave()
    {
        if (_subscribedQuestSystem != null)
            _subscribedQuestSystem.OnActiveStepChanged -= HandleQuestStepChangedAutosave;

        _subscribedQuestSystem = null;
    }

    private void HandleQuestStepChangedAutosave(QuestEditorAsset asset, int currentIndex)
    {
        if (_isRestoring)
            return;

        SaveGame("quest step changed");
    }

    private void HandleDayChangedAutosave()
    {
        if (_isRestoring)
            return;

        SaveGame("day changed");
    }

    private void SaveWorldState()
    {
        savetest worldSave = FindWorldSave();
        if (worldSave == null)
            return;

        InvokeSaveWorldMethod(worldSave, "SaveTools");
        InvokeSaveWorldMethod(worldSave, "SaveMoney");
        InvokeSaveWorldMethod(worldSave, "SaveBuildings");
        InvokeSaveWorldMethod(worldSave, "SaveCars");
        InvokeSaveWorldMethod(worldSave, "SaveSeeds");
        InvokeSaveWorldMethod(worldSave, "SaveInventoryBoth");
    }

    private void LoadWorldState()
    {
        savetest worldSave = FindWorldSave();
        if (worldSave == null)
            return;

        InvokeSaveWorldMethod(worldSave, "LoadTools");
        InvokeSaveWorldMethod(worldSave, "LoadMoney");
        InvokeSaveWorldMethod(worldSave, "LoadBuildings");
        InvokeSaveWorldMethod(worldSave, "LoadCars");
        InvokeSaveWorldMethod(worldSave, "LoadSeeds");
        InvokeSaveWorldMethod(worldSave, "LoadInventoryBoth");
    }

    private void SaveQuestState()
    {
        QuestSave questSave = FindQuestSave();
        if (questSave != null)
            questSave.SaveData();
    }

    private void LoadQuestState()
    {
        QuestSave questSave = FindQuestSave();
        if (questSave != null)
            questSave.LoadData();
    }

    private void SaveQuestProgress()
    {
        ActiveQuestSystem activeQuestSystem = FindObjectOfType<ActiveQuestSystem>(true);
        if (activeQuestSystem == null)
            return;

        QuestProgressModel model = new QuestProgressModel();
        foreach (ActiveQuestSystem.TrackedQuest tracked in activeQuestSystem.allQuests)
        {
            if (tracked == null || tracked.asset == null)
                continue;

            model.quests.Add(new QuestProgressRec
            {
                assetName = tracked.asset.name,
                currentIndex = tracked.currentIndex
            });
        }

        File.WriteAllText(QuestProgressPath, JsonUtility.ToJson(model, true));
    }

    private void RestoreQuestProgress()
    {
        ActiveQuestSystem activeQuestSystem = FindObjectOfType<ActiveQuestSystem>(true);
        if (activeQuestSystem == null || !File.Exists(QuestProgressPath))
            return;

        QuestProgressModel model = JsonUtility.FromJson<QuestProgressModel>(File.ReadAllText(QuestProgressPath));
        if (model?.quests == null)
            return;

        foreach (QuestProgressRec rec in model.quests)
        {
            if (rec == null || string.IsNullOrWhiteSpace(rec.assetName))
                continue;

            ActiveQuestSystem.TrackedQuest tracked = activeQuestSystem.allQuests.Find(q =>
                q != null && q.asset != null && string.Equals(q.asset.name, rec.assetName, StringComparison.OrdinalIgnoreCase));

            if (tracked == null || tracked.asset == null || tracked.asset.quests == null)
                continue;

            tracked.currentIndex = Mathf.Clamp(rec.currentIndex, 0, tracked.asset.quests.Count);
        }
    }

    private void SavePlayerState()
    {
        GameObject playerObject = FindPlayerGameObject();
        StateManger stateManager = StateManger.Instance ?? FindObjectOfType<StateManger>(true);

        PlayerStateModel model = new PlayerStateModel();
        if (stateManager != null && stateManager.state == gamestate.Car && stateManager.car != null)
        {
            model.hasState = true;
            model.wasInCar = true;
            model.position = stateManager.car.transform.position;
            model.eulerAngles = stateManager.car.transform.eulerAngles;

            Car activeCar = stateManager.car.GetComponentInParent<Car>() ?? stateManager.car.GetComponentInChildren<Car>(true);
            model.activeCarId = activeCar ? activeCar.persistentId : string.Empty;
        }
        else if (playerObject != null)
        {
            model.hasState = true;
            model.position = playerObject.transform.position;
            model.eulerAngles = playerObject.transform.eulerAngles;
        }

        File.WriteAllText(PlayerStatePath, JsonUtility.ToJson(model, true));
    }

    private void RestorePlayerState()
    {
        if (!File.Exists(PlayerStatePath))
            return;

        PlayerStateModel model = JsonUtility.FromJson<PlayerStateModel>(File.ReadAllText(PlayerStatePath));
        if (model == null || !model.hasState)
            return;

        if (TryRestorePlayerInCar(model))
            return;

        GameObject playerObject = FindPlayerGameObject();
        if (playerObject != null)
            playerObject.transform.SetPositionAndRotation(model.position, Quaternion.Euler(model.eulerAngles));
    }

    private bool TryRestorePlayerInCar(PlayerStateModel model)
    {
        if (!model.wasInCar || string.IsNullOrWhiteSpace(model.activeCarId))
            return false;

        StateManger stateManager = StateManger.Instance ?? FindObjectOfType<StateManger>(true);
        if (stateManager == null || stateManager.player == null || stateManager.playerCamera == null)
            return false;

        Car[] cars = FindObjectsOfType<Car>(true);
        Car targetCar = Array.Find(cars, c => c != null && string.Equals(c.persistentId, model.activeCarId, StringComparison.OrdinalIgnoreCase));
        if (targetCar == null)
            return false;

        CarEnterable enterable = targetCar.GetComponentInParent<CarEnterable>() ?? targetCar.GetComponentInChildren<CarEnterable>(true);
        if (enterable == null)
            return false;

        stateManager.player.transform.SetPositionAndRotation(targetCar.transform.position, targetCar.transform.rotation);
        if (!enterable.Enter(stateManager.player, stateManager.playerCamera))
            return false;

        stateManager.car = targetCar.gameObject;
        stateManager.state = gamestate.Car;
        if (stateManager.Speedometer) stateManager.Speedometer.SetActive(true);
        if (stateManager.stamina) stateManager.stamina.SetActive(false);
        return true;
    }

    private void SaveMeta(string reason)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt(KeySaveExists, 1);
        PlayerPrefs.SetString(KeyLastScene, sceneName);
        PlayerPrefs.Save();

        MetaSave meta = new MetaSave
        {
            day = ResolveCurrentDay(),
            money = ResolveCurrentMoney(),
            lastQuest = ResolveCurrentQuestName(),
            savedAt = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ({reason})"
        };

        File.WriteAllText(MetaPath, JsonUtility.ToJson(meta, true));
    }

    private void RefreshRuntimeUI()
    {
        if (GameManager.instance?.uiManager != null)
            GameManager.instance.uiManager.RefreshAll();

        QuestUI questUI = FindObjectOfType<QuestUI>(true);
        if (questUI != null)
            questUI.UpdateQuestUI();
    }

    private int ResolveCurrentDay()
    {
        game_start dayController = FindObjectOfType<game_start>(true);
        if (dayController != null)
            return dayController.dayCount;

        if (GameTime.Instance != null)
            return GameTime.Instance.dayCount;

        return PlayerPrefs.GetInt(KeyDayCount, 1);
    }

    private int ResolveCurrentMoney()
    {
        Muhasebeci muhasebeci = FindObjectOfType<Muhasebeci>(true);
        return muhasebeci != null ? muhasebeci.GetMoney() : 0;
    }

    private string ResolveCurrentQuestName()
    {
        ActiveQuestSystem activeQuestSystem = FindObjectOfType<ActiveQuestSystem>(true);
        if (activeQuestSystem == null)
            return string.Empty;

        foreach (ActiveQuestSystem.TrackedQuest tracked in activeQuestSystem.allQuests)
        {
            if (tracked?.asset == null || tracked.asset.quests == null)
                continue;

            if (tracked.currentIndex < 0 || tracked.currentIndex >= tracked.asset.quests.Count)
                continue;

            QuestContainer container = tracked.asset.quests[tracked.currentIndex];
            if (container != null && !string.IsNullOrWhiteSpace(container.questName))
                return container.questName;
        }

        return "All quests complete";
    }

    private void ClearWorldSaveFiles()
    {
        DeleteIfExists(Path.Combine(Application.persistentDataPath, "money_save.json"));
        DeleteIfExists(Path.Combine(Application.persistentDataPath, "tools_save.json"));
        DeleteIfExists(Path.Combine(Application.persistentDataPath, "buildings_save.json"));
        DeleteIfExists(Path.Combine(Application.persistentDataPath, "cars_save.json"));
        DeleteIfExists(Path.Combine(Application.persistentDataPath, "seeds_save.json"));
        DeleteIfExists(Path.Combine(Application.persistentDataPath, "inv_backpack.json"));
        DeleteIfExists(Path.Combine(Application.persistentDataPath, "inv_toolbar.json"));
        DeleteIfExists(Path.Combine(Application.persistentDataPath, "inventory_player.json"));
    }

    private void ClearQuestStatePrefs()
    {
        if (!PlayerPrefs.HasKey(KeyStateKeys))
            return;

        StateKeyListWrapper wrapper = JsonUtility.FromJson<StateKeyListWrapper>(PlayerPrefs.GetString(KeyStateKeys));
        if (wrapper?.keys != null)
        {
            foreach (string typedKey in wrapper.keys)
            {
                if (string.IsNullOrWhiteSpace(typedKey) || !typedKey.Contains(":"))
                    continue;

                string[] parts = typedKey.Split(':');
                if (parts.Length != 2)
                    continue;

                string type = parts[0];
                string rawKey = parts[1];

                PlayerPrefs.DeleteKey(rawKey);

                switch (type)
                {
                    case "int":
                        PlayerPrefs.DeleteKey("state_int_" + rawKey);
                        break;
                    case "bool":
                        PlayerPrefs.DeleteKey("state_bool_" + rawKey);
                        break;
                    case "string":
                        PlayerPrefs.DeleteKey("state_string_" + rawKey);
                        break;
                }
            }
        }

        PlayerPrefs.DeleteKey(KeyStateKeys);
        PlayerPrefs.Save();
    }

    private static void InvokeSaveWorldMethod(savetest worldSave, string methodName)
    {
        if (worldSave == null || string.IsNullOrWhiteSpace(methodName))
            return;

        MethodInfo method = worldSave.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        method?.Invoke(worldSave, null);
    }

    private static savetest FindWorldSave()
    {
        return FindObjectOfType<savetest>(true);
    }

    private static QuestSave FindQuestSave()
    {
        return FindObjectOfType<QuestSave>(true);
    }

    private static GameObject FindPlayerGameObject()
    {
        if (GameManager.instance?.player != null)
            return GameManager.instance.player.gameObject;

        if (Player.Instance != null)
            return Player.Instance.gameObject;

        try
        {
            return GameObject.FindGameObjectWithTag("Player");
        }
        catch
        {
            return null;
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
