using System.Collections.Generic;
using Newtonsoft.Json;

namespace NPCSchedulers.DATA
{

    public class UserScheduleDataType : AbstractScheduleDataType<UserScheduleDataType>
    {
    }



    public class UserScheduleData : AbstractScheduleData
    {
        private static readonly string FilePath = Path.Combine(ModEntry.Instance.Helper.DirectoryPath, "schedules.json");

        public override void LoadData()
        {
            scheduleData.Clear();
            string fileContents = LoadFileContents(FilePath);
            var parsedData = ParseFileContents(fileContents) as Dictionary<string, Dictionary<string, string>>;

            if (parsedData != null)
            {
                foreach (var npcEntry in parsedData)
                {
                    scheduleData[npcEntry.Key] = npcEntry.Value;
                }
            }
        }

        public override object GetSchedule(string npcName, string key)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is Dictionary<string, string> npcSchedules)
            {
                return npcSchedules.ContainsKey(key) ? npcSchedules[key] : null;
            }
            return null;
        }

        public override HashSet<string> GetScheduleKeys(string npcName)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is Dictionary<string, string> npcSchedules)
            {
                return new HashSet<string>(npcSchedules.Keys);
            }
            return new HashSet<string>();
        }

        /// <summary>
        /// 파일 내용을 JSON으로 변환하는 메서드
        /// </summary>
        protected override object ParseFileContents(string fileContents)
        {
            return string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, Dictionary<string, string>>()
                : JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContents)
                  ?? new Dictionary<string, Dictionary<string, string>>();
        }
    }
}
