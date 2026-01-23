using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI hiển thị trạng thái Green Light / Red Light
/// </summary>
public class TrafficLightUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Image hiển thị màu light (nếu dùng Image)")]
    [SerializeField] private Image lightImage;
    
    [Tooltip("Text hiển thị trạng thái (nếu dùng Text)")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Tooltip("Text hiển thị thời gian còn lại")]
    [SerializeField] private TextMeshProUGUI timerText;
    
    [Header("Colors")]
    [Tooltip("Màu Green Light")]
    [SerializeField] private Color greenColor = Color.green;
    
    [Tooltip("Màu Red Light")]
    [SerializeField] private Color redColor = Color.red;
    
    [Header("Settings")]
    [Tooltip("Hiển thị thời gian còn lại")]
    [SerializeField] private bool showTimer = true;
    
    [Tooltip("Format hiển thị thời gian (ví dụ: F1 = 1 chữ số thập phân)")]
    [SerializeField] private string timerFormat = "F1";

    private void Start()
    {
        // Đăng ký lắng nghe sự kiện thay đổi trạng thái light
        if (TrafficLightController.Instance != null)
        {
            TrafficLightController.Instance.OnLightStateChanged += OnLightStateChanged;
            // Cập nhật UI ngay lập tức với trạng thái hiện tại
            OnLightStateChanged(TrafficLightController.Instance.GetCurrentState());
        }
        else
        {
            Debug.LogWarning("TrafficLightUI: Không tìm thấy TrafficLightController.Instance!");
        }
    }

    private void Update()
    {
        // Cập nhật timer mỗi frame
        if (showTimer && timerText != null && TrafficLightController.Instance != null)
        {
            float remainingTime = TrafficLightController.Instance.GetRemainingTime();
            timerText.text = remainingTime.ToString(timerFormat);
        }
    }

    /// <summary>
    /// Xử lý khi trạng thái light thay đổi
    /// </summary>
    private void OnLightStateChanged(TrafficLightController.LightState newState)
    {
        // Cập nhật màu sắc của Image
        if (lightImage != null)
        {
            lightImage.color = newState == TrafficLightController.LightState.Green ? greenColor : redColor;
        }
        
        // Cập nhật text trạng thái
        if (statusText != null)
        {
            statusText.text = newState == TrafficLightController.LightState.Green ? "GREEN LIGHT" : "RED LIGHT";
            statusText.color = newState == TrafficLightController.LightState.Green ? greenColor : redColor;
        }
    }

    private void OnDestroy()
    {
        // Hủy đăng ký khi object bị destroy
        if (TrafficLightController.Instance != null)
        {
            TrafficLightController.Instance.OnLightStateChanged -= OnLightStateChanged;
        }
    }
}
