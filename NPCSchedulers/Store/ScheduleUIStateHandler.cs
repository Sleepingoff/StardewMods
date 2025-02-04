using System.Collections.Generic;
using NPCSchedulers.DATA;

namespace NPCSchedulers.Store
{
    public class ScheduleUIStateHandler : IUIStateHandler<ScheduleEntry>
    {
        private Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> currentNPCSchedules;

        public void LoadData()
        {
            if (UIStateManager.Instance.CurrentNPC != null)
            {
                currentNPCSchedules = ScheduleDataManager.GetFinalSchedule(
                    UIStateManager.Instance.CurrentNPC.Name,
                    UIStateManager.Instance.SelectedSeason,
                    UIStateManager.Instance.SelectedDate,
                    UIStateManager.Instance.DayOfWeek);
            }
            else
            {
                currentNPCSchedules.Clear();
            }
        }

        public void SaveData(ScheduleEntry entry)
        {
            if (UIStateManager.Instance.CurrentNPC != null)
            {
                string npcName = UIStateManager.Instance.CurrentNPC.Name;
                ScheduleDataManager.SaveUserSchedule(npcName, entry.Key, new FriendshipConditionEntry(npcName, entry.Key, new Dictionary<string, int>()), new List<ScheduleEntry> { entry });
            }
        }

        public void UpdateData(ScheduleEntry entry)
        {
            if (currentNPCSchedules.ContainsKey(entry.Key))
            {
                var entries = currentNPCSchedules[entry.Key];
                var index = entries.Item2.FindIndex(e => e.Time == entry.Time);
                if (index != -1)
                {
                    entries.Item2[index] = entry;
                }
                else
                {
                    entries.Item2.Add(entry);
                }
                ScheduleDataManager.SaveUserSchedule(UIStateManager.Instance.CurrentNPC.Name, entry.Key, new FriendshipConditionEntry(UIStateManager.Instance.CurrentNPC.Name, entry.Key, new Dictionary<string, int>()), entries.Item2);
            }
        }

        public void DeleteData(ScheduleEntry entry)
        {
            ScheduleDataManager.DeleteScheduleEntry(UIStateManager.Instance.CurrentNPC.Name, entry.Key, entry);
            LoadData();
        }
    }
}
