using System.Collections.Generic;
using StardewModdingAPI;
using NPCSchedulers.DATA;
using StardewValley;
using StardewValley.Pathfinding;
using Microsoft.Xna.Framework;

namespace NPCSchedulers
{
    public class ScheduleManager
    {
        /// <summary>
        /// 특정 NPC의 최종 스케줄을 결정 (유저 데이터 + 원본 데이터 고려)
        /// </summary>
        public static Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> GetNPCSchedule(NPC npc, string season, int day, string dayOfWeek)
        {
            string npcName = npc.Name;

            // 1️⃣ 유저 스케줄 먼저 확인 (유저가 수정한 데이터가 있으면 우선 적용)
            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> modifiedSchedules = UserScheduleData.LoadScheduleByUser(npcName);

            if (modifiedSchedules.ContainsKey($"{season.ToLower()}_{day}"))
            {
                return modifiedSchedules;
            }

            // 2️⃣ 기본 스케줄 (원본 데이터) 확인
            return ScheduleDataManager.GetFinalSchedule(npcName, season, day, dayOfWeek);
        }

        /// <summary>
        /// 특정 NPC의 스케줄을 저장 (유저 데이터로 추가)
        /// </summary>
        public static void SaveSchedule(string npcName, string season, int dateKey, Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> scheduleEntries)
        {
            // ✅ `ScheduleDataManager`를 활용하여 스케줄 저장
            foreach (var entry in scheduleEntries)
            {
                string key = entry.Key;
                var (friendshipCondition, scheduleList) = entry.Value;

                ScheduleDataManager.SaveUserSchedule(npcName, key, friendshipCondition, scheduleList);
            }

            // ✅ HUD 메시지 출력 (저장 완료)
            Game1.addHUDMessage(new HUDMessage($"{npcName}의 스케줄이 저장되었습니다!", 2));
        }

        /// <summary>
        /// 특정 NPC의 스케줄을 즉시 적용
        /// </summary>
        public static void ApplyScheduleToNPC(string npcName)
        {
            NPC npc = Game1.getCharacterFromName(npcName);
            if (npc == null) return;

            // ✅ NPC의 최종 스케줄 불러오기
            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> schedules = GetNPCSchedule(npc, Game1.currentSeason, Game1.dayOfMonth, Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth));

            if (schedules.Count == 0) return;

            Dictionary<string, (int, SchedulePathDescription)> schedulePathDescription = new Dictionary<string, (int, SchedulePathDescription)>();

            foreach (var element in schedules)
            {
                var (condition, scheduleList) = element.Value;

                foreach (var entry in scheduleList)
                {
                    // 🔹 목표 위치 설정
                    Stack<Point> route = new Stack<Point>();
                    route.Push(new Point(entry.X, entry.Y));

                    var pathDescription = new SchedulePathDescription(
                        route,
                        entry.Direction,
                        entry.Action ?? "None",
                        entry.Talk ?? "",
                        entry.Location,
                        new Point(entry.X, entry.Y)
                    );

                    schedulePathDescription.Add(entry.Key, (entry.Time, pathDescription));
                }
            }

            // ✅ 기존 스케줄 제거 후 새로운 스케줄 적용
            npc.ClearSchedule();
            foreach (var (key, path) in schedulePathDescription)
            {
                var (time, desc) = path;
                npc.Schedule.Add(time, desc);
            }

            // ✅ HUD 메시지 출력 (적용 완료)
            Game1.addHUDMessage(new HUDMessage($"{npcName}의 스케줄이 적용되었습니다!", 2));
        }
    }
}
