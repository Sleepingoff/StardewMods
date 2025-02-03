namespace NPCSchedulers.DATA
{
    public class OriginalScheduleData : AbstractScheduleData
    {
        public override void LoadData()
        {
            scheduleData.Clear();
            var rawData = ScheduleManager.LoadScheduleRawData(); // schedules_data.json 로드

            foreach (var npcEntry in rawData)
            {
                scheduleData[npcEntry.Key] = npcEntry.Value;
            }
        }

        public override object GetSchedule(string npcName, string key)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is NPCScheduleData npcData)
            {
                return npcData.RawData.ContainsKey(key) ? npcData.RawData[key] : null;
            }
            return null;
        }

        public override HashSet<string> GetScheduleKeys(string npcName)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is NPCScheduleData npcData)
            {
                return new HashSet<string>(npcData.RawData.Keys);
            }
            return new HashSet<string>();
        }

        /// <summary>
        /// 원본 스케줄은 변경할 수 없도록 SaveSchedule을 오버라이드하여 비활성화
        /// </summary>
        public override void SaveSchedule(string npcName, string key, object scheduleEntry)
        {
            throw new System.InvalidOperationException("Cannot modify original schedule data.");
        }

        /// <summary>
        /// 원본 스케줄은 업데이트할 수 없도록 UpdateSchedule을 오버라이드하여 비활성화
        /// </summary>
        public override void UpdateSchedule(string npcName, string key, object newScheduleEntry)
        {
            throw new System.InvalidOperationException("Cannot modify original schedule data.");
        }
    }
}
