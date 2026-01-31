using System.Collections;
using System.Collections.Generic;
using MaskGame.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MaskGame.Managers
{
    public enum AnswerOutcome
    {
        Correct,
        Wrong,
        Timeout,
    }

    /// <summary>
    /// 游戏管理器 - 控制游戏流程、天数、难度
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private enum GameState
        {
            Await,
            Resolve,
            DayEnd,
            GameEnd,
        }

        public static GameManager Instance { get; private set; }

        [Header("游戏配置")]
        [SerializeField]
        private GameConfig gameConfig = new GameConfig();

        [Header("对话数据池")]
        [SerializeField]
        private EncounterSet encounterSet;

        [SerializeField]
        private List<EncounterData> encounterPool = new List<EncounterData>();
        private bool resLoaded;

        // 游戏状态
        private int currentDay = 1;
        private int currentEncounterIndex = 0;
        private int socialBattery;
        private float remainingTime;
        private GameState state;

        // 统计数据
        private int totalAnswers = 0;
        private int correctAnswers = 0;

        // 当前对话
        private EncounterData currentEncounter;
        private List<EncounterData> shuffledEncounters = new List<EncounterData>();

        // 事件
        public UnityEvent<int> OnDayChanged = new UnityEvent<int>();
        public UnityEvent<int> OnBatteryChanged = new UnityEvent<int>();
        public UnityEvent<float> OnTimeChanged = new UnityEvent<float>();
        public UnityEvent<EncounterData> OnNewEncounter = new UnityEvent<EncounterData>();
        public UnityEvent<AnswerOutcome, string> OnAnswerResult =
            new UnityEvent<AnswerOutcome, string>();
        public UnityEvent OnGameOver = new UnityEvent();
        public UnityEvent OnDayComplete = new UnityEvent();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                state = GameState.Resolve;
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
            if (state != GameState.Await)
                return;

            // 倒计时
            if (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;
                OnTimeChanged.Invoke(remainingTime);

                // 时间耗尽 = 选错
                if (remainingTime <= 0)
                {
                    ResolveAnswer(MaskType.Mask1, true);
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
            socialBattery = gameConfig.fixedHealth; // 固定4条血
            state = GameState.Resolve;
            totalAnswers = 0;
            correctAnswers = 0;

            OnDayChanged.Invoke(currentDay);
            OnBatteryChanged.Invoke(socialBattery);

            ShuffleEncounters();
            LoadNextEncounter();
        }

        private List<EncounterData> GetPool()
        {
            if (encounterSet != null)
            {
                return encounterSet.items;
            }

            if (!resLoaded && encounterPool.Count == 0)
            {
                resLoaded = true;
                encounterPool.AddRange(Resources.LoadAll<EncounterData>(EncounterRes));
            }

            return encounterPool;
        }

        /// <summary>
        /// 获取当前天的对话数量
        /// </summary>
        private int GetCurrentDayEncounters()
        {
            int dayIndex = currentDay - 1;
            if (dayIndex >= 0 && dayIndex < gameConfig.encountersPerDay.Length)
            {
                return gameConfig.encountersPerDay[dayIndex];
            }
            return 3; // 默认3个对话
        }

        /// <summary>
        /// 打乱对话顺序
        /// </summary>
        private void ShuffleEncounters()
        {
            List<EncounterData> pool = GetPool();
            shuffledEncounters.Clear();
            shuffledEncounters.AddRange(pool);

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
            List<EncounterData> pool = GetPool();
            if (pool.Count == 0)
            {
                UnityEngine.Debug.LogWarning(
                    "GameManager: encounter pool is empty. Assign EncounterData in the Inspector."
                );
                return;
            }

            // 循环使用对话池
            if (shuffledEncounters.Count == 0)
            {
                ShuffleEncounters();
            }

            // 从池中随机取一个
            int randomIndex = Random.Range(0, shuffledEncounters.Count);
            currentEncounter = shuffledEncounters[randomIndex];
            shuffledEncounters.RemoveAt(randomIndex);

            remainingTime = gameConfig.GetDecisionTime(currentDay); // 根据天数获取时间

            OnNewEncounter.Invoke(currentEncounter);
            OnTimeChanged.Invoke(remainingTime);
            state = GameState.Await;
        }

        /// <summary>
        /// 处理玩家选择的面具
        /// </summary>
        public void SelectMask(MaskType selectedMask)
        {
            ResolveAnswer(selectedMask, false);
        }

        private void ResolveAnswer(MaskType selectedMask, bool isTimeout)
        {
            if (state != GameState.Await)
                return;

            state = GameState.Resolve;
            ProcessAnswer(selectedMask, isTimeout);
        }

        /// <summary>
        /// 处理答案
        /// </summary>
        private void ProcessAnswer(MaskType selectedMask, bool isTimeout)
        {
            AnswerOutcome outcome;
            if (isTimeout)
            {
                outcome = AnswerOutcome.Timeout;
            }
            else if (selectedMask == currentEncounter.correctMask)
            {
                outcome = AnswerOutcome.Correct;
            }
            else
            {
                outcome = AnswerOutcome.Wrong;
            }

            totalAnswers++;
            if (outcome == AnswerOutcome.Correct)
            {
                correctAnswers++;
            }

            // 获取选项的反馈文本
            string feedbackText = "";
            if (!isTimeout && currentEncounter.optionFeedbacks != null)
            {
                int selectedIndex = (int)selectedMask;
                if (selectedIndex >= 0 && selectedIndex < currentEncounter.optionFeedbacks.Length)
                {
                    feedbackText = currentEncounter.optionFeedbacks[selectedIndex];
                }
            }
            else if (isTimeout)
            {
                feedbackText = "超时了！";
            }

            OnAnswerResult.Invoke(isCorrect, isTimeout, feedbackText);

            if (outcome != AnswerOutcome.Correct)
            {
                // 选错或超时 - 扣除社交电池
                socialBattery -= gameConfig.batteryPenalty;
                OnBatteryChanged.Invoke(socialBattery);

                if (socialBattery <= 0)
                {
                    GameOver();
                    return;
                }
            }

            // 检查是否完成当前天
            currentEncounterIndex++;
            if (currentEncounterIndex >= GetCurrentDayEncounters())
            {
                CompleteDay();
            }
            else
            {
                // 进入下一段对话
                LoadNextEncounter();
            }
        }

        /// <summary>
        /// 完成当前天
        /// </summary>
        private void CompleteDay()
        {
            state = GameState.DayEnd;
            OnDayComplete.Invoke();

            // 检查是否通关所有天数
            if (currentDay >= gameConfig.totalDays)
            {
                GameWin();
            }
            else
            {
                StartCoroutine(AdvanceToNextDay());
            }
        }

        private IEnumerator AdvanceToNextDay()
        {
            yield return new WaitForSeconds(2f);

            currentDay++;
            currentEncounterIndex = 0;
            // 血量不重置，保持当前值

            OnDayChanged.Invoke(currentDay);

            ShuffleEncounters();
            LoadNextEncounter();
        }

        /// <summary>
        /// 游戏胜利
        /// </summary>
        private void GameWin()
        {
            EndGame(true);
        }

        private void SaveStats(bool won)
        {
            PlayerPrefs.SetInt("TotalAnswers", totalAnswers);
            PlayerPrefs.SetInt("CorrectAnswers", correctAnswers);
            PlayerPrefs.SetInt("GameWon", won ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 游戏结束（社死）
        /// </summary>
        private void GameOver()
        {
            EndGame(false);
        }

        private void EndGame(bool won)
        {
            state = GameState.GameEnd;

            if (!won)
                OnGameOver.Invoke();

            SaveStats(won);
            StartCoroutine(LoadScene(won ? "GameWin" : "GameOver"));
        }

        private IEnumerator LoadScene(string sceneName)
        {
            yield return new WaitForSeconds(1.5f);
            SceneManager.LoadScene(sceneName);
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
        public float RemainingTime => remainingTime;
        public GameConfig Config => gameConfig;
    }
}
