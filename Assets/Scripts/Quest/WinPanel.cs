using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class WinPanel : MonoBehaviour
{
    [Header("Buttons")]
    public Button continueBtn;
    public Button returnHomeBtn;
    
    [Header("UI Elements")]
    public List<GameObject> starList = new List<GameObject>(); // List 3 star objects
    public TextMeshProUGUI rewardText;
    
    void Start() {
        if (continueBtn != null)
        {
            continueBtn.onClick.AddListener(OnContinueButtonClicked);
        }
        if (returnHomeBtn != null)
        {
            returnHomeBtn.onClick.AddListener(OnReturnHomeButtonClicked);
        }
    }

    void OnContinueButtonClicked()
    {
        // Lấy level hiện tại
        int currentLevel = 1;
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            currentLevel = PlayerPrefs.GetInt("CurrentLevel");
        }
        
        // Tính level tiếp theo
        int nextLevel = currentLevel + 1;
        
        // Kiểm tra xem level tiếp theo có tồn tại không
        int totalLevels = 50; // Default
        if (QuestDataManager.Instance != null)
        {
            totalLevels = QuestDataManager.Instance.GetQuestCount();
        }
        
        if (nextLevel > totalLevels)
        {
            // Đã hết level, quay về home
            Debug.Log($"Đã hoàn thành tất cả {totalLevels} level!");
            OnReturnHomeButtonClicked();
            return;
        }
        
        // Load level tiếp theo
        LoadNextLevel(nextLevel);
    }

    void OnReturnHomeButtonClicked()
    {
        GameCommonUtils.LoadScene("HomeScene");
        UIManager.Instance.ShowHomePanel(true);
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        AudioManager.Instance.PlaySelectSound();
    }
    
    /// <summary>
    /// Load level tiếp theo - load lại scene dựa trên level
    /// </summary>
    void LoadNextLevel(int level)
    {
        // Reset health trước khi load level tiếp theo
        if (HealthPanel.Instance != null)
        {
            HealthPanel.Instance.ResetHealth();
        }
        
        // Lưu level mới vào PlayerPrefs
        PlayerPrefs.SetInt("CurrentLevel", level);
        PlayerPrefs.Save();
        
        // Xác định scene dựa trên level (Level 1-5: GamePlay1, Level 6-10: GamePlay2, Level 11-15: GamePlay3, ...)
        string sceneName = LevelSceneHelper.GetSceneNameForLevel(level);
        
        // Load scene (LevelLoader sẽ tự động load level prefab khi scene được load)
        GameCommonUtils.LoadScene(sceneName);
        
        UIManager.Instance.ShowGamePlayPanel(true);
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        AudioManager.Instance.PlaySelectSound();
    }

    public void Init(int star, int reward)
    {
        // Hiển thị stars dựa trên số sao đạt được (1-3)
        UpdateStarsDisplay(star);
        
        if (rewardText != null)
        {
            rewardText.text = reward.ToString();
        }
    }

    private void UpdateStarsDisplay(int starCount)
    {
        // Đảm bảo có đủ 3 stars
        if (starList.Count < 3)
        {
            Debug.LogWarning("WinPanel: Cần 3 star objects trong starList!");
            return;
        }

        // Hiển thị stars: hiện star nếu index < số sao đạt được, ẩn nếu không
        for (int i = 0; i < 3; i++)
        {
            if (starList[i] != null)
            {
                starList[i].SetActive(i < starCount);
            }
        }
    }
}
