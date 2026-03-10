using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component để vẽ đường line đứt nét nối các stage với nhau
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StageConnector : MonoBehaviour
{
    [Header("Line Settings")]
    [Tooltip("Màu của đường line")]
    public Color lineColor = new Color(1f, 1f, 1f, 0.5f);
    
    [Tooltip("Độ dày của đường line")]
    [Range(1f, 10f)]
    public float lineWidth = 2f;
    
    [Tooltip("Độ dài của mỗi đoạn đứt nét")]
    [Range(5f, 50f)]
    public float dashLength = 10f;
    
    [Tooltip("Khoảng cách giữa các đoạn đứt nét")]
    [Range(5f, 50f)]
    public float dashGap = 10f;
    
    [Header("Connection Settings")]
    [Tooltip("Tự động tìm và kết nối các stage khi Start")]
    public bool autoConnectOnStart = true;
    
    [Tooltip("Parent chứa các stage (nếu để trống sẽ tự động tìm)")]
    public Transform stagesParent;
    
    [Tooltip("Tên của các stage object (mặc định: StageComplete)")]
    public string stageNameContains = "StageComplete";
    
    [Header("Connection Points")]
    [Tooltip("Vị trí trên stage để kết nối (0 = left, 0.5 = center, 1 = right)")]
    [Range(0f, 1f)]
    public float connectionPointX = 0.5f; // Center
    
    [Tooltip("Vị trí dọc trên stage để kết nối (0 = bottom, 0.5 = center, 1 = top)")]
    [Range(0f, 1f)]
    public float connectionPointY = 0.5f; // Center

    [Tooltip("Đẩy đường line xuống dưới (giá trị âm = xuống dưới). Không ảnh hưởng điểm nối theo trục X (vẫn ở giữa).")]
    public float connectionYOffset = 0f;
    
    private List<RectTransform> stageTransforms = new List<RectTransform>();
    private List<GameObject> lineSegments = new List<GameObject>();
    
    private void Start()
    {
        if (autoConnectOnStart)
        {
            ConnectStages();
        }
    }
    
    /// <summary>
    /// Kết nối tất cả các stage với đường line đứt nét
    /// </summary>
    public void ConnectStages()
    {
        // Xóa các line cũ
        ClearLines();
        
        // Tìm các stage
        FindStages();
        
        if (stageTransforms.Count < 2)
        {
            Debug.LogWarning("StageConnector: Cần ít nhất 2 stage để kết nối!");
            return;
        }
        
        // Tạo đường line giữa các stage liên tiếp
        for (int i = 0; i < stageTransforms.Count - 1; i++)
        {
            RectTransform fromStage = stageTransforms[i];
            RectTransform toStage = stageTransforms[i + 1];
            
            CreateDashedLine(fromStage, toStage);
        }
    }
    
    /// <summary>
    /// Tìm tất cả các stage trong parent
    /// </summary>
    private void FindStages()
    {
        stageTransforms.Clear();
        
        // Tìm parent nếu chưa gán
        if (stagesParent == null)
        {
            // Thử tìm trong parent của component này
            stagesParent = transform.parent;
            
            // Hoặc tìm LevelPage
            LevelPage levelPage = GetComponentInParent<LevelPage>();
            if (levelPage != null)
            {
                stagesParent = levelPage.transform;
            }
        }
        
        if (stagesParent == null)
        {
            Debug.LogWarning("StageConnector: Không tìm thấy parent chứa các stage!");
            return;
        }
        
        // Tìm tất cả các stage
        Transform[] allChildren = stagesParent.GetComponentsInChildren<Transform>(true);
        
        foreach (Transform child in allChildren)
        {
            if (child.name.Contains(stageNameContains))
            {
                RectTransform rectTransform = child.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    stageTransforms.Add(rectTransform);
                }
            }
        }
        
        // Sắp xếp theo thứ tự trong hierarchy
        stageTransforms.Sort((a, b) =>
        {
            // Sắp xếp theo parent trước, sau đó theo sibling index
            int parentCompare = a.parent.GetSiblingIndex().CompareTo(b.parent.GetSiblingIndex());
            if (parentCompare != 0) return parentCompare;
            return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
        });
        
        Debug.Log($"StageConnector: Tìm thấy {stageTransforms.Count} stage để kết nối");
    }
    
    /// <summary>
    /// Tạo đường line đứt nét giữa 2 stage
    /// </summary>
    private void CreateDashedLine(RectTransform fromStage, RectTransform toStage)
    {
        // Lấy vị trí kết nối trên mỗi stage
        Vector2 fromPos = GetConnectionPoint(fromStage);
        Vector2 toPos = GetConnectionPoint(toStage);
        
        // Tính toán vector và khoảng cách
        Vector2 direction = (toPos - fromPos).normalized;
        float distance = Vector2.Distance(fromPos, toPos);
        
        // Tạo các đoạn đứt nét
        float currentDistance = 0f;
        bool isDash = true;
        
        while (currentDistance < distance)
        {
            float segmentLength = isDash ? dashLength : dashGap;
            
            if (isDash && currentDistance + segmentLength <= distance)
            {
                // Tạo một đoạn line
                Vector2 segmentStart = fromPos + direction * currentDistance;
                Vector2 segmentEnd = fromPos + direction * Mathf.Min(currentDistance + segmentLength, distance);
                
                CreateLineSegment(segmentStart, segmentEnd);
            }
            
            currentDistance += segmentLength;
            isDash = !isDash;
        }
    }
    
    /// <summary>
    /// Lấy điểm kết nối trên stage (trong không gian của connector)
    /// </summary>
    private Vector2 GetConnectionPoint(RectTransform stage)
    {
        RectTransform connectorRect = GetComponent<RectTransform>();
        
        // Tính toán điểm kết nối trong local space của stage
        Vector2 stageLocalPoint = new Vector2(
            stage.rect.width * (connectionPointX - 0.5f),
            stage.rect.height * (connectionPointY - 0.5f)
        );
        
        // Chuyển sang world space
        Vector3 worldPoint = stage.TransformPoint(stageLocalPoint);
        
        // Chuyển về local space của connector
        Vector2 connectorLocalPoint = connectorRect.InverseTransformPoint(worldPoint);

        // Đẩy line xuống dưới để không che stage
        connectorLocalPoint.y += connectionYOffset;

        return connectorLocalPoint;
    }
    
    /// <summary>
    /// Tạo một đoạn line
    /// </summary>
    private void CreateLineSegment(Vector2 start, Vector2 end)
    {
        GameObject segment = new GameObject("LineSegment");
        segment.transform.SetParent(transform, false);
        
        // Đặt ở đầu danh sách để render trước stage (nằm dưới stage trong hierarchy)
        segment.transform.SetAsFirstSibling();
        
        RectTransform rectTransform = segment.AddComponent<RectTransform>();
        Image image = segment.AddComponent<Image>();
        image.color = lineColor;
        
        // Tính toán vị trí và kích thước
        Vector2 direction = (end - start).normalized;
        float length = Vector2.Distance(start, end);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Đặt vị trí ở giữa đoạn line
        rectTransform.anchoredPosition = (start + end) / 2f;
        rectTransform.sizeDelta = new Vector2(length, lineWidth);
        rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        
        // Đặt anchor ở giữa
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        lineSegments.Add(segment);
    }
    
    /// <summary>
    /// Xóa tất cả các line đã tạo
    /// </summary>
    public void ClearLines()
    {
        foreach (GameObject segment in lineSegments)
        {
            if (segment != null)
            {
                DestroyImmediate(segment);
            }
        }
        lineSegments.Clear();
    }
    
    private void OnDestroy()
    {
        ClearLines();
    }
    
    /// <summary>
    /// Refresh lại các đường line (gọi khi stage thay đổi vị trí)
    /// </summary>
    public void Refresh()
    {
        ConnectStages();
    }
}
