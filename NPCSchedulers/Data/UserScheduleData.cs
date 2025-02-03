using System.Collections.Generic;
using Newtonsoft.Json;

namespace NPCSchedulers.DATA
{

    public class UserScheduleDataType : AbstractScheduleDataType<UserScheduleDataType>
    {
        internal void SetData(Dictionary<string, Dictionary<string, string>> dictionary)
        {
            throw new NotImplementedException();
        }
    }



    public class UserScheduleData : AbstractScheduleData
    {
        private static readonly string FilePath = Path.Combine(ModEntry.Instance.Helper.DirectoryPath, "schedules.json");
        //내부 상태 업데이트
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
        //상태 변경 없음

        public Dictionary<string, NPCScheduleDataType> LoadUserSchedules()
        {
            string fileContents = LoadFileContents(FilePath);
            var userRawData = string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, Dictionary<string, string>>()
                : JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContents)
                  ?? new Dictionary<string, Dictionary<string, string>>();

            return ConvertUserDataToNPCScheduleDataType(userRawData);
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

        public void SaveUserSchedules(Dictionary<string, NPCScheduleDataType> userSchedules)
        {
            UserScheduleDataType userScheduleDataType = new UserScheduleDataType();
            userScheduleDataType.SetData(userSchedules.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.RawData
            ));

            string json = userScheduleDataType.ToJson();
            File.WriteAllText(FilePath, json);
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

        private static Dictionary<string, NPCScheduleDataType> ConvertUserDataToNPCScheduleDataType(Dictionary<string, Dictionary<string, string>> userRawData)
        {
            Dictionary<string, NPCScheduleDataType> convertedData = new();

            foreach (var npcEntry in userRawData)
            {
                NPCScheduleDataType npcScheduleData = new NPCScheduleDataType();

                foreach (var scheduleEntry in npcEntry.Value)
                {
                    npcScheduleData.RawData[scheduleEntry.Key] = scheduleEntry.Value;
                }

                convertedData[npcEntry.Key] = npcScheduleData;
            }

            return convertedData;
        }

    }
}
