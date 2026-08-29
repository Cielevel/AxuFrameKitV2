# Save 模块 - 使用案例

本文档提供常见场景的完整用例代码，包括最佳实践和性能优化建议。

> 💡 **快速开始**: 如果您只需要快速保存少量数据，可直接跳转到 [7. 简易用法示例](#7-简易用法示例)

---

## 目录

1. [游戏存档](#1-游戏存档)
2. [玩家设置/配置](#2-玩家设置配置)
3. [多存档槽位](#3-多存档槽位)
4. [运行时缓存 + 自动保存](#4-运行时缓存--自动保存)
5. [加密敏感数据](#5-加密敏感数据)
6. [全局游戏状态](#6-全局游戏状态)
7. [简易用法示例](#7-简易用法示例)
8. [常见问题 & 注意事项](#8-常见问题--注意事项)
9. [性能优化建议](#9-性能优化建议)

---

## 1. 游戏存档

完整的游戏进度存档方案。

### 数据模型

```csharp
[Serializable]
public class GameSave
{
    public int SaveSlot;
    public string PlayerName;
    public int Level;
    public int Experience;
    public int Gold;
    public float PlayTimeHours;
    public Vector3Data LastPosition;
    public List<string> UnlockedItems;
    public DictionaryData<string, int> QuestProgress; // 需要自定义序列化
    public System.DateTime SaveTime;
}

[Serializable]
public struct Vector3Data
{
    public float x, y, z;
    public static Vector3Data FromVector3(Vector3 v) => new() { x = v.x, y = v.y, z = v.z };
    public Vector3 ToVector3() => new(x, y, z);
}

// Dictionary 包装类 (Unity JsonUtility 不支持 Dictionary)
[Serializable]
public class DictionaryData<TKey, TValue>
{
    public List<TKey> keys = new();
    public List<TValue> values = new();

    public void FromDictionary(Dictionary<TKey, TValue> dict)
    {
        keys = dict.Keys.ToList();
        values = dict.Values.ToList();
    }

    public Dictionary<TKey, TValue> ToDictionary()
    {
        var result = new Dictionary<TKey, TValue>();
        for (int i = 0; i < keys.Count; i++)
            result[keys[i]] = values[i];
        return result;
    }
}
```

### 存档管理器

```csharp
public class GameSaveManager : SingletonMono<GameSaveManager>
{
    private SaveUtil _saveUtil;
    private const int MAX_SLOTS = 3;

    private void Awake()
    {
        _saveUtil = new SaveUtilBuilder()
            .UseSerializer(new UnityJsonSerializer())
            .UseStorage(new LocalFileStorage(".sav"))
            .UseEncryption(new AesCrypto("GameSecretKey2024"))
            .Build();
    }

    // 保存到指定槽位
    public void SaveToSlot(int slot, GameSave data)
    {
        data.SaveSlot = slot;
        data.SaveTime = System.DateTime.Now;
        _saveUtil.Save($"slot_{slot}", data);
    }

    // 从槽位加载
    public GameSave LoadFromSlot(int slot)
    {
        return _saveUtil.Load($"slot_{slot}", new GameSave { SaveSlot = slot });
    }

    // 获取所有存档（按保存时间排序）
    public List<(int slot, string playerName, System.DateTime saveTime)> GetAllSaveSlots()
    {
        var result = new List<(int, string, System.DateTime)>();
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (_saveUtil.Exists($"slot_{i}"))
            {
                var save = LoadFromSlot(i);
                result.Add((i, save.PlayerName, save.SaveTime));
            }
        }
        return result.OrderByDescending(s => s.saveTime).ToList();
    }

    // 删除存档
    public void DeleteSlot(int slot)
    {
        _saveUtil.Delete($"slot_{slot}");
    }
}
```

---

## 2. 玩家设置/配置

游戏配置的读取、缓存和自动应用。

### 数据模型

```csharp
[Serializable]
public class GameSettings
{
    // 音频
    public float MasterVolume = 1.0f;
    public float BgmVolume = 0.8f;
    public float SfxVolume = 1.0f;
    public float VoiceVolume = 1.0f;

    // 画面
    public int ResolutionIndex = 0;
    public bool Fullscreen = true;
    public int QualityLevel = 2;
    public int TargetFrameRate = 60;
    public bool VSync = true;

    // 游戏
    public string Language = "zh-CN";
    public bool ShowSubtitles = true;
    public float CameraSensitivity = 1.0f;
    public bool InvertYAxis = false;
}
```

### 配置管理器

> **提示**: SaveUtil 现在包含 `GetAllKeys()` 和 `Clear()` 方法，可用于枚举/清空存储，行为与 ConfigManager 对齐。

### 配置管理器

```csharp
public class SettingsManager : SingletonMono<SettingsManager>
{
    private SaveUtil _saveUtil;
    private GameSettings _currentSettings;

    public GameSettings Settings => _currentSettings;
    public event System.Action<GameSettings> OnSettingsChanged;

    private void Awake()
    {
        _saveUtil = new SaveUtilBuilder()
            .UseSerializer(new UnityJsonSerializer())
            .UseStorage(new LocalFileStorage(".json"))
            .Build();

        _currentSettings = _saveUtil.Load("game", new GameSettings());
    }

    // 修改配置并保存
    public void UpdateSettings(System.Action<GameSettings> modifier)
    {
        modifier?.Invoke(_currentSettings);
        _saveUtil.Save("game", _currentSettings);
        ApplySettings(_currentSettings);
        OnSettingsChanged?.Invoke(_currentSettings);
    }

    // 应用配置到游戏
    private void ApplySettings(GameSettings s)
    {
        AudioListener.volume = s.MasterVolume;
        QualitySettings.SetQualityLevel(s.QualityLevel);
        Screen.fullScreen = s.Fullscreen;
        Application.targetFrameRate = s.TargetFrameRate;
    }

    // 重置为默认
    public void ResetToDefault()
    {
        _currentSettings = new GameSettings();
        _saveUtil.Save("game", _currentSettings);
    }
}

// ===== 使用示例 =====
public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;

    private void Start()
    {
        var settings = SettingsManager.Instance.Settings;
        masterVolumeSlider.value = settings.MasterVolume;

        masterVolumeSlider.onValueChanged.AddListener(value =>
        {
            SettingsManager.Instance.UpdateSettings(s => s.MasterVolume = value);
        });
    }
}
```

---

## 3. 多存档槽位

带元数据的多槽位管理。

```csharp
/// <summary>
/// 支持多个存档槽位，元数据分离方案
/// 元数据用于显示存档列表，避免加载完整数据
/// </summary>
public class MultiSlotSaveSystem
{
    private SaveUtil _saveUtil;
    private const string META_PREFIX = "Meta";
    private const string DATA_PREFIX = "Data";
    private const int MAX_SLOTS = 10;

    public MultiSlotSaveSystem(string encryptionKey = null)
    {
        var builder = new SaveUtilBuilder()
            .UseSerializer(new UnityJsonSerializer())
            .UseStorage(new LocalFileStorage(".sav"));

        if (!string.IsNullOrEmpty(encryptionKey))
        {
            builder.UseEncryption(new AesCrypto(encryptionKey));
        }

        _saveUtil = builder.Build();
    }

    // 存档元数据（用于存档列表显示，避免加载完整数据）
    [Serializable]
    public class SaveMeta
    {
        public int Slot;
        public string PlayerName;
        public int Level;
        public float PlayTimeHours;
        public int TotalKills;
        public string ScreenshotPath; // 缩略图路径
        public System.DateTime SaveTime;
        
        // 便于UI显示的格式化字符串
        public string FormattedSaveTime => SaveTime.ToString("yyyy-MM-dd HH:mm");
        public string DisplayInfo => $"{PlayerName} - Level {Level}";
    }

    // 保存数据和元数据
    public void Save<T>(int slot, T data, SaveMeta meta)
    {
        meta.Slot = slot;
        meta.SaveTime = System.DateTime.Now;

        _saveUtil.SaveDelayed($"{META_PREFIX}_{slot}", meta);
        _saveUtil.SaveDelayed($"{DATA_PREFIX}_{slot}", data);
        _saveUtil.SaveAll();
    }

    // 加载数据
    public T Load<T>(int slot, T defaultValue = null) where T : class, new()
    {
        return _saveUtil.Load($"{DATA_PREFIX}_{slot}", defaultValue ?? new T());
    }

    // 获取元数据
    public SaveMeta GetMeta(int slot)
    {
        return _saveUtil.Load($"{META_PREFIX}_{slot}", new SaveMeta { Slot = slot });
    }

    // 获取所有槽位元数据
    public List<SaveMeta> GetAllMetas()
    {
        var list = new List<SaveMeta>();
        for (int i = 0; i < MAX_SLOTS; i++)
            if (_saveUtil.Exists($"{META_PREFIX}_{i}"))
                list.Add(GetMeta(i));
        return list.OrderByDescending(m => m.SaveTime).ToList();
    }

    // 删除槽位
    public void DeleteSlot(int slot)
    {
        _saveUtil.Delete($"{META_PREFIX}_{slot}");
        _saveUtil.Delete($"{DATA_PREFIX}_{slot}");
    }
}
```

---

## 4. 运行时缓存 + 自动保存

频繁修改的数据使用缓存，定时自动保存。

```csharp
public class AutoSaveManager : SingletonMono<AutoSaveManager>
{
    private SaveUtil _saveUtil;
    private float _autoSaveInterval = 60f;
    private float _timer;
    private bool _hasUnsavedChanges;

    [Serializable]
    public class RuntimeData
    {
        public int KillCount;
        public int DeathCount;
        public float DistanceTraveled;
        public int ItemsCollected;
        public float SessionPlayTimeSeconds;
    }

    public RuntimeData Data { get; private set; }
    public event System.Action<RuntimeData> OnDataChanged;

    private void Awake()
    {
        _saveUtil = new SaveUtilBuilder()
            .UseSerializer(new UnityJsonSerializer())
            .UseStorage(new LocalFileStorage(".json"))
            .Build();

        Data = _saveUtil.Load("runtime", new RuntimeData());
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        Data.SessionPlayTimeSeconds += Time.deltaTime;

        if (_timer >= _autoSaveInterval && _hasUnsavedChanges)
        {
            SaveNow();
            _timer = 0f;
        }
    }

    // 增加击杀数、死亡数等
    public void AddKill(int count = 1) => UpdateData(() => Data.KillCount += count);
    public void AddDeath(int count = 1) => UpdateData(() => Data.DeathCount += count);
    public void AddDistance(float distance) { if (distance > 0) UpdateData(() => Data.DistanceTraveled += distance); }
    public void AddItem(int count = 1) => UpdateData(() => Data.ItemsCollected += count);

    private void UpdateData(System.Action action)
    {
        action?.Invoke();
        MarkDirty();
        OnDataChanged?.Invoke(Data);
    }

    private void MarkDirty()
    {
        _hasUnsavedChanges = true;
        _saveUtil.SaveDelayed("runtime", Data);
    }

    public void SaveNow()
    {
        _saveUtil.SaveAll();
        _hasUnsavedChanges = false;
    }

    private void OnApplicationQuit()
    {
        if (_hasUnsavedChanges) SaveNow();
    }
}

// ===== 使用示例 =====
public class GameplayUI : MonoBehaviour
{
    [SerializeField] private Text killCountText;
    [SerializeField] private Text distanceText;

    private void Start()
    {
        // 初始化UI
        RefreshUI(AutoSaveManager.Instance.Data);

        // 监听数据变化，实时更新UI
        AutoSaveManager.Instance.OnDataChanged += RefreshUI;
    }

    private void RefreshUI(AutoSaveManager.RuntimeData data)
    {
        killCountText.text = $"击杀: {data.KillCount}";
        distanceText.text = $"距离: {data.DistanceTraveled:F1}m";
    }

    private void OnDestroy()
    {
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.OnDataChanged -= RefreshUI;
        }
    }
}
```

---

## 5. 加密敏感数据

登录信息、内购记录等敏感数据的加密存储。

```csharp
public class SecureDataManager : SingletonLazy<SecureDataManager>
{
    private SaveUtil _secureSave;

    public SecureDataManager()
    {
        _secureSave = new SaveUtilBuilder()
            .UseSerializer(new UnityJsonSerializer())
            .UseStorage(new LocalFileStorage(".dat"))
            .UseEncryption(new AesCrypto("SecureKey_32CharactersLong!"))
            .Build();
    }

    // 登录数据
    [Serializable]
    public class LoginData
    {
        public string UserId;
        public string Username;
        public string AuthToken;
        public System.DateTime TokenExpiry;
    }

    public void SaveLoginData(LoginData data) => _secureSave.Save("login", data);
    public LoginData LoadLoginData() => _secureSave.Load("login", new LoginData());
    public bool IsLoggedIn()
    {
        var data = LoadLoginData();
        return !string.IsNullOrEmpty(data.AuthToken) && data.TokenExpiry > System.DateTime.Now;
    }
    public void ClearLoginData() => _secureSave.Delete("login");

    // 内购记录
    [Serializable]
    public class PurchaseRecord
    {
        public List<string> OwnedItemIds = new();
        public int PremiumCurrency;
    }

    public void SavePurchaseRecord(PurchaseRecord record) => _secureSave.Save("purchase", record);
    public PurchaseRecord LoadPurchaseRecord() => _secureSave.Load("purchase", new PurchaseRecord());
    
    public bool OwnsItem(string itemId)
    {
        var record = LoadPurchaseRecord();
        return record.OwnedItemIds.Contains(itemId);
    }

    public void AddOwnedItem(string itemId)
    {
        var record = LoadPurchaseRecord();
        if (!record.OwnedItemIds.Contains(itemId))
        {
            record.OwnedItemIds.Add(itemId);
            SavePurchaseRecord(record);
        }
    }
}
```

---

## 6. 全局游戏状态

跨场景的游戏状态管理。

```csharp
public class GameStateManager : SingletonMono<GameStateManager>
{
    private SaveUtil _stateSave;

    [Serializable]
    public class GameState
    {
        public string CurrentLevel = "Level1";
        public int CurrentChapter = 1;
        public HashSet<string> UnlockedCharacters = new();
        public HashSet<string> CompletedAchievements = new();
    }

    public GameState State { get; private set; }
    public event System.Action<GameState> OnStateChanged;

    private void Awake()
    {
        _stateSave = new SaveUtilBuilder()
            .UseSerializer(new UnityJsonSerializer())
            .UseStorage(new LocalFileStorage(".json"))
            .Build();

        State = _stateSave.Load("main", new GameState());
    }

    public void SaveState()
    {
        _stateSave.Save("main", State);
    }

    // 解锁角色
    public void UnlockCharacter(string characterId)
    {
        if (State.UnlockedCharacters.Add(characterId))
        {
            SaveState();
            OnStateChanged?.Invoke(State);
        }
    }

    public bool IsCharacterUnlocked(string characterId) => State.UnlockedCharacters.Contains(characterId);

    // 完成成就
    public void CompleteAchievement(string achievementId)
    {
        if (State.CompletedAchievements.Add(achievementId))
        {
            SaveState();
            OnStateChanged?.Invoke(State);
        }
    }

    public bool IsAchievementCompleted(string achievementId) => State.CompletedAchievements.Contains(achievementId);

    // 重置进度
    public void ResetProgress()
    {
        State = new GameState();
        SaveState();
    }
}
```

---

## 7. 简易用法示例

### 构建器便捷方法
新版本的 `SaveUtilBuilder` 提供与 Config 管理器类似的快捷配置：
```csharp
var util = new SaveUtilBuilder()
    .UsePlayerPrefs()
    .UseUnityJsonSerializer()
    .UseXorCrypto("key")
    .Build();
```


快速原型或小规模数据存储推荐使用 `SimpleSave`。

```csharp
// ===== 最简单的用法 =====
public class QuickSaveExample : MonoBehaviour
{
    [Serializable]
    public class PlayerProgress
    {
        public int Level = 1;
        public int Score;
        public int HighScore;
    }

    private void Start()
    {
        // 加载进度
        var progress = SimpleSave.LoadData<PlayerProgress>("progress") ?? new PlayerProgress();

        progress.Score += 100;
        if (progress.Score > progress.HighScore)
            progress.HighScore = progress.Score;

        // 保存
        SimpleSave.SaveData("progress", progress);
    }
}

// ===== 使用 PlayerPrefs =====
public class PlayerPrefsExample : MonoBehaviour
{
    private void Start()
    {
        SimpleSave.SaveString("player_name", "Hero", SimpleSave.SaveMode.PlayerPrefs);
        SimpleSave.SaveInt("level", 5, SimpleSave.SaveMode.PlayerPrefs);
        
        string name = SimpleSave.LoadString("player_name", "DefaultName", SimpleSave.SaveMode.PlayerPrefs);
    }
}

// ===== 加密保存 =====
public class EncryptedSaveExample : MonoBehaviour
{
    [Serializable]
    public class UserAccount
    {
        public string UserId;
        public string AuthToken;
        public int Credits;
    }

    private void Start()
    {
        var account = new UserAccount { UserId = "user123", Credits = 100 };
        SimpleSave.SaveData("account", account, SimpleSave.SaveMode.EncryptedJsonFile);
        
        var loaded = SimpleSave.LoadData<UserAccount>("account", SimpleSave.SaveMode.EncryptedJsonFile);
    }
}

// ===== 集合保存 =====
public class CollectionExample : MonoBehaviour
{
    [Serializable]
    public class ItemList
    {
        public List<string> items = new();
    }

    private void Start()
    {
        var inventory = new ItemList();
        inventory.items.AddRange(new[] { "Sword", "Shield", "Potion" });
        SimpleSave.SaveData("inventory", inventory);
    }
}
```

---

## 最佳实践总结

| 场景 | 推荐方案 | 存储模式 | 读写频率 |
|------|----------|----------|----------|
| 游戏进度存档 | SaveUtil + AES加密 | `.sav` 加密文件 | 低（仅保存/加载点） |
| 玩家设置 | SaveUtil + 明文 | `.json` 便于调试 | 中（启动/变更时） |
| 运行时统计 | SaveUtil + 延迟保存 + 定时SaveAll | `.json` | 高（频繁修改） |
| 敏感数据 | SaveUtil + AES加密 | `.dat` 加密文件 | 特低（仅登录时） |
| 快速原型 | SimpleSave | 任意 | 任意 |
| 小型键值对 | SimpleSave + PlayerPrefs | PlayerPrefs | 中等 |

---

## 8. 常见问题 & 注意事项

### Q1: 如何防止存档数据被篡改？

使用 `AesCrypto` 加密敏感数据，不要在代码中硬编码密钥：

```csharp
.UseEncryption(new AesCrypto("YourSecretKey32CharactersLong!"))
```

---

### Q2: SaveDelayed 和 Save 有什么区别？

- `Save()` - 立即写入磁盘（影响性能）
- `SaveDelayed()` - 缓存在内存，等待 `SaveAll()` 批量写入（高性能）

```csharp
// 频繁修改的数据
_saveUtil.SaveDelayed("key1", data1);
_saveUtil.SaveDelayed("key2", data2);
_saveUtil.SaveAll(); // 一次性写入

// 重要数据，必须立即保存
_saveUtil.Save("critical", importantData);
```

---

### Q3: 游戏退出时如何确保数据已保存？

在 `OnApplicationQuit` 中调用 `SaveAll()`：

```csharp
private void OnApplicationQuit()
{
    _saveUtil?.SaveAll();
}
```

---

### Q4: Unity JsonUtility 不支持某些类型怎么办？

使用包装类替代（Dictionary、HashSet 等）：

```csharp
[Serializable]
public class DictWrapper
{
    public List<string> keys = new();
    public List<int> values = new();
}
```

---

### Q5: 如何处理版本兼容性？

添加版本号并在加载时进行迁移：

```csharp
[Serializable]
public class GameSave
{
    public int SaveVersion = 2;
}

public GameSave LoadFromSlot(int slot)
{
    var save = _saveUtil.Load($"slot_{slot}", new GameSave());
    if (save.SaveVersion < 2)
        MigrateFromV1ToV2(save);
    return save;
}
```

---

### Q6: 如何优化大文件加载速度？

使用异步加载和元数据分离：

```csharp
public async Task<GameSave> LoadFromSlotAsync(int slot)
{
    return await Task.Run(() => LoadFromSlot(slot));
}
```

---

### Q7: SaveUtil 是否线程安全？

不是，应该在主线程操作。使用 MainThreadDispatcher 在后台线程调用：

```csharp
UnityMainThreadDispatcher.Instance().Enqueue(() => 
{
    _saveUtil.Save(key, data);
});
```

---

### Q8: 如何删除所有存档？

```csharp
public void DeleteAllSaves()
{
    for (int i = 0; i < MAX_SLOTS; i++)
    {
        if (_saveUtil.Exists($"slot_{i}"))
            _saveUtil.Delete($"slot_{i}");
    }
}
```

## 9. 性能优化建议

### 📊 IO 性能优化

#### 1. 批量操作 vs 单个操作

```csharp
// ❌ 不推荐：多次单独写入（100次IO）
for (int i = 0; i < 100; i++)
    _saveUtil.Save($"item_{i}", items[i]);

// ✅ 推荐：批量写入（1次IO）
for (int i = 0; i < 100; i++)
    _saveUtil.SaveDelayed($"item_{i}", items[i]);
_saveUtil.SaveAll();
```

#### 2. 定时自动保存

```csharp
private float _autoSaveInterval = 60f;
private float _timer = 0f;

private void Update()
{
    _timer += Time.deltaTime;
    if (_timer >= _autoSaveInterval)
    {
        _saveUtil.SaveAll();
        _timer = 0f;
    }
}
```

---

### 🧠 内存优化

#### 1. 只保存必要数据

```csharp
// ❌ 保存整个游戏状态
_saveUtil.Save("game", entireGameState);

// ✅ 只保存变更部分
var delta = new { PlayerLevel = player.Level, Score = player.Score };
_saveUtil.Save("game_delta", delta);
```

#### 2. 异步加载大文件

```csharp
public IEnumerator LoadLargeDataCoroutine(string key, System.Action<GameState> onLoaded)
{
    GameState loadedData = null;
    var task = Task.Run(() => loadedData = _saveUtil.Load(key, new GameState()));
    while (!task.IsCompleted) yield return null;
    onLoaded?.Invoke(loadedData);
}
```

---

### 🔐 安全性优化

#### 1. 加密敏感数据

```csharp
.UseEncryption(new AesCrypto(GetSecureKey()))

private string GetSecureKey() => PlayerPrefs.GetString("encryption_key", "default");
```

#### 2. Editor 和真机使用不同配置

```csharp
#if UNITY_EDITOR
    .UseEncryption(null) // Editor 不加密
#else
    .UseEncryption(new AesCrypto(...)) // 真机加密
#endif
```

---

### 📈 性能基准

| 操作 | 预期耗时 | 优化目标 |
|------|---------|----------|
| 保存 1KB 数据 | < 1ms | < 5ms |
| 批量保存 100 项 | < 10ms | < 50ms |
| 加载完整存档 | < 100ms | < 500ms（异步） |
| 加密/解密 | < 5ms | < 20ms |
