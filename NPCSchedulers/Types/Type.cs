using Newtonsoft.Json;
using NPCSchedulers.DATA;

namespace NPCSchedulers.Type
{
    public abstract class AbstractScheduleDataType<T> where T : AbstractScheduleDataType<T>, new()
    {
        public Dictionary<string, T> Data { get; private set; }

        protected AbstractScheduleDataType()
        {
            Data = new Dictionary<string, T>();
        }
        public void SetData(Dictionary<string, T> newData)
        {
            Data = newData;
        }
        public static T FromJson(string json)
        {
            T instance = new T();
            instance.Data = string.IsNullOrWhiteSpace(json)
                ? new Dictionary<string, T>()
                : JsonConvert.DeserializeObject<Dictionary<string, T>>(json) ?? new Dictionary<string, T>();
            return instance;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(Data, Formatting.Indented);
        }
    }
    public class OriginalScheduleDataType : AbstractScheduleDataType<OriginalScheduleDataType>
    {
    }
    public class UserScheduleDataType : AbstractScheduleDataType<UserScheduleDataType>
    {
        internal void SetData(Dictionary<string, Dictionary<string, string>> dictionary)
        {
            throw new NotImplementedException();
        }
    }
    public class ScheduleDataType : Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>
    {
    }

}