using UnityEngine;
using System.Collections;

/// <summary>
/// Item Boom - Khi người chơi nhặt sẽ bay vào boss và nổ khi va chạm
/// </summary>
public class BoomItem : MonoBehaviour
{
    [Header("Boom Settings")]
    [Tooltip("Tốc độ bay vào boss")]
    [SerializeField] private float flySpeed = 10f;
    
    [Tooltip("Hiệu ứng nổ khi va chạm với boss")]
    [SerializeField] private GameObject explosionEffect;
    
    [Header("Arc Settings")]
    [Tooltip("Độ cao tối đa của đường cong bay (so với boss)")]
    [SerializeField] private float arcHeight = 10f;
    
    [Tooltip("Thời gian bay theo đường cong (giây)")]
    [SerializeField] private float arcDuration = 1f;
    
    [Header("Pickup Settings")]
    [Tooltip("Thời gian delay trước khi bắt đầu bay vào boss (giây)")]
    [SerializeField] private float pickupDelay = 0.3f;
    
    [Header("Visual Settings")]
    [Tooltip("Effect khi nhặt item")]
    [SerializeField] private GameObject pickupEffect;
    
    private bool isCollected = false;
    private bool isFlyingToBoss = false;
    private Transform bossTransform;
    private Collider itemCollider;
    
    private void Start()
    {
        // Lấy components
        itemCollider = GetComponent<Collider>();
        
        // Tìm boss
        FindBoss();
    }
    
    /// <summary>
    /// Tìm boss trong scene
    /// </summary>
    private void FindBoss()
    {
        BossController boss = FindObjectOfType<BossController>();
        if (boss != null)
        {
            bossTransform = boss.transform;
        }
        else
        {
            Debug.LogWarning("BoomItem: Không tìm thấy Boss trong scene!");
        }
    }
    
    /// <summary>
    /// Xử lý va chạm với player hoặc boss
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Nếu đang bay vào boss và va chạm với boss
        if (isFlyingToBoss)
        {
            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
            {
                Explode();
                return;
            }
        }
        
        // Nếu chưa nhặt và va chạm với player
        if (!isCollected && other.CompareTag("Player"))
        {
            CollectBoomItem();
        }
    }
    
    /// <summary>
    /// Xử lý va chạm vật lý với player (nếu collider không phải trigger)
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (isCollected || !collision.gameObject.CompareTag("Player"))
            return;
        
        CollectBoomItem();
    }
    
    /// <summary>
    /// Xử lý khi player nhặt boom item
    /// </summary>
    private void CollectBoomItem()
    {
        if (isCollected)
            return;
        
        isCollected = true;
        
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
        
        // Chuyển collider thành trigger để phát hiện va chạm với boss
        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }
        
        // Tìm lại boss nếu chưa có
        if (bossTransform == null)
        {
            FindBoss();
        }
        
        // Bắt đầu bay vào boss sau delay
        if (bossTransform != null)
        {
            StartCoroutine(FlyToBoss());
        }
        else
        {
            Debug.LogWarning("BoomItem: Không tìm thấy Boss, không thể bay vào!");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Coroutine để bay vào boss theo đường cong
    /// </summary>
    private IEnumerator FlyToBoss()
    {
        // Delay trước khi bay
        yield return new WaitForSeconds(pickupDelay);
        
        isFlyingToBoss = true;
        
        if (bossTransform == null)
            yield break;
        
        // Bay theo đường cong lên trời rồi rơi xuống boss
        yield return StartCoroutine(FlyArcToBoss());
        
        // Đã đến boss, trigger nổ
        if (bossTransform != null)
        {
            Explode();
        }
    }
    
    /// <summary>
    /// Bay theo đường cong parabol lên trời rồi rơi xuống boss
    /// </summary>
    private IEnumerator FlyArcToBoss()
    {
        if (bossTransform == null)
            yield break;
        
        Vector3 startPosition = transform.position;
        Vector3 bossPosition = bossTransform.position;
        
        // Tính toán điểm cao nhất của đường cong
        float maxHeight = Mathf.Max(startPosition.y, bossPosition.y) + arcHeight;
        
        // Tính toán điểm giữa (trên không trung)
        Vector3 midPoint = Vector3.Lerp(startPosition, bossPosition, 0.5f);
        midPoint.y = maxHeight;
        
        float elapsedTime = 0f;
        Vector3 previousPosition = startPosition;
        
        // Bay theo đường cong
        while (elapsedTime < arcDuration && bossTransform != null)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / arcDuration);
            
            // Sử dụng quadratic bezier curve để tạo đường cong mượt
            // P(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
            // Trong đó P₀ = start, P₁ = midPoint, P₂ = boss
            float oneMinusT = 1f - t;
            Vector3 currentPosition = 
                oneMinusT * oneMinusT * startPosition + 
                2f * oneMinusT * t * midPoint + 
                t * t * bossPosition;
            
            transform.position = currentPosition;
            
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
        
        // Đảm bảo đã đến đúng vị trí boss
        if (bossTransform != null)
        {
            transform.position = bossTransform.position;
        }
    }
    
    /// <summary>
    /// Nổ khi va chạm với boss
    /// </summary>
    private void Explode()
    {
        if (isCollected && !isFlyingToBoss)
            return; // Đã nổ rồi
        
        isFlyingToBoss = false;
        
        // Gây damage cho boss
        if (bossTransform != null)
        {
            BossController boss = bossTransform.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage();
            }
        }
        
        // Spawn hiệu ứng nổ
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }
        
        AudioManager.Instance.PlaySound("se_explosion");
        
        // Destroy ngay lập tức khi nổ
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Reset item (dùng khi restart level)
    /// </summary>
    public void ResetItem()
    {
        isCollected = false;
        isFlyingToBoss = false;
        
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
        
        // Dừng tất cả coroutines
        StopAllCoroutines();
        
        // Tìm lại boss
        FindBoss();
    }
}
