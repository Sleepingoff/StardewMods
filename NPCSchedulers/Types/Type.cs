using Newtonsoft.Json;
using NPCSchedulers.DATA;

namespace NPCSchedulers.Type
{
    public abstract class AbstractScheduleDataType<T> where T : AbstractScheduleDataType<T>, new()
    {
        public Dictionary<string, T> Data { get; protected set; }

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

        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(Data, Formatting.Indented);
        }
    }
    public class OriginalScheduleDataType : AbstractScheduleDataType<NPCScheduleDataType>
    {
        public Dictionary<string, string> RawData { get; set; } = new();
        public Dictionary<string, List<string>> ScheduleKeys { get; set; } = new();// Add this property

    }
    public class UserScheduleDataType : AbstractScheduleDataType<NPCScheduleDataType>
    {
        [JsonIgnore]
        public Dictionary<string, string> RawData { get; set; } = new();
        public Dictionary<string, List<string>> ScheduleKeys { get; set; } = new(); // Add this property

        internal void SetData(Dictionary<string, Dictionary<string, string>> dictionary)
        {
            if (dictionary == null) return;

            foreach (var npcEntry in dictionary)
            {
                string npcName = npcEntry.Key;
                Dictionary<string, string> npcSchedules = npcEntry.Value;

                if (!Data.ContainsKey(npcName))
                {
                    Data[npcName] = new NPCScheduleDataType();
                }

                foreach (var scheduleEntry in npcSchedules)
                {
                    string scheduleKey = scheduleEntry.Key;
                    string scheduleValue = scheduleEntry.Value;

                    Data[npcName].Data[scheduleKey] = new NPCScheduleDataType { };
                }
            }
        }

        /// <summary>
        /// JSON 변환 시 Value 값만 저장하도록 오버라이딩
        /// </summary>
        public override string ToJson()
        {

            return JsonConvert.SerializeObject(Data, Formatting.Indented);
        }
    }



    public class ScheduleDataType : Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>, List<string>, string)>
    {

        public void AddRangeWithoutSameKey(ScheduleDataType other)
        {
            foreach (var kvp in other)
            {
                this[kvp.Key] = kvp.Value;

            }
        }
    }

}