using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Quest Data")]
public class QuestData : ScriptableObject
{
    public int questId;

    public QuestObjective[] objectives;

    public float timeFor3Stars = 60f;
    public float timeFor2Stars = 120f;
    
    [Header("Time Limit")]
    [Tooltip("Thời gian giới hạn để hoàn thành level (giây). Nếu hết thời gian chưa đủ EnergyItem thì thua")]
    public float timeLimit = 300f;

    [Header("Energy Settings")]
    [Tooltip("Số điểm EnergyItem cần nhặt để hoàn thành level này (ví dụ: 10 điểm. Nếu mỗi item 2 điểm thì cần 5 items) (0 = không cần Energy)")]
    public int requiredEnergyPoints = 0;

    [Header("Reward List")]
    [Tooltip("Reward cho 1 sao, 2 sao, 3 sao")]
    public List<int> rewardList = new List<int> { 50, 100, 150 };
}