using Microsoft.Xna.Framework;
using MonoMod.Utils;
using NPCSchedulers.DATA;
using NPCSchedulers.Store;
using NPCSchedulers.Type;
using StardewValley;
using StardewValley.Pathfinding;

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
            originalSchedule.SaveSchedules();
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
                var schedule = userSchedule.GetSchedule(npcName, key);
                if (editedKeys.Contains(key) && (schedule == null || schedule.Length == 0))
                {
                    editedKeys.Remove(key);
                }
                if (originalKeys.Contains(key) && !editedKeys.Contains(key) && schedule != null && schedule.Length > 0)
                {
                    editedKeys.Add(key);
                }
            }

            return editedKeys;
        }

        /// <summary>
        /// 해당하는 스케줄키의 호감도 컨디션 반환
        /// </summary>
        public static FriendshipConditionEntry GetFriendshipCondition(string npcName, string scheduleKey)
        {
            // 🔹 유저 데이터에서 스케줄 확인
            Dictionary<string, UserScheduleDataType> userData = UserScheduleData.LoadUserSchedules();
            if (userData.ContainsKey(npcName) && userData[npcName].RawData.ContainsKey(scheduleKey))
            {
                string rawSchedule = userData[npcName].RawData[scheduleKey];
                ScheduleEntry.ParseScheduleEntries(npcName, scheduleKey, rawSchedule, out FriendshipConditionEntry friendshipCondition);
                return friendshipCondition ?? new FriendshipConditionEntry(npcName, scheduleKey, new Dictionary<string, int>());
            }

            // 🔹 원본 데이터에서 스케줄 확인
            Dictionary<string, OriginalScheduleDataType> originalData = new OriginalScheduleData().LoadOriginalSchedules();
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
        public static ScheduleDataType GetFilteredSchedule(string npcName, string season, int day, string filter = "all")
        {
            // 최종 스케줄 데이터를 가져옴
            var finalSchedule = GetFinalSchedule(npcName);
            if (filter == "origin")
            {
                finalSchedule = GetOriginalSchedule(npcName);
            }
            else if (filter == "user")
            {
                finalSchedule = GetUserSchedule(npcName);
            }
            // 필터링할 결과 저장
            ScheduleDataType filteredSchedule = new();
            string dayOfWeek = DateUIStateHandler.CalculateDayOfWeek(day);
            // 🔹 날씨(비)와 이벤트(축제)를 먼저 확인
            if (season == "Rain")
            {
                List<string> rainKeys = new List<string>
        {
            ScheduleType.ScheduleKeyType.Normal.Rain50,  // "rain2" (50% 확률)
            ScheduleType.ScheduleKeyType.Normal.Rain,    // "rain"
            $"{day}",                                    // 특정 날짜 (예: "16")
            ScheduleType.ScheduleKeyType.Normal.Default // "default"
        };

                foreach (var key in rainKeys)
                {
                    if (finalSchedule.ContainsKey(key))
                    {
                        filteredSchedule[key] = finalSchedule[key];
                        break;
                    }
                }
            }
            else if (season == "Festival")
            {
                List<string> festivalKeys = new List<string>
        {
            $"{ScheduleType.ScheduleKeyType.Normal.FestivalDay.Replace("{day}", day.ToString())}",  // "festival_{day}"
            ScheduleType.ScheduleKeyType.Normal.Default  // "default"
        };

                foreach (var key in festivalKeys)
                {
                    if (finalSchedule.ContainsKey(key))
                    {
                        filteredSchedule[key] = finalSchedule[key];
                        break;
                    }
                }
            }
            else
            {
                List<string> normalKeys = new List<string>
                    {
                        $"{ScheduleType.ScheduleKeyType.Normal.SeasonDate.Replace("{season}", season.ToLower()).Replace("{day}", day.ToString())}", // "{season}_{day}" (예: "spring_15")

                        $"{ScheduleType.ScheduleKeyType.Normal.Date.Replace("{day}", day.ToString())}",  // "{day}" (예: "16")

                        $"{ScheduleType.ScheduleKeyType.Normal.SeasonDay.Replace("{season}", season.ToLower()).Replace("{dayOfWeek}", dayOfWeek)}",  // "{season}_{dayOfWeek}" (예: "spring_Mon")

                        $"{ScheduleType.ScheduleKeyType.Normal.Day.Replace("{dayOfWeek}", dayOfWeek)}",  // "{dayOfWeek}" (예: "Mon")

                        $"{ScheduleType.ScheduleKeyType.Normal.Season.Replace("{season}", season.ToLower())}",  // "{season}" (예: "spring")

                        ScheduleType.ScheduleKeyType.Normal.Default  // "default" (기본값)
                    };

                // 🔥 모든 가능성(1~14 호감도)에 대해 스케줄 키를 추가
                for (int i = 1; i <= 14; i++)
                {
                    normalKeys.Add($"{ScheduleType.ScheduleKeyType.Normal.DateHearts.Replace("{day}", day.ToString()).Replace("{hearts}", i.ToString())}");  // "{day}_{hearts}" (예: "16_6")

                    normalKeys.Add($"{ScheduleType.ScheduleKeyType.Normal.SeasonDayHearts.Replace("{season}", season.ToLower()).Replace("{dayOfWeek}", dayOfWeek).Replace("{hearts}", i.ToString())}");  // "{season}_{dayOfWeek}_{hearts}" (예: "spring_Mon_6")

                    normalKeys.Add($"{ScheduleType.ScheduleKeyType.Normal.DayHearts.Replace("{dayOfWeek}", dayOfWeek).Replace("{hearts}", i.ToString())}");  // "{dayOfWeek}_{hearts}" (예: "Mon_6")
                }

                foreach (var key in normalKeys)
                {
                    if (finalSchedule.ContainsKey(key))
                    {
                        filteredSchedule[key] = finalSchedule[key];
                        break;
                    }
                }
            }

            return filteredSchedule;
        }

        public static ScheduleDataType GetOriginalSchedule(string npcName)
        {
            ScheduleDataType finalSchedule = new();
            Dictionary<string, OriginalScheduleDataType> originalData = originalSchedule.LoadOriginalSchedules();

            if (originalData.ContainsKey(npcName))
            {
                OriginalScheduleDataType npcData = originalData[npcName];

                foreach (var rawEntry in npcData.RawData)
                {
                    string scheduleKey = rawEntry.Key;
                    string scheduleValue = rawEntry.Value;

                    // 🔹 `finalSchedule`에 존재하지 않는 경우만 추가
                    if (!finalSchedule.ContainsKey(scheduleKey))
                    {
                        finalSchedule[scheduleKey] = ParseScheduleEntries(npcName, scheduleKey, scheduleValue);
                    }
                }
            }

            return finalSchedule;
        }
        public static ScheduleDataType GetUserSchedule(string npcName)
        {
            ScheduleDataType finalSchedule = new();
            Dictionary<string, UserScheduleDataType> userRawData = UserScheduleData.LoadUserSchedules();

            // 1️⃣ 유저 데이터 먼저 확인 (유저가 추가한 다양한 키를 동적으로 반영)
            if (userRawData.ContainsKey(npcName))
            {
                UserScheduleDataType userNpcData = userRawData[npcName];

                foreach (string userKey in userNpcData.RawData.Keys) // 🔥 유저가 추가한 키 목록을 직접 확인
                {
                    if (!finalSchedule.ContainsKey(userKey)) // 🔹 중복 추가 방지
                    {
                        finalSchedule[userKey] = ParseScheduleEntries(npcName, userKey, userNpcData.RawData[userKey]);
                    }
                }
            }
            return finalSchedule;
        }
        /// <summary>
        /// 특정 NPC의 최종 적용된 스케줄을 반환 (유저 데이터가 있으면 우선)
        /// </summary>
        public static ScheduleDataType GetFinalSchedule(string npcName)
        {
            ScheduleDataType finalSchedule = new();
            originalSchedule.LoadData();
            userSchedule.LoadData();
            finalSchedule.AddRangeWithoutSameKey(GetOriginalSchedule(npcName));
            finalSchedule.AddRangeWithoutSameKey(GetUserSchedule(npcName));

            return finalSchedule;
        }

        /// <summary>
        /// 특정 NPC의 스케줄을 저장 (유저 데이터로 추가)
        /// </summary>
        public static void SaveUserSchedule(string npcName, string key, FriendshipConditionEntry friendshipCondition, List<ScheduleEntry> scheduleList)
        {
            Dictionary<string, UserScheduleDataType> userSchedules = UserScheduleData.LoadUserSchedules();

            if (!userSchedules.ContainsKey(npcName))
            {
                userSchedules[npcName] = new UserScheduleDataType();
            }

            string formattedSchedule = string.Join("/", scheduleList.OrderBy(entry => entry.Time)
                .Select(entry => FormatScheduleEntry(entry)));

            var newCondition = FriendshipUIStateHandler.FilterData(friendshipCondition.Condition);
            friendshipCondition.Condition = newCondition;
            string formattedFriendshipCondition = FormatFriendshipEntry(friendshipCondition);
            string newScheduleEntry = formattedFriendshipCondition + formattedSchedule;

            // ✅ `NPCScheduleDataType.RawData`를 통해 접근하도록 변경
            if (formattedSchedule.Length == 0)
            {
                userSchedules[npcName].RawData.Remove(key);
            }
            else
            {
                userSchedules[npcName].RawData[key] = newScheduleEntry;
            }

            userSchedule.SaveUserSchedules(userSchedules);
            ApplyScheduleToNPC(npcName);
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

            for (int i = 0; i < scheduleParts.Length; i++)
            {
                var part = scheduleParts[i];
                string[] elements = part.Split(' ');
                if (elements.Length == 0) continue;

                if (elements[0] == "NOT" && elements[1] == "friendship" && elements.Length >= 4)
                {
                    friendshipCondition = new FriendshipConditionEntry(npcName, key, new Dictionary<string, int> { { elements[2], int.Parse(elements[3]) } });
                    continue;
                }

                // 🔹 GOTO 처리 (재귀적으로 해당 키를 탐색)
                if (elements[0] == "GOTO")
                {
                    string gotoKey = elements[1];

                    // 🔹 GOTO 키에 해당하는 스케줄 데이터 가져오기
                    var finalSchedule = GetScheduleByKeys(npcName, gotoKey, key);
                    if (finalSchedule.TryGetValue(gotoKey, out var gotoScheduleData))
                    {
                        // 🔥 재귀적으로 해당 GOTO 스케줄을 추가
                        entries.AddRange(gotoScheduleData);
                    }
                    continue;
                }

                if (elements.Length < 5) continue;

                int.TryParse(elements[0], out int time);
                string location = elements[1];
                int.TryParse(elements[2], out int x);
                int.TryParse(elements[3], out int y);
                int.TryParse(elements[4], out int direction);
                string action = "None";
                string talk = "None";

                // 🔹 5번째 또는 6번째 요소가 큰따옴표(`"`)로 시작하면 Talk 스케줄로 분류
                if (elements.Length > 5 && elements[5].StartsWith("\""))
                {
                    talk = string.Join(" ", elements.Skip(5)); // 🔥 대사 문자열 결합
                    talk = talk.Trim('\"'); // 🔥 양쪽 `"` 제거

                    // 🔹 talk이 "Strings"로 시작하면 게임 내 콘텐츠 파일에서 로드
                    if (talk.StartsWith("Strings"))
                    {
                        talk = Game1.content.LoadString(talk);

                    }
                    if (string.IsNullOrWhiteSpace(talk))
                    {
                        talk = "None";
                    }
                }
                else if (elements.Length > 5)
                {
                    action = elements[5] ?? "None"; // 🔹 일반 액션 저장
                }
                entries.Add(new ScheduleEntry(key + "/" + i, time, location, x, y, direction, action, talk));
            }

            return (friendshipCondition ?? new FriendshipConditionEntry(npcName, key, new Dictionary<string, int>()), entries);
        }
        public static Dictionary<string, List<ScheduleEntry>> GetScheduleByKeys(string npcName, string scheduleKey, string currentKey)
        {
            Dictionary<string, List<ScheduleEntry>> scheduleEntries = new();
            Dictionary<string, OriginalScheduleDataType> originalData = originalSchedule.LoadOriginalSchedules();
            Dictionary<string, UserScheduleDataType> userData = UserScheduleData.LoadUserSchedules();

            // 🔹 유저 데이터에서 먼저 확인
            if (userData.ContainsKey(npcName) && userData[npcName].RawData.ContainsKey(scheduleKey))
            {
                var parsedEntries = ScheduleEntry.ParseScheduleEntries(npcName, currentKey, userData[npcName].RawData[scheduleKey], out _);
                scheduleEntries[scheduleKey] = parsedEntries;
            }

            // 🔹 원본 데이터에서 확인
            else if (originalData.ContainsKey(npcName) && originalData[npcName].RawData.ContainsKey(scheduleKey))
            {
                var parsedEntries = ScheduleEntry.ParseScheduleEntries(npcName, currentKey, originalData[npcName].RawData[scheduleKey], out _);
                scheduleEntries[scheduleKey] = parsedEntries;
            }

            return scheduleEntries;
        }

        /// <summary>
        /// 우정 조건을 문자열로 변환
        /// </summary>
        private static string FormatFriendshipEntry(FriendshipConditionEntry friendshipConditionEntry)
        {
            if (friendshipConditionEntry.Condition.Count == 0) return "";
            return $"NOT friendship {string.Join(" ", friendshipConditionEntry.Condition.Select(c => $"{c.Key} {c.Value}"))}/";
        }

        /// <summary>
        /// `ScheduleEntry`를 문자열로 변환
        /// </summary>
        private static string FormatScheduleEntry(ScheduleEntry entry)
        {
            return $"{entry.Time} {entry.Location} {entry.X} {entry.Y} {entry.Direction} {(entry.Action == "None" ? "" : entry.Action)} {(entry.Talk == "None" ? "" : entry.Talk)}";
        }
        public static void ApplyScheduleToNPC(string npcName)
        {
            NPC npc = Game1.getCharacterFromName(npcName);
            if (npc == null) return;

            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> schedules = GetUserSchedule(npcName);
            if (schedules.Count == 0) return;

            var scheduleKeys = GetEditedScheduleKeys(npcName);

            foreach (string key in scheduleKeys)
            {
                string todayKey = key;
                if (!schedules.ContainsKey(todayKey)) continue;

                var (condition, scheduleList) = schedules[todayKey];


                foreach (var entry in scheduleList)
                {
                    // 🔹 경로 설정: 현재는 목표 위치 하나만 설정 (추후 개선 가능)
                    Stack<Point> route = new Stack<Point>();
                    route.Push(new Point(entry.X, entry.Y));

                    // 🔹 SchedulePathDescription 객체 생성
                    var pathDescription = new SchedulePathDescription(
                        route,                        // 이동 경로
                        entry.Direction,              // 방향
                        entry.Action == "None" ? "" : entry.Action,       // 도착 후 행동 (null 방지)
                        entry.Talk == "None" ? "" : entry.Talk,             // 도착 후 대사 (null 방지)
                        entry.Location,               // 도착할 위치
                        new Point(entry.X, entry.Y)   // 목표 타일
                    );

                    // 🔹 기존 키를 제거하고 다시 추가
                    if (npc.Schedule.ContainsKey(entry.Time))
                        npc.Schedule.Remove(entry.Time);

                    npc.Schedule.Add(entry.Time, pathDescription);
                }
            }
            Game1.addHUDMessage(new HUDMessage($"{npcName}의 스케줄이 적용되었습니다!", 2));
        }

    }
}
