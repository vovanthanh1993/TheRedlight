using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool để tự động generate quest data cho nhiều level
/// </summary>
public class QuestDataGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Quest Data")]
    public static void ShowWindow()
    {
        GetWindow<QuestDataGenerator>("Quest Data Generator");
    }
    
    [MenuItem("Tools/Generate 50 Levels (Quick)")]
    public static void Generate50LevelsQuick()
    {
        GenerateQuestDataWithPattern(1, 50, 15, 3, 1, 120f, 10f, 50, 100, 150, 25);
    }
    
    private int startLevel = 1;
    private int endLevel = 50;
    
    // Cấu hình độ khó tăng dần
    private int baseEnergyItems = 3; // Level 1 cần 3 điểm EnergyItem
    private int energyItemsIncrement = 2; // Mỗi level tăng thêm 2 điểm
    
    private float baseTimeLimit = 120f; // Level 1 có 120 giây
    private float timeLimitIncrement = 30f; // Mỗi level tăng thêm 30 giây
    
    private int baseReward1Star = 50;
    private int baseReward2Star = 100;
    private int baseReward3Star = 150;
    private int rewardIncrement = 25; // Mỗi level tăng thêm 25 reward
    
    private void OnGUI()
    {
        GUILayout.Label("Quest Data Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        startLevel = EditorGUILayout.IntField("Start Level", startLevel);
        endLevel = EditorGUILayout.IntField("End Level", endLevel);
        
        GUILayout.Space(10);
        GUILayout.Label("Energy Points Settings", EditorStyles.boldLabel);
        baseEnergyItems = EditorGUILayout.IntField("Base Energy Points (Level 1)", baseEnergyItems);
        energyItemsIncrement = EditorGUILayout.IntField("Energy Points Increment Per Level", energyItemsIncrement);
        
        GUILayout.Space(10);
        GUILayout.Label("Time Limit Settings", EditorStyles.boldLabel);
        baseTimeLimit = EditorGUILayout.FloatField("Base Time Limit (Level 1)", baseTimeLimit);
        timeLimitIncrement = EditorGUILayout.FloatField("Time Limit Increment Per Level", timeLimitIncrement);
        
        GUILayout.Space(10);
        GUILayout.Label("Reward Settings", EditorStyles.boldLabel);
        baseReward1Star = EditorGUILayout.IntField("Base Reward 1 Star", baseReward1Star);
        baseReward2Star = EditorGUILayout.IntField("Base Reward 2 Star", baseReward2Star);
        baseReward3Star = EditorGUILayout.IntField("Base Reward 3 Star", baseReward3Star);
        rewardIncrement = EditorGUILayout.IntField("Reward Increment Per Level", rewardIncrement);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Generate Quest Data", GUILayout.Height(40)))
        {
            GenerateQuestData();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Script này sẽ tạo quest data cho các level từ " + startLevel + " đến " + endLevel + 
            " với:\n" +
            "- Energy Points tăng dần (điểm số, không phải số lượng item)\n" +
            "- Time Limit tăng dần\n" +
            "- Reward tăng dần\n" +
            "- Không có objectives (chỉ cần nhặt đủ điểm EnergyItem)",
            MessageType.Info
        );
    }
    
    private void GenerateQuestData()
    {
        GenerateQuestDataStatic(
            startLevel, endLevel,
            baseEnergyItems, energyItemsIncrement,
            baseTimeLimit, timeLimitIncrement,
            baseReward1Star, baseReward2Star, baseReward3Star, rewardIncrement
        );
    }
    
    private static void GenerateQuestDataStatic(
        int startLevel, int endLevel,
        int baseEnergyItems, int energyItemsIncrement,
        float baseTimeLimit, float timeLimitIncrement,
        int baseReward1Star, int baseReward2Star, int baseReward3Star, int rewardIncrement)
    {
        Dictionary<int, QuestData> quests = new Dictionary<int, QuestData>();
        
        for (int level = startLevel; level <= endLevel; level++)
        {
            QuestData quest = ScriptableObject.CreateInstance<QuestData>();
            quest.questId = level;
            
            // Không có objectives (chỉ cần nhặt EnergyItem)
            quest.objectives = new QuestObjective[0];
            
            // Tính số điểm EnergyItem cần nhặt (tăng dần)
            quest.requiredEnergyPoints = baseEnergyItems + (level - 1) * energyItemsIncrement;
            
            // Tính Time Limit (tăng dần)
            quest.timeLimit = baseTimeLimit + (level - 1) * timeLimitIncrement;
            
            // Tính thời gian để đạt sao (dựa trên timeLimit)
            // 3 sao = hoàn thành trong 40% thời gian
            // 2 sao = hoàn thành trong 70% thời gian
            // 1 sao = hoàn thành trong timeLimit
            quest.timeFor3Stars = quest.timeLimit * 0.4f;
            quest.timeFor2Stars = quest.timeLimit * 0.7f;
            
            // Tính reward (tăng dần theo level)
            quest.rewardList = new List<int>
            {
                baseReward1Star + (level - 1) * rewardIncrement,
                baseReward2Star + (level - 1) * rewardIncrement,
                baseReward3Star + (level - 1) * rewardIncrement
            };
            
            quests[level] = quest;
        }
        
        // Lưu vào JSON
        QuestDataStorage.SaveAllQuests(quests);
        
        Debug.Log($"QuestDataGenerator: Đã tạo quest data cho {quests.Count} levels (từ level {startLevel} đến {endLevel})");
        
        EditorUtility.DisplayDialog(
            "Success",
            $"Đã tạo quest data cho {quests.Count} levels!\n" +
            $"File được lưu tại: {QuestDataStorage.GetQuestFilePath()}",
            "OK"
        );
    }
    
    /// <summary>
    /// Generate quest data với pattern lặp lại: tạo 15 level đầu, sau đó lặp lại pattern đó
    /// </summary>
    private static void GenerateQuestDataWithPattern(
        int startLevel, int endLevel,
        int patternLength, int baseEnergyItems, int energyItemsIncrement,
        float baseTimeLimit, float timeLimitIncrement,
        int baseReward1Star, int baseReward2Star, int baseReward3Star, int rewardIncrement)
    {
        Dictionary<int, QuestData> quests = new Dictionary<int, QuestData>();
        Dictionary<int, QuestData> patternQuests = new Dictionary<int, QuestData>();
        
        // Tạo pattern cho 15 level đầu
        for (int patternLevel = 1; patternLevel <= patternLength; patternLevel++)
        {
            QuestData quest = ScriptableObject.CreateInstance<QuestData>();
            quest.questId = patternLevel;
            quest.objectives = new QuestObjective[0];
            
            // Tính số điểm EnergyItem (tăng dần từ baseEnergyItems, tối đa 20)
            int energyPoints = baseEnergyItems + (patternLevel - 1) * energyItemsIncrement;
            quest.requiredEnergyPoints = Mathf.Min(energyPoints, 20); // Tối đa 20 điểm
            
            // Tính Time Limit (tăng dần)
            quest.timeLimit = baseTimeLimit + (patternLevel - 1) * timeLimitIncrement;
            
            // Tính thời gian để đạt sao
            quest.timeFor3Stars = quest.timeLimit * 0.4f;
            quest.timeFor2Stars = quest.timeLimit * 0.7f;
            
            // Tính reward (tăng dần theo level)
            quest.rewardList = new List<int>
            {
                baseReward1Star + (patternLevel - 1) * rewardIncrement,
                baseReward2Star + (patternLevel - 1) * rewardIncrement,
                baseReward3Star + (patternLevel - 1) * rewardIncrement
            };
            
            patternQuests[patternLevel] = quest;
        }
        
        // Tạo tất cả các level bằng cách lặp lại pattern
        for (int level = startLevel; level <= endLevel; level++)
        {
            // Tính level trong pattern (1-15)
            int patternIndex = ((level - 1) % patternLength) + 1;
            QuestData patternQuest = patternQuests[patternIndex];
            
            // Tạo quest mới với ID là level hiện tại
            QuestData quest = ScriptableObject.CreateInstance<QuestData>();
            quest.questId = level;
            quest.objectives = patternQuest.objectives;
            quest.requiredEnergyPoints = patternQuest.requiredEnergyPoints;
            quest.timeLimit = patternQuest.timeLimit;
            quest.timeFor3Stars = patternQuest.timeFor3Stars;
            quest.timeFor2Stars = patternQuest.timeFor2Stars;
            
            // Reward vẫn tăng dần theo level thực tế
            quest.rewardList = new List<int>
            {
                baseReward1Star + (level - 1) * rewardIncrement,
                baseReward2Star + (level - 1) * rewardIncrement,
                baseReward3Star + (level - 1) * rewardIncrement
            };
            
            quests[level] = quest;
        }
        
        // Lưu vào JSON
        QuestDataStorage.SaveAllQuests(quests);
        
        Debug.Log($"QuestDataGenerator: Đã tạo quest data cho {quests.Count} levels (từ level {startLevel} đến {endLevel})");
        Debug.Log($"QuestDataGenerator: Pattern lặp lại mỗi {patternLength} level, từ {baseEnergyItems} đến tối đa 20 điểm Energy");
        
        EditorUtility.DisplayDialog(
            "Success",
            $"Đã tạo quest data cho {quests.Count} levels!\n" +
            $"Pattern: {patternLength} level đầu, sau đó lặp lại\n" +
            $"Energy Points: {baseEnergyItems} → 20 (tối đa)\n" +
            $"File được lưu tại: {QuestDataStorage.GetQuestFilePath()}",
            "OK"
        );
    }
}
