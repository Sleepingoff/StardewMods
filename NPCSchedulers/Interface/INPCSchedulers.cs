namespace NPCSchedulers.API
{
    public class INPCSchedulers
    {
        public List<string> GetActionList(string npcName)
        {
            return ScheduleDataManager.GetActionList(npcName);

        }
    }
}