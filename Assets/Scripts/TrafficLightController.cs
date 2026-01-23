using UnityEngine;
using System.Collections;

/// <summary>
/// Controller quản lý trạng thái Green Light / Red Light
/// Khi Red Light, player không được di chuyển, nếu di chuyển sẽ chết
/// </summary>
public class TrafficLightController : MonoBehaviour
{
    public enum LightState
    {
        Green,
        Red
    }

    [Header("Light Settings")]
    [Tooltip("Trạng thái ban đầu của light")]
    [SerializeField] private LightState initialState = LightState.Green;
    
    [Tooltip("Sử dụng thời gian ngẫu nhiên")]
    [SerializeField] private bool useRandomDuration = true;
    
    [Tooltip("Thời gian Green Light (giây) - chỉ dùng khi useRandomDuration = false")]
    [SerializeField] private float greenLightDuration = 5f;
    
    [Tooltip("Thời gian Red Light (giây) - chỉ dùng khi useRandomDuration = false")]
    [SerializeField] private float redLightDuration = 3f;
    
    [Header("Random Duration Settings")]
    [Tooltip("Thời gian Green Light tối thiểu (giây)")]
    [SerializeField] private float greenLightMinDuration = 3f;
    
    [Tooltip("Thời gian Green Light tối đa (giây)")]
    [SerializeField] private float greenLightMaxDuration = 7f;
    
    [Tooltip("Thời gian Red Light tối thiểu (giây)")]
    [SerializeField] private float redLightMinDuration = 2f;
    
    [Tooltip("Thời gian Red Light tối đa (giây)")]
    [SerializeField] private float redLightMaxDuration = 5f;
    
    [Tooltip("Tự động chuyển đổi giữa Green và Red Light")]
    [SerializeField] private bool autoSwitch = true;
    
    [Header("Debug")]
    [Tooltip("Hiển thị trạng thái light trong Console")]
    [SerializeField] private bool debugLog = true;

    private LightState currentState;
    private float stateTimer = 0f;
    
    public static TrafficLightController Instance { get; private set; }
    
    // Events để các component khác có thể lắng nghe
    public System.Action<LightState> OnLightStateChanged;

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
        // Khởi tạo trạng thái ban đầu
        currentState = initialState;
        stateTimer = 0f;
        
        // Nếu auto switch, bắt đầu timer
        if (autoSwitch)
        {
            stateTimer = GetDurationForState(currentState);
        }
        
        // Phát sound effect cho trạng thái ban đầu
        PlayLightSound();
        
        // Thông báo trạng thái ban đầu
        NotifyStateChanged();
        
        if (debugLog)
        {
            Debug.Log($"TrafficLightController: Khởi tạo với trạng thái {currentState}");
        }
    }

    private void Update()
    {
        if (!autoSwitch)
            return;

        // Đếm ngược timer
        stateTimer -= Time.deltaTime;
        
        // Khi hết thời gian, chuyển đổi trạng thái
        if (stateTimer <= 0f)
        {
            SwitchLight();
        }
    }

    /// <summary>
    /// Chuyển đổi giữa Green Light và Red Light
    /// </summary>
    public void SwitchLight()
    {
        // Chuyển đổi trạng thái
        currentState = (currentState == LightState.Green) ? LightState.Red : LightState.Green;
        
        // Reset timer với thời gian tương ứng (ngẫu nhiên hoặc cố định)
        stateTimer = GetDurationForState(currentState);
        
        // Phát sound effect tương ứng
        PlayLightSound();
        
        // Thông báo thay đổi trạng thái
        NotifyStateChanged();
        
        if (debugLog)
        {
            Debug.Log($"TrafficLightController: Chuyển sang {currentState} (thời gian: {stateTimer:F1}s)");
        }
    }
    
    /// <summary>
    /// Lấy thời gian duration cho trạng thái hiện tại (ngẫu nhiên hoặc cố định)
    /// </summary>
    private float GetDurationForState(LightState state)
    {
        if (useRandomDuration)
        {
            if (state == LightState.Green)
            {
                return Random.Range(greenLightMinDuration, greenLightMaxDuration);
            }
            else
            {
                return Random.Range(redLightMinDuration, redLightMaxDuration);
            }
        }
        else
        {
            return state == LightState.Green ? greenLightDuration : redLightDuration;
        }
    }

    /// <summary>
    /// Set trạng thái light thủ công
    /// </summary>
    public void SetLightState(LightState newState)
    {
        if (currentState == newState)
            return;
        
        currentState = newState;
        stateTimer = GetDurationForState(currentState);
        
        // Phát sound effect tương ứng
        PlayLightSound();
        
        NotifyStateChanged();
        
        if (debugLog)
        {
            Debug.Log($"TrafficLightController: Set trạng thái {currentState} (thời gian: {stateTimer:F1}s)");
        }
    }

    /// <summary>
    /// Lấy trạng thái hiện tại
    /// </summary>
    public LightState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Kiểm tra xem có phải Green Light không
    /// </summary>
    public bool IsGreenLight()
    {
        return currentState == LightState.Green;
    }

    /// <summary>
    /// Kiểm tra xem có phải Red Light không
    /// </summary>
    public bool IsRedLight()
    {
        return currentState == LightState.Red;
    }

    /// <summary>
    /// Lấy thời gian còn lại của trạng thái hiện tại
    /// </summary>
    public float GetRemainingTime()
    {
        return Mathf.Max(0f, stateTimer);
    }

    /// <summary>
    /// Thông báo thay đổi trạng thái cho các component khác
    /// </summary>
    private void NotifyStateChanged()
    {
        OnLightStateChanged?.Invoke(currentState);
        
        // Cập nhật traffic light image trong GUI
        UpdateTrafficLightImage();
    }
    
    /// <summary>
    /// Cập nhật traffic light image trong GUI
    /// Green Light: ẩn image, Red Light: hiện image
    /// </summary>
    private void UpdateTrafficLightImage()
    {
        if (GUIPanel.Instance != null)
        {
            // Green Light: false (ẩn), Red Light: true (hiện)
            bool shouldShow = currentState == LightState.Red;
            GUIPanel.Instance.SetTrafficLightImage(shouldShow);
        }
    }
    
    /// <summary>
    /// Phát sound effect tương ứng với trạng thái light hiện tại
    /// </summary>
    private void PlayLightSound()
    {
        if (AudioManager.Instance == null)
            return;
        
        if (currentState == LightState.Green)
        {
            AudioManager.Instance.PlayGreenLightSound();
        }
        else
        {
            AudioManager.Instance.PlayRedLightSound();
        }
    }

    /// <summary>
    /// Pause/Resume auto switch
    /// </summary>
    public void SetAutoSwitch(bool enable)
    {
        autoSwitch = enable;
    }

    /// <summary>
    /// Reset về trạng thái ban đầu
    /// </summary>
    public void Reset()
    {
        currentState = initialState;
        stateTimer = GetDurationForState(currentState);
        NotifyStateChanged();
        
        if (debugLog)
        {
            Debug.Log($"TrafficLightController: Reset về trạng thái {currentState}");
        }
    }

    /// <summary>
    /// Vẽ Gizmo để hiển thị trạng thái trong Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
        
        // Vẽ icon màu xanh hoặc đỏ tùy theo trạng thái
        Gizmos.color = currentState == LightState.Green ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
