using UnityEngine;
using System.Collections;

/// <summary>
/// Quả cầu lửa được boss bắn ra khi player di chuyển trong Red Light
/// </summary>
public class Fireball : MonoBehaviour
{
    [Header("Fireball Settings")]
    [Tooltip("Tốc độ bay của fireball")]
    [SerializeField] private float speed = 15f;
    
    [Tooltip("Hiệu ứng khi trúng player")]
    [SerializeField] private GameObject hitEffect;
    
    [Header("Visual Settings")]
    [Tooltip("TrailRenderer của fireball")]
    [SerializeField] private TrailRenderer trailRenderer;
    
    private Transform target; // Player target
    private Vector3 targetPosition;
    private bool hasHit = false;
    
    /// <summary>
    /// Khởi tạo fireball với target là player
    /// </summary>
    public void Initialize(Transform targetTransform, Vector3 startPosition)
    {
        target = targetTransform;
        transform.position = startPosition;
        
        // Lưu vị trí target tại thời điểm bắn
        if (target != null)
        {
            targetPosition = target.position;
        }
        
        // Tự động tìm TrailRenderer nếu chưa có
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }
        
        // Bắt đầu bay đến player
        StartCoroutine(FlyToTarget());
    }
    
    /// <summary>
    /// Bay đến target (player)
    /// </summary>
    private IEnumerator FlyToTarget()
    {
        Vector3 startPosition = transform.position;
        
        // Tính toán hướng đến target
        Vector3 direction = (targetPosition - startPosition).normalized;
        
        // Xoay fireball hướng về target
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // Bay đến target
        float distance = Vector3.Distance(startPosition, targetPosition);
        float travelTime = distance / speed;
        float elapsedTime = 0f;
        
        while (elapsedTime < travelTime && !hasHit)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / travelTime;
            
            // Di chuyển theo đường thẳng đến target
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            yield return null;
        }
        
        // Đã đến target, trigger hit
        if (!hasHit)
        {
            HitTarget();
        }
    }
    
    /// <summary>
    /// Xử lý va chạm với player
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
            return;
        
        if (other.CompareTag("Player"))
        {
            HitTarget();
        }
    }
    
    /// <summary>
    /// Xử lý khi trúng target
    /// </summary>
    private void HitTarget()
    {
        if (hasHit)
            return;
        
        hasHit = true;
        
        // Spawn hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        // Gây damage cho player (trigger death)
        if (target != null)
        {
            PlayerController player = target.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeFireballDamage();
            }
        }
        
        // Destroy fireball
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Update target position nếu target di chuyển (optional - có thể bỏ nếu muốn fireball bay đến vị trí cố định)
    /// </summary>
    private void Update()
    {
        // Có thể update target position nếu muốn fireball đuổi theo player
        // Nhưng theo yêu cầu, fireball bay đến vị trí player tại thời điểm bắn
    }
}
