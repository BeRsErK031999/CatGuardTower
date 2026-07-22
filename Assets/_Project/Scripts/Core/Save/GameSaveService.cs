using System;
using System.IO;
using UnityEngine;

namespace CatGuard.Core.Save
{
    public enum GameSaveLoadStatus
    {
        Fresh,
        Loaded,
        Migrated,
        RecoveredCorrupt,
        RecoveredFutureVersion
    }

    public static class GameSaveService
    {
        private const string FileName = "catguard-save.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public static GameSaveLoadStatus LastLoadStatus { get; private set; } = GameSaveLoadStatus.Fresh;
        public static string LastBackupPath { get; private set; } = string.Empty;
        public static string LastLoadMessage { get; private set; } = string.Empty;

        public static GameSaveData LoadOrCreate(string firstLevelId)
        {
            return LoadOrCreateAtPath(SavePath, firstLevelId, true);
        }

        public static GameSaveData LoadOrCreateAtPath(string path, string firstLevelId, bool updateDiagnostics = false)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                SetDiagnostics(updateDiagnostics, GameSaveLoadStatus.Fresh, string.Empty, "Created a fresh save.");
                return GameSaveData.CreateDefault(firstLevelId);
            }

            try
            {
                var json = File.ReadAllText(path);
                var hasExplicitSchemaVersion = json.IndexOf("\"schemaVersion\"", StringComparison.Ordinal) >= 0;
                var data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null)
                {
                    throw new InvalidDataException("Save JSON produced no data.");
                }

                if (!hasExplicitSchemaVersion)
                {
                    data.schemaVersion = 0;
                }

                if (data.schemaVersion > GameSaveMigrationService.CurrentSchemaVersion)
                {
                    var futureBackup = CreateBackup(path, $"future-v{data.schemaVersion}");
                    SetDiagnostics(
                        updateDiagnostics,
                        GameSaveLoadStatus.RecoveredFutureVersion,
                        futureBackup,
                        $"Preserved unsupported future schema {data.schemaVersion} and started a recoverable fresh save.");
                    Debug.LogError($"Unsupported future save schema {data.schemaVersion}. Backup: {futureBackup}");
                    return GameSaveData.CreateDefault(firstLevelId);
                }

                var sourceVersion = data.schemaVersion;
                var migrationBackup = string.Empty;
                if (sourceVersion < GameSaveMigrationService.CurrentSchemaVersion)
                {
                    migrationBackup = CreateBackup(path, $"v{sourceVersion}-premigration");
                }

                if (!GameSaveMigrationService.TryMigrate(data, firstLevelId, out var changed, out var migrationError))
                {
                    throw new InvalidDataException(migrationError);
                }

                var status = changed ? GameSaveLoadStatus.Migrated : GameSaveLoadStatus.Loaded;
                var message = changed
                    ? $"Migrated save schema {sourceVersion} to {GameSaveMigrationService.CurrentSchemaVersion}."
                    : $"Loaded save schema {data.schemaVersion}.";
                SetDiagnostics(updateDiagnostics, status, migrationBackup, message);
                return data;
            }
            catch (Exception exception)
            {
                var corruptBackup = CreateBackup(path, "corrupt");
                SetDiagnostics(
                    updateDiagnostics,
                    GameSaveLoadStatus.RecoveredCorrupt,
                    corruptBackup,
                    $"Preserved unreadable save and started a recoverable fresh save: {exception.Message}");
                Debug.LogWarning($"Failed to read save file. Backup: {corruptBackup}. {exception.Message}");
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
            SaveAtPath(SavePath, data);
        }

        public static void SaveAtPath(string path, GameSaveData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            data.schemaVersion = GameSaveMigrationService.CurrentSchemaVersion;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonUtility.ToJson(data, true);
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }

        private static string CreateBackup(string path, string label)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return string.Empty;
            }

            var backupPath = $"{path}.{label}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.bak";
            File.Copy(path, backupPath, false);
            return backupPath;
        }

        private static void SetDiagnostics(
            bool updateDiagnostics,
            GameSaveLoadStatus status,
            string backupPath,
            string message)
        {
            if (!updateDiagnostics)
            {
                return;
            }

            LastLoadStatus = status;
            LastBackupPath = backupPath ?? string.Empty;
            LastLoadMessage = message ?? string.Empty;
        }
    }
}
