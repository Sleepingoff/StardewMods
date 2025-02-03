using System.Collections.Generic;

namespace NPCSchedulers.DATA
{
    public abstract class AbstractScheduleData
    {
        protected Dictionary<string, object> scheduleData = new Dictionary<string, object>();

        /// <summary>
        /// 데이터를 로드하는 메서드 (각 자식 클래스에서 구현)
        /// </summary>
        public abstract void LoadData();

        /// <summary>
        /// 특정 NPC의 특정 키에 해당하는 스케줄 데이터를 가져옴
        /// </summary>
        public abstract object GetSchedule(string npcName, string key);

        /// <summary>
        /// NPC의 모든 스케줄 키 반환
        /// </summary>
        public abstract HashSet<string> GetScheduleKeys(string npcName);

        /// <summary>
        /// 특정 NPC의 스케줄을 저장하는 메서드
        /// </summary>
        public virtual void SaveSchedule(string npcName, string key, object scheduleEntry)
        {
            if (!scheduleData.ContainsKey(npcName))
            {
                scheduleData[npcName] = new Dictionary<string, object>();
            }

            var npcSchedules = scheduleData[npcName] as Dictionary<string, object>;
            if (npcSchedules != null)
            {
                npcSchedules[key] = scheduleEntry;
            }
        }

        /// <summary>
        /// 기존 스케줄을 업데이트하는 메서드 (새로운 값이 있으면 덮어쓰기)
        /// </summary>
        public virtual void UpdateSchedule(string npcName, string key, object newScheduleEntry)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is Dictionary<string, object> npcSchedules)
            {
                if (npcSchedules.ContainsKey(key))
                {
                    npcSchedules[key] = newScheduleEntry;
                }
                else
                {
                    SaveSchedule(npcName, key, newScheduleEntry);
                }
            }
            else
            {
                SaveSchedule(npcName, key, newScheduleEntry);
            }
        }
    }
}
