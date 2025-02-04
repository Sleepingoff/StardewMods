
using Microsoft.Xna.Framework;
using NPCSchedulers.DATA;
using StardewValley;

namespace NPCSchedulers.UI
{
    public class UIStateManager
    {
        // 🔹 현재 UI 상태를 저장하는 싱글톤 인스턴스
        private static UIStateManager instance;
        public static UIStateManager Instance => instance ??= new UIStateManager();

        // 🔹 UI 상태 변수들
        public bool IsSchedulePageOpen { get; private set; } = false;
        public string SelectedSeason { get; private set; } = "Spring";
        public int SelectedDate { get; private set; } = 1;
        public bool IsEditMode { get; private set; } = false;
        public string DayOfWeek { get; private set; } = "Mon"; // 🔹 날짜 기반 요일 계산

        // 🔹 현재 스케줄을 가진 NPC
        public NPC CurrentNPC { get; private set; } = null;

        // 🔹 스케줄의 호감도에 적용할 NPC 리스트
        public List<NPC> SelectedNPC { get; private set; } = new();

        public string EditedScheduleKey { get; private set; } = null;

        // 🔹 스케줄의 호감도에 적용할 NPC들의 호감도 관련 상태
        public List<int> FriendshipLevel { get; private set; } = new();  // 선택된 NPC들과의 호감도
        public Dictionary<string, int> EditedFriendshipCondition { get; private set; } = new();

        // 🔹 현재 NPC의 스케줄 데이터 (UI에서 참조)
        private Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> currentNPCSchedules = new();

        /*
        UI
        */

        public static Rectangle GetMenuPosition()
        {
            var currentMenu = Game1.activeClickableMenu;
            if (currentMenu == null) return new Rectangle(0, 0, 0, 0);
            return new Rectangle(currentMenu.xPositionOnScreen, currentMenu.yPositionOnScreen, currentMenu.width, currentMenu.height);
        }

        /*
        DATA
        */

        // 🔹 스케줄 페이지 열고 닫기
        public void ToggleSchedulePage()
        {
            IsSchedulePageOpen = !IsSchedulePageOpen;
        }

        public void SetCurrentNpc(NPC npc)
        {
            CurrentNPC = npc;
        }

        // 🔹 호감도 Condition 일괄 업데이트
        public void UpdateFriendshipCondition()
        {
            foreach (var npc in SelectedNPC)
            {
                int index = SelectedNPC.FindIndex(n => n.Name == npc.Name);
                int friendshipLevel = FriendshipLevel[index];
                EditedFriendshipCondition[npc.Name] = friendshipLevel;
            }
        }

        // 🔹 NPC 선택 & 호감도 업데이트
        public void SetSelectedNPC(NPC npc)
        {
            if (!SelectedNPC.Any(n => n.Name == npc.Name))
            {
                SelectedNPC.Add(npc);
                InitFriendshipLevel(npc); // 🔥 NPC 선택 시 자동으로 호감도 업데이트
            }
        }

        // 🔹 NPC 선택 & 호감도 업데이트
        public void SetFriendshipLevel(NPC npc, int newLevel)
        {
            int index = SelectedNPC.FindIndex(n => n.Name == npc.Name);
            if (index != -1)
                FriendshipLevel[index] = newLevel;
            else
            {
                SetSelectedNPC(npc);
                SetFriendshipLevel(npc, newLevel);
            }
        }

        // 🔹 NPC의 현재 호감도 초기화
        public void InitFriendshipLevel(NPC npc)
        {
            int index = SelectedNPC.FindIndex(n => n.Name == npc.Name);
            // 🔹 NPC가 리스트에 없으면 추가

            if (FriendshipLevel.Count > index)
                FriendshipLevel[index] = 0;
            else
            {
                for (int i = FriendshipLevel.Count - 1; i <= index; i++)
                {
                    FriendshipLevel.Add(0);
                }
            }

        }

        public void SetSeasonNext(int direction)
        {
            List<string> seasons = new List<string> { "Spring", "Summer", "Fall", "Winter" };
            int index = seasons.IndexOf(SelectedSeason);
            // 🔹 계절 순환 로직 수정
            int nextIndex = (index + direction) % seasons.Count;
            if (nextIndex < 0) nextIndex += seasons.Count; // 🔹 음수 방지 (리스트 끝으로 이동)
            SetSelectedSeason(seasons[nextIndex]);
        }

        // 🔹 계절 변경
        public void SetSelectedSeason(string season)
        {
            SelectedSeason = season;
            LoadNPCSchedules();
        }

        // 🔹 날짜 변경
        public void SetSelectedDate(int date)
        {
            SelectedDate = Math.Clamp(date, 1, 28);
            DayOfWeek = CalculateDayOfWeek(SelectedDate); // 🔥 날짜 기준 요일 업데이트
            LoadNPCSchedules();
        }

        // 🔹 요일 계산
        private string CalculateDayOfWeek(int date)
        {
            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            return days[(date - 1) % 7]; // 1일부터 시작하므로 (date - 1)
        }

        // 🔹 편집 UI 활성화/비활성화
        public void ToggleEditMode(string scheduleKey = null)
        {
            IsEditMode = !IsEditMode;
            EditedScheduleKey = IsEditMode ? scheduleKey : null;

            // 🔹 편집 모드가 켜질 때, 기존 우정 조건을 불러옴
            if (IsEditMode && SelectedNPC.Count > 0)
            {
                LoadFriendshipCondition();
            }
            else
            {
                EditedFriendshipCondition.Clear();
            }
        }

        // 🔹 편집 중인 스케줄의 우정 조건 불러오기
        private void LoadFriendshipCondition()
        {
            if (SelectedNPC.Count > 0 && EditedScheduleKey != null)
            {
                FriendshipConditionEntry condition = ScheduleDataManager.GetFriendshipCondition(CurrentNPC.Name, EditedScheduleKey);
                EditedFriendshipCondition = condition?.Condition ?? new Dictionary<string, int>();
            }
        }

        // 🔹 편집 중인 스케줄의 우정 조건 변경
        public void SetEditedFriendshipCondition(string npcName, int requiredLevel = 0)
        {
            if (EditedFriendshipCondition.ContainsKey(npcName))
            {
                EditedFriendshipCondition[npcName] = requiredLevel;
            }
            else
            {
                EditedFriendshipCondition.Add(npcName, requiredLevel);
            }
        }

        // 🔹 현재 선택된 NPC의 스케줄 데이터 로드
        private void LoadNPCSchedules()
        {
            if (CurrentNPC != null)
            {
                currentNPCSchedules = ScheduleDataManager.GetFinalSchedule(CurrentNPC.Name, SelectedSeason, SelectedDate, DayOfWeek);
            }
            else
            {
                currentNPCSchedules.Clear();
            }
        }

        // 🔹 UI에서 현재 NPC의 스케줄을 가져오는 메서드
        public Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> GetCurrentNPCSchedules()
        {
            return currentNPCSchedules;
        }
        public void UpdateScheduleEntry(string scheduleKey, ScheduleEntry updatedEntry)
        {
            if (currentNPCSchedules.ContainsKey(scheduleKey))
            {
                // 🔹 기존 엔트리 찾아서 업데이트
                var entries = currentNPCSchedules[scheduleKey];
                var index = entries.Item2.FindIndex(e => e.Time == updatedEntry.Time);
                if (index != -1)
                {
                    entries.Item2[index] = updatedEntry;
                }
                else
                {
                    entries.Item2.Add(updatedEntry);
                }
                // entries.Item2.Sort((a,b)=> a.Time);
                UpdateFriendshipCondition();
                var newFriendshipEntry = new FriendshipConditionEntry(CurrentNPC.Name, scheduleKey, EditedFriendshipCondition);
                // 🔹 데이터 매니저에도 반영
                ScheduleDataManager.SaveUserSchedule(CurrentNPC.Name, scheduleKey, newFriendshipEntry, entries.Item2);
            }
        }

        public void DeleteScheduleEntry(string scheduleKey, ScheduleEntry entry)
        {

            ScheduleDataManager.DeleteScheduleEntry(CurrentNPC.Name, scheduleKey, entry);
            LoadNPCSchedules();

        }

    }
}
