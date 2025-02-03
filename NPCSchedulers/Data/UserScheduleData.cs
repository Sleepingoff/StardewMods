namespace NPCSchedulers.DATA
{
    public class UserScheduleData : AbstractScheduleData
    {
        public override void LoadData()
        {
            scheduleData.Clear();
            var modifiedData = ScheduleManager.LoadScheduleByUser("");

            foreach (var npcEntry in modifiedData)
            {
                scheduleData[npcEntry.Key] = npcEntry.Value.Item2;
            }
        }

        public override object GetSchedule(string npcName, string key)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is List<ScheduleEntry> scheduleList)
            {
                return scheduleList.Find(entry => entry.Key == key);
            }
            return null;
        }

        public override HashSet<string> GetScheduleKeys(string npcName)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is List<ScheduleEntry> scheduleList)
            {
                return new HashSet<string>(scheduleList.ConvertAll(entry => entry.Key));
            }
            return new HashSet<string>();
        }

        public void SaveUserSchedule(string npcName, string key, ScheduleEntry scheduleEntry)
        {
            SaveSchedule(npcName, key, scheduleEntry);
            ScheduleManager.SaveSchedule(npcName, key.Split('_')[0], int.Parse(key.Split('_')[1]), new Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>
            {
                { key, (new FriendshipConditionEntry(npcName, key, new Dictionary<string, int>()), new List<ScheduleEntry> { scheduleEntry }) }
            });
        }

        public void UpdateUserSchedule(string npcName, string key, ScheduleEntry newScheduleEntry)
        {
            UpdateSchedule(npcName, key, newScheduleEntry);
            ScheduleManager.SaveSchedule(npcName, key.Split('_')[0], int.Parse(key.Split('_')[1]), new Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>
            {
                { key, (new FriendshipConditionEntry(npcName, key, new Dictionary<string, int>()), new List<ScheduleEntry> { newScheduleEntry }) }
            });
        }
    }
}
