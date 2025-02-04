using System.Collections.Generic;
using NPCSchedulers.DATA;
using StardewValley;

namespace NPCSchedulers.Store
{
    public class FriendshipUIStateHandler : IUIStateHandler<(string npcName, int heartLevel)>
    {
        private Dictionary<string, int> friendshipConditions = new();

        public void LoadData()
        {
            if (UIStateManager.Instance.CurrentNPC != null && UIStateManager.Instance.EditedScheduleKey != null)
            {
                FriendshipConditionEntry condition = ScheduleDataManager.GetFriendshipCondition(
                    UIStateManager.Instance.CurrentNPC.Name,
                    UIStateManager.Instance.EditedScheduleKey);

                friendshipConditions = condition?.Condition ?? new Dictionary<string, int>();
            }
            else
            {
                friendshipConditions.Clear();
            }
        }

        public void SaveData((string npcName, int heartLevel) data)
        {
            if (UIStateManager.Instance.CurrentNPC != null)
            {
                friendshipConditions[data.npcName] = data.heartLevel;
                UpdateScheduleCondition();
            }
        }

        public void UpdateData((string npcName, int heartLevel) data)
        {
            if (friendshipConditions.ContainsKey(data.npcName))
            {
                friendshipConditions[data.npcName] = data.heartLevel;
                UpdateScheduleCondition();
            }
        }

        public void DeleteData((string npcName, int heartLevel) data)
        {
            if (friendshipConditions.ContainsKey(data.npcName))
            {
                friendshipConditions.Remove(data.npcName);
                UpdateScheduleCondition();
            }
        }

        private void UpdateScheduleCondition()
        {
            if (UIStateManager.Instance.CurrentNPC != null && UIStateManager.Instance.EditedScheduleKey != null)
            {
                var newFriendshipEntry = new FriendshipConditionEntry(
                    UIStateManager.Instance.CurrentNPC.Name,
                    UIStateManager.Instance.EditedScheduleKey,
                    friendshipConditions);

                ScheduleDataManager.UpdateFriendshipCondition(UIStateManager.Instance.CurrentNPC.Name, UIStateManager.Instance.EditedScheduleKey, newFriendshipEntry);
            }
        }
    }
}
