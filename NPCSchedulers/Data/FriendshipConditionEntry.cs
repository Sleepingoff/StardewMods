namespace NPCSchedulers.DATA
{
    public class FriendshipConditionEntry
    {
        public string ScheduleKey { get; }
        public string Target { get; }
        public Dictionary<string, int> Condition { get; set; }

        public FriendshipConditionEntry(string currentScheduleNpcName, string key, Dictionary<string, int> condition)
        {
            Target = currentScheduleNpcName;
            ScheduleKey = key;
            Condition = condition;
        }

        public void SetCondition(string npcName, int heartLevel)
        {
            Condition = new Dictionary<string, int> { { npcName, heartLevel } };
        }
    }
}