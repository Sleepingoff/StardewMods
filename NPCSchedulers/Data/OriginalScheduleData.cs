using Newtonsoft.Json;
using NPCSchedulers.Type;
using StardewValley;

namespace NPCSchedulers.DATA
{

    public class OriginalScheduleData : AbstractScheduleData
    {
        private static readonly string DataPath = Path.Combine(ModEntry.Instance.Helper.DirectoryPath, "schedule_data.json");

        //내부 상태 업데이트
        public override void LoadData()
        {
            scheduleData.Clear();
            string fileContents = LoadFileContents(DataPath);
            var parsedData = ParseFileContents(fileContents) as Dictionary<string, OriginalScheduleDataType>;

            if (parsedData != null)
            {
                foreach (var npcEntry in parsedData)
                {
                    scheduleData[npcEntry.Key] = npcEntry.Value;
                }
            }
        }

        //상태 변경 없음
        public Dictionary<string, OriginalScheduleDataType> LoadOriginalSchedules()
        {
            string fileContents = LoadFileContents(DataPath);
            return string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, OriginalScheduleDataType>()
                : JsonConvert.DeserializeObject<Dictionary<string, OriginalScheduleDataType>>(fileContents)
                  ?? new Dictionary<string, OriginalScheduleDataType>();
        }
        public override object GetSchedule(string npcName, string key)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is OriginalScheduleDataType npcData)
            {
                return npcData.RawData.ContainsKey(key) ? npcData.RawData[key] : null;
            }
            return null;
        }

        public override HashSet<string> GetScheduleKeys(string npcName)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is OriginalScheduleDataType npcData)
            {
                return new HashSet<string>(npcData.RawData.Keys);
            }
            return new HashSet<string>();
        }
        public static Dictionary<string, NPCScheduleDataType> GetAllNPCSchedules()
        {
            Dictionary<string, NPCScheduleDataType> npcScheduleData = new();

            // 🔹 모든 NPC 가져오기
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (npc.Schedule == null || !npc.IsVillager) continue; // 🏡 마을 NPC만 가져옴
                var rawData = npc.getMasterScheduleRawData();
                // 🔹 NPC의 스케줄 데이터 변환
                NPCScheduleDataType scheduleData = new NPCScheduleDataType();
                var filteredSchedule = FilterScheduleKeys(scheduleData.RawData);

                foreach (var scheduleEntry in rawData)
                {
                    scheduleData.RawData[scheduleEntry.Key] = scheduleEntry.Value; // 🔹 RawData 저장
                    scheduleData.ScheduleKeys = filteredSchedule; // ✅ 필터링된 스케줄 키 적용
                }

                npcScheduleData[npc.Name] = scheduleData; // 🔹 NPC 이름을 키로 저장
            }

            return npcScheduleData;
        }
        public void SaveSchedules()
        {

            var rawSchedule = GetAllNPCSchedules();

            string json = JsonConvert.SerializeObject(rawSchedule, Formatting.Indented);

            // 파일 저장
            File.WriteAllText(DataPath, json);
        }
        /// <summary>
        /// 파일 내용을 JSON으로 변환하는 메서드
        /// </summary>
        protected override object ParseFileContents(string fileContents)
        {
            return string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, OriginalScheduleDataType>()
                : JsonConvert.DeserializeObject<Dictionary<string, OriginalScheduleDataType>>(fileContents)
                  ?? new Dictionary<string, OriginalScheduleDataType>();
        }

        /// <summary>
        /// RawData에서 스케줄 키를 계절, 요일, 이벤트 등으로 필터링
        /// </summary>
        private static Dictionary<string, List<string>> FilterScheduleKeys(Dictionary<string, string> rawData)
        {
            Dictionary<string, List<string>> classifiedKeys = new();

            foreach (var key in rawData.Keys)
            {
                string category = CategorizeScheduleKey(key);

                if (!classifiedKeys.ContainsKey(category))
                {
                    classifiedKeys[category] = new List<string>();
                }

                classifiedKeys[category].Add(key);
            }

            return classifiedKeys;
        }

        /// <summary>
        /// 스케줄 키를 계절, 요일, 이벤트 등으로 필터링하는 로직 적용
        /// </summary>
        private static string CategorizeScheduleKey(string scheduleKey)
        {
            if (scheduleKey.Contains("_"))
            {
                string[] parts = scheduleKey.Split('_');

                if (parts.Length == 2)
                {
                    if (parts[0] == "spring" || parts[0] == "summer" || parts[0] == "fall" || parts[0] == "winter")
                    {
                        return "Season";
                    }
                    else if (parts[0] == "Mon" || parts[0] == "Tue" || parts[0] == "Wed" || parts[0] == "Thu" || parts[0] == "Fri" || parts[0] == "Sat" || parts[0] == "Sun")
                    {
                        return "Weekday";
                    }
                }

                if (parts.Length == 3 && parts[0] == "marriage")
                {
                    return "Marriage";
                }

                if (parts[0] == "festival" || parts[0] == "event")
                {
                    return "Event";
                }
            }

            return "General"; // 기본 분류
        }

    }
}
