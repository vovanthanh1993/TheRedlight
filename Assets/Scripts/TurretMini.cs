using UnityEngine;
using System.Collections;

/// <summary>
/// Turret Mini - Bắn laser mỗi 10 giây, laser tồn tại 2 giây
/// Dùng Raycast để phát hiện va chạm với player
/// Lưu ý: Turret đứng yên, không di chuyển
/// </summary>
public class TurretMini : MonoBehaviour
{
    [Header("Laser Settings")]
    [Tooltip("Prefab VFX laser để bắn")]
    [SerializeField] private GameObject laserVFXPrefab;

    [Tooltip("Vị trí spawn laser (nếu null sẽ dùng transform.position)")]
    [SerializeField] private Transform laserSpawnPoint;


    [Header("Shoot Settings")]
    [Tooltip("Thời gian giữa các lần bắn (giây)")]
    [SerializeField] private float shootInterval = 10f;

    [Tooltip("Thời gian laser tồn tại (giây)")]
    [SerializeField] private float laserDuration = 2f;

    [Header("Damage Settings")]
    [Tooltip("Damage khi player chạm vào laser")]
    [SerializeField] private int damage = 1;

    [Tooltip("Khoảng thời gian giữa các lần gây damage (giây) - tránh damage liên tục")]
    [SerializeField] private float damageCooldown = 0.5f;


    [Header("Debug")]
    [Tooltip("Hiển thị log trong Console")]
    [SerializeField] private bool debugLog = false;

    private Coroutine shootCoroutine;
    private GameObject currentLaserInstance;
    private bool isLaserActive = false;
    private float lastDamageTime = 0f;
    private bool hasDamagedPlayerThisLaser = false; // Đánh dấu đã damage player trong lần laser này
    private bool isLaserInstanceCreated = false; // Đánh dấu đã tạo laser instance

    private void Start()
    {
        // Tự động tìm spawn point nếu chưa gán
        if (laserSpawnPoint == null)
        {
            Transform spawnPoint = transform.Find("LaserSpawnPoint");
            if (spawnPoint == null)
            {
                // Tạo spawn point mới ở vị trí transform
                GameObject spawnObj = new GameObject("LaserSpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnObj.transform.localPosition = Vector3.zero;
                laserSpawnPoint = spawnObj.transform;
            }
            else
            {
                laserSpawnPoint = spawnPoint;
            }
        }

        // Tạo laser VFX instance một lần (ẩn ban đầu)
        CreateLaserInstance();

        // Bắt đầu bắn
        StartShooting();
    }
    
    /// <summary>
    /// Tạo laser VFX instance một lần (thay vì tạo/xóa mỗi lần)
    /// </summary>
    private void CreateLaserInstance()
    {
        if (isLaserInstanceCreated || laserVFXPrefab == null)
            return;

        // Xác định vị trí spawn
        Vector3 spawnPosition = laserSpawnPoint != null ? laserSpawnPoint.position : transform.position;
        Vector3 shootDirection = transform.forward;

        // Spawn laser VFX một lần
        currentLaserInstance = Instantiate(laserVFXPrefab, spawnPosition, Quaternion.LookRotation(shootDirection));
        currentLaserInstance.transform.SetParent(transform); // Gắn vào turret để xoay theo
        
        // Ẩn ban đầu
        currentLaserInstance.SetActive(false);
        
        // Thêm script để detect trigger collision với player
        LaserTriggerDetector detector = currentLaserInstance.GetComponent<LaserTriggerDetector>();
        if (detector == null)
        {
            detector = currentLaserInstance.AddComponent<LaserTriggerDetector>();
        }
        detector.Initialize(this);
        
        isLaserInstanceCreated = true;
        
        if (debugLog)
        {
            Debug.Log("TurretMini: Đã tạo laser instance (ẩn ban đầu)");
        }
    }

    /// <summary>
    /// Bắt đầu bắn tự động
    /// </summary>
    public void StartShooting()
    {
        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
        }
        shootCoroutine = StartCoroutine(ShootCoroutine());
    }

    /// <summary>
    /// Dừng bắn
    /// </summary>
    public void StopShooting()
    {
        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
        }

        // Dừng laser hiện tại nếu có
        StopLaser();
    }

    /// <summary>
    /// Coroutine bắn laser: bắn 2s, tắt VFX, đợi 10s, lặp lại
    /// </summary>
    private IEnumerator ShootCoroutine()
    {
        while (true)
        {
            // Bắn laser ngay
            ShootLaser();
            
            // Đợi laser tồn tại 2 giây
            yield return new WaitForSeconds(laserDuration);
            
            // Tắt VFX (đã được gọi trong StopLaserAfterDuration, nhưng đảm bảo chắc chắn)
            StopLaser();
            
            // Đợi 10 giây trước khi bắn lại
            yield return new WaitForSeconds(shootInterval);
        }
    }

    /// <summary>
    /// Bắn laser
    /// </summary>
    private void ShootLaser()
    {
        if (isLaserActive)
        {
            if (debugLog)
            {
                Debug.LogWarning("TurretMini: Laser đang active, không thể bắn laser mới!");
            }
            return;
        }

        // Xác định vị trí spawn
        Vector3 spawnPosition = laserSpawnPoint != null ? laserSpawnPoint.position : transform.position;
        
        // Hướng bắn là forward của turret
        Vector3 shootDirection = transform.forward;


        // Đảm bảo laser instance đã được tạo
        if (!isLaserInstanceCreated)
        {
            CreateLaserInstance();
        }

        // Hiện laser VFX (thay vì tạo mới)
        if (currentLaserInstance != null)
        {
            // Cập nhật vị trí và hướng
            currentLaserInstance.transform.position = spawnPosition;
            currentLaserInstance.transform.rotation = Quaternion.LookRotation(shootDirection);
            
            // Hiện laser
            currentLaserInstance.SetActive(true);
        }

        // Reset flag damage cho lần laser mới
        hasDamagedPlayerThisLaser = false;

        // Đánh dấu laser đang active (để track trong OnTriggerEnter)
        isLaserActive = true;

        if (debugLog)
        {
            Debug.Log($"TurretMini: Bắn laser về phía {shootDirection}, tồn tại {laserDuration}s!");
        }
    }

    /// <summary>
    /// Xử lý khi player trigger với laser (được gọi từ LaserTriggerDetector)
    /// </summary>
    public void OnPlayerTriggerLaser(GameObject player)
    {
        // Nếu player trigger với laser trong lúc laser active, player mất 1 mạng
        // Chỉ damage 1 lần trong suốt thời gian laser active
        if (isLaserActive && !hasDamagedPlayerThisLaser)
        {
            // Phát âm thanh khi va chạm với laser
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLaserHitSound();
            }
            
            OnPlayerHit(player);
            hasDamagedPlayerThisLaser = true; // Đánh dấu đã damage
            
            if (debugLog)
            {
                Debug.Log($"TurretMini: Player trigger với laser! Player mất 1 mạng.");
            }
        }
    }


    /// <summary>
    /// Dừng laser (ẩn thay vì destroy)
    /// </summary>
    private void StopLaser()
    {
        isLaserActive = false;

        // Ẩn laser VFX (thay vì destroy)
        if (currentLaserInstance != null)
        {
            currentLaserInstance.SetActive(false);
        }

        if (debugLog)
        {
            Debug.Log("TurretMini: Laser đã tắt (ẩn)!");
        }
    }

    /// <summary>
    /// Xử lý khi player bị hit bởi laser
    /// </summary>
    private void OnPlayerHit(GameObject player)
    {
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Gây damage cho player (dùng TakeLaserDamage để không phát âm thanh nổ)
            playerController.TakeLaserDamage();
            
            if (debugLog)
            {
                Debug.Log($"TurretMini: Player bị hit bởi laser! Damage: {damage}");
            }
        }
    }

    private void OnDestroy()
    {
        StopShooting();
    }

}
