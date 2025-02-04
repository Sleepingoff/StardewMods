namespace NPCSchedulers.Store
{
    public interface IUIStateHandler<T>
    {
        void LoadData();
        void SaveData(T data);
        void UpdateData(T data);
        void DeleteData(T data);
    }
}
