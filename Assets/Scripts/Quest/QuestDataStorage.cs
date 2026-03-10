using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Class để lưu trữ và load QuestData từ JSON
/// </summary>
public static class QuestDataStorage
{
    private const string QuestFileName = "quests.json";
    
    /// <summary>
    /// Đường dẫn đến file quests.json trong StreamingAssets (ưu tiên)
    /// </summary>
    private static string StreamingAssetsPath => Path.Combine(Application.streamingAssetsPath, QuestFileName);
    
    /// <summary>
    /// Đường dẫn đến file quests.json trong persistentDataPath (backup)
    /// </summary>
    private static string PersistentDataPath => Path.Combine(Application.persistentDataPath, QuestFileName);
    
    /// <summary>
    /// Lấy đường dẫn file quests.json (ưu tiên StreamingAssets, sau đó persistentDataPath)
    /// </summary>
    private static string QuestFilePath
    {
        get
        {
            // Ưu tiên đọc từ StreamingAssets (có thể đọc được trong Editor và Build)
            if (File.Exists(StreamingAssetsPath))
            {
                return StreamingAssetsPath;
            }
            // Nếu không có trong StreamingAssets, dùng persistentDataPath
            return PersistentDataPath;
        }
    }
    
    /// <summary>
    /// Public property để Editor script có thể truy cập
    /// </summary>
    public static string GetQuestFilePath() => QuestFilePath;
    
    /// <summary>
    /// Load tất cả quest từ JSON file
    /// </summary>
    public static Dictionary<int, QuestData> LoadAllQuests()
    {
        Dictionary<int, QuestData> quests = new Dictionary<int, QuestData>();
        
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file {QuestFilePath}!");
            return quests;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            if (!string.IsNullOrEmpty(json))
            {
                QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
                if (questList != null && questList.quests != null)
                {
                    foreach (var questJson in questList.quests)
                    {
                        QuestData questData = questJson.ToQuestData();
                        if (questData != null)
                        {
                            quests[questData.questId] = questData;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi load quest từ JSON: {ex.Message}");
        }
        
        return quests;
    }
    
    /// <summary>
    /// Load một quest cụ thể theo ID
    /// </summary>
    public static QuestData LoadQuest(int questId)
    {
        Dictionary<int, QuestData> allQuests = LoadAllQuests();
        if (allQuests.ContainsKey(questId))
        {
            return allQuests[questId];
        }
        
        Debug.LogWarning($"QuestDataStorage: Không tìm thấy quest với ID: {questId}");
        return null;
    }
    
    /// <summary>
    /// Lưu tất cả quest vào JSON file
    /// Lưu vào persistentDataPath (có thể ghi được) và copy vào StreamingAssets nếu có thể
    /// </summary>
    public static void SaveAllQuests(Dictionary<int, QuestData> quests)
    {
        if (quests == null || quests.Count == 0)
        {
            Debug.LogWarning("QuestDataStorage: Không có quest nào để lưu!");
            return;
        }
        
        try
        {
            QuestDataList questList = new QuestDataList();
            questList.quests = new List<QuestDataJSON>();
            
            foreach (var quest in quests.Values)
            {
                questList.quests.Add(new QuestDataJSON(quest));
            }
            
            string json = JsonUtility.ToJson(questList, true);
            
            // Lưu vào persistentDataPath (luôn có thể ghi)
            File.WriteAllText(PersistentDataPath, json);
            Debug.Log($"QuestDataStorage: Đã lưu {quests.Count} quest vào {PersistentDataPath}");
            
            // Cố gắng copy vào StreamingAssets nếu có thể (chỉ trong Editor)
            #if UNITY_EDITOR
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            File.WriteAllText(StreamingAssetsPath, json);
            Debug.Log($"QuestDataStorage: Đã copy quest vào StreamingAssets: {StreamingAssetsPath}");
            #endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi lưu quest vào JSON: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Lưu một quest cụ thể
    /// </summary>
    public static void SaveQuest(QuestData questData)
    {
        if (questData == null)
        {
            Debug.LogWarning("QuestDataStorage: QuestData là null!");
            return;
        }
        
        Dictionary<int, QuestData> allQuests = LoadAllQuests();
        allQuests[questData.questId] = questData;
        SaveAllQuests(allQuests);
    }
    
    /// <summary>
    /// Lưu kết quả sao cho một quest và unlock quest tiếp theo
    /// </summary>
    public static void SaveQuestStars(int questId, int stars)
    {
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để lưu stars cho quest {questId}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                bool updated = false;
                
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        // Chỉ cập nhật nếu số sao mới cao hơn
                        if (stars > questJson.stars)
                        {
                            questJson.stars = stars;
                            updated = true;
                            Debug.Log($"QuestDataStorage: Đã lưu {stars} sao cho quest {questId}");
                        }
                    }
                    
                    // Unlock quest tiếp theo nếu quest hiện tại đã hoàn thành
                    if (questJson.questId == questId + 1 && questJson.isLocked)
                    {
                        questJson.isLocked = false;
                        updated = true;
                        Debug.Log($"QuestDataStorage: Đã unlock quest {questId + 1}");
                    }
                }
                
                if (updated)
                {
                    // Lưu lại file
                    string updatedJson = JsonUtility.ToJson(questList, true);
                    File.WriteAllText(QuestFilePath, updatedJson);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi lưu stars: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Lấy kết quả sao của một quest
    /// </summary>
    public static int GetQuestStars(int questId)
    {
        if (!File.Exists(QuestFilePath))
        {
            return 0;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        return questJson.stars;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi load stars từ JSON: {ex.Message}");
        }
        
        return 0;
    }
    
    /// <summary>
    /// Lấy trạng thái locked của một quest
    /// </summary>
    public static bool IsQuestLocked(int questId)
    {
        if (!File.Exists(QuestFilePath))
        {
            // Quest đầu tiên không locked, các quest khác locked mặc định
            return questId != 1;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        return questJson.isLocked;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi load isLocked từ JSON: {ex.Message}");
        }
        
        // Fallback: Quest đầu tiên không locked, các quest khác locked
        return questId != 1;
    }
    
    /// <summary>
    /// Unlock một quest
    /// </summary>
    public static void UnlockQuest(int questId)
    {
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để unlock quest {questId}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        if (questJson.isLocked)
                        {
                            questJson.isLocked = false;
                            Debug.Log($"QuestDataStorage: Đã unlock quest {questId}");
                            
                            // Lưu lại file
                            string updatedJson = JsonUtility.ToJson(questList, true);
                            File.WriteAllText(QuestFilePath, updatedJson);
                        }
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi unlock quest: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Unlock tất cả các quest (dùng cho cheat code F1)
    /// </summary>
    public static void UnlockAllQuests()
    {
        // Lấy đường dẫn file hiện tại (ưu tiên StreamingAssets)
        string filePath = QuestFilePath;
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để unlock tất cả quest tại {filePath}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(filePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                bool updated = false;
                int unlockedCount = 0;
                
                foreach (var questJson in questList.quests)
                {
                    if (questJson.isLocked)
                    {
                        questJson.isLocked = false;
                        updated = true;
                        unlockedCount++;
                    }
                }
                
                if (updated)
                {
                    // Lưu lại file vào cả hai vị trí để đảm bảo đồng bộ
                    string updatedJson = JsonUtility.ToJson(questList, true);
                    
                    // Lưu vào file gốc
                    File.WriteAllText(filePath, updatedJson);
                    
                    // Nếu đọc từ StreamingAssets, cũng lưu vào persistentDataPath
                    if (filePath == StreamingAssetsPath)
                    {
                        File.WriteAllText(PersistentDataPath, updatedJson);
                    }
                    // Nếu đọc từ persistentDataPath, cũng copy vào StreamingAssets (nếu có thể)
                    else if (filePath == PersistentDataPath)
                    {
                        #if UNITY_EDITOR
                        if (!Directory.Exists(Application.streamingAssetsPath))
                        {
                            Directory.CreateDirectory(Application.streamingAssetsPath);
                        }
                        File.WriteAllText(StreamingAssetsPath, updatedJson);
                        #endif
                    }
                    
                    Debug.Log($"QuestDataStorage: Đã unlock {unlockedCount} quest!");
                }
                else
                {
                    Debug.Log("QuestDataStorage: Tất cả quest đã được unlock rồi!");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi unlock tất cả quest: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Reset tất cả quest về trạng thái ban đầu (chỉ level 1 unlock, các level khác locked, stars = 0)
    /// </summary>
    public static void ResetAllQuests()
    {
        // Lấy đường dẫn file hiện tại (ưu tiên StreamingAssets)
        string filePath = QuestFilePath;
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để reset quest tại {filePath}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(filePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                bool updated = false;
                int resetCount = 0;
                
                foreach (var questJson in questList.quests)
                {
                    bool needsUpdate = false;
                    
                    // Reset locked status: chỉ level 1 unlock, các level khác locked
                    if (questJson.questId == 1)
                    {
                        if (questJson.isLocked)
                        {
                            questJson.isLocked = false;
                            needsUpdate = true;
                        }
                    }
                    else
                    {
                        if (!questJson.isLocked)
                        {
                            questJson.isLocked = true;
                            needsUpdate = true;
                        }
                    }
                    
                    // Reset stars về 0
                    if (questJson.stars != 0)
                    {
                        questJson.stars = 0;
                        needsUpdate = true;
                    }
                    
                    if (needsUpdate)
                    {
                        updated = true;
                        resetCount++;
                    }
                }
                
                if (updated)
                {
                    // Lưu lại file vào cả hai vị trí để đảm bảo đồng bộ
                    string updatedJson = JsonUtility.ToJson(questList, true);
                    
                    // Lưu vào file gốc
                    File.WriteAllText(filePath, updatedJson);
                    
                    // Nếu đọc từ StreamingAssets, cũng lưu vào persistentDataPath
                    if (filePath == StreamingAssetsPath)
                    {
                        File.WriteAllText(PersistentDataPath, updatedJson);
                    }
                    // Nếu đọc từ persistentDataPath, cũng copy vào StreamingAssets (nếu có thể)
                    else if (filePath == PersistentDataPath)
                    {
                        #if UNITY_EDITOR
                        if (!Directory.Exists(Application.streamingAssetsPath))
                        {
                            Directory.CreateDirectory(Application.streamingAssetsPath);
                        }
                        File.WriteAllText(StreamingAssetsPath, updatedJson);
                        #endif
                    }
                    
                    Debug.Log($"QuestDataStorage: Đã reset {resetCount} quest về trạng thái ban đầu!");
                }
                else
                {
                    Debug.Log("QuestDataStorage: Tất cả quest đã ở trạng thái ban đầu rồi!");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi reset tất cả quest: {ex.Message}");
        }
    }
}

/// <summary>
/// Class JSON serializable cho QuestData
/// </summary>
[Serializable]
public class QuestDataJSON
{
    public int questId;
    public QuestObjective[] objectives;
    public float timeFor3Stars;
    public float timeFor2Stars;
    public float timeLimit;
    public int requiredEnergyPoints;
    public int[] rewardList;
    public int stars = 0; // Kết quả sao đạt được (0 = chưa hoàn thành, 1-3 = số sao)
    public bool isLocked = true; // Trạng thái locked (true = bị khóa, false = đã unlock)
    
    public QuestDataJSON() { }
    
    public QuestDataJSON(QuestData questData)
    {
        if (questData == null) return;
        
        questId = questData.questId;
        objectives = questData.objectives;
        timeFor3Stars = questData.timeFor3Stars;
        timeFor2Stars = questData.timeFor2Stars;
        timeLimit = questData.timeLimit;
        requiredEnergyPoints = questData.requiredEnergyPoints;
        rewardList = questData.rewardList != null ? questData.rewardList.ToArray() : new int[] { 50, 100, 150 };
        stars = 0; // Mặc định chưa có sao
        isLocked = questId != 1; // Quest đầu tiên không locked, các quest khác locked mặc định
    }
    
    public QuestData ToQuestData()
    {
        QuestData questData = ScriptableObject.CreateInstance<QuestData>();
        questData.questId = questId;
        questData.objectives = objectives;
        questData.timeFor3Stars = timeFor3Stars;
        questData.timeFor2Stars = timeFor2Stars;
        questData.timeLimit = timeLimit;
        questData.requiredEnergyPoints = requiredEnergyPoints;
        questData.rewardList = rewardList != null ? new List<int>(rewardList) : new List<int> { 50, 100, 150 };
        return questData;
    }
}

/// <summary>
/// Wrapper class để serialize list quest
/// </summary>
[Serializable]
public class QuestDataList
{
    public List<QuestDataJSON> quests;
}

