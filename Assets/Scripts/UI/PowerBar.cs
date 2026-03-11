using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý PowerBar UI - hiển thị progress khi nhặt EnergyItem
/// </summary>
public class PowerBar : MonoBehaviour
{
    public static PowerBar Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("Image Fill của PowerBar (sẽ được fill từ 0-1)")]
    [SerializeField] private Image fillImage;

    [Tooltip("RectTransform của Fill Area (khung tối đa). Nếu null sẽ tự lấy parent của Fill")]
    [SerializeField] private RectTransform fillAreaRect;
    
    [Tooltip("Text hiển thị % (ví dụ: 50%)")]
    [SerializeField] private TextMeshProUGUI textValue;
    
    [Tooltip("NoticeText hiển thị khi đạt 100% (ví dụ: 'Đã đủ năng lượng!')")]
    [SerializeField] private TextMeshProUGUI noticeText;
    
    [Header("Settings")]
    [Tooltip("Có tự động cập nhật trong Update() không")]
    [SerializeField] private bool autoUpdate = true;
    
    [Tooltip("Tốc độ fill animation (0 = tức thì, 1 = mượt mà)")]
    [SerializeField] private float fillAnimationSpeed = 5f;
    
    private float targetFillAmount = 0f;
    private int lastCollectedPoints = -1;
    private float maxFillWidth = 0f;
    private RectTransform fillRect;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    private void Start()
    {
        CacheRectsAndMaxWidth();

        // Khởi tạo UI
        UpdatePowerBar();
    }
    
    private void Update()
    {
        if (autoUpdate)
        {
            // Kiểm tra xem có thay đổi không
            if (LevelManager.Instance != null)
            {
                int currentCollected = LevelManager.Instance.GetCollectedEnergyPoints();
                
                if (currentCollected != lastCollectedPoints)
                {
                    lastCollectedPoints = currentCollected;
                    UpdatePowerBar();
                }
            }
            
            // Animate fill bar
            if (fillImage != null)
            {
                if (fillImage.type == Image.Type.Filled)
                {
                    // Animate fillAmount cho Filled type
                    if (Mathf.Abs(fillImage.fillAmount - targetFillAmount) > 0.001f)
                    {
                        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, Time.deltaTime * fillAnimationSpeed);
                    }
                }
                else if (fillRect != null && fillAreaRect != null && maxFillWidth > 0f)
                {
                    // Animate width cho Sliced/Simple type
                    float currentWidth = fillRect.rect.width;
                    float targetWidth = maxFillWidth * targetFillAmount;
                    
                    if (Mathf.Abs(currentWidth - targetWidth) > 0.1f)
                    {
                        float newWidth = Mathf.Lerp(currentWidth, targetWidth, Time.deltaTime * fillAnimationSpeed);
                        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
                    }
                }
            }
        }
    }

    private void CacheRectsAndMaxWidth()
    {
        if (fillImage == null)
            return;

        fillRect = fillImage.GetComponent<RectTransform>();

        if (fillAreaRect == null)
        {
            fillAreaRect = fillRect != null ? fillRect.parent as RectTransform : null;
        }

        // Force layout update để lấy rect.width chính xác
        Canvas.ForceUpdateCanvases();

        if (fillAreaRect != null)
        {
            maxFillWidth = fillAreaRect.rect.width;
        }
        else if (fillRect != null)
        {
            maxFillWidth = fillRect.rect.width;
        }

        // Nếu Fill là Sliced (không dùng fillAmount), chuẩn hóa anchor/pivot để resize theo chiều ngang từ trái sang phải
        if (fillRect != null && fillImage.type != Image.Type.Filled)
        {
            // Anchor trái, stretch dọc (để fill từ trái sang phải)
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);

            // Đặt sát mép trái của Fill Area
            fillRect.anchoredPosition = new Vector2(0f, 0f);
            
            // Set size delta để stretch dọc hoàn toàn
            fillRect.sizeDelta = new Vector2(0f, 0f);
        }
    }
    
    /// <summary>
    /// Cập nhật PowerBar dựa trên số điểm EnergyItem đã nhặt
    /// </summary>
    public void UpdatePowerBar()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogWarning("PowerBar: LevelManager.Instance không tồn tại!");
            return;
        }
        
        int collected = LevelManager.Instance.GetCollectedEnergyPoints();
        int required = LevelManager.Instance.GetRequiredEnergyPointsForCurrentLevel();
        
        // Tính % fill (0-1)
        float fillAmount = 0f;
        if (required > 0)
        {
            fillAmount = Mathf.Clamp01((float)collected / (float)required);
        }
        
        // Cập nhật target fill amount (sẽ được animate trong Update)
        targetFillAmount = fillAmount;
        
        // Update UI fill
        if (fillImage != null)
        {
            // Nếu Image Type = Filled thì dùng fillAmount native
            if (fillImage.type == Image.Type.Filled)
            {
                if (fillAnimationSpeed <= 0f)
                {
                    fillImage.fillAmount = fillAmount;
                }
                // còn lại sẽ animate trong Update()
            }
            else
            {
                // Nếu Image Type = Sliced/Simple thì resize width (giữ 9-slice đẹp)
                if (fillRect == null || fillAreaRect == null || maxFillWidth <= 0f)
                {
                    CacheRectsAndMaxWidth();
                }

                if (fillRect != null && fillAreaRect != null && maxFillWidth > 0f)
                {
                    float targetWidth = maxFillWidth * fillAmount;

                    if (fillAnimationSpeed <= 0f)
                    {
                        // Set width trực tiếp
                        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                    }
                    else
                    {
                        // Animate width
                        float currentWidth = fillRect.rect.width;
                        float newWidth = Mathf.Lerp(currentWidth, targetWidth, Time.deltaTime * fillAnimationSpeed);
                        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
                    }
                }
            }
        }
        
        // Cập nhật text % (hiển thị điểm số thay vì %)
        if (textValue != null)
        {
            int percentage = Mathf.RoundToInt(fillAmount * 100f);
            textValue.text = $"{percentage}%";
        }
        
        // Hiển thị/ẩn NoticeText khi đạt 100%
        if (noticeText != null)
        {
            // Hiển thị khi đạt 100%, ẩn khi chưa đủ
            noticeText.gameObject.SetActive(fillAmount >= 1.0f);
        }
        
        Debug.Log($"PowerBar: Updated - {collected}/{required} điểm ({fillAmount * 100f:F1}%)");
    }
    
    /// <summary>
    /// Reset PowerBar về 0 (khi bắt đầu level mới)
    /// </summary>
    public void ResetPowerBar()
    {
        targetFillAmount = 0f;
        lastCollectedPoints = -1;
        
        if (fillImage != null)
        {
            if (fillImage.type == Image.Type.Filled)
            {
                fillImage.fillAmount = 0f;
            }
            else
            {
                if (fillRect == null || fillAreaRect == null || maxFillWidth <= 0f)
                {
                    CacheRectsAndMaxWidth();
                }

                if (fillRect != null)
                {
                    fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
                }
            }
        }
        
        if (textValue != null)
        {
            textValue.text = "0%";
        }
        
        // Ẩn NoticeText khi reset
        if (noticeText != null)
        {
            noticeText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Được gọi khi EnergyItem được nhặt (có thể gọi từ LevelManager)
    /// </summary>
    public void OnEnergyItemCollected()
    {
        UpdatePowerBar();
    }
}
