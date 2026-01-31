using UnityEngine;

namespace MaskGame.Data
{
    /// <summary>
    /// 游戏配置数据 - 存储游戏全局参数
    /// </summary>
    [System.Serializable]
    public class GameConfig
    {
        [Header("时间设置")]
        [Tooltip("基础决策时间（秒）")]
        public float baseDecisionTime = 10f;

        [Tooltip("每天减少的决策时间（秒）")]
        public float decisionTimeDecrement = 1.5f;

        [Tooltip("最小决策时间（秒）")]
        public float minDecisionTime = 4f;

        [Header("关卡设置")]
        [Tooltip("总天数（无上限）")]
        public int totalDays = 999;

        [Tooltip("每天的对话数量")]
        public int encountersPerDay = 5;

        [Header("生命值设置")]
        [Tooltip("初始生命值")]
        public int initialHealth = 4;

        [Tooltip("最大生命值")]
        public int maxHealth = 7;

        [Tooltip("选错或超时扣除的血量")]
        public int batteryPenalty = 1;

        /// <summary>
        /// 获取指定天的决策时间（每天递减）
        /// </summary>
        public float GetDecisionTime(int day)
        {
            float time = baseDecisionTime - (day - 1) * decisionTimeDecrement;
            return Mathf.Max(time, minDecisionTime);
        }
    }
}
