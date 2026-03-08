using UnityEngine;
using System.Collections;

/// <summary>
/// Item Energy - Khi người chơi nhặt sẽ bay vào Port
/// </summary>
public class EnergyItem : MonoBehaviour
{
    [Header("Port Settings")]
    [Tooltip("Port transform để bay vào (nếu null sẽ tự động tìm bằng tag 'Port')")]
    [SerializeField] private Transform portTransform;
    
    [Tooltip("Tag của Port để tự động tìm (mặc định: 'Port')")]
    [SerializeField] private string portTag = "Port";
    
    [Tooltip("Khoảng cách tối thiểu để coi là đã đến Port (đơn vị)")]
    [SerializeField] private float arrivalDistance = 0.5f;
    
    [Header("Fly Settings")]
    [Tooltip("Tốc độ bay vào Port")]
    [SerializeField] private float flySpeed = 10f;
    
    [Tooltip("Hiệu ứng khi đến Port")]
    [SerializeField] private GameObject arrivalEffect;
    
    [Header("Arc Settings")]
    [Tooltip("Độ cao tối đa của đường cong bay (so với Port)")]
    [SerializeField] private float arcHeight = 10f;
    
    [Tooltip("Thời gian bay theo đường cong (giây)")]
    [SerializeField] private float arcDuration = 1f;
    
    [Header("Pickup Settings")]
    [Tooltip("Thời gian delay trước khi bắt đầu bay vào Port (giây)")]
    [SerializeField] private float pickupDelay = 0.3f;
    
    [Header("Score Settings")]
    [Tooltip("Điểm số khi nhặt được EnergyItem này (1 hoặc 5 điểm)")]
    [SerializeField] private int score = 1;
    
    [Header("Visual Settings")]
    [Tooltip("Effect khi nhặt item")]
    [SerializeField] private GameObject pickupEffect;
    
    [Header("Bounce Animation")]
    [Tooltip("Bật/tắt animation nhảy lên xuống")]
    [SerializeField] private bool enableBounceAnimation = true;
    
    [Tooltip("Độ cao nhảy lên (đơn vị)")]
    [SerializeField] private float bounceHeight = 8f;
    
    [Tooltip("Tốc độ animation nhảy (chu kỳ/giây) - giá trị nhỏ hơn = chậm hơn")]
    [SerializeField] private float bounceSpeed = 0.1f;
    
    private bool isCollected = false;
    private bool isFlyingToPort = false;
    private Collider itemCollider;
    private TrailRenderer trailRenderer;
    private Vector3 originalPosition;
    private Coroutine bounceCoroutine;
    
    private void Start()
    {
        // Lấy components
        itemCollider = GetComponent<Collider>();
        trailRenderer = GetComponent<TrailRenderer>();
        
        // Tắt TrailRenderer ban đầu
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
        
        // Lưu vị trí gốc
        originalPosition = transform.position;
        
        // Tìm Port
        FindPort();
        
        // Bắt đầu animation nhảy lên xuống
        if (enableBounceAnimation && !isCollected)
        {
            bounceCoroutine = StartCoroutine(BounceAnimation());
        }
    }
    
    /// <summary>
    /// Tìm Port trong scene
    /// </summary>
    private void FindPort()
    {
        // Nếu đã được gán trong Inspector, không cần tìm
        if (portTransform != null)
            return;
        
        // Tìm Port bằng tag
        GameObject portObj = GameObject.FindGameObjectWithTag(portTag);
        if (portObj != null)
        {
            portTransform = portObj.transform;
        }
        else
        {
            Debug.LogWarning($"EnergyItem: Không tìm thấy Port với tag '{portTag}' trong scene!");
        }
    }
    
    /// <summary>
    /// Xử lý va chạm với player hoặc Port
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Nếu đang bay vào Port và va chạm với Port
        if (isFlyingToPort)
        {
            if (other.CompareTag(portTag) || other.transform == portTransform)
            {
                ArriveAtPort();
                return;
            }
        }
        
        // Nếu chưa nhặt và va chạm với player
        if (!isCollected && other.CompareTag("Player"))
        {
            CollectEnergyItem();
        }
    }
    
    /// <summary>
    /// Xử lý va chạm vật lý với player (nếu collider không phải trigger)
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (isCollected || !collision.gameObject.CompareTag("Player"))
            return;
        
        CollectEnergyItem();
    }
    
    /// <summary>
    /// Xử lý khi player nhặt energy item
    /// </summary>
    private void CollectEnergyItem()
    {
        if (isCollected)
            return;
        
        isCollected = true;
        
        // Dừng animation nhảy
        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
            bounceCoroutine = null;
        }
        
        // Phát sound khi nhặt
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollectSound();
        }
        
        // Spawn pickup effect
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
        
        // Chuyển collider thành trigger để phát hiện va chạm với Port
        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }
        
        // Tìm lại Port nếu chưa có
        if (portTransform == null)
        {
            FindPort();
        }
        
        // Bắt đầu bay vào Port sau delay
        if (portTransform != null)
        {
            StartCoroutine(FlyToPort());
        }
        else
        {
            Debug.LogWarning($"EnergyItem: Không tìm thấy Port, không thể bay vào!");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Animation nhảy lên xuống tại chỗ
    /// </summary>
    private IEnumerator BounceAnimation()
    {
        while (!isCollected)
        {
            float elapsedTime = 0f;
            float cycleDuration = 1f / bounceSpeed; // Thời gian cho 1 chu kỳ (lên + xuống)
            
            // Lên
            while (elapsedTime < cycleDuration / 2f && !isCollected)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (cycleDuration / 2f);
                
                // Sử dụng sin để tạo chuyển động mượt
                float height = Mathf.Sin(t * Mathf.PI) * bounceHeight;
                transform.position = originalPosition + Vector3.up * height;
                
                yield return null;
            }
            
            // Xuống
            while (elapsedTime < cycleDuration && !isCollected)
            {
                elapsedTime += Time.deltaTime;
                float t = (elapsedTime - cycleDuration / 2f) / (cycleDuration / 2f);
                
                // Sử dụng sin để tạo chuyển động mượt
                float height = Mathf.Sin((1f - t) * Mathf.PI) * bounceHeight;
                transform.position = originalPosition + Vector3.up * height;
                
                yield return null;
            }
            
            // Đảm bảo về đúng vị trí gốc
            transform.position = originalPosition;
        }
    }
    
    /// <summary>
    /// Coroutine để bay vào Port theo đường cong
    /// </summary>
    private IEnumerator FlyToPort()
    {
        // Delay trước khi bay
        yield return new WaitForSeconds(pickupDelay);
        
        isFlyingToPort = true;
        
        // Bật TrailRenderer khi bắt đầu bay vào Port
        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
        }
        
        if (portTransform == null)
            yield break;
        
        // Bay theo đường cong lên trời rồi rơi xuống Port
        yield return StartCoroutine(FlyArcToPort());
        
        // Đã đến Port, trigger arrival
        if (portTransform != null)
        {
            ArriveAtPort();
        }
    }
    
    /// <summary>
    /// Bay theo đường cong parabol lên trời rồi rơi xuống Port
    /// </summary>
    private IEnumerator FlyArcToPort()
    {
        if (portTransform == null)
            yield break;
        
        Vector3 startPosition = transform.position;
        Vector3 portPosition = portTransform.position;
        
        // Tính toán điểm cao nhất của đường cong
        float maxHeight = Mathf.Max(startPosition.y, portPosition.y) + arcHeight;
        
        // Tính toán điểm giữa (trên không trung)
        Vector3 midPoint = Vector3.Lerp(startPosition, portPosition, 0.5f);
        midPoint.y = maxHeight;
        
        float elapsedTime = 0f;
        Vector3 previousPosition = startPosition;
        
        // Bay theo đường cong
        while (elapsedTime < arcDuration && portTransform != null)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / arcDuration);
            
            // Sử dụng quadratic bezier curve để tạo đường cong mượt
            // P(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
            // Trong đó P₀ = start, P₁ = midPoint, P₂ = port
            float oneMinusT = 1f - t;
            Vector3 currentPosition = 
                oneMinusT * oneMinusT * startPosition + 
                2f * oneMinusT * t * midPoint + 
                t * t * portPosition;
            
            transform.position = currentPosition;
            
            // Kiểm tra khoảng cách đến Port, nếu đủ gần thì biến mất ngay
            float distanceToPort = Vector3.Distance(currentPosition, portPosition);
            if (distanceToPort <= arrivalDistance)
            {
                // Đã đến Port, biến mất ngay
                ArriveAtPort();
                yield break;
            }
            
            // Xoay item theo hướng di chuyển
            if (elapsedTime > 0.01f)
            {
                Vector3 moveDirection = (currentPosition - previousPosition).normalized;
                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }
            }
            
            previousPosition = currentPosition;
            yield return null;
        }
        
        // Đảm bảo đã đến đúng vị trí Port (nếu chưa biến mất)
        if (portTransform != null && isFlyingToPort)
        {
            transform.position = portTransform.position;
            // Biến mất ngay khi đến vị trí Port
            ArriveAtPort();
        }
    }
    
    /// <summary>
    /// Xử lý khi đến Port - EnergyItem sẽ biến mất
    /// </summary>
    private void ArriveAtPort()
    {
        // Tránh gọi nhiều lần
        if (!isFlyingToPort)
            return;
        
        isFlyingToPort = false;
        
        // Đảm bảo đã đến đúng vị trí Port
        if (portTransform != null)
        {
            transform.position = portTransform.position;
        }
        
        // Spawn hiệu ứng khi đến Port
        if (arrivalEffect != null)
        {
            GameObject effect = Instantiate(arrivalEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Phát sound effect khi đến Port
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCheckpointSound();
        }

        // Thông báo cho LevelManager là đã nhặt được một EnergyItem với điểm số
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnEnergyItemArrived(score);
        }
        
        // Ẩn renderer trước khi destroy để tạo hiệu ứng biến mất mượt hơn
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        
        // Tắt TrailRenderer
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
        
        // Destroy ngay lập tức khi đến Port - EnergyItem biến mất
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Reset item (dùng khi restart level)
    /// </summary>
    public void ResetItem()
    {
        isCollected = false;
        isFlyingToPort = false;
        
        // Dừng animation nhảy
        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
            bounceCoroutine = null;
        }
        
        // Khôi phục vị trí gốc
        originalPosition = transform.position;
        transform.position = originalPosition;
        
        // Bật lại collider
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }
        
        // Hiện lại renderer
        if (GetComponent<Renderer>() != null)
        {
            GetComponent<Renderer>().enabled = true;
        }
        
        // Tắt TrailRenderer
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
        
        // Dừng tất cả coroutines
        StopAllCoroutines();
        
        // Tìm lại Port
        FindPort();
        
        // Bắt đầu lại animation nhảy
        if (enableBounceAnimation)
        {
            bounceCoroutine = StartCoroutine(BounceAnimation());
        }
    }
}
