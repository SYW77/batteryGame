using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BatteryGame.EditorTools
{
    /// <summary>
    /// 씬 전체(방 / 작업대 / 기기 / 제출대 / 플레이어 / HUD)를 코드로 생성한다.
    /// 레이아웃을 바꾸려면 이 파일의 상수와 Devices 배열을 고치고 메뉴를 다시 실행하면 된다.
    /// 주의: 실행하면 현재 열린 씬의 루트 오브젝트를 전부 지우고 다시 만든다.
    /// </summary>
    public static class LevelBuilder
    {
        public const string ScenePath = "Assets/Scenes/Workshop.unity";

        const float RoomX = 14f;
        const float RoomZ = 12f;
        const float WallH = 3.2f;
        const float WallT = 0.25f;
        const float TableTopY = 0.92f;

        /// <summary>
        /// 쓰레기통으로 가야 하는 방전 건전지 수. 더미의 부피와 플레이 타임을 결정하는 주 손잡이다.
        /// 기기들이 요구하는 정상 건전지(현재 41개)가 여기에 더해진다.
        /// </summary>
        const int DeadBatteryCount = 60;

        /// <summary>
        /// 더미가 쌓이는 영역. 통이 없으므로 작업대 상판보다 충분히 작아야
        /// 무너지면서 퍼져도 바닥으로 굴러떨어지지 않는다.
        /// </summary>
        static readonly Vector3 PileArea = new Vector3(0.8f, 0.4f, 0.55f);

        /// <summary>건전지 작업대 상판 크기. 더미가 퍼질 공간을 넉넉히 준다.</summary>
        const float BatteryTableX = 2.4f;
        const float BatteryTableZ = 2.0f;

        /// <summary>
        /// 전자기기 작업대. 기기를 여기에 나눠 흩어 놓는다.
        /// 기기가 늘면 자리가 촘촘해질 뿐이니, 많이 늘릴 때는 작업대를 추가하면 된다.
        /// </summary>
        struct StationDef
        {
            public Vector3 Center;
            public float SizeX;
            public float SizeZ;
            public float Yaw;

            public StationDef(Vector3 center, float sizeX, float sizeZ, float yaw)
            {
                Center = center; SizeX = sizeX; SizeZ = sizeZ; Yaw = yaw;
            }
        }

        static readonly StationDef[] DeviceStations =
        {
            new StationDef(new Vector3(4.0f, 0f, 2.3f), 3.1f, 1.8f, -28f),
            new StationDef(new Vector3(4.4f, 0f, -1.4f), 3.1f, 1.8f, -68f),
            new StationDef(new Vector3(-4.2f, 0f, -1.9f), 3.1f, 1.8f, 62f),
        };

        /// <summary>기기 흩뿌리기를 매번 같은 모습으로 만들기 위한 고정 시드.</summary>
        const int DeviceScatterSeed = 20260827;

        struct DeviceDef
        {
            /// <summary>세이브가 기기를 식별하는 고유 키. 표시 이름이나 배열 순서와 무관해야 하므로
            /// 한 번 정하면 바꾸지 말 것. 바꾸면 기존 세이브에서 그 기기가 사라진 것으로 취급된다.</summary>
            public string Key;

            public string Name;
            public BatterySize Size;
            public int Count;
            public Vector3 Body;
            public Color Color;

            public DeviceDef(string key, string name, BatterySize size, int count, Vector3 body, Color color)
            {
                Key = key; Name = name; Size = size; Count = count; Body = body; Color = color;
            }
        }

        static readonly DeviceDef[] Devices =
        {
            new DeviceDef("tv_remote", "TV 리모컨", BatterySize.AAA, 2, new Vector3(0.055f, 0.030f, 0.20f), new Color(0.16f, 0.17f, 0.20f)),
            new DeviceDef("wall_clock", "벽시계", BatterySize.AA, 1, new Vector3(0.24f, 0.055f, 0.24f), new Color(0.90f, 0.88f, 0.82f)),
            new DeviceDef("wireless_mouse", "무선 마우스", BatterySize.AA, 2, new Vector3(0.070f, 0.040f, 0.115f), new Color(0.30f, 0.33f, 0.38f)),
            new DeviceDef("flashlight", "손전등", BatterySize.D, 2, new Vector3(0.075f, 0.075f, 0.27f), new Color(0.85f, 0.55f, 0.10f)),
            new DeviceDef("desk_radio", "탁상 라디오", BatterySize.C, 2, new Vector3(0.26f, 0.15f, 0.11f), new Color(0.45f, 0.28f, 0.18f)),
            new DeviceDef("toy_robot", "장난감 로봇", BatterySize.AA, 4, new Vector3(0.13f, 0.21f, 0.095f), new Color(0.75f, 0.20f, 0.22f)),
            new DeviceDef("fire_alarm", "화재경보기", BatterySize.NineVolt, 1, new Vector3(0.17f, 0.055f, 0.17f), new Color(0.94f, 0.94f, 0.90f)),
            new DeviceDef("game_controller", "게임 컨트롤러", BatterySize.AAA, 3, new Vector3(0.17f, 0.050f, 0.12f), new Color(0.22f, 0.26f, 0.45f)),
            new DeviceDef("wireless_keyboard", "무선 키보드", BatterySize.AA, 2, new Vector3(0.34f, 0.030f, 0.13f), new Color(0.24f, 0.25f, 0.28f)),
            new DeviceDef("desk_clock", "탁상 시계", BatterySize.AAA, 2, new Vector3(0.15f, 0.11f, 0.07f), new Color(0.72f, 0.60f, 0.38f)),
            new DeviceDef("handy_fan", "휴대용 선풍기", BatterySize.AA, 2, new Vector3(0.13f, 0.16f, 0.08f), new Color(0.35f, 0.75f, 0.80f)),
            new DeviceDef("body_scale", "체중계", BatterySize.AAA, 4, new Vector3(0.30f, 0.040f, 0.26f), new Color(0.86f, 0.87f, 0.89f)),
            new DeviceDef("walkie_talkie", "무전기", BatterySize.AA, 3, new Vector3(0.09f, 0.20f, 0.05f), new Color(0.18f, 0.30f, 0.18f)),
            new DeviceDef("electric_toothbrush", "전동 칫솔", BatterySize.AA, 1, new Vector3(0.045f, 0.19f, 0.045f), new Color(0.92f, 0.92f, 0.95f)),
            new DeviceDef("metronome", "메트로놈", BatterySize.NineVolt, 1, new Vector3(0.13f, 0.19f, 0.11f), new Color(0.55f, 0.22f, 0.16f)),
            new DeviceDef("camping_lantern", "캠핑 랜턴", BatterySize.D, 3, new Vector3(0.17f, 0.22f, 0.17f), new Color(0.20f, 0.55f, 0.35f)),
            new DeviceDef("kitchen_scale", "전자 저울", BatterySize.AAA, 2, new Vector3(0.22f, 0.045f, 0.17f), new Color(0.30f, 0.32f, 0.36f)),
            new DeviceDef("rc_car", "리모컨 자동차", BatterySize.C, 4, new Vector3(0.26f, 0.10f, 0.13f), new Color(0.88f, 0.30f, 0.15f)),
            new DeviceDef("piano_keyboard", "전자 키보드", BatterySize.AA, 4, new Vector3(0.38f, 0.045f, 0.14f), new Color(0.18f, 0.19f, 0.23f)),
            new DeviceDef("wall_thermometer", "벽걸이 온도계", BatterySize.AAA, 1, new Vector3(0.11f, 0.16f, 0.035f), new Color(0.88f, 0.89f, 0.86f)),
            new DeviceDef("wireless_doorbell", "무선 도어벨", BatterySize.C, 2, new Vector3(0.14f, 0.075f, 0.14f), new Color(0.75f, 0.72f, 0.64f)),
            new DeviceDef("headlamp", "헤드램프", BatterySize.AAA, 3, new Vector3(0.13f, 0.06f, 0.075f), new Color(0.16f, 0.42f, 0.30f)),
            new DeviceDef("led_stand", "LED 스탠드", BatterySize.D, 2, new Vector3(0.16f, 0.30f, 0.16f), new Color(0.92f, 0.90f, 0.84f)),
            new DeviceDef("calculator", "계산기", BatterySize.AAA, 2, new Vector3(0.11f, 0.025f, 0.17f), new Color(0.26f, 0.28f, 0.32f)),
            new DeviceDef("blanket_controller", "전기요 조절기", BatterySize.AA, 2, new Vector3(0.09f, 0.05f, 0.14f), new Color(0.70f, 0.35f, 0.45f)),
            new DeviceDef("mini_fan", "미니 선풍기", BatterySize.AA, 1, new Vector3(0.10f, 0.13f, 0.07f), new Color(0.45f, 0.78f, 0.60f)),
            new DeviceDef("radio_clock", "라디오 시계", BatterySize.C, 3, new Vector3(0.24f, 0.11f, 0.13f), new Color(0.35f, 0.30f, 0.55f)),
            new DeviceDef("portable_radio", "휴대 무전기", BatterySize.AA, 3, new Vector3(0.085f, 0.19f, 0.05f), new Color(0.30f, 0.30f, 0.14f)),
            new DeviceDef("smoke_detector", "연기 감지기", BatterySize.NineVolt, 1, new Vector3(0.15f, 0.05f, 0.15f), new Color(0.90f, 0.90f, 0.92f)),
            new DeviceDef("door_lock", "전자 도어락", BatterySize.AA, 4, new Vector3(0.11f, 0.26f, 0.06f), new Color(0.32f, 0.34f, 0.38f)),
            new DeviceDef("hand_massager", "손 마사지기", BatterySize.AAA, 2, new Vector3(0.09f, 0.09f, 0.15f), new Color(0.80f, 0.55f, 0.72f)),
            new DeviceDef("small_lantern", "소형 랜턴", BatterySize.D, 1, new Vector3(0.11f, 0.17f, 0.11f), new Color(0.85f, 0.75f, 0.25f)),
            new DeviceDef("power_driver", "전동 드라이버", BatterySize.C, 4, new Vector3(0.075f, 0.20f, 0.075f), new Color(0.85f, 0.45f, 0.10f)),
            new DeviceDef("thermometer", "체온계", BatterySize.AAA, 1, new Vector3(0.03f, 0.02f, 0.13f), new Color(0.95f, 0.95f, 0.97f)),
            new DeviceDef("emergency_light", "비상 손전등", BatterySize.D, 3, new Vector3(0.09f, 0.09f, 0.30f), new Color(0.80f, 0.20f, 0.18f)),
            new DeviceDef("guitar_tuner", "기타 튜너", BatterySize.NineVolt, 1, new Vector3(0.10f, 0.035f, 0.07f), new Color(0.20f, 0.55f, 0.62f)),
        };

        [MenuItem("BatteryGame/Build Level")]
        public static void BuildLevel()
        {
            Scene scene = SceneManager.GetActiveScene();
            string path = scene.path;
            bool safe = string.IsNullOrEmpty(path) || path == ScenePath || path.EndsWith("SampleScene.unity");
            if (!safe &&
                !EditorUtility.DisplayDialog("Build Level",
                    "현재 씬(" + path + ")의 모든 오브젝트를 지우고 다시 만듭니다. 계속할까요?", "계속", "취소"))
                return;

            foreach (var go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);

            Battery[] batteryPrefabs = BatteryPrefabFactory.BuildAll();

            BuildEnvironment();
            Transform pileOrigin = BuildBatteryStation();
            BuildDeviceStation();
            BuildTrashCan();
            SubmitCounter counter = BuildSubmitCounter();
            ClearButton clearButton = BuildClearButton();
            PlayerInteractor player = BuildPlayer();
            HUD hud = BuildHud(player);

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gmGo.AddComponent<UIFontApplier>();
            gm.BindSpawnData(batteryPrefabs, pileOrigin, PileArea, DeadBatteryCount);

            GameFlow flow = BuildMenus(gm, player, hud, counter, clearButton);
            clearButton.gameObject.SetActive(false);

            EditorUtility.SetDirty(gm);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(counter);
            EditorUtility.SetDirty(flow);

            MatLib.EnsureFolder("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            RegisterInBuildSettings();

            int charged = 0;
            for (int i = 0; i < Devices.Length; i++) charged += Devices[i].Count;
            Debug.Log("[BatteryGame] 레벨 생성 완료: " + ScenePath +
                      "  |  기기 " + Devices.Length + "종" +
                      "  |  건전지 " + (charged + DeadBatteryCount) +
                      "개 (정상 " + charged + " + 방전 " + DeadBatteryCount + ")");
        }

        static void RegisterInBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ---------------------------------------------------------------- 환경

        static void BuildEnvironment()
        {
            Material floorMat = MatLib.Get("Env_Floor", new Color(0.26f, 0.27f, 0.30f), 0.15f);
            Material wallMat = MatLib.Get("Env_Wall", new Color(0.46f, 0.45f, 0.43f), 0.08f);
            Material ceilMat = MatLib.Get("Env_Ceiling", new Color(0.28f, 0.28f, 0.31f), 0.05f);

            var root = new GameObject("Environment").transform;

            Prim.Box("Floor", root, new Vector3(0f, -0.1f, 0f), new Vector3(RoomX, 0.2f, RoomZ), floorMat, true);
            Prim.Box("Ceiling", root, new Vector3(0f, WallH + 0.1f, 0f), new Vector3(RoomX, 0.2f, RoomZ), ceilMat);

            float hx = RoomX * 0.5f + WallT * 0.5f;
            float hz = RoomZ * 0.5f + WallT * 0.5f;
            Prim.Box("Wall_North", root, new Vector3(0f, WallH * 0.5f, hz), new Vector3(RoomX + WallT * 2f, WallH, WallT), wallMat, true);
            Prim.Box("Wall_South", root, new Vector3(0f, WallH * 0.5f, -hz), new Vector3(RoomX + WallT * 2f, WallH, WallT), wallMat, true);
            Prim.Box("Wall_East", root, new Vector3(hx, WallH * 0.5f, 0f), new Vector3(WallT, WallH, RoomZ + WallT * 2f), wallMat, true);
            Prim.Box("Wall_West", root, new Vector3(-hx, WallH * 0.5f, 0f), new Vector3(WallT, WallH, RoomZ + WallT * 2f), wallMat, true);

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            sunGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.45f;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.shadows = LightShadows.Soft;

            AddCeilingLamp(root, new Vector3(-3.6f, WallH - 0.3f, 1.6f));
            AddCeilingLamp(root, new Vector3(3.8f, WallH - 0.3f, 1.6f));
            AddCeilingLamp(root, new Vector3(0f, WallH - 0.3f, -2.2f));
            AddCeilingLamp(root, new Vector3(0f, WallH - 0.3f, 4.2f));

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.31f, 0.36f);
            RenderSettings.ambientEquatorColor = new Color(0.24f, 0.24f, 0.27f);
            RenderSettings.ambientGroundColor = new Color(0.13f, 0.13f, 0.15f);
            RenderSettings.fog = false;
        }

        static void AddCeilingLamp(Transform root, Vector3 pos)
        {
            Material lampMat = MatLib.Get("Env_Lamp", new Color(1f, 0.98f, 0.92f), 0.5f, 0f,
                                          new Color(1f, 0.96f, 0.85f) * 0.45f);
            var go = Prim.Box("CeilingLamp", root, pos, new Vector3(1.1f, 0.06f, 0.35f), lampMat);

            // 포인트 라이트를 쓰면 바로 위 천장이 하얗게 타버린다.
            // 아래로 향하는 넓은 스포트라이트를 쓰면 천장에는 빛이 닿지 않는다.
            var lightGo = Prim.Empty("Light", go.transform, new Vector3(0f, -1.5f, 0f));
            lightGo.transform.localScale = new Vector3(1f / 1.1f, 1f / 0.06f, 1f / 0.35f);
            lightGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 9f;
            l.spotAngle = 125f;
            l.innerSpotAngle = 55f;
            l.intensity = 4.5f;
            l.color = new Color(1f, 0.96f, 0.88f);
            l.shadows = LightShadows.None;
        }

        static Transform BuildTable(string name, Transform parent, Vector3 center, float sizeX, float sizeZ,
                                    float yawDegrees = 0f)
        {
            Material topMat = MatLib.Get("Prop_TableTop", new Color(0.62f, 0.46f, 0.32f), 0.3f);
            Material legMat = MatLib.Get("Prop_TableLeg", new Color(0.25f, 0.26f, 0.28f), 0.4f, 0.6f);

            var t = Prim.Empty(name, parent, center).transform;
            t.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            Prim.Box("Top", t, new Vector3(0f, TableTopY - 0.04f, 0f), new Vector3(sizeX, 0.08f, sizeZ), topMat, true);

            float lx = sizeX * 0.5f - 0.09f;
            float lz = sizeZ * 0.5f - 0.09f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -lx : lx;
                float z = (i < 2) ? -lz : lz;
                Prim.Cylinder("Leg" + i, t, new Vector3(x, (TableTopY - 0.08f) * 0.5f, z), 0.07f, TableTopY - 0.08f, legMat);
            }
            return t;
        }

        // ---------------------------------------------------------------- 건전지 더미

        static Transform BuildBatteryStation()
        {
            var root = new GameObject("BatteryStation").transform;
            // 통이 없어 더미가 퍼지므로 상판에 여유를 크게 둔다.
            Transform table = BuildTable("Table", root, new Vector3(-3.6f, 0f, 1.6f),
                                         BatteryTableX, BatteryTableZ, 25f);

            // 통 없이 작업대 상판 위에 그대로 쌓는다.
            var pileOrigin = Prim.Empty("PileOrigin", table, new Vector3(0f, TableTopY + 0.005f, 0f)).transform;

            AddSign(table, new Vector3(0f, TableTopY + 0.62f, -BatteryTableZ * 0.5f + 0.15f),
                    "건전지 더미", new Color(1f, 0.9f, 0.55f));
            return pileOrigin;
        }

        // ---------------------------------------------------------------- 전자기기

        static void BuildDeviceStation()
        {
            var root = new GameObject("DeviceStation").transform;
            var rng = new System.Random(DeviceScatterSeed);

            int perStation = Mathf.CeilToInt(Devices.Length / (float)DeviceStations.Length);
            int index = 0;

            for (int s = 0; s < DeviceStations.Length && index < Devices.Length; s++)
            {
                StationDef station = DeviceStations[s];
                Transform table = BuildTable("Table" + (s + 1), root, station.Center,
                                             station.SizeX, station.SizeZ, station.Yaw);

                int count = Mathf.Min(perStation, Devices.Length - index);
                ScatterDevices(table, station, index, count, rng);
                index += count;

                AddSign(table, new Vector3(0f, TableTopY + 0.78f, -station.SizeZ * 0.5f + 0.12f),
                        "전자기기", new Color(0.6f, 0.9f, 1f));
            }
        }

        /// <summary>
        /// 격자에 올리되 위치와 각도를 흔들어 "대충 올려둔" 모습으로 만든다.
        /// 완전 무작위로 뿌리면 겹치므로, 칸은 유지하고 칸 안에서만 흔든다.
        /// </summary>
        static void ScatterDevices(Transform table, StationDef station, int startIndex, int count,
                                   System.Random rng)
        {
            int cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count * station.SizeX / station.SizeZ)));
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)cols));

            float stepX = (station.SizeX - 0.34f) / cols;
            float stepZ = (station.SizeZ - 0.34f) / rows;
            float jitterX = stepX * 0.16f;
            float jitterZ = stepZ * 0.16f;

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;

                float x = (col - (cols - 1) * 0.5f) * stepX + (float)(rng.NextDouble() - 0.5) * 2f * jitterX;
                float z = ((rows - 1) * 0.5f - row) * stepZ + (float)(rng.NextDouble() - 0.5) * 2f * jitterZ;
                float yaw = (float)rng.NextDouble() * 360f;

                BuildDevice(Devices[startIndex + i], table, new Vector3(x, TableTopY, z), yaw);
            }
        }

        static void BuildDevice(DeviceDef def, Transform parent, Vector3 tablePos, float yaw)
        {
            float bw = BatterySizes.Width(def.Size);
            float bh = BatterySizes.Height(def.Size);
            float spacing = bw * 1.18f;

            // 요구 규격/개수가 실제로 들어갈 만큼 몸통 크기를 보정한다.
            Vector3 body = def.Body;
            body.x = Mathf.Max(body.x, def.Count * spacing + 0.03f);
            body.z = Mathf.Max(body.z, bh + 0.035f);
            body.y = Mathf.Max(body.y, bw * 0.85f + 0.015f);

            Material bodyMat = MatLib.Get("Device_" + def.Name, def.Color, 0.35f, 0.15f);
            Material recessMat = MatLib.Get("Device_Recess", new Color(0.08f, 0.08f, 0.09f), 0.1f);
            Material offMat = MatLib.Get("Indicator_Off", new Color(0.18f, 0.18f, 0.20f), 0.6f);
            Material onMat = MatLib.Get("Indicator_On", new Color(0.35f, 1f, 0.45f), 0.7f, 0f,
                                        new Color(0.35f, 1f, 0.45f) * 2.2f);

            var root = new GameObject(def.Name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = tablePos + new Vector3(0f, body.y * 0.5f + 0.01f, 0f);
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Prim.Box("Body", root.transform, Vector3.zero, body, bodyMat);

            float top = body.y * 0.5f;
            Prim.Box("Recess", root.transform, new Vector3(0f, top - 0.004f, 0f),
                     new Vector3(def.Count * spacing + 0.012f, 0.012f, bh + 0.014f), recessMat);

            var anchors = new List<Transform>();
            for (int i = 0; i < def.Count; i++)
            {
                float ax = (i - (def.Count - 1) * 0.5f) * spacing;
                var a = Prim.Empty("Slot" + i, root.transform, new Vector3(ax, top - bw * 0.22f, 0f)).transform;
                a.localRotation = Quaternion.Euler(90f, 0f, 0f); // 원통 축을 기기 앞뒤 방향으로 눕힌다
                anchors.Add(a);
            }

            var indGo = Prim.Sphere("Indicator", root.transform,
                                    new Vector3(body.x * 0.5f - 0.022f, top + 0.006f, -body.z * 0.5f + 0.022f),
                                    0.022f, offMat);
            var lampGo = Prim.Empty("IndicatorLight", indGo.transform, Vector3.zero);
            lampGo.transform.localScale = Vector3.one / 0.022f;
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 0.45f;
            lamp.intensity = 1.1f;
            lamp.color = new Color(0.5f, 1f, 0.55f);
            lamp.shadows = LightShadows.None;
            lamp.enabled = false;

            // 기기가 36개라 라벨이 서로 겹친다. 작게 만들고, 가까이 간 것만 읽히게 한다.
            // 자세한 정보는 어차피 조준하면 크로스헤어 옆에 뜬다.
            Text label = MakeWorldLabelText(root.transform, new Vector3(0f, top + 0.20f, 0f), "", Color.white,
                                            1.7f, 3.2f, 0.00055f);

            var col = root.AddComponent<BoxCollider>();
            col.size = body;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.45f;
            rb.angularDamping = 2f;

            var device = root.AddComponent<Device>();
            device.Configure(def.Key, def.Name, def.Size, def.Count);
            device.BindVisuals(anchors, indGo.GetComponent<Renderer>(), offMat, onMat, lamp, label);
        }

        // ---------------------------------------------------------------- 쓰레기통 / 제출대

        static void BuildTrashCan()
        {
            Material bodyMat = MatLib.Get("Prop_Trash", new Color(0.20f, 0.45f, 0.28f), 0.3f);
            Material rimMat = MatLib.Get("Prop_TrashRim", new Color(0.85f, 0.85f, 0.30f), 0.4f);
            Material holeMat = MatLib.Get("Prop_TrashHole", new Color(0.05f, 0.05f, 0.06f), 0.1f);

            var root = new GameObject("TrashCan");
            root.transform.position = new Vector3(-1.6f, 0f, -0.6f);

            // 낮으면 바로 앞에 섰을 때 60도 넘게 내려다봐야 조준이 된다. 허리 높이로 세운다.
            const float canH = 0.95f;
            Prim.Cylinder("Body", root.transform, new Vector3(0f, canH * 0.5f, 0f), 0.46f, canH, bodyMat);
            Prim.Cylinder("Rim", root.transform, new Vector3(0f, canH + 0.02f, 0f), 0.52f, 0.06f, rimMat);
            Prim.Cylinder("Hole", root.transform, new Vector3(0f, canH + 0.04f, 0f), 0.36f, 0.03f, holeMat);

            // 콜라이더는 보이는 것보다 넉넉하게 잡아 조준을 쉽게 한다.
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, (canH + 0.06f) * 0.5f, 0f);
            col.size = new Vector3(0.58f, canH + 0.06f, 0.58f);

            root.AddComponent<TrashCan>();
            MakeWorldLabelText(root.transform, new Vector3(0f, canH + 0.38f, 0f), "폐건전지 수거함\n(잔량 0%)",
                               new Color(1f, 0.78f, 0.78f));
        }

        static SubmitCounter BuildSubmitCounter()
        {
            Material topMat = MatLib.Get("Prop_CounterTop", new Color(0.30f, 0.55f, 0.75f), 0.35f);
            Material baseMat = MatLib.Get("Prop_CounterBase", new Color(0.22f, 0.24f, 0.28f), 0.3f);

            var root = new GameObject("SubmitCounter");
            root.transform.position = new Vector3(0f, 0f, 5.1f);

            Prim.Box("Base", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(4.8f, 0.9f, 1.0f), baseMat);
            Prim.Box("Top", root.transform, new Vector3(0f, 0.94f, 0f), new Vector3(5.0f, 0.08f, 1.1f), topMat);

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(5.0f, 1.0f, 1.1f);

            // 자리는 기기 수보다 넉넉하게. 모자라면 마지막 자리에 겹쳐 쌓인다.
            const int placeCols = 10;
            int placeRows = Mathf.CeilToInt(Devices.Length / (float)placeCols) + 1;
            var anchors = new List<Transform>();
            for (int i = 0; i < placeCols * placeRows; i++)
            {
                float x = (i % placeCols - (placeCols - 1) * 0.5f) * 0.46f;
                float z = ((placeRows - 1) * 0.5f - i / placeCols) * 0.20f;
                anchors.Add(Prim.Empty("Place" + i, root.transform, new Vector3(x, 0.98f, z)).transform);
            }

            var counter = root.AddComponent<SubmitCounter>();
            counter.BindAnchors(anchors);
            MakeWorldLabelText(root.transform, new Vector3(0f, 1.6f, 0f), "제출대\n작동하는 기기를 올려놓기",
                               new Color(0.7f, 0.95f, 1f));
            return counter;
        }

        /// <summary>제출대 앞에 서 있는 물리 버튼. 정리가 끝나기 전에는 비활성 상태다.</summary>
        static ClearButton BuildClearButton()
        {
            Material standMat = MatLib.Get("Prop_ButtonStand", new Color(0.22f, 0.24f, 0.28f), 0.3f, 0.4f);
            Material capMat = MatLib.Get("Prop_ButtonCap", new Color(0.85f, 0.25f, 0.20f), 0.4f);
            Material lampOff = MatLib.Get("ClearLamp_Off", new Color(0.20f, 0.20f, 0.22f), 0.6f);
            Material lampOn = MatLib.Get("ClearLamp_On", new Color(0.4f, 1f, 0.5f), 0.7f, 0f,
                                         new Color(0.4f, 1f, 0.5f) * 1.4f);

            var root = new GameObject("ClearButton");
            root.transform.position = new Vector3(0f, 0f, 4.15f);

            Prim.Cylinder("Pole", root.transform, new Vector3(0f, 0.45f, 0f), 0.09f, 0.9f, standMat);
            Prim.Cylinder("Base", root.transform, new Vector3(0f, 0.025f, 0f), 0.34f, 0.05f, standMat);
            Prim.Cylinder("Housing", root.transform, new Vector3(0f, 0.94f, 0f), 0.26f, 0.10f, standMat);
            Prim.Cylinder("Cap", root.transform, new Vector3(0f, 1.01f, 0f), 0.19f, 0.06f, capMat);

            var lampGo = Prim.Sphere("Lamp", root.transform, new Vector3(0f, 1.06f, 0f), 0.07f, lampOff);
            var lightGo = Prim.Empty("LampLight", lampGo.transform, Vector3.zero);
            lightGo.transform.localScale = Vector3.one / 0.07f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 0.7f;
            light.intensity = 1.1f;
            light.color = new Color(0.45f, 1f, 0.55f);
            light.shadows = LightShadows.None;
            light.enabled = false;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.62f, 0f);
            col.size = new Vector3(0.34f, 1.1f, 0.34f);

            var button = root.AddComponent<ClearButton>();
            button.Bind(null, lampGo.GetComponent<Renderer>(), lampOff, lampOn, light);

            MakeWorldLabelText(root.transform, new Vector3(0f, 1.36f, 0f), "정리 완료",
                               new Color(0.6f, 1f, 0.7f));
            return button;
        }

        static void AddSign(Transform parent, Vector3 localPos, string text, Color color)
        {
            var t = MakeWorldLabelText(parent, localPos, text, color);
            t.fontSize = 52;
        }

        // ---------------------------------------------------------------- 플레이어

        static PlayerInteractor BuildPlayer()
        {
            var root = new GameObject("Player");
            // CharacterController 바닥이 루트 원점이므로 바닥에 딱 붙여 놓는다.
            root.transform.position = new Vector3(0f, 0.05f, -3.4f);

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.72f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.86f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.35f;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 60f;
            cam.fieldOfView = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            camGo.AddComponent<AudioListener>();

            var hand = Prim.Empty("Hand", camGo.transform, Vector3.zero).transform;

            var fpc = root.AddComponent<FirstPersonController>();
            var so = new SerializedObject(fpc);
            so.FindProperty("cameraPivot").objectReferenceValue = camGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var interactor = root.AddComponent<PlayerInteractor>();
            var so2 = new SerializedObject(interactor);
            so2.FindProperty("viewCamera").objectReferenceValue = cam;
            so2.FindProperty("hand").objectReferenceValue = hand;
            so2.ApplyModifiedPropertiesWithoutUndo();

            return interactor;
        }

        // ---------------------------------------------------------------- UI

        static Font UiFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static Text MakeWorldLabelText(Transform parent, Vector3 localPos, string content, Color color,
                                       float fadeStart = 0f, float fadeEnd = 0f, float worldScale = 0.00085f)
        {
            var canvasGo = new GameObject("Label");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = localPos;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasGroup>();
            canvasGo.AddComponent<Billboard>().ConfigureFade(fadeStart, fadeEnd);

            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500f, 160f);

            // 부모(기기 몸통 등)의 스케일 영향을 받지 않도록 월드 스케일을 직접 맞춘다.
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            rt.localScale = new Vector3(worldScale / Mathf.Max(0.0001f, parentScale.x),
                                        worldScale / Mathf.Max(0.0001f, parentScale.y),
                                        worldScale / Mathf.Max(0.0001f, parentScale.z));

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(canvasGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = 40;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var trt = text.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            return text;
        }

        static Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos,
                             Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = UiFont();
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.15f;
            t.raycastTarget = false;

            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return t;
        }

        static Image MakeImage(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos,
                               Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return img;
        }

        static HUD BuildHud(PlayerInteractor player)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 mid = new Vector2(0.5f, 0.5f);

            var crosshair = MakeImage("Crosshair", canvasGo.transform, center, mid, Vector2.zero,
                                      new Vector2(7f, 7f), new Color(1f, 1f, 1f, 0.55f));

            var hoverTitle = MakeText("HoverTitle", canvasGo.transform, center, mid, new Vector2(0f, -78f),
                                      new Vector2(1200f, 40f), 30, TextAnchor.MiddleCenter, Color.white);
            var hoverDetail = MakeText("HoverDetail", canvasGo.transform, center, mid, new Vector2(0f, -114f),
                                       new Vector2(1200f, 34f), 22, TextAnchor.MiddleCenter,
                                       new Color(0.78f, 0.80f, 0.85f));

            // 손에 든 물건
            var heldPanel = new GameObject("HeldPanel");
            heldPanel.transform.SetParent(canvasGo.transform, false);
            var heldRt = heldPanel.AddComponent<RectTransform>();
            heldRt.anchorMin = new Vector2(0.5f, 0f);
            heldRt.anchorMax = new Vector2(0.5f, 0f);
            heldRt.pivot = new Vector2(0.5f, 0f);
            heldRt.anchoredPosition = new Vector2(0f, 62f);
            heldRt.sizeDelta = new Vector2(520f, 130f);
            var heldBg = heldPanel.AddComponent<Image>();
            heldBg.color = new Color(0f, 0f, 0f, 0.55f);
            heldBg.raycastTarget = false;

            var heldTitle = MakeText("HeldTitle", heldPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                     new Vector2(0f, -14f), new Vector2(500f, 40f), 30,
                                     TextAnchor.MiddleCenter, Color.white);
            var heldDetail = MakeText("HeldDetail", heldPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                      new Vector2(0f, -56f), new Vector2(500f, 32f), 24,
                                      TextAnchor.MiddleCenter, new Color(0.8f, 0.95f, 0.8f));

            var barBg = MakeImage("ChargeBarBg", heldPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                  new Vector2(0f, 18f), new Vector2(440f, 14f), new Color(1f, 1f, 1f, 0.16f));
            var barFill = MakeImage("ChargeBarFill", barBg.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                    Vector2.zero, new Vector2(440f, 14f), new Color(0.5f, 1f, 0.6f));

            var progress = MakeText("Progress", canvasGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                    new Vector2(28f, -24f), new Vector2(520f, 80f), 26,
                                    TextAnchor.UpperLeft, new Color(1f, 0.95f, 0.8f));

            MakeText("Controls", canvasGo.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                     new Vector2(28f, 24f), new Vector2(1000f, 60f), 19, TextAnchor.LowerLeft,
                     new Color(0.75f, 0.77f, 0.82f)).text =
                "WASD 이동 · Shift 달리기 · 마우스 시점\n좌클릭 집기/넣기/제출 · 우클릭 건전지 빼기 · G 내려놓기 · Esc 커서 해제";

            // 알림
            var toastGo = new GameObject("Toast");
            toastGo.transform.SetParent(canvasGo.transform, false);
            var toastRt = toastGo.AddComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0.5f, 0f);
            toastRt.anchorMax = new Vector2(0.5f, 0f);
            toastRt.pivot = new Vector2(0.5f, 0f);
            toastRt.anchoredPosition = new Vector2(0f, 212f);
            toastRt.sizeDelta = new Vector2(1300f, 50f);
            var toastCg = toastGo.AddComponent<CanvasGroup>();
            toastCg.alpha = 0f;
            var toastText = MakeText("Text", toastGo.transform, center, mid, Vector2.zero,
                                     new Vector2(1300f, 50f), 28, TextAnchor.MiddleCenter, Color.white);

            var hud = canvasGo.AddComponent<HUD>();
            hud.Bind(player, hoverTitle, hoverDetail, crosshair, heldPanel, heldTitle, heldDetail,
                     barFill.rectTransform, barFill, progress, toastText, toastCg);
            return hud;
        }

        // ---------------------------------------------------------------- 메뉴 (타이틀 / 클리어)

        static Button MakeButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size,
                                 string label, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            MakeText("Text", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                     Vector2.zero, size, 32, TextAnchor.MiddleCenter, Color.white).text = label;
            return button;
        }

        static GameObject MakeFullScreenPanel(string name, Transform parent, Color background)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = background;
            img.raycastTarget = true; // 뒤쪽 클릭이 새어나가지 않게 막는다
            return go;
        }

        static GameFlow BuildMenus(GameManager gm, PlayerInteractor player, HUD hud,
                                   SubmitCounter counter, ClearButton clearButton)
        {
            // UI 버튼을 누르려면 EventSystem이 있어야 한다.
            // 이 프로젝트는 New Input System 전용이라 InputSystemUIInputModule을 쓴다.
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var canvasGo = new GameObject("MenuCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // HUD 위에 그린다
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Vector2 center = new Vector2(0.5f, 0.5f);
            Color buttonColor = new Color(0.20f, 0.40f, 0.58f, 0.95f);
            Color quitColor = new Color(0.42f, 0.22f, 0.22f, 0.95f);
            Vector2 buttonSize = new Vector2(420f, 84f);

            // ---- 타이틀
            var titlePanel = MakeFullScreenPanel("TitlePanel", canvasGo.transform, new Color(0.04f, 0.05f, 0.07f, 0.96f));
            MakeText("Title", titlePanel.transform, center, center, new Vector2(0f, 250f),
                     new Vector2(1200f, 120f), 84, TextAnchor.MiddleCenter,
                     new Color(1f, 0.92f, 0.6f)).text = "건전지 정리";
            MakeText("Subtitle", titlePanel.transform, center, center, new Vector2(0f, 160f),
                     new Vector2(1200f, 50f), 26, TextAnchor.MiddleCenter,
                     new Color(0.72f, 0.76f, 0.82f)).text =
                "방전된 건전지는 수거함으로, 남은 건전지는 맞는 기기에";

            var newGame = MakeButton("NewGameButton", titlePanel.transform, new Vector2(0f, 20f), buttonSize,
                                     "새 게임", buttonColor);
            var continueBtn = MakeButton("ContinueButton", titlePanel.transform, new Vector2(0f, -80f), buttonSize,
                                         "이어하기", buttonColor);
            var quitBtn = MakeButton("QuitButton", titlePanel.transform, new Vector2(0f, -180f), buttonSize,
                                     "게임 종료", quitColor);

            // ---- 클리어
            // 제출대의 발광 소품과 월드 라벨이 밝아서, 알파를 0.98까지 올려도 글자 뒤로 비친다.
            // 결과 화면은 가독성이 우선이라 완전 불투명으로 간다. (타이틀은 반투명 유지)
            var clearPanel = MakeFullScreenPanel("ClearPanel", canvasGo.transform, new Color(0.03f, 0.06f, 0.05f, 1f));
            var clearText = MakeText("Result", clearPanel.transform, center, center, new Vector2(0f, 110f),
                                     new Vector2(1200f, 460f), 44, TextAnchor.MiddleCenter,
                                     new Color(0.85f, 1f, 0.9f));
            var toTitle = MakeButton("ToTitleButton", clearPanel.transform, new Vector2(0f, -220f), buttonSize,
                                     "처음으로", buttonColor);
            var clearQuit = MakeButton("ClearQuitButton", clearPanel.transform, new Vector2(0f, -320f), buttonSize,
                                       "게임 종료", quitColor);
            clearPanel.SetActive(false);

            var flowGo = new GameObject("GameFlow");
            var flow = flowGo.AddComponent<GameFlow>();
            flow.Bind(gm, player.GetComponent<FirstPersonController>(), player, hud, counter, clearButton,
                      titlePanel, newGame, continueBtn, quitBtn,
                      clearPanel, clearText, toTitle, clearQuit);
            return flow;
        }
    }
}
