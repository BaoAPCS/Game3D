using UnityEngine;
using UnityEngine.SceneManagement;

namespace DormitoryMystery.Chapter2
{
    [DisallowMultipleComponent]
    public sealed class Chapter2SaveManager : MonoBehaviour
    {
        public static Chapter2SaveManager Instance { get; private set; }

        [SerializeField] private bool autoLoadOnAwake = true;

        private IChapter2SaveService saveService;
        private Chapter2SaveData currentData;

        public Chapter2SaveData CurrentData =>
            currentData ??= Chapter2SaveData.CreateDefault();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError(
                    $"[Chapter2SaveManager] Phát hiện instance trùng trên '{gameObject.name}'. Component trùng sẽ bị disable.",
                    this);
                enabled = false;
                return;
            }

            Instance = this;
            saveService = new JsonChapter2SaveService();
            if (autoLoadOnAwake)
            {
                LoadChapter2();
            }
            else
            {
                currentData = Chapter2SaveData.CreateDefault();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static Chapter2SaveManager EnsureForScene(Scene scene)
        {
            if (Instance != null)
            {
                return Instance;
            }

            Chapter2SaveManager[] managers =
                Object.FindObjectsByType<Chapter2SaveManager>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < managers.Length; i++)
            {
                Chapter2SaveManager manager = managers[i];
                if (manager != null &&
                    manager.gameObject.scene == scene)
                {
                    return manager;
                }
            }

            GameObject managerObject = new GameObject(
                "Chapter2SaveManager");
            SceneManager.MoveGameObjectToScene(managerObject, scene);
            return managerObject.AddComponent<Chapter2SaveManager>();
        }

        public void LoadChapter2()
        {
            EnsureSaveService();
            currentData = saveService.Load();
            currentData.EnsureValidDefaults();
        }

        public void SaveChapter2()
        {
            EnsureSaveService();
            CurrentData.EnsureValidDefaults();
            saveService.Save(CurrentData);
        }

        public void SaveMission01Progress(
            bool crowbarCollected,
            bool toiletPried,
            bool serviceCardCollected)
        {
            CurrentData.Mission01CrowbarCollected =
                crowbarCollected;
            CurrentData.Mission01ToiletPried = toiletPried;
            CurrentData.Mission01ServiceCardCollected =
                serviceCardCollected;
            SaveChapter2();
        }

        public void ResetMission01()
        {
            SaveMission01Progress(false, false, false);
        }

        private void EnsureSaveService()
        {
            saveService ??= new JsonChapter2SaveService();
        }
    }
}
