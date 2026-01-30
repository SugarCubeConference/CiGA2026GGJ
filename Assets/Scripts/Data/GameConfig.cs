using UnityEngine;

namespace MaskGame.Data
{
    /// <summary>
    /// 游戏配置数据 - 存储游戏全局参数
    /// </summary>
    [System.Serializable]
    public class GameConfig
    {
        [Header("难度设置")]
        [Tooltip("初始选择时间（秒）")]
        public float initialDecisionTime = 5f;
        
        [Tooltip("每天减少的时间（秒）")]
        public float timeDecreasePerDay = 0.3f;
        
        [Tooltip("最小决策时间（秒）")]
        public float minDecisionTime = 1.5f;

        [Header("进度设置")]
        [Tooltip("每天需要完成的对话数")]
        public int encountersPerDay = 5;

        [Header("生命值设置")]
        [Tooltip("初始社交电池值")]
        public int initialSocialBattery = 3;
        
        [Tooltip("选错扣除的电池值")]
        public int batteryPenalty = 1;

        /// <summary>
        /// 根据天数计算决策时间
        /// </summary>
        public float GetDecisionTime(int day)
        {
            float time = initialDecisionTime - (day - 1) * timeDecreasePerDay;
            return Mathf.Max(time, minDecisionTime);
        }
    }
}