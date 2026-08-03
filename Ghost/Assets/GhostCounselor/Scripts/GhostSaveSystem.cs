using System.IO;
using UnityEngine;

namespace GhostCounselor
{
    public static class GhostSaveSystem
    {
        private const string FileName = "ghost_counselor_save.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public static bool HasSave => File.Exists(SavePath);

        public static void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public static SaveData Load()
        {
            if (!HasSave)
                return new SaveData();

            try
            {
                string json = File.ReadAllText(SavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                return data ?? new SaveData();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"저장 파일을 읽지 못해 새 게임을 시작합니다: {exception.Message}");
                return new SaveData();
            }
        }

        public static void Delete()
        {
            if (HasSave)
                File.Delete(SavePath);
        }
    }
}
