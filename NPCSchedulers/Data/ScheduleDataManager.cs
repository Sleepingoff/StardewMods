using System.Collections.Generic;
using NPCSchedulers.DATA;

namespace NPCSchedulers
{
    public class ScheduleDataManager
    {
        private static OriginalScheduleData originalSchedule = new OriginalScheduleData();
        private static UserScheduleData userSchedule = new UserScheduleData();

        /// <summary>
        /// 모든 스케줄 데이터를 로드 (유저 데이터 + 원본 데이터)
        /// </summary>
        public static void LoadAllSchedules()
        {
            originalSchedule.LoadData();
            userSchedule.LoadData();
        }

        /// <summary>
        /// 기존 원본 데이터와 유저 데이터를 비교하여 편집된 스케줄 키 목록 반환
        /// </summary>
        public static HashSet<string> GetEditedScheduleKeys(string npcName)
        {
            HashSet<string> editedKeys = new HashSet<string>();
            HashSet<string> originalKeys = originalSchedule.GetScheduleKeys(npcName);
            HashSet<string> userKeys = userSchedule.GetScheduleKeys(npcName);

            foreach (string key in userKeys)
            {
                if (!originalKeys.Contains(key) || originalKeys.Contains(key) && userSchedule.GetSchedule(npcName, key) != originalSchedule.GetSchedule(npcName, key))
                {
                    editedKeys.Add(key);
                }
            }

            return editedKeys;
        }
        public static FriendshipConditionEntry GetFriendshipCondition(string npcName, string scheduleKey)
        {
            // 🔹 유저 데이터에서 스케줄 확인
            Dictionary<string, NPCScheduleDataType> userData = UserScheduleData.LoadUserSchedules();
            if (userData.ContainsKey(npcName) && userData[npcName].RawData.ContainsKey(scheduleKey))
            {
                string rawSchedule = userData[npcName].RawData[scheduleKey];
                ScheduleEntry.ParseScheduleEntries(npcName, scheduleKey, rawSchedule, out FriendshipConditionEntry friendshipCondition);
                return friendshipCondition ?? new FriendshipConditionEntry(npcName, scheduleKey, new Dictionary<string, int>());
            }

            // 🔹 원본 데이터에서 스케줄 확인
            Dictionary<string, NPCScheduleDataType> originalData = new OriginalScheduleData().LoadOriginalSchedules();
            if (originalData.ContainsKey(npcName) && originalData[npcName].RawData.ContainsKey(scheduleKey))
            {
                string rawSchedule = originalData[npcName].RawData[scheduleKey];
                ScheduleEntry.ParseScheduleEntries(npcName, scheduleKey, rawSchedule, out FriendshipConditionEntry friendshipCondition);
                return friendshipCondition ?? new FriendshipConditionEntry(npcName, scheduleKey, new Dictionary<string, int>());
            }

            // 🔹 우정 조건이 없으면 기본값 반환
            return new FriendshipConditionEntry(npcName, scheduleKey, new Dictionary<string, int>());
        }

        /// <summary>
        /// 특정 NPC의 최종 적용된 스케줄을 반환 (유저 데이터가 있으면 우선)
        /// </summary>
        public static Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> GetFinalSchedule(string npcName, string season, int day, string dayOfWeek)
        {
            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> finalSchedule = new();
            Dictionary<string, NPCScheduleDataType> originalData = originalSchedule.LoadOriginalSchedules();
            Dictionary<string, NPCScheduleDataType> userRawData = UserScheduleData.LoadUserSchedules();


            // 1️⃣ 유저 데이터 먼저 확인
            string userKey = $"{season.ToLower()}_{day}";
            if (userRawData.ContainsKey(npcName) && userRawData[npcName].RawData.ContainsKey(userKey))
            {
                finalSchedule[userKey] = ParseScheduleEntries(npcName, userKey, userRawData[npcName].RawData[userKey]);
            }


            // 2️⃣ 원본 데이터 적용
            if (originalData.ContainsKey(npcName))
            {
                NPCScheduleDataType npcData = originalData[npcName];

                foreach (var keyCategory in npcData.ScheduleKeys)
                {
                    foreach (var scheduleKey in keyCategory.Value)
                    {
                        if (npcData.RawData.ContainsKey(scheduleKey) && !finalSchedule.ContainsKey(scheduleKey))
                        {
                            finalSchedule[scheduleKey] = ParseScheduleEntries(npcName, scheduleKey, npcData.RawData[scheduleKey]);
                        }
                    }
                }
            }

            return finalSchedule;
        }

        /// <summary>
        /// 특정 NPC의 스케줄을 저장 (유저 데이터로 추가)
        /// </summary>
        public static void SaveUserSchedule(string npcName, string key, FriendshipConditionEntry friendshipCondition, List<ScheduleEntry> scheduleList)
        {
            Dictionary<string, NPCScheduleDataType> userSchedules = UserScheduleData.LoadUserSchedules();

            if (!userSchedules.ContainsKey(npcName))
            {
                userSchedules[npcName] = new NPCScheduleDataType();
            }

            string formattedSchedule = string.Join("/", scheduleList.OrderBy(entry => entry.Time)
                .Select(entry => FormatScheduleEntry(entry)));

            string formattedFriendshipCondition = FormatFriendshipEntry(friendshipCondition);
            string newScheduleEntry = formattedFriendshipCondition + formattedSchedule;

            // ✅ `NPCScheduleDataType.RawData`를 통해 접근하도록 변경
            userSchedules[npcName].RawData[key] = newScheduleEntry;

            userSchedule.SaveUserSchedules(userSchedules);
        }

        /// <summary>
        /// 스케줄 데이터를 `FriendshipConditionEntry`와 `ScheduleEntry` 리스트로 변환
        /// </summary>
        private static (FriendshipConditionEntry, List<ScheduleEntry>) ParseScheduleEntries(string npcName, string key, string scheduleData)
        {
            List<ScheduleEntry> entries = new();
            FriendshipConditionEntry friendshipCondition = null;

            if (string.IsNullOrWhiteSpace(scheduleData)) return (friendshipCondition, entries);

            string[] scheduleParts = scheduleData.Split('/');

            foreach (var part in scheduleParts)
            {
                string[] elements = part.Split(' ');
                if (elements.Length == 0) continue;

                if (elements[0] == "NOT" && elements[1] == "friendship" && elements.Length >= 4)
                {
                    friendshipCondition = new FriendshipConditionEntry(npcName, key, new Dictionary<string, int> { { elements[2], int.Parse(elements[3]) } });
                    continue;
                }

                if (elements.Length < 5) continue;

                int.TryParse(elements[0], out int time);
                string location = elements[1];
                int.TryParse(elements[2], out int x);
                int.TryParse(elements[3], out int y);
                int.TryParse(elements[4], out int direction);
                string action = elements.Length > 5 ? elements[5] : "None";

                entries.Add(new ScheduleEntry(key, time, location, x, y, direction, action, "None"));
            }

            return (friendshipCondition ?? new FriendshipConditionEntry(npcName, key, new Dictionary<string, int>()), entries);
        }

        /// <summary>
        /// 우정 조건을 문자열로 변환
        /// </summary>
        private static string FormatFriendshipEntry(FriendshipConditionEntry friendshipConditionEntry)
        {
            if (friendshipConditionEntry.Condition.Count == 0) return "";
            return $"NOT friendship {string.Join("/", friendshipConditionEntry.Condition.Select(c => $"{c.Key} {c.Value}"))}/";
        }

        /// <summary>
        /// `ScheduleEntry`를 문자열로 변환
        /// </summary>
        private static string FormatScheduleEntry(ScheduleEntry entry)
        {
            return $"{entry.Time} {entry.Location} {entry.X} {entry.Y} {entry.Direction} {entry.Action}";
        }

        public static void DeleteScheduleEntry(string npcName, string scheduleKey, ScheduleEntry entry)
        {

        }
    }
}
