using UnityEngine;

/// <summary>
/// Tự động load level prefab khi scene được load
/// </summary>
public class LevelLoader : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tự động load level khi Start() được gọi")]
    [SerializeField] private bool autoLoadOnStart = true;
    
    private void Start()
    {
        if (autoLoadOnStart)
        {
            LoadLevelFromPlayerPrefs();
        }
    }
    
    /// <summary>
    /// Load level từ PlayerPrefs "CurrentLevel"
    /// </summary>
    public void LoadLevelFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            int levelNumber = PlayerPrefs.GetInt("CurrentLevel");
            LoadLevel(levelNumber);
        }
        else
        {
            Debug.LogWarning("LevelLoader: Không tìm thấy 'CurrentLevel' trong PlayerPrefs! Sử dụng level 1 mặc định.");
            LoadLevel(1);
        }
    }
    
    /// <summary>
    /// Load level cụ thể
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadLevel(levelNumber);
        }
        else
        {
            Debug.LogError("LevelLoader: LevelManager.Instance không tồn tại! Hãy đảm bảo có LevelManager trong scene.");
        }
    }
}
