/*
 * 파일 역할: 게임 진행 데이터를 디스크에 저장하고 다시 불러온다.
 * - SavePath/HasSave: Unity persistentDataPath 안의 저장 파일 위치와 존재 여부를 제공한다.
 * - Save(): SaveData를 JSON으로 변환해 저장한다.
 * - Load(): 저장 파일을 읽고, 파일이 없거나 손상되면 새 SaveData를 반환한다.
 * - Delete(): 저장 파일을 삭제해 새 게임 상태로 되돌린다.
 * 날짜, 돈, 귀신별 관계/방문 횟수, 물건, 업적, 본 대사가 이 경로를 사용한다.
 */
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
