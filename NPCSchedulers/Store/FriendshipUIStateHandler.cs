using System.Collections.Generic;
using NPCSchedulers.DATA;
using StardewValley;

namespace NPCSchedulers.Store
{
    public class FriendshipUIStateHandler : BaseUIStateHandler<Dictionary<string, int>>
    {
        private FriendshipConditionEntry friendshipConditions;
        private List<string> villagers = new();

        public FriendshipUIStateHandler(string npcName, string scheduleKey) : base(npcName, scheduleKey)
        {
            InitData();
        }

        public override void InitData()
        {
            if (scheduleKey == null) return;
            FriendshipConditionEntry condition = ScheduleDataManager.GetFriendshipCondition(npcName, scheduleKey);
            friendshipConditions = condition;
            villagers = Utility.getAllCharacters().Where(npc => npc.IsVillager).Select(npc => npc.Name).ToList();

            foreach (var villager in villagers)
            {
                if (!friendshipConditions.Condition.ContainsKey(villager))
                {
                    friendshipConditions.Condition.Add(villager, 0);
                }
            }
        }
        public override Dictionary<string, int> GetData()
        {
            return friendshipConditions.Condition;
        }

        public override void SaveData(Dictionary<string, int> data)
        {
            friendshipConditions.Condition = data;
        }

        public override void UpdateData(Dictionary<string, int> data)
        {
            var conditions = friendshipConditions.Condition;
            foreach (var key in conditions.Keys)
            {
                if (data.ContainsKey(key))
                {
                    int newLevel = data[key];
                    if (newLevel >= 0 && newLevel <= 14)
                    {
                        conditions[key] = newLevel;
                    }
                }
            }
            SaveData(conditions);
        }

        //지우고 싶은 npc이름을 data의 키로 등록하여 호출
        public override void DeleteData(Dictionary<string, int> data)
        {
            foreach (var npc in data.Keys)
            {
                friendshipConditions.Condition.Remove(npc);
            }
        }
        public static Dictionary<string, int> FilterData(Dictionary<string, int> friendshipCondition)
        {
            Dictionary<string, int> newFriendshipConditionEntry = friendshipCondition;
            var target = newFriendshipConditionEntry.Where(value => value.Value != 0);
            newFriendshipConditionEntry = target.ToDictionary(pair => pair.Key, pair => pair.Value);
            return newFriendshipConditionEntry;
        }
    }
}
