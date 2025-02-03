using Newtonsoft.Json;

namespace NPCSchedulers.DATA
{
    public class NPCScheduleDataType : AbstractScheduleDataType<NPCScheduleDataType>
    {
        public Dictionary<string, string> RawData { get; private set; }
        public Dictionary<string, List<string>> ScheduleKeys { get; private set; }

        public NPCScheduleDataType()
        {
            RawData = new Dictionary<string, string>();
            ScheduleKeys = new Dictionary<string, List<string>>();
        }

        public static NPCScheduleDataType FromJson(string json)
        {
            return string.IsNullOrWhiteSpace(json)
                ? new NPCScheduleDataType()
                : JsonConvert.DeserializeObject<NPCScheduleDataType>(json) ?? new NPCScheduleDataType();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

}