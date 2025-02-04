namespace NPCSchedulers.Store
{
    public abstract class BaseUIStateHandler<T>
    {
        protected string npcName;
        protected string scheduleKey;

        public BaseUIStateHandler(string npcName, string scheduleKey)
        {
            this.npcName = npcName;
            this.scheduleKey = scheduleKey;
        }

        public abstract void InitData();
        public abstract T GetData();
        public abstract void SaveData(T data);
        public abstract void UpdateData(T data);
        public abstract void DeleteData(T data);
    }
}
