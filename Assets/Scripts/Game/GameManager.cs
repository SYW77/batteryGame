using System;
using System.Collections.Generic;
using UnityEngine;

namespace BatteryGame
{
    /// <summary>
    /// 건전지 더미를 생성하고 진행도를 집계한다.
    /// 더미는 "기기들이 요구하는 정상 건전지 + 방전 건전지"로만 구성되므로 항상 정확히 0개로 끝낼 수 있다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        static GameManager instance;

        // Enter Play Mode Options로 도메인 리로드를 껐기 때문에 static이 저절로 초기화되지 않는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
        }

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<GameManager>();
                    if (instance == null)
                        instance = new GameObject("GameManager").AddComponent<GameManager>();
                }
                return instance;
            }
        }

        [Header("건전지 프리팹 (BatterySize 순서: AAA, AA, C, D, 9V)")]
        [SerializeField] Battery[] batteryPrefabs = new Battery[5];

        [Header("더미 생성")]
        [SerializeField] Transform pileOrigin;
        [Tooltip("더미가 쌓이는 영역. 받침면보다 작아야 무너져도 밖으로 쏟아지지 않는다.")]
        [SerializeField] Vector3 pileAreaSize = new Vector3(0.8f, 0.4f, 0.55f);
        [Tooltip("한 칸의 크기. 가장 긴 건전지보다 커야 서로 겹치지 않는다.")]
        [SerializeField] float pileCellSize = 0.135f;
        [Tooltip("층 간격. 가장 굵은 건전지 지름보다 커야 한다.")]
        [SerializeField] float pileLayerStep = 0.075f;
        [Tooltip("한 층을 떨어뜨리고 다음 층까지 기다리는 시간. 짧으면 탑이 무너진다.")]
        [SerializeField] float pileLayerDelay = 0.12f;
        [Tooltip("전부 쓰레기통으로 가야 하는 방전 건전지 개수. 더미의 부피를 결정한다.")]
        [SerializeField] int deadBatteryCount = 28;
        [SerializeField] Vector2 chargeRange = new Vector2(0.18f, 1f);
        [Tooltip("0이면 매번 다른 배치, 0이 아니면 고정 배치")]
        [SerializeField] int randomSeed = 0;

        readonly List<Device> devices = new List<Device>();

        public int TotalBatteries { get; private set; }
        public int ResolvedBatteries { get; private set; }
        public int RemainingBatteries { get { return TotalBatteries - ResolvedBatteries; } }
        public int TotalDevices { get { return devices.Count; } }
        public int SubmittedDevices { get; private set; }
        public bool IsCleared { get; private set; }

        public event Action ProgressChanged;
        public event Action Cleared;

        public IList<Device> Devices { get { return devices; } }

        void Awake()
        {
            instance = this;
            CollectDevices();
        }

        /// <summary>
        /// 세이브에서 기기를 인덱스로 찾으므로 순서가 매번 같아야 한다.
        /// FindObjectsByType은 순서를 보장하지 않아서 계층 순서로 정렬한다.
        /// </summary>
        void CollectDevices()
        {
            devices.Clear();
            devices.AddRange(FindObjectsByType<Device>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            devices.Sort(delegate (Device a, Device b)
            {
                return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
            });
        }

        /// <summary>새 게임. 더미를 새로 만든다.</summary>
        public void BeginNewGame()
        {
            ClearPile();

            var rng = randomSeed != 0 ? new System.Random(randomSeed)
                                      : new System.Random(Environment.TickCount);
            var plan = BuildPlan(rng);

            TotalBatteries = plan.Count;
            ResolvedBatteries = 0;
            SubmittedDevices = 0;
            IsCleared = false;
            StartCoroutine(SpawnPileRoutine(plan, rng));
            RaiseProgress();
        }

        List<KeyValuePair<BatterySize, float>> BuildPlan(System.Random rng)
        {
            var plan = new List<KeyValuePair<BatterySize, float>>();

            // 1) 기기들이 요구하는 만큼의 "살아있는" 건전지
            for (int i = 0; i < devices.Count; i++)
            {
                var d = devices[i];
                for (int n = 0; n < d.RequiredCount; n++)
                {
                    float t = (float)rng.NextDouble();
                    float charge = Mathf.Lerp(chargeRange.x, chargeRange.y, t);
                    plan.Add(new KeyValuePair<BatterySize, float>(d.RequiredSize, charge));
                }
            }

            // 2) 전부 쓰레기통행인 방전 건전지
            for (int n = 0; n < deadBatteryCount; n++)
            {
                BatterySize size = BatterySizes.All[rng.Next(BatterySizes.All.Length)];
                plan.Add(new KeyValuePair<BatterySize, float>(size, 0f));
            }

            Shuffle(plan, rng);
            return plan;
        }

        /// <summary>
        /// 통이 없으므로 한 번에 전부 떨어뜨리면 탑이 무너지면서 사방으로 퍼진다.
        /// 한 층씩 낮은 높이에서 떨어뜨리고 잠깐 기다리면 실제로 위로 쌓인다.
        /// </summary>
        System.Collections.IEnumerator SpawnPileRoutine(List<KeyValuePair<BatterySize, float>> plan,
                                                        System.Random rng)
        {
            Transform origin = pileOrigin != null ? pileOrigin : transform;
            float cell = Mathf.Max(0.05f, pileCellSize);
            int cols = Mathf.Max(1, Mathf.FloorToInt(pileAreaSize.x / cell));
            int rows = Mathf.Max(1, Mathf.FloorToInt(pileAreaSize.z / cell));
            int perLayer = cols * rows;

            var parent = new GameObject("BatteryPile").transform;
            parent.SetParent(origin, false);

            var wait = new WaitForSeconds(pileLayerDelay);
            var spawned = new List<Transform>(plan.Count);
            float dropY = pileLayerStep;

            for (int i = 0; i < plan.Count; i++)
            {
                if (i > 0 && i % perLayer == 0)
                {
                    yield return wait;
                    // 층마다 일정하게 올리면 층이 많아질수록 낙하 높이가 커져 튕겨 흩어진다.
                    // 실제로 쌓인 높이 바로 위에서 떨어뜨린다.
                    dropY = PileTop(spawned, origin) + pileLayerStep;
                }

                Battery prefab = PrefabFor(plan[i].Key);
                if (prefab == null) continue;

                int idx = i % perLayer;
                int c = idx % cols;
                int r = idx / cols;

                float x = (c - (cols - 1) * 0.5f) * cell + ((float)rng.NextDouble() - 0.5f) * 0.015f;
                float z = (r - (rows - 1) * 0.5f) * cell + ((float)rng.NextDouble() - 0.5f) * 0.015f;

                // 받침이 회전해 있을 수 있으므로 격자를 origin 기준 로컬 축으로 배치한다.
                Vector3 spawnPos = origin.position + origin.rotation * new Vector3(x, dropY, z);
                var battery = Instantiate(prefab, spawnPos,
                                          Quaternion.Euler(90f, (float)rng.NextDouble() * 360f, 0f), parent);
                battery.Configure(plan[i].Key, plan[i].Value);
                battery.name = "Battery_" + BatterySizes.Label(plan[i].Key) + "_" + i;
                spawned.Add(battery.transform);
            }
        }

        /// <summary>
        /// 지금까지 쌓인 더미의 높이. origin은 요(yaw)만 돌아가 있으므로 월드 y 차이가 곧 로컬 높이다.
        /// 밖으로 굴러떨어진 건전지는 y가 낮아 최댓값에 영향을 주지 않는다.
        /// </summary>
        static float PileTop(List<Transform> spawned, Transform origin)
        {
            float top = 0f;
            float baseY = origin.position.y;
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] == null) continue;
                float h = spawned[i].position.y - baseY;
                if (h > top) top = h;
            }
            return top;
        }

        Battery PrefabFor(BatterySize size)
        {
            int i = (int)size;
            if (batteryPrefabs == null || i < 0 || i >= batteryPrefabs.Length) return null;
            return batteryPrefabs[i];
        }

        // ---------------------------------------------------------------- 저장 / 복원

        Transform pileParent;

        Transform NewPileParent()
        {
            Transform origin = pileOrigin != null ? pileOrigin : transform;
            var parent = new GameObject("BatteryPile").transform;
            parent.SetParent(origin, false);
            pileParent = parent;
            return parent;
        }

        /// <summary>
        /// 페널티로 방전 건전지를 쏟아낸다. 총량이 늘어나므로 진행도도 같이 갱신한다.
        /// </summary>
        public void SpawnPenaltyBatteries(int count, Vector3 center, float scatterRadius)
        {
            if (count <= 0) return;
            if (pileParent == null) pileParent = NewPileParent();

            for (int i = 0; i < count; i++)
            {
                BatterySize size = BatterySizes.All[UnityEngine.Random.Range(0, BatterySizes.All.Length)];
                Battery prefab = PrefabFor(size);
                if (prefab == null) continue;

                Vector2 offset = UnityEngine.Random.insideUnitCircle * scatterRadius;
                Vector3 pos = center + new Vector3(offset.x, UnityEngine.Random.Range(0f, 0.18f), offset.y);

                var battery = Instantiate(prefab, pos, UnityEngine.Random.rotation, pileParent);
                battery.Configure(size, 0f);
                battery.name = "Battery_Penalty_" + BatterySizes.Label(size) + "_" + TotalBatteries;

                var rb = battery.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.linearVelocity = new Vector3(offset.x, 0.4f, offset.y).normalized *
                                        UnityEngine.Random.Range(0.8f, 1.6f);

                TotalBatteries++;
            }

            RaiseProgress();
        }

        void ClearPile()
        {
            StopAllCoroutines();
            var all = FindObjectsByType<Battery>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].InstalledIn != null) all[i].InstalledIn = null;
                Destroy(all[i].gameObject);
            }

            Transform origin = pileOrigin != null ? pileOrigin : transform;
            for (int i = origin.childCount - 1; i >= 0; i--)
                if (origin.GetChild(i).name == "BatteryPile") Destroy(origin.GetChild(i).gameObject);
        }

        /// <summary>현재 진행 상황을 세이브 데이터에 기록한다.</summary>
        public void WriteInto(SaveData data)
        {
            data.totalBatteries = TotalBatteries;
            data.resolvedBatteries = ResolvedBatteries;
            data.submittedDevices = SubmittedDevices;

            data.deviceSubmitted = new bool[devices.Count];
            for (int i = 0; i < devices.Count; i++)
                data.deviceSubmitted[i] = devices[i].IsSubmitted;

            data.batteries.Clear();
            var all = FindObjectsByType<Battery>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (b == null || b.IsDiscarded) continue;
                var rec = new BatterySave();
                rec.size = (int)b.Size;
                rec.charge = b.Charge;
                rec.inspected = b.Inspected;
                rec.deviceIndex = b.InstalledIn != null ? devices.IndexOf(b.InstalledIn) : -1;
                rec.position = b.transform.position;
                rec.euler = b.transform.eulerAngles;
                data.batteries.Add(rec);
            }
        }

        /// <summary>세이브 데이터로 진행 상황을 복원한다. 기기는 씬의 제자리에 있다고 가정한다.</summary>
        public void RestoreFrom(SaveData data, SubmitCounter counter)
        {
            ClearPile();
            IsCleared = false;

            Transform parent = NewPileParent();
            for (int i = 0; i < data.batteries.Count; i++)
            {
                BatterySave rec = data.batteries[i];
                var size = (BatterySize)rec.size;
                Battery prefab = PrefabFor(size);
                if (prefab == null) continue;

                var battery = Instantiate(prefab, rec.position, Quaternion.Euler(rec.euler), parent);
                battery.Configure(size, rec.charge);
                if (rec.inspected) battery.MarkInspected();
                battery.name = "Battery_" + BatterySizes.Label(size) + "_" + i;

                if (rec.deviceIndex >= 0 && rec.deviceIndex < devices.Count)
                {
                    string message;
                    devices[rec.deviceIndex].TryInsert(battery, out message);
                }
            }

            for (int i = 0; i < devices.Count && i < data.deviceSubmitted.Length; i++)
            {
                if (!data.deviceSubmitted[i]) continue;
                if (counter != null) counter.RestoreSubmitted(devices[i]);
                else devices[i].MarkSubmitted();
            }

            TotalBatteries = data.totalBatteries;
            ResolvedBatteries = data.resolvedBatteries;
            SubmittedDevices = data.submittedDevices;
            RaiseProgress();
        }

        static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        public void NotifyBatteryDiscarded(Battery battery)
        {
            if (battery != null) battery.MarkDiscarded();
            ResolvedBatteries++;
            RaiseProgress();
        }

        public void NotifyDeviceSubmitted(Device device)
        {
            SubmittedDevices++;
            ResolvedBatteries += device.FilledCount;
            RaiseProgress();
        }

        void RaiseProgress()
        {
            if (ProgressChanged != null) ProgressChanged();

            if (!IsCleared && TotalBatteries > 0 && RemainingBatteries <= 0 && SubmittedDevices >= devices.Count)
            {
                IsCleared = true;
                if (Cleared != null) Cleared();
            }
        }

        public void BindSpawnData(Battery[] prefabs, Transform origin, Vector3 areaSize, int deadCount)
        {
            batteryPrefabs = prefabs;
            pileOrigin = origin;
            pileAreaSize = areaSize;
            deadBatteryCount = deadCount;
        }
    }
}
