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
        Neutral, // 无效选项，不加分也不扣血
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

        [Header("NPC生成点")]
        [SerializeField]
        private Transform npcSpawnPoint;

        [SerializeField]
        private List<EncounterData> encounterPool = new List<EncounterData>();
        private bool resLoaded;
        private const string EncounterRes = "Encounters";
        private int poolDay = -1;
        private List<EncounterData> dayPool = new List<EncounterData>();

        // 游戏状态
        private int currentDay = 1;
        private int currentEncounterIndex = 0;
        private int socialBattery;
        private float remainingTime;
        private GameState state;
        private bool isPaused = false;

        // 统计数据
        private int totalAnswers = 0;
        private int correctAnswers = 0;
        private int dailyCorrectAnswers = 0; // 当日正确答案计数

        // 当前对话
        private EncounterData currentEncounter;
        private List<EncounterData> shuffledEncounters = new List<EncounterData>();
        private GameObject currentNPC;

        // 事件
        public UnityEvent<int> OnDayChanged = new UnityEvent<int>();
        public UnityEvent<int> OnBatteryChanged = new UnityEvent<int>();
        public UnityEvent<float> OnTimeChanged = new UnityEvent<float>();
        public UnityEvent<EncounterData> OnNewEncounter = new UnityEvent<EncounterData>();
        public UnityEvent<AnswerOutcome, string> OnAnswerResult =
            new UnityEvent<AnswerOutcome, string>();
        public UnityEvent OnGameOver = new UnityEvent();
        public UnityEvent OnDayComplete = new UnityEvent();
        public UnityEvent OnShowSkillSelection = new UnityEvent(); // 显示技能选择事件

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
            if (state != GameState.Await || isPaused)
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
            socialBattery = gameConfig.initialHealth; // 初始4条血，最大可达7条
            state = GameState.Resolve;
            isPaused = false;
            totalAnswers = 0;
            correctAnswers = 0;
            dailyCorrectAnswers = 0;

            OnDayChanged.Invoke(currentDay);
            OnBatteryChanged.Invoke(socialBattery);

            ShuffleEncounters();
            LoadNextEncounter();
        }

        private List<EncounterData> GetPool()
        {
            if (poolDay == currentDay)
                return dayPool;

            poolDay = currentDay;
            dayPool.Clear();

            IReadOnlyList<EncounterData> src = encounterPool;
            if (encounterSet != null)
            {
                src = encounterSet.Items;
            }
            else if (!resLoaded && encounterPool.Count == 0)
            {
                resLoaded = true;
                encounterPool.AddRange(Resources.LoadAll<EncounterData>(EncounterRes));
            }

            for (int i = 0; i < src.Count; i++)
            {
                EncounterData encounter = src[i];
                if (encounter.dayNumber == currentDay)
                {
                    dayPool.Add(encounter);
                }
            }

            return dayPool;
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
            if (shuffledEncounters.Count == 0)
            {
                ShuffleEncounters();

                if (shuffledEncounters.Count == 0)
                {
                    UnityEngine.Debug.LogWarning(
                        $"GameManager: No encounters found for Day {currentDay}. Check EncounterData dayNumber settings."
                    );
                    return;
                }
            }

            int lastIndex = shuffledEncounters.Count - 1;
            currentEncounter = shuffledEncounters[lastIndex];
            shuffledEncounters.RemoveAt(lastIndex);

            SpawnNPC(currentEncounter);

            // 获取决策时间（应用电池技能加成）
            float baseTime = gameConfig.GetDecisionTime(currentDay);
            float timeBonus = SkillManager.Instance != null ? SkillManager.Instance.GetTimeBonus() : 1f;
            remainingTime = baseTime * timeBonus;

            // 重置遭遇相关的技能状态
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.ResetEncounterSkillStates();
            }

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
            if (state != GameState.Await || isPaused)
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
            else if (IsNeutralMask(selectedMask))
            {
                outcome = AnswerOutcome.Neutral;
            }
            else
            {
                outcome = AnswerOutcome.Wrong;
            }

            totalAnswers++;
            if (outcome == AnswerOutcome.Correct)
            {
                correctAnswers++;
                dailyCorrectAnswers++;
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

            OnAnswerResult.Invoke(outcome, feedbackText);

            if (outcome == AnswerOutcome.Correct)
            {
                // 回答正确 - 增加一点生命值（不超过最大值7）
                if (socialBattery < gameConfig.maxHealth)
                {
                    socialBattery++;
                    OnBatteryChanged.Invoke(socialBattery);
                }
            }
            else if (outcome == AnswerOutcome.Neutral)
            {
                // 无效选项 - 不加分也不扣血
                Debug.Log($"[GameManager] 选择了无效选项，无效果");
            }
            else
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
        /// 检查是否为无效选项
        /// </summary>
        private bool IsNeutralMask(MaskType mask)
        {
            if (currentEncounter == null || currentEncounter.neutralMasks == null)
                return false;

            foreach (var neutralMask in currentEncounter.neutralMasks)
            {
                if (neutralMask == mask)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 完成当前天
        /// </summary>
        private void CompleteDay()
        {
            state = GameState.DayEnd;

            // 输出当日统计
            int totalQuestionsToday = GetCurrentDayEncounters();
            Debug.Log($"===== 第{currentDay}天结束 =====");
            Debug.Log($"当日回答正确: {dailyCorrectAnswers}/{totalQuestionsToday}");
            Debug.Log($"正确率: {(dailyCorrectAnswers * 100f / totalQuestionsToday):F1}%");
            Debug.Log($"剩余生命值: {socialBattery}/{gameConfig.maxHealth}");
            Debug.Log("===================");

            // 重置当日计数器
            dailyCorrectAnswers = 0;

            OnDayComplete.Invoke();

            // 检查是否通关所有天数
            if (currentDay >= gameConfig.totalDays)
            {
                GameWin();
            }
            else
            {
                // 显示技能选择面板（而不是直接进入下一天）
                ShowSkillSelection();
            }
        }

        /// <summary>
        /// 显示技能选择面板
        /// </summary>
        private void ShowSkillSelection()
        {
            Debug.Log("[技能系统] 显示技能选择面板");
            OnShowSkillSelection.Invoke();

            // 查找AwardPanelUI（包括禁用的对象）
            var awardPanel = FindObjectOfType<MaskGame.UI.AwardPanelUI>(true);
            if (awardPanel != null)
            {
                Debug.Log("[技能系统] 找到AwardPanelUI，调用ShowSkillSelection");
                awardPanel.ShowSkillSelection();
            }
            else
            {
                Debug.LogWarning("[GameManager] AwardPanelUI未找到，跳过技能选择");
                StartCoroutine(AdvanceToNextDay());
            }
        }

        /// <summary>
        /// 技能选择完成后调用
        /// </summary>
        public void OnSkillSelectionComplete()
        {
            Debug.Log("[技能系统] 技能选择完成，进入下一天");
            StartCoroutine(AdvanceToNextDay());
        }

        /// <summary>
        /// 恢复生命值（由SkillManager调用）
        /// </summary>
        public void RestoreHealth(int amount)
        {
            int oldHealth = socialBattery;
            socialBattery = Mathf.Min(socialBattery + amount, gameConfig.maxHealth);

            if (socialBattery != oldHealth)
            {
                OnBatteryChanged.Invoke(socialBattery);
                Debug.Log($"[GameManager] 生命值恢复: {oldHealth} -> {socialBattery}");
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
            Debug.Log($"===== 游戏胜利 =====");
            Debug.Log($"总回答正确: {correctAnswers}/{totalAnswers}");
            Debug.Log($"总正确率: {(correctAnswers * 100f / totalAnswers):F1}%");
            Debug.Log($"最终生命值: {socialBattery}/{gameConfig.maxHealth}");
            Debug.Log("===================");

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
        /// 生成NPC
        /// </summary>
        private void SpawnNPC(EncounterData encounter)
        {
            // 清除旧NPC
            if (currentNPC != null)
            {
                Destroy(currentNPC);
            }

            // 生成新NPC
            if (encounter.npcPrefab != null && npcSpawnPoint != null)
            {
                currentNPC = Instantiate(
                    encounter.npcPrefab,
                    npcSpawnPoint.position,
                    Quaternion.identity,
                    npcSpawnPoint
                );

                // 添加入场动画组件（如果预制体上没有）
                if (currentNPC.GetComponent<MaskGame.UI.NPCEntranceAnimation>() == null)
                {
                    currentNPC.AddComponent<MaskGame.UI.NPCEntranceAnimation>();
                }
            }
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

        /// <summary>
        /// 暂停倒计时
        /// </summary>
        public void PauseTimer()
        {
            isPaused = true;
        }

        /// <summary>
        /// 恢复倒计时
        /// </summary>
        public void ResumeTimer()
        {
            isPaused = false;
        }
    }
}
