using System.Collections.Generic;
using Newtonsoft.Json;
using NPCSchedulers.Type;

namespace NPCSchedulers.DATA
{



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

        public static Dictionary<string, UserScheduleDataType> LoadUserSchedules()
        {
            string fileContents = LoadFileContents(FilePath);
            var userRawData = string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, Dictionary<string, string>>()
                : JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContents)
                  ?? new Dictionary<string, Dictionary<string, string>>();

            return ConvertUserDataToNPCScheduleDataType(userRawData);
        }

        public static ScheduleDataType LoadScheduleByUser(string npcName)
        {
            ScheduleDataType userSchedules = new();

            // 🔹 `LoadUserSchedules()`를 사용하여 최신 데이터 가져오기
            Dictionary<string, UserScheduleDataType> userData = LoadUserSchedules();

            if (!userData.ContainsKey(npcName))
                return userSchedules;

            foreach (var scheduleEntry in userData[npcName].RawData)
            {
                string key = scheduleEntry.Key;
                string rawSchedule = scheduleEntry.Value;

                var parsedEntries = ScheduleEntry.ParseScheduleEntries(npcName, key, rawSchedule, out var parsedCondition);

                var (parsedFriendshipCondition, parsedMailList, gotoKey) = parsedCondition;
                userSchedules[key] = (parsedFriendshipCondition, parsedEntries, parsedMailList, gotoKey);
            }

            return userSchedules;
        }

        public override string GetSchedule(string npcName, string key)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is Dictionary<string, string> npcSchedules)
            {
                return npcSchedules.ContainsKey(key) ? npcSchedules[key] : null;
            }
            return null;
        }

        public List<string> GetAllNPCList()
        {
            return scheduleData.Keys.ToList();
        }

        public override HashSet<string> GetScheduleKeys(string npcName)
        {
            if (scheduleData.ContainsKey(npcName) && scheduleData[npcName] is Dictionary<string, string> npcSchedules)
            {
                return new HashSet<string>(npcSchedules.Keys);
            }
            return new HashSet<string>();
        }

        public void SaveUserSchedules(Dictionary<string, UserScheduleDataType> userSchedules)
        {
            UserScheduleDataType userScheduleDataType = new UserScheduleDataType();
            HashSet<string> visitedKeys = new HashSet<string>();
            Dictionary<string, Dictionary<string, string>> formattedData = userSchedules
            .ToDictionary(
                kvp => kvp.Key, // 🔹 NPC 이름
                kvp => kvp.Value.RawData // 🔹 해당 NPC의 RawData (scheduleKey -> scheduleValue)
            );

            string json = JsonConvert.SerializeObject(formattedData, Formatting.Indented);
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

        private static Dictionary<string, UserScheduleDataType> ConvertUserDataToNPCScheduleDataType(Dictionary<string, Dictionary<string, string>> userRawData)
        {
            Dictionary<string, UserScheduleDataType> convertedData = new();

            foreach (var npcEntry in userRawData)
            {
                UserScheduleDataType npcScheduleData = new UserScheduleDataType();

                foreach (var scheduleEntry in npcEntry.Value)
                {
                    if (string.IsNullOrWhiteSpace(scheduleEntry.Key) || string.IsNullOrWhiteSpace(scheduleEntry.Value))
                    {
                        continue; // 🚨 잘못된 데이터는 저장하지 않고 넘어감
                    }

                    npcScheduleData.RawData[scheduleEntry.Key] = scheduleEntry.Value;
                }

                convertedData[npcEntry.Key] = npcScheduleData;
            }

            return convertedData;
        }

    }
}
