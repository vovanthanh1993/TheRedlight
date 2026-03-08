using UnityEngine;

/// <summary>
/// Script để detect trigger collision giữa laser VFX và player
/// Gắn vào laser VFX GameObject (cần có Collider với Is Trigger = true)
/// </summary>
[RequireComponent(typeof(Collider))]
public class LaserTriggerDetector : MonoBehaviour
{
    private TurretMini turretMini;

    /// <summary>
    /// Khởi tạo detector với reference đến TurretMini
    /// </summary>
    public void Initialize(TurretMini turret)
    {
        turretMini = turret;
        
        // Đảm bảo collider là trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"LaserTriggerDetector: GameObject '{gameObject.name}' không có Collider! Vui lòng thêm Collider với Is Trigger = true.");
        }
    }

    /// <summary>
    /// Xử lý khi player vào trigger
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (turretMini != null)
            {
                turretMini.OnPlayerTriggerLaser(other.gameObject);
            }
        }
    }
}
