using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
// AddressableResourceManager �N���X�ւ̃A�N�Z�X���K�v
using AddressableManagementSystem;

namespace AddressableManagementSystem
{
    /// <summary>
    /// Extensions for the AddressableResourceManager to provide convenience methods for common operations.
    /// </summary>
    public static class AddressableResourceManagerExtensions
    {
        /// <summary>
        /// Loads a GameObject and instantiates it in the scene.
        /// </summary>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="key">Addressable key for the prefab</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="worldPositionStays">Whether to maintain world position when setting parent</param>
        /// <param name="priority">Loading priority</param>
        /// <param name="callback">Optional callback when instantiation completes</param>
        /// <returns>Task that completes with the instantiated GameObject</returns>
        public static async Task<GameObject> LoadAndInstantiatePrefabAsync(
            this AddressableResourceManager manager,
            string key,
            Transform parent = null,
            bool worldPositionStays = true,
            // ���C��: ���S�C�������g�p
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal,
            Action<GameObject> callback = null)
        {
            try
            {
                // Load the prefab
                // ���C��: ���S�C�������g�p
                var prefabHandle = manager.LoadAssetAsync<GameObject>(key, priority);
                await prefabHandle.Task;

                if (prefabHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load prefab: {key} - {prefabHandle.OperationException}");
                    return null;
                }

                // Instantiate the prefab
                GameObject prefab = prefabHandle.Result;
                GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays);

                // Set a name that doesn't have "(Clone)" appended
                instance.name = prefab.name;

                // Invoke callback if provided
                callback?.Invoke(instance);

                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error instantiating prefab {key}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a texture and applies it to a UI RawImage component.
        /// </summary>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="key">Addressable key for the texture</param>
        /// <param name="rawImage">RawImage component to apply the texture to</param>
        /// <param name="priority">Loading priority</param>
        /// <returns>Task that completes when the texture is applied</returns>
        public static async Task LoadTextureToRawImageAsync(
            this AddressableResourceManager manager,
            string key,
            UnityEngine.UI.RawImage rawImage,
            // ���C��: ���S�C�������g�p
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal)
        {
            if (rawImage == null)
            {
                Debug.LogError("RawImage component cannot be null");
                return;
            }

            try
            {
                // ���C��: ���S�C�������g�p
                var textureHandle = manager.LoadAssetAsync<Texture2D>(key, priority);
                await textureHandle.Task;

                if (textureHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    // Apply texture to the RawImage
                    rawImage.texture = textureHandle.Result;
                }
                else
                {
                    Debug.LogError($"Failed to load texture: {key} - {textureHandle.OperationException}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading texture {key} to RawImage: {e.Message}");
            }
        }

        /// <summary>
        /// Loads a sprite and applies it to a UI Image component.
        /// </summary>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="key">Addressable key for the sprite</param>
        /// <param name="image">Image component to apply the sprite to</param>
        /// <param name="priority">Loading priority</param>
        /// <returns>Task that completes when the sprite is applied</returns>
        public static async Task LoadSpriteToImageAsync(
            this AddressableResourceManager manager,
            string key,
            UnityEngine.UI.Image image,
            // ���C��: ���S�C�������g�p
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal)
        {
            if (image == null)
            {
                Debug.LogError("Image component cannot be null");
                return;
            }

            try
            {
                // ���C��: ���S�C�������g�p
                var spriteHandle = manager.LoadAssetAsync<Sprite>(key, priority);
                await spriteHandle.Task;

                if (spriteHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    // Apply sprite to the Image
                    image.sprite = spriteHandle.Result;
                }
                else
                {
                    Debug.LogError($"Failed to load sprite: {key} - {spriteHandle.OperationException}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading sprite {key} to Image: {e.Message}");
            }
        }

        /// <summary>
        /// Loads multiple assets of the same type at once.
        /// </summary>
        /// <typeparam name="T">Type of assets to load</typeparam>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="keys">Collection of asset keys to load</param>
        /// <param name="priority">Loading priority</param>
        /// <param name="progressCallback">Optional callback to report batch loading progress</param>
        /// <returns>Task that completes with a dictionary of loaded assets</returns>
        public static async Task<Dictionary<string, T>> LoadAssetBatchAsync<T>(
            this AddressableResourceManager manager,
            IEnumerable<string> keys,
            // ���C��: ���S�C�������g�p
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal,
            Action<float> progressCallback = null)
        {
            Dictionary<string, T> results = new Dictionary<string, T>();
            // ���C��: AsyncOperationHandle<T> �𒼐� Task �Ƃ��Ĉ����̂ł͂Ȃ��AHandle ��ێ�����
            List<AsyncOperationHandle<T>> handles = new List<AsyncOperationHandle<T>>();
            List<string> keyList = new List<string>(keys);

            try
            {
                // Start all load operations
                foreach (var key in keyList)
                {
                    // ���C��: ���S�C�������g�p���AHandle �����X�g�ɒǉ�
                    var handle = manager.LoadAssetAsync<T>(key, priority);
                    handles.Add(handle);
                }

                // Wait for all tasks to complete or report progress
                int completedCount = 0;
                while (completedCount < handles.Count)
                {
                    completedCount = 0;
                    float totalProgress = 0f;
                    foreach (var handle in handles) // ���C��: handles ���g�p
                    {
                        totalProgress += handle.PercentComplete;
                        if (handle.IsDone) // ���C��: IsDone ���g�p
                        {
                            completedCount++;
                        }
                    }

                    float progress = handles.Count > 0 ? totalProgress / handles.Count : 1f; // ���C��: �i���v�Z���C��
                    progressCallback?.Invoke(progress);

                    if (completedCount < handles.Count)
                    {
                        await Task.Delay(50); // Wait a bit before checking again
                    }
                }

                // Collect results
                for (int i = 0; i < keyList.Count; i++)
                {
                    var handle = handles[i]; // ���C��: handles ���g�p
                    if (handle.Status == AsyncOperationStatus.Succeeded) // ���C��: handle.Status ���g�p
                    {
                        results[keyList[i]] = handle.Result; // ���C��: handle.Result ���g�p
                    }
                    else
                    {
                        Debug.LogError($"Failed to load asset: {keyList[i]} - {handle.OperationException}"); // ���C��: handle.OperationException ���g�p
                    }
                }

                return results;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading asset batch: {e.Message}");
                // ���ǉ�: ���s�����ꍇ�ł������I�Ȍ��ʂ�Ԃ����A���Ԃ����������B�����ł͕����I�Ȍ��ʂ�Ԃ��B
                // ���ǉ�: �n���h�����������K�v�����邩���� (�ʏ�A������Ɏ����������邱�Ƃ��������A�G���[���͒���)
                foreach (var handle in handles)
                {
                    if (handle.IsValid() && !handle.IsDone)
                    {
                        // �G���[�������ɂ܂��������Ă��Ȃ��n���h��������Ή�������݂�iAddressables.Release�̎d�l�ɂ��j
                        // Addressables.Release(handle); // �K�v�ɉ����ĉ��
                    }
                }
                return results;
            }
            // ���C��: finally �u���b�N�͕s�v�ɂȂ������ߍ폜
        }


        /// <summary>
        /// Loads all assets with a specific label.
        /// </summary>
        /// <typeparam name="T">Type of assets to load</typeparam>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="label">Addressable label to load assets for</param>
        /// <param name="priority">Loading priority</param>
        /// <param name="progressCallback">Optional callback to report batch loading progress</param>
        /// <returns>Task that completes with a list of loaded assets</returns>
        public static async Task<List<T>> LoadAssetsWithLabelAsync<T>(
            this AddressableResourceManager manager,
            string label,
            // ���C��: ���S�C�������g�p
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal,
            Action<float> progressCallback = null)
        {
            List<T> results = new List<T>();

            try
            {
                // Get all assets with the specified label
                var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
                await locationsHandle.Task;

                if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load resource locations for label: {label} - {locationsHandle.OperationException}");
                    Addressables.Release(locationsHandle); // ���ǉ�: �n���h�������
                    return results;
                }

                var locations = locationsHandle.Result;
                if (locations.Count == 0)
                {
                    Debug.LogWarning($"No assets found with label: {label}");
                    Addressables.Release(locationsHandle); // ���ǉ�: �n���h�������
                    return results;
                }

                // Create a list of keys from the locations
                // ���C��: location.PrimaryKey �𒼐ڎg���̂ł͂Ȃ��A�K�؂ȃL�[���o���s��
                // Addressables �̃o�[�W������ݒ�ɂ���ẮAPrimaryKey ������LoadAssetAsync���ĂׂȂ��ꍇ������
                // LoadAssetsAsync ���g���������x���w��ɂ͓K���Ă���
                // List<string> keys = new List<string>();
                // foreach (var location in locations)
                // {
                //     // PrimaryKey �� string �ł��邱�Ƃ����҂���̂ł͂Ȃ��A
                //     // Addressables.LoadAssetAsync ���󂯕t����`���ł���K�v������
                //     // �ʏ�AIResourceLocation ���̂��̂�Alocation.PrimaryKey ���L�[�Ƃ��Ďg���邱�Ƃ�����
                //     if (location.PrimaryKey != null) // �L�[�����݂��邩�m�F
                //     {
                //         keys.Add(location.PrimaryKey); // �����ł� PrimaryKey ���g���Ɖ���
                //     }
                // }
                // Addressables.Release(locationsHandle); // ���ǉ�: locations ���g���I�������n���h�������

                // �����P: ���x�����璼�ڃ��[�h���� Addressables.LoadAssetsAsync ���g�����������I
                var loadAssetsHandle = Addressables.LoadAssetsAsync<T>(label, asset => {
                    // �X�̃��[�h�������̃R�[���o�b�N (�I�v�V����)
                    if (asset != null)
                    {
                        results.Add(asset);
                    }
                });

                // �i���Ď�
                while (!loadAssetsHandle.IsDone)
                {
                    progressCallback?.Invoke(loadAssetsHandle.PercentComplete);
                    await Task.Yield(); // ���̃t���[���܂őҋ@
                }

                if (loadAssetsHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load assets with label {label}: {loadAssetsHandle.OperationException}");
                }
                // else // �������̓R�[���o�b�N�� results �ɒǉ�����Ă���͂�
                // {
                //    // loadAssetsHandle.Result �ɂ̓��[�h���ꂽ�A�Z�b�g�̃��X�g���܂܂�邪�A
                //    // �R�[���o�b�N�Œǉ����Ă���ꍇ�͕s�v�ȏꍇ������
                // }

                Addressables.Release(loadAssetsHandle); // ���ǉ�: �n���h�������
                Addressables.Release(locationsHandle); // ���ǉ�: �n���h������� (LoadAssetsAsync���g���ꍇ�ł�locationsHandle�͉������)

                return results; // �R�[���o�b�N�Œǉ����ꂽ���ʂ�Ԃ�
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading assets with label {label}: {e.Message}");
                // ���ǉ�: ��O���������n���h���̉�������݂� (try-finally����)
                return results;
            }
        }

        /// <summary>
        /// Releases multiple assets at once.
        /// </summary>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="keys">Collection of asset keys to release</param>
        public static void ReleaseAssets(
            this AddressableResourceManager manager,
            IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                manager.ReleaseAsset(key);
            }
        }
    }
}