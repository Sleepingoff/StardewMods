using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace NPCSchedulers.DATA
{
    public class OriginalScheduleDataType : AbstractScheduleDataType<OriginalScheduleDataType>
    {
    }
    public class OriginalScheduleData : AbstractScheduleData
    {
        private static readonly string DataPath = Path.Combine(ModEntry.Instance.Helper.DirectoryPath, "schedule_data.json");

        //내부 상태 업데이트
        public override void LoadData()
        {
            scheduleData.Clear();
            string fileContents = LoadFileContents(DataPath);
            var parsedData = ParseFileContents(fileContents) as Dictionary<string, NPCScheduleDataType>;

            if (parsedData != null)
            {
                foreach (var npcEntry in parsedData)
                {
                    scheduleData[npcEntry.Key] = npcEntry.Value;
                }
            }
        }

        //상태 변경 없음
        public Dictionary<string, NPCScheduleDataType> LoadOriginalSchedules()
        {
            string fileContents = LoadFileContents(DataPath);
            return string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, NPCScheduleDataType>()
                : JsonConvert.DeserializeObject<Dictionary<string, NPCScheduleDataType>>(fileContents)
                  ?? new Dictionary<string, NPCScheduleDataType>();
        }
        public override object GetSchedule(string npcName, string key)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is NPCScheduleDataType npcData)
            {
                return npcData.RawData.ContainsKey(key) ? npcData.RawData[key] : null;
            }
            return null;
        }

        public override HashSet<string> GetScheduleKeys(string npcName)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is NPCScheduleDataType npcData)
            {
                return new HashSet<string>(npcData.RawData.Keys);
            }
            return new HashSet<string>();
        }

        /// <summary>
        /// 파일 내용을 JSON으로 변환하는 메서드
        /// </summary>
        protected override object ParseFileContents(string fileContents)
        {
            return string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, NPCScheduleDataType>()
                : JsonConvert.DeserializeObject<Dictionary<string, NPCScheduleDataType>>(fileContents)
                  ?? new Dictionary<string, NPCScheduleDataType>();
        }
    }
}
