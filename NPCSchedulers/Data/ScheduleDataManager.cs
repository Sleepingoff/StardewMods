using Microsoft.Xna.Framework;
using MonoMod.Utils;
using NPCSchedulers.DATA;
using NPCSchedulers.Store;
using NPCSchedulers.Type;
using StardewValley;
using StardewValley.Network;
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
        /// 유저 가 수정한 스케줄 npc 리스트를 가져옴
        /// </summary>
        public static List<string> GetAllNPCListByUser()
        {
            return userSchedule.GetAllNPCList();
        }

        /// <summary>
        /// 로드 된 모든 스케줄을 가져옴
        /// </summary>
        public static HashSet<string> GetAllScheduleKeys(string npcName)
        {
            HashSet<string> allSchedules = new();
            var userKeys = userSchedule.GetScheduleKeys(npcName);
            var originalKeys = originalSchedule.GetScheduleKeys(npcName);
            foreach (string key in userKeys)
            {
                allSchedules.Add(key);
            }
            foreach (string key in originalKeys)
            {
                if (allSchedules.Contains(key)) continue;
                allSchedules.Add(key);
            }

            return allSchedules;
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
        /// 해당하는 스케줄키의 메일 리스트 반환
        /// </summary>
        public static List<string> GetMailList(string npcName, string scheduleKey)
        {
            Dictionary<string, UserScheduleDataType> userData = UserScheduleData.LoadUserSchedules();
            if (userData.ContainsKey(npcName) && userData[npcName].RawData.ContainsKey(scheduleKey))
            {
                string rawSchedule = userData[npcName].RawData[scheduleKey];
                ScheduleEntry.ParseScheduleEntries(npcName, scheduleKey, rawSchedule, out var conditions);
                return conditions.Item2;
            }

            Dictionary<string, OriginalScheduleDataType> originalData = new OriginalScheduleData().LoadOriginalSchedules();
            if (originalData.ContainsKey(npcName) && originalData[npcName].RawData.ContainsKey(scheduleKey))
            {
                string rawSchedule = originalData[npcName].RawData[scheduleKey];
                ScheduleEntry.ParseScheduleEntries(npcName, scheduleKey, rawSchedule, out var conditions);

                return conditions.Item2;
            }

            return new List<string>();
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
                ScheduleEntry.ParseScheduleEntries(npcName, scheduleKey, rawSchedule, out var conditions);
                return conditions.Item1 ?? new FriendshipConditionEntry(npcName, scheduleKey, new Dictionary<string, int>());
            }

            // 🔹 원본 데이터에서 스케줄 확인
            Dictionary<string, OriginalScheduleDataType> originalData = new OriginalScheduleData().LoadOriginalSchedules();
            if (originalData.ContainsKey(npcName) && originalData[npcName].RawData.ContainsKey(scheduleKey))
            {
                string rawSchedule = originalData[npcName].RawData[scheduleKey];
                ScheduleEntry.ParseScheduleEntries(npcName, scheduleKey, rawSchedule, out var conditions);
                return conditions.Item1 ?? new FriendshipConditionEntry(npcName, scheduleKey, new Dictionary<string, int>());
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

                    }
                }
            }
            else if (season == "Festival")
            {
                //todo: festival에 속한 키들 가져오기
                List<string> festivalKeys = new List<string>
        {
            $"{ScheduleType.ScheduleKeyType.Normal.FestivalDay.Replace("{day}", day.ToString())}",  // "festival_{day}"
           //marriage로 시작하는 키
            $"{ScheduleType.ScheduleKeyType.Normal.MarriageDay.Replace("{dayOfWeek}", dayOfWeek)}",  // "marriage_{dayOfWeek}"
         
        };

                foreach (var key in festivalKeys)
                {

                    if (finalSchedule.ContainsKey(key))
                    {
                        filteredSchedule[key] = finalSchedule[key];
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
                        //marriage로 시작하는 키
                        $"{ScheduleType.ScheduleKeyType.Normal.MarriageDay.Replace("{dayOfWeek}", dayOfWeek)}",  // "marriage_{dayOfWeek}"
                        $"{ScheduleType.ScheduleKeyType.Normal.Day.Replace("{dayOfWeek}", dayOfWeek)}",  // "{dayOfWeek}" (예: "Mon")

                        $"{ScheduleType.ScheduleKeyType.Normal.Season.Replace("{season}", season.ToLower())}",  // "{season}" (예: "spring")

                        ScheduleType.ScheduleKeyType.Normal.Default  // "default" (기본값)
                    };

                // 🔥 모든 가능성(1~14 호감도)에 대해 스케줄 키를 추가
                for (int i = 0; i <= 14; i++)
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
                        var result = ParseScheduleEntries(npcName, scheduleKey, scheduleValue);
                        finalSchedule.Add(result.Item1.ScheduleKey, result);
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

                foreach (string userKey in userNpcData.RawData.Keys) // 🔥v0.0.1 유저가 추가한 키 목록을 직접 확인
                {
                    if (!finalSchedule.ContainsKey(userKey)) // 🔹 중복 추가 방지
                    {
                        var result = ParseScheduleEntries(npcName, userKey, userNpcData.RawData[userKey]);
                        finalSchedule.Add(result.Item1.ScheduleKey, result);
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
        public static void SaveUserSchedule(string npcName, string key, ScheduleDataType scheduleData)
        {
            var (friendshipCondition, scheduleList, mailKeys, gotoKey) = scheduleData[key];
            Dictionary<string, UserScheduleDataType> userSchedules = UserScheduleData.LoadUserSchedules();

            if (!userSchedules.ContainsKey(npcName))
            {
                userSchedules[npcName] = new UserScheduleDataType();
            }

            string formattedSchedule = string.Join("/", scheduleList.OrderBy(entry => entry.Time)
                .Select(entry => FormatScheduleEntry(entry)));


            var newCondition = FriendshipUIStateHandler.FilterData(friendshipCondition.Condition);
            friendshipCondition.Condition = newCondition;

            var formattedMail = FormatMailEntry(mailKeys);
            string formattedGoto = "";

            if (formattedMail.Length > 0 && scheduleList.Count > 0)
            {
                if (gotoKey != null && gotoKey.Length > 0)
                {
                    formattedGoto = FormatGOTOEntry(gotoKey);
                    Game1.addHUDMessage(new HUDMessage("applied GOTO scheduleKey", 2));
                }
                else
                {
                    formattedGoto = FormatGOTOEntry("season");
                    //메일은 있는데 goto키가 없을 경우 기본 스케줄로 할당한다.
                    Game1.addHUDMessage(new HUDMessage("Assigned to 'season' key due to missing GOTO key.", 2));
                }
            }
            else if (formattedSchedule.Length > 0 && formattedGoto.Length > 0)
            {
                formattedGoto = "";
                Game1.addHUDMessage(new HUDMessage("Not Applied GOTO key due to remaining schedules", 2));
            }


            string formattedFriendshipCondition = FormatFriendshipEntry(friendshipCondition);

            string newScheduleEntry = formattedFriendshipCondition + formattedMail + formattedGoto + formattedSchedule;

            //v0.0.1 ✅ `NPCScheduleDataType.RawData`를 통해 접근하도록 변경
            if (formattedSchedule.Length == 0)
            {
                userSchedules[npcName].RawData.Remove(key);
            }
            else
            {
                userSchedules[npcName].RawData[key] = newScheduleEntry;
            }

            userSchedule.SaveUserSchedules(userSchedules);
            Game1.addHUDMessage(new HUDMessage("saved schedule", 2));
            ApplyScheduleToNPC(npcName);
        }

        /// <summary>
        /// 스케줄 데이터를 `FriendshipConditionEntry`와 `ScheduleEntry` 리스트로 변환
        /// </summary>
        private static (FriendshipConditionEntry, List<ScheduleEntry>, List<string>, string) ParseScheduleEntries(string npcName, string key, string scheduleData)
        {
            List<ScheduleEntry> entries = new();
            FriendshipConditionEntry friendshipCondition = new FriendshipConditionEntry(npcName, key, new Dictionary<string, int>());
            //v0.0.3 + 메일 파싱 추가
            List<string> mailKeys = new(); // 📌 메일 키만 저장
            string gotoKey = null;
            if (string.IsNullOrWhiteSpace(scheduleData)) return (friendshipCondition, entries, mailKeys, gotoKey);

            string[] scheduleParts = scheduleData.Split('/');
            int i = 0; // 루프 인덱스
                       // 📌 일반 스케줄 엔트리 추가
            while (i < scheduleParts.Length)
            {
                var part = scheduleParts[i];
                string[] elements = part.Split(' ');

                if (elements.Length == 0) break;


                // 📌 Friendship 조건 처리
                if (elements[0] == "NOT" && elements[1] == "friendship" && elements.Length >= 4)
                {
                    friendshipCondition = new FriendshipConditionEntry(npcName, key, new Dictionary<string, int> { { elements[2], int.Parse(elements[3]) } });
                    i++;
                    continue;
                }
                else if (elements[0] == "GOTO")
                {
                    gotoKey = elements[1];
                    if (gotoKey == "season") gotoKey = DateUIStateHandler.selectedSeason.ToLower();
                    if (gotoKey == "NO_SCHEDULE") gotoKey = ScheduleType.ScheduleKeyType.Normal.Default;
                    var finalSchedule = GetScheduleByKeys(npcName, gotoKey, key);
                    //default로 실패하면 always로 재시도
                    if (finalSchedule.Count == 0 && gotoKey == ScheduleType.ScheduleKeyType.Normal.Default)
                    {
                        gotoKey = ScheduleType.ScheduleKeyType.Normal.Always;
                        finalSchedule = GetScheduleByKeys(npcName, gotoKey, key);
                    }
                    if (finalSchedule.TryGetValue(gotoKey, out var gotoScheduleData))
                    {
                        gotoKey = elements[1];
                        entries.AddRange(gotoScheduleData);
                    }
                    i++;
                    continue;
                }
                else if (elements.Length > 1 && elements[0] == "MAIL")
                {
                    for (int k = 1; k < elements.Length; k++)
                    {
                        mailKeys.Add(elements[k]); // 🔹 메일 키 리스트에 추가
                    }
                    i++;
                    continue;

                }
                else if (elements.Length > 4)
                {
                    string entryKey = $"{key}/{i}";

                    var parsed = ParseScheduleEntry(entryKey, part);

                    entries.Add(parsed);
                }
                i++;
            }


            return (friendshipCondition, entries, mailKeys, gotoKey);
        }
        // 🔹 단일 스케줄 엔트리를 파싱하는 메서드
        private static ScheduleEntry ParseScheduleEntry(string entryKey, string schedulePart)
        {
            string[] elements = schedulePart.Split(' ');

            int.TryParse(elements[0], out int time);
            string location = elements[1];
            int.TryParse(elements[2], out int x);
            int.TryParse(elements[3], out int y);
            int.TryParse(elements[4], out int direction);
            string action = null;
            string talk = null;

            // 📌 Talk 스케줄 처리
            if (elements.Length > 5 && elements[5].StartsWith("\""))
            {
                talk = string.Join(" ", elements.Skip(5)).Trim('\"');
                if (talk.StartsWith("Strings"))
                {
                    talk = Game1.content.LoadString(talk);
                }
                if (string.IsNullOrWhiteSpace(talk))
                {
                    talk = null;
                }
            }
            else if (elements.Length > 6 && elements[6].StartsWith("\""))
            {
                action = elements[5];
                talk = string.Join(" ", elements.Skip(6)).Trim('\"');
                if (talk.StartsWith("Strings"))
                {
                    talk = Game1.content.LoadString(talk);
                }
                if (string.IsNullOrWhiteSpace(talk))
                {
                    talk = null;
                }
            }
            else if (elements.Length > 5)
            {
                action = elements[5];
            }

            return new ScheduleEntry(entryKey, time, location, x, y, direction, action, talk);
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
        /// 메일 조건을 문자열로 변환
        /// </summary>
        private static string FormatMailEntry(List<string> mailKeys)
        {
            if (mailKeys.Count == 0) return "";
            return $"MAIL {string.Join(" ", mailKeys)}/";
        }
        /// <summary>
        /// GOTO 조건을 문자열로 변환
        /// </summary>
        private static string FormatGOTOEntry(string gotoKey)
        {
            if (string.IsNullOrWhiteSpace(gotoKey)) return "";
            return $"GOTO {gotoKey}/";
        }



        /// <summary>
        /// `ScheduleEntry`를 문자열로 변환
        /// </summary>
        private static string FormatScheduleEntry(ScheduleEntry entry)
        {
            string scheduleEntry = $"{entry.Time} {entry.Location} {entry.X} {entry.Y} {entry.Direction}";
            //v0.0.2 + null이거나 빈문자열일 경우 예외처리
            if (entry.Action != null && entry.Action != "" && entry.Action != "None")
            {
                scheduleEntry += " " + entry.Action;
            }
            if (entry.Talk != null && entry.Talk != "")
            {
                scheduleEntry += " \"" + entry.Talk + "\"";
            }
            return scheduleEntry;
        }
        public static void ApplyScheduleToNPC(string npcName)
        {
            NPC npc = Game1.getCharacterFromName(npcName);
            if (npc == null) return;
            ScheduleDataType schedules = GetUserSchedule(npcName);
            if (schedules.Count == 0) return;
            //v0.0.2 + 오늘 스케줄과 같은 키만 수정
            //v0.0.3 + 스케줄 키가 없는 경우 기본 스케줄로 변경
            foreach (var key in schedules.Keys)
            {
                //v0.0.4 +
                //todo: 우선순위가 더 높은 키로 사용자가 추가하는 경우
                if (npc.ScheduleKey != key) continue;
                var (_, scheduleList, _, _) = schedules[key];

                foreach (var entry in scheduleList)
                {
                    // 🔹 경로 설정: 현재는 목표 위치 하나만 설정 (추후 개선 가능)
                    Stack<Point> route = new Stack<Point>();
                    route.Push(new Point(entry.X, entry.Y));

                    // 🔹 SchedulePathDescription 객체 생성
                    var pathDescription = new SchedulePathDescription(
                        route,                        // 이동 경로
                        entry.Direction,              // 방향
                        entry.Action ?? entry.Action,       // 도착 후 행동 (null 방지)
                        entry.Talk ?? entry.Talk,             // 도착 후 대사 (null 방지)
                        entry.Location,               // 도착할 위치
                        new Point(entry.X, entry.Y)   // 목표 타일
                    );

                    // 🔹 기존 키를 제거하고 다시 추가
                    if (npc.Schedule.ContainsKey(entry.Time))
                    {
                        npc.Schedule.Remove(entry.Time);
                    }
                    npc.Schedule.Add(entry.Time, pathDescription);

                }
                bool loaded = npc.TryLoadSchedule(key);
                if (loaded) Game1.addHUDMessage(new HUDMessage($"applied {npcName}'s schedule with {key}", 1));
            }

        }


    }
}
