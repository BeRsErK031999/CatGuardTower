using System;
using System.IO;
using UnityEngine;

namespace CatGuard.Core.Save
{
    public static class GameSaveService
    {
        private const string FileName = "catguard-save.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static GameSaveData LoadOrCreate(string firstLevelId)
        {
            if (!File.Exists(SavePath))
            {
                return GameSaveData.CreateDefault(firstLevelId);
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<GameSaveData>(json);
                return data ?? GameSaveData.CreateDefault(firstLevelId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read save file, starting with a new save. {exception.Message}");
                return GameSaveData.CreateDefault(firstLevelId);
            }
        }

        public static void Save(GameSaveData data)
        {
            if (data == null)
            {
                return;
            }

            Directory.CreateDirectory(Application.persistentDataPath);
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
    }
}
