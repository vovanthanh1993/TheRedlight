using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component để hiển thị và điều khiển skill tăng tốc
/// </summary>
public class SpeedSkillButton : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Button để kích hoạt skill")]
    [SerializeField] private Button skillButton;

    [Tooltip("Image để hiển thị cooldown fill (sẽ fill từ 0-1)")]
    [SerializeField] private Image cooldownFillImage;

    [Tooltip("Text để hiển thị thời gian cooldown còn lại")]
    [SerializeField] private TextMeshProUGUI cooldownText;

    [Header("Settings")]
    [Tooltip("Hiển thị thời gian cooldown dưới dạng số")]
    [SerializeField] private bool showCooldownTimer = true;

    private bool isInitialized = false;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        // Tự động tìm button nếu chưa được assign
        if (skillButton == null)
        {
            skillButton = GetComponent<Button>();
        }

        // Setup button click event
        if (skillButton != null)
        {
            skillButton.onClick.RemoveAllListeners();
            skillButton.onClick.AddListener(OnSkillButtonClicked);
        }

        // Subscribe to PlayerController events
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnSpeedSkillCooldownChanged += UpdateCooldownUI;
            PlayerController.Instance.OnSpeedSkillStateChanged += UpdateSkillStateUI;
            
            // Cập nhật UI ban đầu
            UpdateCooldownUI(PlayerController.Instance.GetSpeedSkillCooldownProgress());
            UpdateSkillStateUI(PlayerController.Instance.IsSpeedSkillActive());
        }
        else
        {
            Debug.LogWarning("SpeedSkillButton: PlayerController.Instance không tồn tại! Đảm bảo PlayerController đã được khởi tạo.");
        }

        isInitialized = true;
    }

    private void OnEnable()
    {
        if (isInitialized && PlayerController.Instance != null)
        {
            PlayerController.Instance.OnSpeedSkillCooldownChanged += UpdateCooldownUI;
            PlayerController.Instance.OnSpeedSkillStateChanged += UpdateSkillStateUI;
        }
    }

    private void OnDisable()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnSpeedSkillCooldownChanged -= UpdateCooldownUI;
            PlayerController.Instance.OnSpeedSkillStateChanged -= UpdateSkillStateUI;
        }
    }

    private void Update()
    {
        if (PlayerController.Instance == null)
            return;

        // Cập nhật fill image theo cooldown progress
        if (cooldownFillImage != null)
        {
            float cooldownProgress = PlayerController.Instance.GetSpeedSkillCooldownProgress();
            
            if (cooldownProgress > 0f)
            {
                // Khi đang cooldown: fillAmount giảm từ 1 về 0
                cooldownFillImage.fillAmount = 1f - cooldownProgress;
            }
            else
            {
                // Khi cooldown xong (sẵn sàng): fillAmount = 0
                cooldownFillImage.fillAmount = 0f;
            }
        }

        // Cập nhật cooldown timer text mỗi frame
        if (showCooldownTimer && cooldownText != null)
        {
            float remainingTime = PlayerController.Instance.GetSpeedSkillCooldownRemaining();
            if (remainingTime > 0f)
            {
                cooldownText.text = Mathf.CeilToInt(remainingTime).ToString();
            }
            else
            {
                cooldownText.text = "";
            }
        }
    }

    /// <summary>
    /// Xử lý khi click button skill
    /// </summary>
    private void OnSkillButtonClicked()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.TryActivateSpeedSkill();
        }
        else
        {
            Debug.LogWarning("SpeedSkillButton: PlayerController.Instance không tồn tại!");
        }
    }

    /// <summary>
    /// Cập nhật UI cooldown (được gọi từ SpeedSkill event)
    /// </summary>
    private void UpdateCooldownUI(float cooldownProgress)
    {
        // Fill image được cập nhật trong Update() mỗi frame
        // Chỉ cập nhật button interactable ở đây
        if (skillButton != null)
        {
            skillButton.interactable = (cooldownProgress <= 0f);
        }
    }

    /// <summary>
    /// Cập nhật UI trạng thái skill (được gọi từ SpeedSkill event)
    /// </summary>
    private void UpdateSkillStateUI(bool isActive)
    {
        // Fill image được cập nhật trong Update() mỗi frame
        // Không cần làm gì ở đây
    }
}
