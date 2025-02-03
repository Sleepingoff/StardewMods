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

        /*
        
        사용 목적	LoadData()	LoadUserSchedules()
        scheduleData를 업데이트해야 하는가?	✅	❌
        파일(schedules.json)에서 데이터를 직접 가져오는가?	✅	✅
        내부 상태(scheduleData)를 변경하는가?	✅	❌
        특정 함수에서 최신 데이터를 임시로 가져오는가?	❌	✅

        */
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

        public static Dictionary<string, NPCScheduleDataType> LoadUserSchedules()
        {
            string fileContents = LoadFileContents(FilePath);
            var userRawData = string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, Dictionary<string, string>>()
                : JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContents)
                  ?? new Dictionary<string, Dictionary<string, string>>();

            return ConvertUserDataToNPCScheduleDataType(userRawData);
        }

        public static Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> LoadScheduleByUser(string npcName)
        {
            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> userSchedules = new();

            // 🔹 `LoadUserSchedules()`를 사용하여 최신 데이터 가져오기
            Dictionary<string, NPCScheduleDataType> userData = LoadUserSchedules();

            if (!userData.ContainsKey(npcName))
                return userSchedules;

            foreach (var scheduleEntry in userData[npcName].RawData)
            {
                string key = scheduleEntry.Key;
                string rawSchedule = scheduleEntry.Value;

                var parsedEntries = ScheduleEntry.ParseScheduleEntries(npcName, key, rawSchedule, out var parsedCondition);
                if (parsedCondition == null)
                {
                    parsedCondition = new FriendshipConditionEntry(npcName, key, new Dictionary<string, int>());
                }

                userSchedules[key] = (parsedCondition, parsedEntries);
            }

            return userSchedules;
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
