using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BatteryGame
{
    public enum GameState { Title, Playing, Cleared }

    /// <summary>
    /// 타이틀 → 플레이 → 클리어 흐름과 세이브를 담당한다.
    /// 씬은 하나뿐이고, 메뉴는 그 위에 겹쳐 뜨는 캔버스다.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] GameManager gameManager;
        [SerializeField] FirstPersonController controller;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] HUD hud;
        [SerializeField] SubmitCounter submitCounter;
        [SerializeField] ClearButton clearButton;

        [Header("타이틀 화면")]
        [SerializeField] GameObject titlePanel;
        [SerializeField] Button newGameButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button quitButton;

        [Header("클리어 화면")]
        [SerializeField] GameObject clearPanel;
        [SerializeField] Text clearText;
        [SerializeField] Button toTitleButton;
        [SerializeField] Button clearQuitButton;

        [Header("설정")]
        [Tooltip("플레이 중 자동 저장 간격(초). 0 이하면 자동 저장 안 함.")]
        [SerializeField] float autosaveInterval = 20f;

        float autosaveTimer;
        bool subscribed;

        public GameState State { get; private set; }
        public float ElapsedSeconds { get; private set; }

        void Awake()
        {
            if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
            if (controller == null) controller = FindAnyObjectByType<FirstPersonController>();
            if (interactor == null) interactor = FindAnyObjectByType<PlayerInteractor>();
            if (hud == null) hud = FindAnyObjectByType<HUD>();
            if (submitCounter == null) submitCounter = FindAnyObjectByType<SubmitCounter>();

            if (newGameButton != null) newGameButton.onClick.AddListener(StartNewGame);
            if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            if (toTitleButton != null) toTitleButton.onClick.AddListener(ReturnToTitle);
            if (clearQuitButton != null) clearQuitButton.onClick.AddListener(QuitGame);
        }

        void Start()
        {
            if (gameManager != null && !subscribed)
            {
                gameManager.Cleared += OnCleared;
                subscribed = true;
            }
            ShowTitle();
        }

        void OnDestroy()
        {
            if (gameManager != null && subscribed) gameManager.Cleared -= OnCleared;
        }

        void Update()
        {
            if (State != GameState.Playing) return;

            ElapsedSeconds += Time.deltaTime;

            if (autosaveInterval <= 0f) return;
            autosaveTimer -= Time.deltaTime;
            if (autosaveTimer <= 0f)
            {
                autosaveTimer = autosaveInterval;
                SaveNow();
            }
        }

        void OnApplicationQuit()
        {
            if (State == GameState.Playing) SaveNow();
        }

        // ---------------------------------------------------------------- 상태 전환

        void SetPlayerControl(bool on)
        {
            if (controller != null) controller.SetControlEnabled(on);
            if (interactor != null) interactor.SetActiveState(on);
            if (hud != null) hud.SetVisible(on);
        }

        public void ShowTitle()
        {
            State = GameState.Title;
            SetPlayerControl(false);

            if (clearPanel != null) clearPanel.SetActive(false);
            if (titlePanel != null) titlePanel.SetActive(true);
            if (clearButton != null) clearButton.Disarm();

            // 저장된 게임이 없으면 이어하기는 감추고, 빈 자리가 남지 않게 종료 버튼을 끌어올린다.
            bool hasSave = SaveSystem.HasSave();
            if (continueButton != null) continueButton.gameObject.SetActive(hasSave);
            if (quitButton != null && continueButton != null)
            {
                var quitRect = quitButton.GetComponent<RectTransform>();
                var contRect = continueButton.GetComponent<RectTransform>();
                if (quitRect != null && contRect != null)
                {
                    if (quitSlotY == 0f) quitSlotY = quitRect.anchoredPosition.y;
                    float y = hasSave ? quitSlotY : contRect.anchoredPosition.y;
                    quitRect.anchoredPosition = new Vector2(quitRect.anchoredPosition.x, y);
                }
            }
        }

        float quitSlotY;

        public void StartNewGame()
        {
            SaveSystem.Delete();
            if (submitCounter != null) submitCounter.ResetPlacements();

            ElapsedSeconds = 0f;
            autosaveTimer = autosaveInterval;
            if (gameManager != null) gameManager.BeginNewGame();

            EnterPlaying();
            HUD.Toast("건전지를 집어서 규격과 잔량을 확인하세요.", ToastKind.Info);
        }

        public void ContinueGame()
        {
            SaveData data = SaveSystem.Load();
            if (data == null)
            {
                HUD.Toast("불러올 수 있는 게임이 없습니다.", ToastKind.Error);
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                return;
            }

            if (submitCounter != null) submitCounter.ResetPlacements();
            if (gameManager != null) gameManager.RestoreFrom(data, submitCounter);

            ElapsedSeconds = data.elapsedSeconds;
            autosaveTimer = autosaveInterval;

            if (controller != null)
                controller.Teleport(data.playerPosition, data.playerYaw, data.cameraPitch);

            EnterPlaying();
            HUD.Toast("이어서 시작합니다. (" + FormatTime(ElapsedSeconds) + " 경과)", ToastKind.Info);
        }

        void EnterPlaying()
        {
            State = GameState.Playing;
            if (titlePanel != null) titlePanel.SetActive(false);
            if (clearPanel != null) clearPanel.SetActive(false);
            SetPlayerControl(true);
        }

        void OnCleared()
        {
            if (State != GameState.Playing) return;
            if (clearButton != null) clearButton.Arm();
            HUD.Toast("모두 정리했습니다! 제출대 앞의 버튼을 누르세요.", ToastKind.Success);
        }

        public void ShowClearScreen()
        {
            State = GameState.Cleared;
            SaveSystem.Delete();

            SetPlayerControl(false);
            if (titlePanel != null) titlePanel.SetActive(false);
            if (clearPanel != null) clearPanel.SetActive(true);

            if (clearText != null && gameManager != null)
            {
                clearText.text =
                    "정리 완료!\n\n" +
                    "소요 시간   " + FormatTime(ElapsedSeconds) + "\n\n" +
                    "건전지 " + gameManager.TotalBatteries + "개 처리\n" +
                    "기기 " + gameManager.SubmittedDevices + "개 제출";
            }
        }

        public void ReturnToTitle()
        {
            if (State == GameState.Playing) SaveNow();
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        public void QuitGame()
        {
            if (State == GameState.Playing) SaveNow();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- 저장

        public void SaveNow()
        {
            if (gameManager == null) return;

            var data = new SaveData();
            data.elapsedSeconds = ElapsedSeconds;
            gameManager.WriteInto(data);

            if (controller != null)
            {
                data.playerPosition = controller.transform.position;
                data.playerYaw = controller.transform.eulerAngles.y;
                data.cameraPitch = controller.Pitch;
            }

            SaveSystem.Save(data);
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int h = total / 3600;
            int m = (total % 3600) / 60;
            int s = total % 60;
            if (h > 0) return h + "시간 " + m + "분 " + s + "초";
            if (m > 0) return m + "분 " + s + "초";
            return s + "초";
        }

        // ---------------------------------------------------------------- 빌더 연결

        public void Bind(GameManager manager, FirstPersonController fpc, PlayerInteractor player, HUD gameHud,
                         SubmitCounter counter, ClearButton button,
                         GameObject title, Button newGame, Button cont, Button quit,
                         GameObject clear, Text clearLabel, Button toTitle, Button clearQuit)
        {
            gameManager = manager;
            controller = fpc;
            interactor = player;
            hud = gameHud;
            submitCounter = counter;
            clearButton = button;
            titlePanel = title;
            newGameButton = newGame;
            continueButton = cont;
            quitButton = quit;
            clearPanel = clear;
            clearText = clearLabel;
            toTitleButton = toTitle;
            clearQuitButton = clearQuit;
        }
    }
}
