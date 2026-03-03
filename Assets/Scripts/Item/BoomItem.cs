using UnityEngine;

/// <summary>
/// BoomItem - Khi player chạm vào sẽ nổ và làm mất 1 mạng
/// </summary>
public class BoomItem : MonoBehaviour
{
    [Header("Explosion Settings")]
    [Tooltip("Hiệu ứng nổ khi player chạm vào")]
    [SerializeField] private GameObject explosionEffect;
    
    private bool hasExploded = false;
    
    /// <summary>
    /// Xử lý va chạm với player (trigger)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded)
            return;
        
        // Nếu player chạm vào
        if (other.CompareTag("Player"))
        {
            Debug.Log("BoomItem: OnTriggerEnter - Player chạm vào!");
            TriggerExplosion();
        }
    }
    
    /// <summary>
    /// Xử lý va chạm vật lý với player (CharacterController)
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded)
            return;
        
        // Nếu player chạm vào
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("BoomItem: OnCollisionEnter - Player chạm vào!");
            TriggerExplosion();
        }
    }
    
    /// <summary>
    /// Kích hoạt nổ ngay khi player chạm vào
    /// </summary>
    private void TriggerExplosion()
    {
        if (hasExploded)
            return;
        
        hasExploded = true;
        
        // Nổ ngay lập tức
        Explode();
    }
    
    /// <summary>
    /// Nổ và gây damage cho player ngay lập tức
    /// </summary>
    private void Explode()
    {
        // Spawn hiệu ứng nổ
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Phát sound effect nổ
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayExplosion();
        }
        
        // Gây damage cho player (mất 1 mạng)
        if (HealthPanel.Instance != null)
        {
            bool stillHasLives = HealthPanel.Instance.LoseLife();
            
            Debug.LogWarning($"BoomItem: Player chạm vào BoomItem! Đã mất 1 mạng. Số mạng còn lại: {HealthPanel.Instance.GetCurrentLives()}");
            
            // Nếu hết mạng, hiển thị lose panel
            if (!stillHasLives)
            {
                Debug.LogWarning("BoomItem: Player đã hết mạng!");
                
                if (UIManager.Instance != null && UIManager.Instance.gamePlayPanel != null)
                {
                    UIManager.Instance.gamePlayPanel.ShowLosePanel(true);
                    Time.timeScale = 0f;
                }
            }
            else
            {
                // Nếu còn mạng, đưa player về spawn point
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.TakeBoomDamage();
                }
            }
        }
        else
        {
            Debug.LogWarning("BoomItem: Không tìm thấy HealthPanel.Instance!");
        }
        
        // Destroy item ngay lập tức khi chạm vào player
        Destroy(gameObject);
    }
    
}
