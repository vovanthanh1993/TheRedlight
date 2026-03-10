using UnityEngine;

/// <summary>
/// Camera Controller để điều chỉnh camera dựa trên tỉ lệ màn hình
/// iPad sẽ dùng position và rotation cố định
/// </summary>
public class CameraFollowController : MonoBehaviour
{
    [Header("iPad Settings")]
    [Tooltip("Camera rotation khi chạy trên iPad (X=54.33, Y=90, Z=0)")]
    public Vector3 iPadRotation = new Vector3(54.33f, 90f, 0f);
    
    [Tooltip("Camera position khi chạy trên iPad")]
    public Vector3 iPadPosition = new Vector3(-16.54f, 29.6f, 3.3f);

    private bool isIPad = false;

    private void Start()
    {
        // Điều chỉnh camera theo tỉ lệ màn hình
        AdjustForAspectRatio();
    }

    /// <summary>
    /// Điều chỉnh camera dựa trên tỉ lệ màn hình
    /// iPad (tỉ lệ gần 4:3) sẽ dùng position và rotation cố định
    /// </summary>
    private void AdjustForAspectRatio()
    {
        // Bảo vệ nếu không có camera trong scene
        if (Camera.main == null)
            return;

        float aspect = (float)Screen.width / Screen.height; // ví dụ: iPhone ~2.16, iPad ~1.33

        // Nếu màn hình \"vuông\" hơn (aspect nhỏ), coi như tablet/iPad → dùng cấu hình iPad
        if (aspect < 1.6f)
        {
            isIPad = true;
            
            // Set camera position và rotation cố định cho iPad
            transform.position = iPadPosition;
            transform.rotation = Quaternion.Euler(iPadRotation);
            
            Debug.Log($"CameraFollowController: Detected tablet-like aspect ({aspect:F2}), using iPad settings");
            Debug.Log($"CameraFollowController: Position set to ({iPadPosition.x:F2}, {iPadPosition.y:F2}, {iPadPosition.z:F2})");
            Debug.Log($"CameraFollowController: Rotation set to ({iPadRotation.x:F2}, {iPadRotation.y:F2}, {iPadRotation.z:F2})");
        }
        else
        {
            isIPad = false;
        }
    }

    private void LateUpdate()
    {
        // Nếu là iPad, giữ nguyên position và rotation cố định
        if (isIPad)
        {
            transform.position = iPadPosition;
            transform.rotation = Quaternion.Euler(iPadRotation);
        }
    }
}

