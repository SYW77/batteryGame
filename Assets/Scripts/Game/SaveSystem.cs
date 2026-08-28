using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BatteryGame
{
    /// <summary>건전지 한 개의 저장 상태.</summary>
    [Serializable]
    public class BatterySave
    {
        public int size;
        public float charge;
        public bool inspected;

        /// <summary>꽂혀 있는 기기의 고유 키. 비어 있으면 더미/바닥에 놓인 상태.</summary>
        public string deviceKey = "";

        public Vector3 position;
        public Vector3 euler;
    }

    /// <summary>기기 한 대의 저장 상태.</summary>
    [Serializable]
    public class DeviceSave
    {
        public string key;
        public bool submitted;
    }

    [Serializable]
    public class SaveData
    {
        // v1은 기기를 배열 인덱스로 식별해서, 기기 목록 순서가 바뀌면 세이브가 어긋났다.
        // v2부터는 고유 키로 식별한다. 버전이 다르면 Load가 null을 돌려주고 이어하기가 사라진다.
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public float elapsedSeconds;

        public int totalBatteries;
        public int resolvedBatteries;
        public int submittedDevices;

        public List<DeviceSave> devices = new List<DeviceSave>();
        public List<BatterySave> batteries = new List<BatterySave>();

        public Vector3 playerPosition;
        public float playerYaw;
        public float cameraPitch;
    }

    /// <summary>진행 중인 게임을 JSON 한 파일로 저장한다.</summary>
    public static class SaveSystem
    {
        const string FileName = "battery_save.json";

        public static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        public static bool HasSave()
        {
            try { return File.Exists(FilePath) && new FileInfo(FilePath).Length > 2; }
            catch (Exception) { return false; }
        }

        public static void Save(SaveData data)
        {
            try
            {
                data.version = SaveData.CurrentVersion;
                File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BatteryGame] 저장 실패: " + e.Message);
            }
        }

        /// <summary>읽지 못하거나 버전이 다르면 null.</summary>
        public static SaveData Load()
        {
            if (!HasSave()) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                if (data == null || data.version != SaveData.CurrentVersion) return null;
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BatteryGame] 세이브 읽기 실패: " + e.Message);
                return null;
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogWarning("[BatteryGame] 세이브 삭제 실패: " + e.Message); }
        }
    }
}
