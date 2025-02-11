using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;
namespace NPCSchedulers.DATA
{
    public class FriendshipConditionEntry
    {
        public string ScheduleKey { get; private set; }
        public string Target { get; }
        public Dictionary<string, int> Condition { get; set; }

        /// <summary>
        /// 상세 스케줄 생성
        /// </summary>
        /// <param name="currentScheduleNpcName">현재 스케줄 NPC 이름</param>
        /// <param name="key">스케줄 키</param>
        /// <param name="condition">key: 호감도 대상 npc 이름, value: 최소 호감도 숫자</param>
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

        public void SetScheduleKey(string key)
        {
            ScheduleKey = key;
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

        public static List<ScheduleEntry> ParseScheduleEntries(
            string npcName, string key, string rawSchedule,
            out (FriendshipConditionEntry friendshipCondition, List<string> mailKeys, string gotoKey) scheduleCondition)
        {
            List<ScheduleEntry> entries = new();
            Dictionary<string, int> friendshipConditions = new();
            List<string> mailKeys = new();
            string gotoKey = null;

            if (string.IsNullOrWhiteSpace(rawSchedule))
            {
                scheduleCondition = (new FriendshipConditionEntry(npcName, key, friendshipConditions), mailKeys, gotoKey);
                return entries;
            }

            string[] scheduleParts = rawSchedule.Split('/');

            for (int i = 0; i < scheduleParts.Length; i++)
            {
                string part = scheduleParts[i];
                string[] elements = part.Split(' ');

                if (elements.Length == 0) continue;

                // 🔹 여러 NPC 우정 조건 처리
                if (elements[0] == "NOT" && elements[1] == "friendship")
                {
                    for (int k = 2; k < elements.Length - 1; k += 2)
                    {
                        if (k + 1 < elements.Length && int.TryParse(elements[k + 1], out int level))
                        {
                            friendshipConditions[elements[k]] = level;  // NPC 이름 → 우정 레벨 저장
                        }
                    }
                    continue;
                }

                // 🔹 메일 조건 처리
                if (elements[0] == "MAIL")
                {
                    for (int k = 1; k < elements.Length; k++)
                    {
                        mailKeys.Add(elements[k]);
                    }
                    continue;
                }

                if (elements[0] == "GOTO")
                {
                    gotoKey = elements[1];
                }

                // 🔹 시간 확인 (없으면 기본값 `600` 적용)
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
                    string routine = elements[startIndex];  // "bed"
                    entries.Add(new ScheduleEntry($"{key}/{i}", time, "", 0, 0, 0, routine, null));
                    continue;
                }

                // 🔹 일반적인 스케줄 데이터 파싱 (시간이 없을 수도 있음)
                if (elements.Length - startIndex < 4) continue;

                string location = elements[startIndex];
                int.TryParse(elements[startIndex + 1], out int x);
                int.TryParse(elements[startIndex + 2], out int y);
                int.TryParse(elements[startIndex + 3], out int direction);

                string action = (elements.Length > startIndex + 4) ? elements[startIndex + 4] : null;
                string talk = null;

                // 🔹 "대사"가 있는 경우 처리
                if (elements.Length > startIndex + 5 && elements[startIndex + 5].StartsWith("\""))
                {
                    talk = string.Join(" ", elements.Skip(startIndex + 5)).Trim('\"');

                    // 게임 내 문자열 리소스를 사용하는 경우 변환
                    if (talk.StartsWith("Strings"))
                    {
                        talk = Game1.content.LoadString(talk);
                    }
                }

                entries.Add(new ScheduleEntry($"{key}/{i}", time, location, x, y, direction, action, talk));
            }

            scheduleCondition = (new FriendshipConditionEntry(npcName, key, friendshipConditions), mailKeys, gotoKey);
            return entries;
        }


    }


}


