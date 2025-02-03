using Microsoft.Xna.Framework;
using StardewValley.Menus;
namespace NPCSchedulers.DATA
{

    public static class ScheduleType
    {
        /// <summary>
        /// 스케줄 키 (ScheduleKey) 타입 정의
        /// </summary>
        public static class ScheduleKeyType
        {
            /// <summary>
            /// Special schedules (최우선 적용)
            /// </summary>
            public static class Special
            {
                public const string GreenRain = "GreenRain";  // 초록비 (Year 1 전용)
            }

            /// <summary>
            /// Marriage schedules (결혼한 NPC 전용)
            /// </summary>
            public static class Marriage
            {
                public const string FestivalDay = "marriage_{festival}_{day}";
                public const string Festival = "marriage_{festival}";
                public const string Date = "marriage_{season}_{day}";
                public const string Job = "marriageJob";  // 특정 NPC 전용 (Harvey, Maru, Penny)
                public const string DayOfWeek = "marriage_{dayOfWeek}";
            }

            /// <summary>
            /// Normal schedules (일반 NPC 전용)
            /// </summary>
            public static class Normal
            {
                public const string FestivalDay = "{festival}_{day}";  // 패시브 페스티벌 진행 중
                public const string SeasonDate = "{season}_{day}";  // 특정 날짜 적용 (예: spring_15)
                public const string DateHearts = "{day}_{hearts}";  // 특정 날짜 + 우정 조건 (예: 11_6)
                public const string Date = "{day}";  // 특정 날짜 (예: 16)
                public const string Bus = "bus";  // Pam 전용, 버스 복구 이후
                public const string Rain50 = "rain2";  // 50% 확률로 적용
                public const string Rain = "rain";  // 비 오는 날 적용
                public const string SeasonDayHearts = "{season}_{dayOfWeek}_{hearts}";  // 특정 계절+요일+우정 조건 (예: spring_Mon_6)
                public const string SeasonDay = "{season}_{dayOfWeek}";  // 특정 계절+요일 (예: spring_Mon)
                public const string DayHearts = "{dayOfWeek}_{hearts}";  // 특정 요일+우정 조건 (예: Mon_6)
                public const string Day = "{dayOfWeek}";  // 특정 요일 (예: Mon)
                public const string Season = "{season}";  // 특정 계절 전체 (예: spring)
                public const string SeasonDayGlobal = "spring_{dayOfWeek}";  // (계절 무관) 특정 요일 (예: spring_Mon)
                public const string Always = "spring";  // 기본 스케줄 (항상 존재해야 함)
                public const string Default = "default";  // 기본 스케줄 (없으면 spring 사용)
            }
        }

        public static class ScheduleFormat
        {
            /// <summary>
            /// 이동 스케줄 (Movement) → 특정 location로 이동
            /// {time} {location} {X} {Y} {direction}
            /// </summary>
            public const string Movement = "{time} {location} {X} {Y} {direction}";

            /// <summary>
            /// action 스케줄 (Action) → 이동 후 특정 action 수행
            /// {time} {location} {X} {Y} {direction} {action}
            /// </summary>
            public const string Action = "{time} {location} {X} {Y} {direction} {action}";

            /// <summary>
            /// 대화 스케줄 (Talk) → 특정 위치에서 talk 출력
            /// {time} {location} {X} {Y} {direction} "{talk}"
            /// </summary>
            public const string Talk = "{time} {location} {X} {Y} {direction} \"{talk}\"";

            /// <summary>
            /// 조건부 스케줄 (Friendship Condition) → 특정 NPC와의 우정 조건을 만족해야 실행
            /// NOT friendship {NPC} {레벨}
            /// </summary>
            public const string FriendshipCondition = "NOT friendship {NPC} {레벨}";

            /// <summary>
            /// GOTO 스케줄 → 다른 스케줄을 참조하여 이동
            /// GOTO {ScheduleKey}
            /// </summary>
            public const string Goto = "GOTO {ScheduleKey}";

            /// <summary>
            /// 메일 조건 (Mail Condition) → 특정 메일을 받았을 때만 실행
            /// MAIL {mailKey}
            /// </summary>
            public const string MailCondition = "MAIL {mailKey}";

            /// <summary>
            /// time + routine → 특정 routine 실행 (예: "2440 bed")
            /// {time} {routine}
            /// </summary>
            public const string TimeRoutineKey = "{time} {routine}";
        }
    }

    public class FriendshipConditionEntry
    {
        public string ScheduleKey { get; }
        public string Target { get; }
        public Dictionary<string, int> Condition { get; set; }

        public FriendshipConditionEntry(string currentScheduleNpcName, string key, Dictionary<string, int> condition)
        {
            Target = currentScheduleNpcName;
            ScheduleKey = key;
            Condition = condition;
        }

        public void SetCondition(string npcName, int heartLevel)
        {
            Condition = new Dictionary<string, int> { { npcName, heartLevel } };
        }


    }
    public class ScheduleEntry
    {
        public int Time;  // 🔥 속성이 아닌 필드로 변경
        public string Key;
        public string Location;
        public int X;
        public int Y;
        public int Direction;
        public string Action;
        public string Talk;
        private ClickableComponent bounds; // 🔹 필드로 변경

        public ClickableComponent Bounds => bounds; // 🔹 읽기 전용 속성 유지

        public ScheduleEntry(string key, int time, string location, int x, int y, int direction, string action, string talk)
        {
            Key = key;
            Time = time;
            Location = location;
            X = x;
            Y = y;
            Direction = direction;
            Action = action;
            Talk = talk;
            // 🔹 기본값으로 빈 영역 설정 (추후 UI에서 변경)
            bounds = new ClickableComponent(new Rectangle(0, 0, 0, 0), "");
        }

        // 🔹 UI에서 위치를 동기화하기 위한 메서드
        public void SetBounds(int x, int y, int width, int height)
        {
            bounds.bounds.X = x;
            bounds.bounds.Y = y;
            bounds.bounds.Width = width;
            bounds.bounds.Height = height;

        }

        // 🔹 클릭된 좌표가 현재 스케줄 영역에 포함되는지 확인
        public bool Contains(int x, int y)
        {
            return bounds.bounds.Contains(x, y);
        }

        // 🔹 데이터 수정 메서드 추가
        public void SetScheduleEntry(Dictionary<string, object> updates)
        {
            foreach (var entry in updates)
            {
                switch (entry.Key)
                {
                    case "Time":
                        if (entry.Value is int time) Time = time;
                        break;
                    case "Location":
                        if (entry.Value is string location) Location = location;
                        break;
                    case "X":
                        if (entry.Value is int x) X = x;
                        break;
                    case "Y":
                        if (entry.Value is int y) Y = y;
                        break;
                    case "Direction":
                        if (entry.Value is int direction) Direction = direction;
                        break;
                    case "Action":
                        if (entry.Value is string action) Action = action;
                        break;
                    case "Talk":
                        if (entry.Value is string talk) Talk = talk;
                        break;
                }
            }
        }

        public static List<ScheduleEntry> ParseScheduleEntries(string npcName, string key, string rawSchedule, out FriendshipConditionEntry friendshipCondition)
        {
            List<ScheduleEntry> entries = new();
            friendshipCondition = null;

            if (string.IsNullOrWhiteSpace(rawSchedule)) return entries;

            string[] scheduleParts = rawSchedule.Split('/');

            foreach (var part in scheduleParts)
            {
                string[] elements = part.Split(' ');
                if (elements.Length == 0) continue;

                // 🔹 여러 NPC 우정 조건 처리
                if (elements[0] == "NOT" && elements[1] == "friendship")
                {
                    Dictionary<string, int> condition = new();
                    for (int i = 2; i < elements.Length; i += 2)
                    {
                        if (i + 1 < elements.Length && int.TryParse(elements[i + 1], out int level))
                        {
                            condition[elements[i]] = level;  // NPC 이름 → 우정 레벨 저장
                        }
                    }
                    friendshipCondition = new FriendshipConditionEntry(npcName, key, condition);
                    continue;
                }

                // 🔹 시간 파싱 (시간이 없으면 기본값 `600` 적용)
                int time = 600;
                int startIndex = 0;

                if (int.TryParse(elements[0], out int parsedTime))
                {
                    time = parsedTime;
                    startIndex = 1;  // 시간이 포함된 경우 인덱스 조정
                }

                // 🔹 "2440 bed" 같은 경우를 처리
                if (startIndex == 1 && elements.Length == 2)
                {
                    string action = elements[startIndex];  // "bed"
                    entries.Add(new ScheduleEntry(key, time, "", 0, 0, 0, action, "None"));
                    continue;
                }

                // 🔹 일반적인 스케줄 데이터 파싱 (시간이 없을 수도 있음)
                if (elements.Length - startIndex < 4) continue;

                string location = elements[startIndex];
                int x = int.Parse(elements[startIndex + 1]);
                int y = int.Parse(elements[startIndex + 2]);
                int direction = int.Parse(elements[startIndex + 3]);
                string actionValue = elements.Length > startIndex + 4 ? elements[startIndex + 4] : "None";

                entries.Add(new ScheduleEntry(key, time, location, x, y, direction, actionValue, "None"));
            }

            return entries;
        }

    }
}


