using UnityEngine;

/// <summary>
/// Controller cho Boss
/// Khi Green Light: rotation Y = 90 độ
/// Khi Red Light: rotation Y = -90 độ
/// Boss không di chuyển, chỉ xoay mặt
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Tốc độ quay của boss")]
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Rotation Angles")]
    [Tooltip("Rotation Y khi Green Light (độ)")]
    [SerializeField] private float greenLightRotationY = 90f;
    
    [Tooltip("Rotation Y khi Red Light (độ)")]
    [SerializeField] private float redLightRotationY = -90f;
    
    [Header("Red Light Settings")]
    [Tooltip("Bật/tắt cơ chế quay khi Red Light")]
    [SerializeField] private bool enableRedLightReverse = true;
    
    [Header("Animation Settings")]
    [Tooltip("Animator component của boss")]
    [SerializeField] private Animator animator;
    
    [Tooltip("Tên trigger parameter cho animation hit")]
    [SerializeField] private string hitTriggerParameter = "Hit";
    
    [Header("Debug")]
    [Tooltip("Hiển thị log trong Console")]
    [SerializeField] private bool debugLog = false;
    
    // Rotation khi green light
    private Quaternion greenLightRotation;
    
    // Rotation khi red light
    private Quaternion redLightRotation;
    
    // Trạng thái red light hiện tại
    private bool isRedLightActive = false;
    
    // Đang trong quá trình quay
    private bool isRotating = false;

    private void Awake()
    {
        // Tự động tìm Animator nếu chưa được assign
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                // Tìm trong children
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        // Tính rotation cho green light (Y = 90)
        Vector3 currentEuler = transform.eulerAngles;
        greenLightRotation = Quaternion.Euler(currentEuler.x, greenLightRotationY, currentEuler.z);
        
        // Tính rotation cho red light (Y = -90)
        redLightRotation = Quaternion.Euler(currentEuler.x, redLightRotationY, currentEuler.z);
    }

    private void Start()
    {
        // Đăng ký lắng nghe sự kiện thay đổi trạng thái light
        if (TrafficLightController.Instance != null)
        {
            TrafficLightController.Instance.OnLightStateChanged += OnLightStateChanged;
            
            // Kiểm tra trạng thái ban đầu
            if (TrafficLightController.Instance.IsRedLight())
            {
                OnLightStateChanged(TrafficLightController.LightState.Red);
            }
            else
            {
                OnLightStateChanged(TrafficLightController.LightState.Green);
            }
        }
        else
        {
            Debug.LogWarning("BossController: Không tìm thấy TrafficLightController.Instance!");
        }
    }

    /// <summary>
    /// Xử lý khi trạng thái light thay đổi
    /// </summary>
    private void OnLightStateChanged(TrafficLightController.LightState newState)
    {
        if (!enableRedLightReverse)
            return;
        
        bool wasRedLight = isRedLightActive;
        isRedLightActive = (newState == TrafficLightController.LightState.Red);
        
        if (isRedLightActive && !wasRedLight)
        {
            // Khi chuyển sang Red Light: quay ngược lại
            ReverseDirection();
        }
        else if (!isRedLightActive && wasRedLight)
        {
            // Khi chuyển sang Green Light: quay lại hướng ban đầu
            RestoreDirection();
        }
    }

    /// <summary>
    /// Quay khi Red Light (rotation Y = -90)
    /// </summary>
    private void ReverseDirection()
    {
        if (isRotating)
            return;
        
        if (debugLog)
        {
            Debug.Log($"BossController: Red Light! Quay đến rotation Y = {redLightRotationY} độ.");
        }
        
        // Quay mượt mà đến red light rotation (Y = -90)
        StartCoroutine(RotateSmoothly(redLightRotation));
    }

    /// <summary>
    /// Quay khi Green Light (rotation Y = 90)
    /// </summary>
    private void RestoreDirection()
    {
        if (isRotating)
            return;
        
        if (debugLog)
        {
            Debug.Log($"BossController: Green Light! Quay đến rotation Y = {greenLightRotationY} độ.");
        }
        
        // Quay mượt mà đến green light rotation (Y = 90)
        StartCoroutine(RotateSmoothly(greenLightRotation));
    }

    /// <summary>
    /// Quay mượt mà đến rotation mục tiêu
    /// </summary>
    private System.Collections.IEnumerator RotateSmoothly(Quaternion targetRotation)
    {
        isRotating = true;
        
        Quaternion startRotation = transform.rotation;
        float elapsedTime = 0f;
        float rotationDuration = 1f / rotationSpeed; // Thời gian quay dựa trên rotationSpeed
        
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / rotationDuration);
            
            // Sử dụng Slerp để quay mượt mà
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            yield return null;
        }
        
        // Đảm bảo đã quay đến đúng vị trí
        transform.rotation = targetRotation;
        
        isRotating = false;
    }

    /// <summary>
    /// Nhận damage và chạy animation hit
    /// </summary>
    public void TakeDamage()
    {
        // Trigger animation hit
        if (animator != null && !string.IsNullOrEmpty(hitTriggerParameter))
        {
            animator.SetTrigger(hitTriggerParameter);
            
            if (debugLog)
            {
                Debug.Log("BossController: Nhận damage, chạy animation hit.");
            }
        }
    }
    
    /// <summary>
    /// Xử lý va chạm với boom item
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra nếu va chạm với boom item
        BoomItem boomItem = other.GetComponent<BoomItem>();
        if (boomItem != null)
        {
            TakeDamage();
        }
    }
    
    private void OnDestroy()
    {
        // Hủy đăng ký event khi destroy
        if (TrafficLightController.Instance != null)
        {
            TrafficLightController.Instance.OnLightStateChanged -= OnLightStateChanged;
        }
    }

    /// <summary>
    /// Vẽ Gizmo để hiển thị hướng nhìn trong Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
        
        // Vẽ mũi tên chỉ hướng nhìn
        Gizmos.color = isRedLightActive ? Color.red : Color.green;
        Vector3 direction = transform.forward;
        Gizmos.DrawRay(transform.position, direction * 2f);
        
        // Vẽ sphere ở đầu mũi tên
        Gizmos.DrawWireSphere(transform.position + direction * 2f, 0.2f);
    }
}
