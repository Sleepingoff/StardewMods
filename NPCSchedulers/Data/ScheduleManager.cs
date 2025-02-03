using System.IO;
using System.Collections.Generic;
using StardewModdingAPI;
using Newtonsoft.Json;
using NPCSchedulers.UI;
using NPCSchedulers.DATA;
using StardewValley;
using StardewValley.Pathfinding;
using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace NPCSchedulers
{
    public class ScheduleManager
    {
        private static string filePath = Path.Combine(ModEntry.Instance.Helper.DirectoryPath, "schedules.json");
        public static void SaveSchedule(string npcName, string season, int dateKey, Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> scheduleEntries)
        {
            string formattedSchedule = "";
            string key = $"{season.ToLower()}_{dateKey}";
            if (scheduleEntries.Count == 0) return;
            string currentKey = scheduleEntries.Keys.First();
            // if (!scheduleEntries.ContainsKey(key)) return;
            // ✅ `scheduleEntries[npcName]`은 튜플이므로 직접 접근해야 함.
            var (friendshipCondition, scheduleList) = scheduleEntries[currentKey];
            if (scheduleList.Count != 0)
                formattedSchedule = string.Join("/", scheduleList
                    .OrderBy(entry => entry.Time)
                    .Select(entry => FormatScheduleEntry(entry))
                );

            string formattedFriendshipCondition = FormatFriendshipEntry(friendshipCondition);
            string newScheduleEntry = formattedFriendshipCondition + formattedSchedule;
            // JSON 파일 저장
            SaveToJson(npcName, key, newScheduleEntry);
        }

        // JSON 저장 메서드
        private static void SaveToJson(string npcName, string key, string schedule)
        {
            var formattedData = new Dictionary<string, Dictionary<string, string>>();

            // ✅ 기존 JSON 파일 로드
            if (File.Exists(filePath))
            {
                string existingJson = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(existingJson))
                {
                    formattedData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(existingJson)
                                    ?? new Dictionary<string, Dictionary<string, string>>();
                }
            }

            // ✅ 같은 NPC가 이미 존재하는지 확인
            if (!formattedData.ContainsKey(npcName))
            {
                formattedData[npcName] = new Dictionary<string, string>(); // ✅ 새로운 NPC 추가
            }

            // ✅ 같은 scheduleKey가 있으면 덮어쓰기, 없으면 추가
            if (formattedData[npcName].ContainsKey(key))
            {
                Console.WriteLine($"🔄 덮어쓰기: {npcName} - {key}");
                formattedData[npcName][key] = schedule; // ✅ 기존 데이터 덮어쓰기
            }
            else
            {
                Console.WriteLine($"🆕 새 스케줄 추가: {npcName} - {key}");
                formattedData[npcName].Add(key, schedule); // ✅ 새로운 데이터 추가
            }

            // ✅ JSON 파일로 저장
            string json = JsonConvert.SerializeObject(formattedData, Formatting.Indented);
            File.WriteAllText(filePath, json);
            // 성공 메시지 출력
            Game1.addHUDMessage(new HUDMessage($"스케줄 저장 완료!", 2));
        }

        public static Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> LoadScheduleByUser(string npcName)
        {
            if (!File.Exists(filePath))
                return new Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>();

            string json = File.ReadAllText(filePath);
            var rawData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json)
                          ?? new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> scheduleData = new Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>();
            foreach (var npcEntry in rawData)
            {
                if (npcName != npcEntry.Key) continue;
                var npcSchedules = new Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>();

                List<KeyValuePair<string, string>> scheduleList = npcEntry.Value.ToList();

                for (int i = 0; i < scheduleList.Count; i++)
                {
                    string key = scheduleList[i].Key; // 예: "spring_14"
                    string rawSchedule = scheduleList[i].Value;

                    var parsedEntries = ParseScheduleEntries(npcName, key, rawSchedule, out var parsedCondition);

                    if (parsedCondition == null)
                    {
                        parsedCondition = new FriendshipConditionEntry(npcName, key, new Dictionary<string, int>());
                    }

                    npcSchedules[key] = (parsedCondition, parsedEntries);
                }

                scheduleData = npcSchedules;
            }

            // ✅ NPC별 스케줄을 하나의 Dictionary로 합쳐서 반환
            return scheduleData;
        }

        // 스케줄 문자열을 ScheduleEntry 리스트로 변환하는 메서드
        private static List<ScheduleEntry> ParseScheduleEntries(string npcName, string key, string rawSchedule, out FriendshipConditionEntry friendshipCondition)
        {
            var entries = new List<ScheduleEntry>();
            friendshipCondition = null;

            if (string.IsNullOrWhiteSpace(rawSchedule))
                return entries;

            var scheduleParts = rawSchedule.Split('/');

            for (int i = 0; i < scheduleParts.Length; i++)
            {
                var elements = scheduleParts[i].Split(' ');
                if (elements.Length == 0) continue;

                // 🔹 1. `NOT friendship` 조건 확인 (우정 조건이 있는 경우)
                if (elements[0] == "NOT" && elements[1] == "friendship" && elements.Length >= 4)
                {
                    string targetNpc = elements[2];
                    int requiredFriendship = int.Parse(elements[3]);

                    friendshipCondition = new FriendshipConditionEntry(npcName, key, new Dictionary<string, int> { { targetNpc, requiredFriendship } });
                    continue;
                }

                // 🔹 2. 일반적인 스케줄 데이터 파싱
                if (elements.Length < 5) continue;

                int.TryParse(elements[0], out int time);
                string location = elements[1];
                int.TryParse(elements[2], out int x);
                int.TryParse(elements[3], out int y);
                int.TryParse(elements[4], out int direction);
                string action = elements.Length > 5 ? elements[5] : "None";

                // 🔥 key 값에 인덱스를 추가하여 고유한 키 생성
                string newKey = $"{key}/{i}";

                entries.Add(new ScheduleEntry(newKey, time, location, x, y, direction, action, "None"));
            }
            // 🔹 3. `friendshipCondition`이 설정되지 않았다면 기본값 생성
            if (friendshipCondition == null)
            {
                friendshipCondition = new FriendshipConditionEntry(npcName, key, new Dictionary<string, int>());
            }

            return entries;
        }


        private static string FormatFriendshipEntry(FriendshipConditionEntry friendshipConditionEntry)
        {
            if (friendshipConditionEntry.Condition.Count == 0) return "";
            string formatted = $"NOT friendship";

            foreach (var condition in friendshipConditionEntry.Condition)
            {
                formatted += $" {condition.Key} {condition.Value}/";
            }

            return formatted;
        }

        private static string FormatScheduleEntry(ScheduleEntry entry)
        {
            // 기본 문자열 (Time 포함)
            string formatted = $"{entry.Time} {entry.Location} {entry.X} {entry.Y} {entry.Direction}";

            // Action이 "None"이 아니면 추가
            if (!string.IsNullOrEmpty(entry.Action) && entry.Action != "None")
            {
                formatted += $" {entry.Action}";
            }
            // Action이 "None"이 아니면 추가
            if (!string.IsNullOrEmpty(entry.Talk) && entry.Talk != "None")
            {
                formatted += $" \"{entry.Talk}\"";
            }

            return formatted;
        }

        //위에는 기본 저장

        //아래는 데이터 파싱
        private static string dataPath = Path.Combine(ModEntry.Instance.Helper.DirectoryPath, "schedule_data.json");

        // 🔥 모든 NPC의 스케줄 키 및 로우 데이터를 분석하고 저장
        public static void SaveScheduleData()
        {
            Dictionary<string, Dictionary<string, object>> allSchedules = new Dictionary<string, Dictionary<string, object>>();

            foreach (NPC npc in Utility.getAllCharacters())
            {
                Dictionary<string, object> npcScheduleData = new Dictionary<string, object>();

                // 🔹 스케줄 키 분석
                Dictionary<string, List<string>> analyzedKeys = AnalyzeScheduleKeys(npc);
                npcScheduleData["ScheduleKeys"] = analyzedKeys;

                // 🔹 로우 데이터 저장
                Dictionary<string, string> rawData = npc.getMasterScheduleRawData();
                npcScheduleData["RawData"] = rawData ?? new Dictionary<string, string>();

                allSchedules[npc.Name] = npcScheduleData;
            }

            string json = JsonConvert.SerializeObject(allSchedules, Formatting.Indented);
            File.WriteAllText(dataPath, json);
            ModEntry.Instance.Monitor.Log("All NPC schedule data (keys & rawData) saved successfully.", LogLevel.Info);
        }

        // 🔥 저장된 JSON 파일을 불러오기
        public static Dictionary<string, Dictionary<string, object>> LoadScheduleData()
        {
            if (!File.Exists(dataPath))
                return new Dictionary<string, Dictionary<string, object>>();

            string json = File.ReadAllText(dataPath);
            return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json)
                   ?? new Dictionary<string, Dictionary<string, object>>();
        }

        // 🔥 특정 NPC의 스케줄 키를 분석하는 함수
        public static Dictionary<string, List<string>> AnalyzeScheduleKeys(NPC npc)
        {
            Dictionary<string, List<string>> scheduleMapping = new Dictionary<string, List<string>>();
            Dictionary<string, string> rawData = npc.getMasterScheduleRawData();

            if (rawData == null || rawData.Count == 0)
            {
                return scheduleMapping; // 🔥 스케줄 데이터 없음
            }

            foreach (string key in rawData.Keys)
            {
                string category = "Events"; // 기본값
                //만약 default 키가 없으면 계절키로, 계절키가 없으면 spring으로 변경?
                if (key.Contains("_"))
                {
                    string[] parts = key.Split('_');
                    if (parts.Length == 2 && int.TryParse(parts[1], out _))
                    {
                        category = "Season + Date"; // 예: "Spring_14"
                    }
                    else if (parts.Length == 2 && !int.TryParse(parts[1], out _) && IsDayOfWeek(parts[1]))
                    {
                        category = "Season + Day of Week"; // 예: "Spring_Sun"
                    }
                    else if (IsDayOfWeek(parts[0]))
                    {
                        category = "Day of Week"; // 예: "Wed_normal"
                    }
                }
                else if (int.TryParse(key, out _))
                {
                    category = "Date"; // 예: "14"
                }
                else if (IsDayOfWeek(key))
                {
                    category = "Day of Week"; // 예: "Tue", "Wed"
                }
                else if (IsValidSeason(key))
                {
                    category = "Season";
                }
                else if (key == "default")
                {
                    category = "Default"; // 기본 스케줄
                }

                // 🔥 매핑 저장
                if (!scheduleMapping.ContainsKey(category))
                {
                    scheduleMapping[category] = new List<string>();
                }
                scheduleMapping[category].Add(key);
            }

            return scheduleMapping;
        }

        // 🔥 요일 키인지 확인하는 함수
        private static bool IsDayOfWeek(string key)
        {
            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            return days.Contains(key);
        }
        public static Dictionary<string, NPCScheduleDataType> LoadScheduleRawData()
        {
            string dataPath = Path.Combine(ModEntry.Instance.Helper.DirectoryPath, "schedule_data.json");

            if (!File.Exists(dataPath))
            {
                return new Dictionary<string, NPCScheduleDataType>();
            }

            try
            {
                string json = File.ReadAllText(dataPath);
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,  // null 값 무시
                    MissingMemberHandling = MissingMemberHandling.Ignore  // 예상치 못한 필드 무시
                };

                // ✅ 확실한 타입 지정하여 JSON 파싱
                var parsedData = JsonConvert.DeserializeObject<Dictionary<string, NPCScheduleDataType>>(json, settings);
                return parsedData ?? new Dictionary<string, NPCScheduleDataType>();
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.Log($"❌ Error parsing schedule data: {ex.Message}", LogLevel.Error);
                return new Dictionary<string, NPCScheduleDataType>();
            }
        }
        // 🔥 새로운 시즌을 고려한 유효성 검사 함수 추가
        private static bool IsValidSeason(string eventKey, string compareSeason = null)
        {
            // 🔹 특별한 시즌 목록 (Rain 관련 키도 포함)
            string[] seasons = { "Spring", "Summer", "Fall", "Winter" };
            string[] specialSeasons = { "Rain", "Festival", "Marriage" };
            string[] festKeys = { "DesertFestival", "TroutDerby", "SquidFest" };

            string lowerEventKey = eventKey.ToLower();

            // 🔹 비교 시즌이 주어진 경우, 정확히 일치하거나 포함되면 `true`
            if (!string.IsNullOrEmpty(compareSeason))
            {

                string lowerCompareSeason = compareSeason.ToLower();
                if (lowerCompareSeason == "festival")
                {
                    return festKeys.Any(f => lowerEventKey.Contains(f.ToLower()));
                }
                else if (lowerCompareSeason == "rain")
                {
                    return specialSeasons.Any(s => lowerEventKey.Contains(s.ToLower()));
                }
                else
                {
                    return seasons.Any(s => lowerEventKey.Contains(s.ToLower())) || specialSeasons.Any(s => lowerEventKey.Contains(s.ToLower()));
                }
            }

            // 🔹 비교 시즌이 없을 경우, 기본적인 시즌/축제 키워드 감지
            if (seasons.Any(s => lowerEventKey.Contains(s.ToLower())) ||
                specialSeasons.Any(s => lowerEventKey.Contains(s.ToLower())) ||
                festKeys.Any(f => lowerEventKey.Contains(f.ToLower())))
            {
                return true;
            }

            return false;
        }
        public static Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> GetNPCSchedule(NPC npc, string season, int day, string dayOfWeek)
        {
            string npcName = npc.Name;
            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> modifiedSchedules = LoadScheduleByUser(npc.Name); // 수정된 schedules.json 불러오기

            Dictionary<string, NPCScheduleDataType> scheduleData = LoadScheduleRawData(); // 🔥 기존 schedule_data.json 로드
            var finalSchedule = new Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>();
            // 1️⃣ 수정된 스케줄이 있으면 우선 적용
            string modifiedKey = $"{season.ToLower()}_{day}";
            if (modifiedSchedules.ContainsKey(modifiedKey))
            {
                var (friendshipCondition, scheduleList) = modifiedSchedules[modifiedKey];

                finalSchedule[modifiedKey] = (friendshipCondition, scheduleList); // 🎯 수정된 스케줄 반환

            }
            // 2️⃣ 기본 스케줄 적용 (schedule_data.json)
            if (!scheduleData.ContainsKey(npcName))
                return new Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>(); // 존재하지 않는 NPC라면 빈 리스트 반환

            var npcData = scheduleData[npcName];


            // 1. Season + Date (가장 구체적인 스케줄, 최우선 적용)
            string seasonDateKey = $"{season.ToLower()}_{day}";
            if (npcData.ScheduleKeys.ContainsKey("Season + Date") && npcData.ScheduleKeys["Season + Date"].Contains(seasonDateKey) && IsValidSeason(seasonDateKey, season))
            {
                var schedule = LoadSchedule(npcName, seasonDateKey, npcData.RawData[seasonDateKey]);
                foreach (var kvp in schedule)
                {
                    string scheduleKey = kvp.Key;
                    List<ScheduleEntry> scheduleList = kvp.Value.Item2;

                    if (!finalSchedule.ContainsKey(scheduleKey))
                    {
                        finalSchedule[scheduleKey] = (null, new List<ScheduleEntry>());
                    }

                    finalSchedule[scheduleKey] = (kvp.Value.Item1, scheduleList);

                }
            }
            string seasonDayKey = $"{season.ToLower()}_{dayOfWeek}";
            // 2. Season + DayOfWeek
            if (npcData.ScheduleKeys.ContainsKey("Season + Day of Week") && npcData.ScheduleKeys["Season + Day of Week"].Contains(seasonDayKey))
            {

                var schedule = LoadSchedule(npcName, seasonDayKey, npcData.RawData[seasonDayKey]);
                foreach (var kvp in schedule)
                {
                    string scheduleKey = kvp.Key;
                    List<ScheduleEntry> scheduleList = kvp.Value.Item2;

                    if (!finalSchedule.ContainsKey(scheduleKey))
                    {
                        finalSchedule[scheduleKey] = (null, new List<ScheduleEntry>());
                    }

                    finalSchedule[scheduleKey] = (kvp.Value.Item1, scheduleList);

                }
            }
            if (npcData.ScheduleKeys.ContainsKey("Events"))
            {
                // 3. 이벤트 이후 스케줄 (이전 일정과 병합)
                foreach (var specialKey in npcData.ScheduleKeys["Events"])
                {
                    if (npcData.RawData.ContainsKey(specialKey))
                    {
                        var specialSchedule = LoadSchedule(npcName, specialKey, npcData.RawData[specialKey]);
                        foreach (var kvp in specialSchedule) // 🔹 specialSchedules의 각 key-value 쌍 반복
                        {
                            string scheduleKey = kvp.Key;
                            List<ScheduleEntry> scheduleList = kvp.Value.Item2;

                            if (!finalSchedule.ContainsKey(scheduleKey))
                            {
                                finalSchedule[scheduleKey] = (null, new List<ScheduleEntry>());
                            }
                            finalSchedule[scheduleKey] = (kvp.Value.Item1, scheduleList);
                            // 🔹 특정 조건에서 특정 이벤트 키 삭제
                            if (IsValidSeason(scheduleKey))
                            {
                                finalSchedule.Remove(scheduleKey);
                            }
                        }
                    }
                }
            }

            // 4. 특정 날짜(Date) 스케줄
            if (npcData.ScheduleKeys.ContainsKey("Date") && npcData.ScheduleKeys["Date"].Contains(day.ToString()))
            {

                var schedule = LoadSchedule(npcName, day.ToString(), npcData.RawData[day.ToString()]);
                foreach (var kvp in schedule)
                {
                    string scheduleKey = kvp.Key;
                    List<ScheduleEntry> scheduleList = kvp.Value.Item2;

                    if (!finalSchedule.ContainsKey(scheduleKey))
                    {
                        finalSchedule[scheduleKey] = (null, new List<ScheduleEntry>());
                    }

                    finalSchedule[scheduleKey] = (kvp.Value.Item1, scheduleList);
                }
            }

            // 5. 요일(Day of Week) 스케줄 추가
            if (npcData.ScheduleKeys.ContainsKey("Day of Week") && npcData.ScheduleKeys["Day of Week"].Contains(dayOfWeek))
            {
                var schedule = LoadSchedule(npcName, dayOfWeek, npcData.RawData[dayOfWeek]);
                foreach (var kvp in schedule)
                {
                    string scheduleKey = kvp.Key;
                    List<ScheduleEntry> scheduleList = kvp.Value.Item2;

                    if (!finalSchedule.ContainsKey(scheduleKey))
                    {
                        finalSchedule[scheduleKey] = (null, new List<ScheduleEntry>());
                    }

                    finalSchedule[scheduleKey] = (kvp.Value.Item1, scheduleList);
                }
            }
            // 7. Default 스케줄 (모든 조건이 없을 때)
            if (finalSchedule.Count == 0 && npcData.ScheduleKeys.ContainsKey("Default"))
            {
                finalSchedule = LoadSchedule(npcName, "default", npcData.RawData["default"]);
            }
            else if (finalSchedule.Count == 0 && !npcData.ScheduleKeys.ContainsKey("Default") && npcData.ScheduleKeys.ContainsKey("Season") && npcData.RawData.ContainsKey("spring"))
            {
                finalSchedule = LoadSchedule(npcName, "spring", npcData.RawData["spring"]);
            }

            // 6. 계절(Season) 스케줄 추가
            if (npcData.ScheduleKeys.ContainsKey("Season") && finalSchedule.Count == 0)
            {
                foreach (var seasonKey in npcData.ScheduleKeys["Season"])
                {
                    // 🔹 현재 시즌이 이벤트 키에 포함되거나, 특별한 시즌(Rain, Festival, Marriage)과 매칭되면 추가
                    if (IsValidSeason(seasonKey, season) && npcData.RawData.ContainsKey(seasonKey))
                    {
                        var seasonSchedule = LoadSchedule(npcName, seasonKey, npcData.RawData[seasonKey]);

                        foreach (var kvp in seasonSchedule)
                        {
                            string scheduleKey = kvp.Key;
                            List<ScheduleEntry> scheduleList = kvp.Value.Item2;

                            if (!finalSchedule.ContainsKey(scheduleKey))
                            {
                                finalSchedule[scheduleKey] = (null, new List<ScheduleEntry>());
                            }

                            finalSchedule[scheduleKey] = (kvp.Value.Item1, scheduleList);
                        }

                    }
                }
            }

            return finalSchedule;
        }

        public static string GetScheduleDataByKey(string npcName, string key)
        {
            var modifiedSchedules = LoadScheduleByUser(npcName); // 수정된 schedules.json 불러오기
            var scheduleData = LoadScheduleRawData(); // 기존 schedule_data.json 불러오기

            // 1️⃣ 특정 NPC의 수정된 스케줄에서 검색
            if (modifiedSchedules.TryGetValue(npcName, out var npcSchedules)) // ✅ NPC 데이터 확인 (튜플)
            {
                var scheduleList = npcSchedules.Item2; // ✅ 튜플에서 List<ScheduleEntry> 가져오기

                // ✅ 리스트에서 특정 날짜(key)에 해당하는 ScheduleEntry 찾기
                var matchingEntries = scheduleList.Where(e => e.Key == key).ToList();

                if (matchingEntries.Count > 0)
                {
                    return string.Join("/", matchingEntries.Select(FormatScheduleEntry));
                }
            }

            // 2️⃣ 기본 스케줄 데이터에서 검색
            if (scheduleData.TryGetValue(npcName, out var npcRawData))
            {
                if (npcRawData.RawData.TryGetValue(key, out var rawSchedule))
                {
                    return rawSchedule;
                }
            }

            return null;
        }



        // 스케줄 엔트리 문자열 변환


        public static Dictionary<string, List<string>> actionList = new Dictionary<string, List<string>>();

        public static void SetActionList(string npcName, string newAction)
        {
            if (!actionList.ContainsKey(npcName))
            {
                actionList.Add(npcName, new List<string>());
            }
            if (actionList[npcName].Contains(newAction)) return;

            else actionList[npcName].Add(newAction);
        }
        public static List<string> GetActionList(string npcName)
        {
            if (!actionList.ContainsKey(npcName))
            {
                return new List<string>();
            }
            return actionList[npcName];

        }
        public static Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> LoadSchedule(string npcName, string key, string scheduleData)
        {
            var scheduleEntries = new Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>();
            FriendshipConditionEntry friendshipCondition = null;
            var entries = new List<ScheduleEntry>();

            if (string.IsNullOrWhiteSpace(scheduleData)) return scheduleEntries;

            var scheduleParts = scheduleData.Split('/');
            var gotoMappings = new Dictionary<string, string>(); // 🔥 GOTO 매핑 저장

            for (int i = 0; i < scheduleParts.Length; i++)
            {
                var part = scheduleParts[i];
                var elements = part.Split(' ');
                if (elements.Length == 0) continue;

                // 🔹 1. NOT friendship 조건 확인 (별도로 저장)
                if (elements[0] == "NOT" && elements[1] == "friendship")
                {
                    var condition = ProcessFriendshipCondition(elements);
                    friendshipCondition = new FriendshipConditionEntry(npcName, key, condition);
                    continue;
                }

                // 🔹 2. GOTO 키를 만나면 즉시 파싱하여 `elements` 변경
                if (elements[0] == "GOTO")
                {
                    string gotoKey = elements[1];

                    // 🔥 `gotoKey`에 해당하는 스케줄 데이터를 가져오기
                    var gotoEntries = ProcessGotoSchedule(npcName, key, gotoKey);

                    if (gotoEntries != null)
                    {
                        entries.AddRange(gotoEntries);
                    }

                    gotoMappings[key] = gotoKey; // 🔥 GOTO 매핑 저장 (예: "Mon" -> "Thu")
                    continue;
                }

                // 🔹 3. 일반적인 스케줄 데이터 파싱
                var entry = ParseScheduleEntry(elements, key, i);
                if (entry != null) entries.Add(entry);
            }

            if (friendshipCondition == null)
            {
                friendshipCondition = new FriendshipConditionEntry(npcName, key, new Dictionary<string, int> { { npcName, 0 } });
            }

            // 🔹 4. 조건과 함께 저장
            scheduleEntries[key] = (friendshipCondition, entries);
            return scheduleEntries;
        }



        private static ScheduleEntry ParseScheduleEntry(string[] elements, string key, int order)
        {
            if (elements.Length < 5) return null;

            int time = int.TryParse(elements[0], out int parsedTime) ? parsedTime : 600;
            string location = elements[1];
            int x = int.TryParse(elements[2], out int parsedX) ? parsedX : 0;
            int y = int.TryParse(elements[3], out int parsedY) ? parsedY : 0;
            int direction = int.TryParse(elements[4], out int parsedDir) ? parsedDir : 2;


            string action = elements.Length > 5 ? elements[5] : "None";
            string talk = elements.Length > 6 && elements[6].StartsWith("\"") ? GetLocalizedString(elements[6]) : "None";

            return new ScheduleEntry(key + "/" + order, time, location, x, y, direction, action, talk);
        }

        private static Dictionary<string, int> ProcessFriendshipCondition(string[] elements)
        {
            if (elements.Length < 4) return new Dictionary<string, int>();

            NPC npc = Game1.getCharacterFromName(elements[2]);
            string npcName = npc.Name;
            int.TryParse(elements[3], out int requiredFriendship);

            return new Dictionary<string, int> { { npcName, requiredFriendship } };
        }
        private static List<ScheduleEntry> ProcessGotoSchedule(string npcName, string key, string gotoKey)
        {
            string gotoScheduleData = GetScheduleDataByKey(npcName, gotoKey);
            if (string.IsNullOrEmpty(gotoScheduleData)) return null;

            var gotoSchedule = LoadSchedule(npcName, key, gotoScheduleData);
            // 🔹 GOTO 대상 스케줄이 존재하는 경우 현재 키에도 동일한 데이터 추가
            if (gotoSchedule.ContainsKey(gotoKey))
            {
                gotoSchedule[key] = gotoSchedule[gotoKey];
            }

            return gotoSchedule.ContainsKey(key) ? gotoSchedule[key].Item2 : new List<ScheduleEntry>();
        }

        private static string GetLocalizedString(string key)
        {
            return Game1.content.LoadString(key.Trim('\"')); // 🔥 파일에서 해당 문자열을 가져옴
        }

        public static void ApplyScheduleToNPC(string npcName)
        {
            NPC npc = Game1.getCharacterFromName(npcName);
            if (npc == null) return;

            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> schedules = LoadScheduleByUser(npcName);
            Dictionary<string, (int, SchedulePathDescription)> schedulePathDescription = new Dictionary<string, (int, SchedulePathDescription)>();
            if (schedules.Count == 0) return;
            foreach (var element in schedules)
            {
                var (condition, scheduleList) = element.Value;
                foreach (var entry in scheduleList)
                {
                    // 🔹 경로 설정: 현재는 단순히 목표 위치 하나만 설정 (추후 경로 계산 필요)
                    Stack<Point> route = new Stack<Point>();
                    route.Push(new Point(entry.X, entry.Y)); // 목표 타일을 경로에 추가

                    var pathDescription = new SchedulePathDescription(
                        route,                        // 이동 경로
                        entry.Direction,              // 방향
                        entry.Action ?? "None",       // 도착 후 행동 (null 방지)
                        entry.Talk ?? "",             // 도착 후 대사 (null 방지)
                        entry.Location,               // 도착할 위치
                        new Point(entry.X, entry.Y)   // 목표 타일
                    );

                    schedulePathDescription.Add(entry.Key, (entry.Time, pathDescription));
                }
            }
            foreach (var (key, path) in schedulePathDescription)
            {
                var (time, desc) = path;
                if (npc.ScheduleKey == key)
                {
                    npc.ClearSchedule();
                    npc.Schedule.Add(time, desc);

                }
            }
            // 🔹 기존 키를 제거하고 다시 추가

            Game1.addHUDMessage(new HUDMessage($"{npcName}의 스케줄이 적용되었습니다!", 2));
        }



    }
}