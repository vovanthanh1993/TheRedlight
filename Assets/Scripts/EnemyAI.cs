using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy AI Agent - Di chuyển dựa trên trạng thái đèn giao thông
/// - Đèn xanh (Green Light): Ngừng di chuyển
/// - Đèn đỏ (Red Light): Di chuyển về hướng player
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("NavMesh Agent Settings")]
    [Tooltip("NavMeshAgent component để di chuyển")]
    [SerializeField] private NavMeshAgent navAgent;
    
    [Tooltip("Tốc độ di chuyển của enemy")]
    [SerializeField] private float moveSpeed = 2f;
    
    [Tooltip("Tốc độ xoay của enemy")]
    [SerializeField] private float angularSpeed = 120f;
    
    [Tooltip("Khoảng cách dừng lại khi đến gần player")]
    [SerializeField] private float stoppingDistance = 1f;
    
    [Header("Player Detection")]
    [Tooltip("Khoảng cách tối đa để enemy có thể phát hiện player (0 = không giới hạn)")]
    [SerializeField] private float detectionRange = 0f;
    
    [Header("Update Settings")]
    [Tooltip("Tần suất cập nhật destination (giây) - 0 = mỗi frame")]
    [SerializeField] private float updateInterval = 0.1f;
    
    [Header("Debug")]
    [Tooltip("Hiển thị debug info trong Console")]
    [SerializeField] private bool debugLog = false;
    
    [Tooltip("Hiển thị Gizmo để debug trong Scene view")]
    [SerializeField] private bool showGizmos = true;
    
    private Transform playerTransform;
    private bool isMoving = false;
    private float lastUpdateTime = 0f;
    
    private void Awake()
    {
        // Tự động tìm NavMeshAgent nếu chưa được gán
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
            
            if (navAgent == null)
            {
                Debug.LogError($"EnemyAI: NavMeshAgent component is missing on {gameObject.name}! Please add NavMeshAgent component.");
            }
        }
    }
    
    private void Start()
    {
        // Tìm player transform
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }
        else
        {
            // Nếu không tìm thấy PlayerController, tìm bằng tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning($"EnemyAI: Không tìm thấy Player! Enemy sẽ không thể di chuyển về hướng player.");
            }
        }
        
        // Cấu hình NavMeshAgent
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.angularSpeed = angularSpeed;
            navAgent.stoppingDistance = stoppingDistance;
        }
        
        // Đăng ký lắng nghe sự kiện thay đổi trạng thái đèn
        if (TrafficLightController.Instance != null)
        {
            TrafficLightController.Instance.OnLightStateChanged += OnLightStateChanged;
        }
    }
    
    private void Update()
    {
        if (navAgent == null)
        {
            return;
        }
        
        // Kiểm tra trạng thái đèn và di chuyển
        HandleMovement();
    }
    
    /// <summary>
    /// Xử lý di chuyển dựa trên trạng thái đèn
    /// </summary>
    private void HandleMovement()
    {
        // Kiểm tra xem có TrafficLightController không
        if (TrafficLightController.Instance == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("EnemyAI: TrafficLightController.Instance không tồn tại!");
            }
            return;
        }
        
        // Nếu đèn xanh: ngừng di chuyển
        if (TrafficLightController.Instance.IsGreenLight())
        {
            StopMovement();
            return;
        }
        
        // Nếu đèn đỏ: di chuyển về hướng player
        if (TrafficLightController.Instance.IsRedLight())
        {
            MoveTowardsPlayer();
            return;
        }
    }
    
    /// <summary>
    /// Di chuyển về hướng player
    /// </summary>
    private void MoveTowardsPlayer()
    {
        if (playerTransform == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("EnemyAI: Không tìm thấy player transform!");
            }
            return;
        }
        
        // Kiểm tra khoảng cách nếu có detection range
        if (detectionRange > 0f)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > detectionRange)
            {
                // Quá xa, không di chuyển
                StopMovement();
                return;
            }
        }
        
        // Cập nhật destination theo interval để tối ưu performance
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            // Kiểm tra xem có thể tìm đường đến player không
            NavMeshHit hit;
            if (NavMesh.SamplePosition(playerTransform.position, out hit, 5f, NavMesh.AllAreas))
            {
                // Set destination cho NavMeshAgent
                if (navAgent.isOnNavMesh)
                {
                    navAgent.SetDestination(hit.position);
                    navAgent.isStopped = false;
                    isMoving = true;
                    
                    if (debugLog)
                    {
                        Debug.Log($"EnemyAI: Đang di chuyển về hướng player. Khoảng cách: {Vector3.Distance(transform.position, playerTransform.position):F2}");
                    }
                }
                else
                {
                    if (debugLog)
                    {
                        Debug.LogWarning("EnemyAI: NavMeshAgent không trên NavMesh!");
                    }
                }
            }
            else
            {
                if (debugLog)
                {
                    Debug.LogWarning("EnemyAI: Không tìm thấy vị trí hợp lệ trên NavMesh gần player!");
                }
            }
            
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Ngừng di chuyển
    /// </summary>
    private void StopMovement()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            // Dừng NavMeshAgent
            navAgent.isStopped = true;
        }
        
        isMoving = false;
        
        if (debugLog)
        {
            Debug.Log("EnemyAI: Đã ngừng di chuyển (Green Light)");
        }
    }
    
    /// <summary>
    /// Callback khi trạng thái đèn thay đổi
    /// </summary>
    private void OnLightStateChanged(TrafficLightController.LightState newState)
    {
        if (debugLog)
        {
            Debug.Log($"EnemyAI: Trạng thái đèn đã thay đổi sang {newState}");
        }
        
        // Nếu chuyển sang Red Light, bắt đầu di chuyển ngay
        if (newState == TrafficLightController.LightState.Red)
        {
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
            }
        }
        // Nếu chuyển sang Green Light, dừng ngay
        else if (newState == TrafficLightController.LightState.Green)
        {
            StopMovement();
        }
    }
    
    /// <summary>
    /// Vẽ Gizmo để debug trong Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying)
            return;
        
        // Vẽ đường đến player nếu đang di chuyển
        if (isMoving && playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
        
        // Vẽ path của NavMeshAgent nếu có
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] path = navAgent.path.corners;
            for (int i = 0; i < path.Length - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
        
        // Vẽ detection range nếu có
        if (detectionRange > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
        
        // Vẽ trạng thái đèn hiện tại
        if (TrafficLightController.Instance != null)
        {
            Gizmos.color = TrafficLightController.Instance.IsGreenLight() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
        }
        
        // Vẽ destination của NavMeshAgent
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(navAgent.destination, 0.5f);
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
}
