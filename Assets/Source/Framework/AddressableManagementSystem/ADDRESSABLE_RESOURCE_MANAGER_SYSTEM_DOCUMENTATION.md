```md
# Unity Addressables Resource Management System

This guide explains how to set up and use the **Addressable Resource Management System** in Unity, leveraging the provided scripts:

- **AddressableResourceManager**  
- **AddressableResourceManagerExtensions**  
- **AddressableObjectPool**  
- **AddressableLocalizationManager**  
- **AddressableHelper**  

## Table of Contents

1. [Overview](#overview)  
2. [Installation and Setup](#installation-and-setup)  
3. [Initializing the System](#initializing-the-system)  
4. [Loading Assets](#loading-assets)  
   - [LoadAssetAsync](#loadassetasync)  
   - [Convenience Extension Methods](#convenience-extension-methods)  
5. [Scene Management](#scene-management)  
6. [Memory Management](#memory-management)  
7. [Releasing and Unloading Assets](#releasing-and-unloading-assets)  
8. [Preloading Assets](#preloading-assets)  
9. [Object Pooling](#object-pooling)  
   - [AddressableObjectPool](#addressableobjectpool)  
   - [Creating and Using Pools](#creating-and-using-pools)  
   - [Retrieving and Releasing Instances](#retrieving-and-releasing-instances)  
10. [Localization](#localization)  
   - [Setting the Language](#setting-the-language)  
   - [Loading Localized Assets](#loading-localized-assets)  
   - [Getting Localized Text](#getting-localized-text)  
11. [Debugging and Monitoring](#debugging-and-monitoring)  
12. [Common Workflow Examples](#common-workflow-examples)  
13. [FAQ / Troubleshooting](#faq--troubleshooting)  

---

## 1. Overview

This system wraps UnityÅfs [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) functionality with a convenient, extensible API. It includes:

- **AddressableResourceManager**  
  A singleton service class that handles initialization, asset loading, unloading, remote updates, memory checks, and more.
  
- **AddressableResourceManagerExtensions**  
  Additional helper extension methods for **AddressableResourceManager** (e.g. loading and instantiating prefabs, loading batched assets, etc.).

- **AddressableObjectPool**  
  A pooling system built on top of Addressables, letting you maintain reusable instances of prefabs to reduce load times and memory churn.

- **AddressableLocalizationManager**  
  A localization helper that automatically picks the right version of an asset or text file based on the current language.

- **AddressableHelper**  
  Static utility methods (e.g. validating if a key exists, getting dependencies, retrieving all labels/keys, etc.).

These pieces can be used independently or together to create a robust resource management pipeline.

---

## 2. Installation and Setup

1. **Import Addressables**  
   In Unity, open the Package Manager and import the **Addressables** package.  
   (Tested with Unity 2019+ and the official Addressables package.)

2. **Copy Scripts**  
   Add the five scripts below into your Unity project, ideally under a folder named `Scripts/Framework/AddressableManagementSystem`:
   - `AddressableResourceManager.cs`
   - `AddressableResourceManagerExtensions.cs`
   - `AddressableObjectPool.cs`
   - `AddressableLocalizationManager.cs`
   - `AddressableHelper.cs`

3. **Configure Addressable Assets**  
   - Mark your assets (prefabs, textures, etc.) as Addressables in their Inspector.
   - Group them in Addressable Groups, using labels as needed.

---

## 3. Initializing the System

- **AddressableResourceManager** and **AddressableLocalizationManager** are singletons that automatically create themselves when first accessed.  
- **AddressableObjectPool** also has a similar pattern.  

In most workflows, you do not need to manually create these managers. However, you can force initialization if needed:

```csharp
// Force initialization early in your game (optional)
await AddressableResourceManager.Instance.Initialize();

// Likewise for the Object Pool
await AddressableObjectPool.Instance.Initialize();

// Similarly for Localization Manager
// ...though it usually initializes automatically on Awake.
```

The system will otherwise auto-initialize itself when you first attempt to load or pool an asset.

---

## 4. Loading Assets

### 4.1 LoadAssetAsync

**`AddressableResourceManager`** provides `LoadAssetAsync<T>`:

```csharp
AsyncOperationHandle<T> handle = AddressableResourceManager.Instance.LoadAssetAsync<T>(
    "MyAddressableKey",
    LoadPriority.Normal,
    (result) => {
        // This callback runs when loading completes
        Debug.Log("Asset loaded!");
    }
);
```

- **Parameters**  
  - `key`: String key for the asset (exactly as labeled in the Addressables system).  
  - `priority`: (Optional) `LoadPriority` enum (Low, Normal, High) to influence the loading queue.  
  - `callback`: (Optional) Called once the load completes successfully.  

- **Return Value**  
  Returns an `AsyncOperationHandle<T>`. You can await the `.Task` if you want to do async/await flow:
  
  ```csharp
  var handle = manager.LoadAssetAsync<MyScriptableObject>("ScriptableKey");
  await handle.Task;
  if (handle.Status == AsyncOperationStatus.Succeeded) {
      MyScriptableObject loadedObj = handle.Result;
  }
  ```

### 4.2 Convenience Extension Methods

**`AddressableResourceManagerExtensions`** defines extra methods:

- **`LoadAndInstantiatePrefabAsync`**:  
  Loads a prefab by key, then instantiates it in the scene.  

  ```csharp
  GameObject instance = await manager.LoadAndInstantiatePrefabAsync("MyPrefabKey", parent: someTransform);
  ```

- **`LoadTextureToRawImageAsync`**:  
  Loads a `Texture2D` by key and applies it to a `UI.RawImage`.

- **`LoadSpriteToImageAsync`**:  
  Loads a `Sprite` by key and applies it to a `UI.Image`.

- **`LoadAssetBatchAsync<T>`**:  
  Loads multiple assets simultaneously, with an optional progress callback.

- **`LoadAssetsWithLabelAsync<T>`**:  
  Loads all assets (of type T) that share a given label.

---

## 5. Scene Management

**LoadSceneAsync** in **AddressableResourceManager**:

```csharp
AsyncOperationHandle<SceneInstance> loadOp = AddressableResourceManager.Instance.LoadSceneAsync(
    "SceneKey", 
    activateOnLoad: true
);
```

- Loads the scene additively. If `activateOnLoad` is `false`, you can activate later with `SceneInstance.ActivateAsync()`.

**UnloadSceneAsync**:

```csharp
AddressableResourceManager.Instance.UnloadSceneAsync(sceneInstance);
```

- Unloads a scene previously loaded with `LoadSceneAsync`.

---

## 6. Memory Management

**AddressableResourceManager** includes logic for:

- **Memory Budgets** (`_memoryBudgetMB`)  
  If you exceed the budget or assets go too long without being accessed, the system can unload them.

- **Asset Timeout** (`_assetTimeoutSeconds`)  
  This determines how long an asset can remain unused before it's automatically unloaded.

- **CheckMemoryUsage**  
  Periodically checks memory usage, compares it to `_memoryBudgetMB`, and unloads unused assets if necessary.

Configure these in the Inspector on the `[AddressableResourceManager]` GameObject or via script:

```csharp
AddressableResourceManager.Instance.SetMemoryBudget(1024f); // e.g. 1GB
```

---

## 7. Releasing and Unloading Assets

The system tracks reference counts. Each time you load an asset, the reference count goes up; when you release it, it goes down. When an assetÅfs reference count hits zero, itÅfs flagged for potential unloading.

- **ReleaseAsset**:
  ```csharp
  AddressableResourceManager.Instance.ReleaseAsset("MyAddressableKey");
  ```

- **ForceUnloadAsset**:  
  Forcibly unloads the asset, ignoring reference counts.

**Important**: If you loaded an asset multiple times, call `ReleaseAsset` once for each load.

---

## 8. Preloading Assets

If you want to load assets in advance (e.g., during a loading screen), use **`PreloadAssetsAsync<T>`**:

```csharp
var keysToPreload = new List<string> { "EnemyPrefab", "UILayout", "BackgroundMusic" };
await AddressableResourceManager.Instance.PreloadAssetsAsync<GameObject>(keysToPreload, LoadPriority.Low);
```

- This loads the assets in the background, raising their reference counts. They remain available until explicitly released or automatically unloaded.

---

## 9. Object Pooling

### 9.1 AddressableObjectPool

**`AddressableObjectPool`** is a singleton that manages reusable GameObjects (prefabs). It creates pools keyed by their Addressable `PrefabKey`.

### 9.2 Creating and Using Pools

In the `AddressableObjectPool` Inspector, you can predefine:

- **Default Initial Pool Size**  
- **Default Max Pool Size**  
- **_preWarmData_**: a list of `(PrefabKey, InitialSize, MaxSize)` for pre-warming.

When the game starts, if you call:

```csharp
await AddressableObjectPool.Instance.Initialize();
```

Åcit will automatically create pools for any pre-warm entries. Alternatively, the pool system will lazy-initialize the first time you call `GetAsync` or `PreWarmPool`.

### 9.3 Retrieving and Releasing Instances

- **GetAsync**:  
  Retrieves (or instantiates) a GameObject from the pool.

  ```csharp
  GameObject obj = await AddressableObjectPool.Instance.GetAsync(
      "EnemyPrefabKey",
      position,
      rotation,
      parentTransform
  );
  ```

- **Release**:  
  Returns it to the pool.

  ```csharp
  AddressableObjectPool.Instance.Release("EnemyPrefabKey", obj);
  ```

When an object is grabbed from the pool, it calls any `IPoolable.OnGetFromPool()` methods on its components. When released, it calls `IPoolable.OnReturnToPool()` before deactivating.

---

## 10. Localization

### 10.1 Setting the Language

**`AddressableLocalizationManager`** determines which language to load. By default, it tries:

1. A saved language in `PlayerPrefs`  
2. The userÅfs system language  
3. Falls back to `_defaultLanguage` (often ÅgenÅh)

You can override by calling:

```csharp
await AddressableLocalizationManager.Instance.SetLanguageAsync("ja");
```

This updates `_currentLanguage` and optionally preloads common localized assets (if `_preloadCommonAssets` is true).

### 10.2 Loading Localized Assets

Use:

```csharp
T asset = await AddressableLocalizationManager.Instance.LoadLocalizedAssetAsync<T>("BaseKey");
```

Behind the scenes, the manager tries to load `BaseKey_CurrentLanguage`. If that fails, it can optionally fall back to `BaseKey` if `fallbackToBase` is `true`.

### 10.3 Getting Localized Text

```csharp
string text = await AddressableLocalizationManager.Instance.GetLocalizedTextAsync("Menu_Play");
```

- By default, tries `Menu_Play_[language]` or loads a dictionary file called `LocalizationDictionary` containing multiple entries.

---

## 11. Debugging and Monitoring

- **Verbose Logging**  
  You can enable `_verboseLogging` on the `AddressableResourceManager` to log every load/unload.

- **GetMemoryUsage**  
  Returns a `MemoryUsageInfo` struct with `TotalMemoryUsageMB`, `MemoryBudgetMB`, etc.  
  ```csharp
  MemoryUsageInfo info = AddressableResourceManager.Instance.GetMemoryUsage();
  Debug.Log($"Loaded assets: {info.LoadedAssetsCount}");
  ```

- **GetAssetLoadingStats**  
  For a specific key, you can see `ReferenceCount`, `LastLoadTime`, etc.

- **AddressableHelper**  
  Additional debugging utilities:
  - `ValidateKeyExists(key)`
  - `GetAssetSize(key)`
  - `IsAssetDownloaded(key)`
  - `GetAllStringKeys()`
  - `GetAllLabels()`
  Åcand more.

---

## 12. Common Workflow Examples

### 12.1 Loading a Prefab and Instantiating

```csharp
var manager = AddressableResourceManager.Instance;
GameObject instance = await manager.LoadAndInstantiatePrefabAsync("SomePrefabKey");
```

### 12.2 Loading a Sprite and Applying to a UI.Image

```csharp
await manager.LoadSpriteToImageAsync("MySpriteKey", myImageComponent);
```

### 12.3 Switching Language

```csharp
await AddressableLocalizationManager.Instance.SetLanguageAsync("fr");
string localizedString = await AddressableLocalizationManager.Instance.GetLocalizedTextAsync("UI_PlayButton");
Debug.Log(localizedString);
```

### 12.4 Object Pool Usage

```csharp
// Pre-warm a pool
await AddressableObjectPool.Instance.PreWarmPool("EnemyPrefabKey", initialSize: 5, maxSize: 20);

// Get an instance
GameObject enemy = await AddressableObjectPool.Instance.GetAsync("EnemyPrefabKey", spawnPos, Quaternion.identity);

// Return it to the pool later
AddressableObjectPool.Instance.Release("EnemyPrefabKey", enemy);
```

---

## 13. FAQ / Troubleshooting

1. **My asset isnÅft loading or says Ågkey not foundÅh!**  
   - Check that your asset is **marked as Addressable** and that the key youÅfre using matches exactly (case-sensitive).

2. **I get a ÅgCannot load asset before initializationÅh warning.**  
   - You can ignore it if auto-initialization is allowed. Otherwise, explicitly call `await AddressableResourceManager.Instance.Initialize()` in a startup script.

3. **Why is my memory not going down after releasing assets?**  
   - The system may still hold references. Confirm you call `ReleaseAsset` for each time you loaded the asset. Also, UnityÅfs garbage collection or internal memory usage can delay the visible memory reduction.

4. **Preloading is slow or stalls.**  
   - Check your concurrency settings: `_maxConcurrentOperations` in the `AddressableResourceManager`. You might need to reduce or increase it depending on your platform and network constraints.

5. **How do I clear the local cache for testing?**  
   - `await AddressableResourceManager.Instance.ClearCacheAsync()` clears the cached data. This can be used to simulate a fresh install or for debugging content updates.

---

## Conclusion

This system simplifies many aspects of UnityÅfs Addressables:

- Straightforward **asset loading** with reference counting.  
- Easy **preloading** and queued loading with priorities.  
- Built-in **scene management** for additive loading.  
- **Memory monitoring** and automatic unloading.  
- **Localization** support, automatically picking the correct version of assets.  
- **Object pooling** to reduce instantiate/destroy overhead.

Feel free to combine or extend these scripts according to your projectÅfs needs. Always ensure your Addressable Groups and labels are well-structured for the best performance.

**Happy developing!**
```