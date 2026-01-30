using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MaskGame.Data;

namespace MaskGame.Managers
{
    /// <summary>
    /// 游戏管理器 - 控制游戏流程、天数、难度
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏配置")]
        [SerializeField] private GameConfig gameConfig = new GameConfig();

        [Header("对话数据池")]
        [SerializeField] private List<EncounterData> encounterPool = new List<EncounterData>();

        // 游戏状态
        private int currentDay = 1;
        private int currentEncounterIndex = 0;
        private int socialBattery;
        private float remainingTime;
        private bool isGameOver = false;

        // 当前对话
        private EncounterData currentEncounter;
        private List<EncounterData> shuffledEncounters = new List<EncounterData>();

        // 事件
        public UnityEvent<int> OnDayChanged = new UnityEvent<int>();
        public UnityEvent<float> OnProgressChanged = new UnityEvent<float>();
        public UnityEvent<int> OnBatteryChanged = new UnityEvent<int>();
        public UnityEvent<float> OnTimeChanged = new UnityEvent<float>();
        public UnityEvent<EncounterData> OnNewEncounter = new UnityEvent<EncounterData>();
        public UnityEvent<bool> OnAnswerResult = new UnityEvent<bool>();
        public UnityEvent OnGameOver = new UnityEvent();
        public UnityEvent OnDayComplete = new UnityEvent();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeGame();
        }

        private void Update()
        {
            if (isGameOver) return;

            // 倒计时
            if (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;
                OnTimeChanged.Invoke(remainingTime);

                // 时间耗尽 = 选错
                if (remainingTime <= 0)
                {
                    ProcessAnswer(MaskType.Mask1, true); // 超时视为选错
                }
            }
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        private void InitializeGame()
        {
            currentDay = 1;
            currentEncounterIndex = 0;
            socialBattery = gameConfig.initialSocialBattery;
            isGameOver = false;

            OnDayChanged.Invoke(currentDay);
            OnBatteryChanged.Invoke(socialBattery);

            ShuffleEncounters();
            LoadNextEncounter();
        }

        /// <summary>
        /// 打乱对话顺序
        /// </summary>
        private void ShuffleEncounters()
        {
            shuffledEncounters.Clear();
            shuffledEncounters.AddRange(encounterPool);

            // Fisher-Yates 洗牌
            for (int i = shuffledEncounters.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = shuffledEncounters[i];
                shuffledEncounters[i] = shuffledEncounters[j];
                shuffledEncounters[j] = temp;
            }
        }

        /// <summary>
        /// 加载下一个对话
        /// </summary>
        private void LoadNextEncounter()
        {
            if (encounterPool.Count == 0)
            {
                Debug.LogWarning("对话数据池为空！");
                return;
            }

            // 循环使用对话池
            if (currentEncounterIndex >= shuffledEncounters.Count)
            {
                ShuffleEncounters();
                currentEncounterIndex = 0;
            }

            currentEncounter = shuffledEncounters[currentEncounterIndex];
            remainingTime = gameConfig.GetDecisionTime(currentDay);

            OnNewEncounter.Invoke(currentEncounter);
            OnTimeChanged.Invoke(remainingTime);
        }

        /// <summary>
        /// 处理玩家选择的面具
        /// </summary>
        public void SelectMask(MaskType selectedMask)
        {
            if (isGameOver) return;
            ProcessAnswer(selectedMask, false);
        }

        /// <summary>
        /// 处理答案
        /// </summary>
        private void ProcessAnswer(MaskType selectedMask, bool isTimeout)
        {
            bool isCorrect = !isTimeout && (selectedMask == currentEncounter.correctMask);

            OnAnswerResult.Invoke(isCorrect);

            if (isCorrect)
            {
                // 选对 - 进入下一个对话
                currentEncounterIndex++;
                float progress = (float)currentEncounterIndex / gameConfig.encountersPerDay;
                OnProgressChanged.Invoke(progress);

                // 完成一天
                if (currentEncounterIndex >= gameConfig.encountersPerDay)
                {
                    CompleteDay();
                }
                else
                {
                    LoadNextEncounter();
                }
            }
            else
            {
                // 选错或超时 - 扣除社交电池
                socialBattery -= gameConfig.batteryPenalty;
                OnBatteryChanged.Invoke(socialBattery);

                if (socialBattery <= 0)
                {
                    GameOver();
                }
                else
                {
                    // 继续当前对话或加载新对话
                    LoadNextEncounter();
                }
            }
        }

        /// <summary>
        /// 完成一天
        /// </summary>
        private void CompleteDay()
        {
            OnDayComplete.Invoke();
            StartCoroutine(AdvanceToNextDay());
        }

        private IEnumerator AdvanceToNextDay()
        {
            yield return new WaitForSeconds(1.5f);

            currentDay++;
            currentEncounterIndex = 0;

            OnDayChanged.Invoke(currentDay);
            OnProgressChanged.Invoke(0f);

            LoadNextEncounter();
        }

        /// <summary>
        /// 游戏结束（社死）
        /// </summary>
        private void GameOver()
        {
            isGameOver = true;
            OnGameOver.Invoke();
        }

        /// <summary>
        /// 重新开始游戏
        /// </summary>
        public void RestartGame()
        {
            InitializeGame();
        }

        // 公开属性
        public int CurrentDay => currentDay;
        public int SocialBattery => socialBattery;
        public GameConfig Config => gameConfig;
    }
}