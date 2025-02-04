
using Microsoft.Xna.Framework;
using NPCSchedulers.DATA;
using StardewValley;

namespace NPCSchedulers.Store
{
    public class UIStateManager
    {
        // 🔹 현재 UI 상태를 저장하는 싱글톤 인스턴스
        private static UIStateManager instance;
        public static UIStateManager Instance => instance ??= new UIStateManager();

        public bool IsSchedulePageOpen { get; private set; } = false;
        public bool IsEditMode { get; private set; } = false;
        public string SelectedSeason { get; private set; } = "Spring";
        public int SelectedDate { get; private set; } = 1;
        public string DayOfWeek { get; private set; } = "Mon";
        public NPC CurrentNPC { get; private set; } = null;
        public string EditedScheduleKey { get; private set; } = null;

        private readonly ScheduleUIStateHandler scheduleHandler = new();
        private readonly FriendshipUIStateHandler friendshipHandler = new();
        private readonly DateUIStateHandler dateHandler = new();
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
        public void ToggleEditMode(string scheduleKey = null)
        {
            IsEditMode = !IsEditMode;
            EditedScheduleKey = IsEditMode ? scheduleKey : null;

            if (IsEditMode)
                friendshipHandler.LoadData();
        }
        public void SetCurrentNpc(NPC npc)
        {
            CurrentNPC = npc;
        }

        // 🔹 NPC 선택 & 호감도 업데이트
        public void SetSelectedNPC(NPC npc)
        {
            CurrentNPC = npc;
            scheduleHandler.LoadData();
            friendshipHandler.LoadData();
        }

        public void UpdateFriendshipCondition(string npcName, int heartLevel)
        {
            friendshipHandler.UpdateData((npcName, heartLevel));
        }
        public void ChangeSeason(int direction)
        {
            dateHandler.ChangeSeason(direction);
        }
        // 🔹 계절 변경
        public void SetSelectedSeason(string season)
        {
            SelectedSeason = season;
        }

        // 🔹 날짜 변경
        public void SetSelectedDate(int date)
        {
            SelectedDate = date;
            DayOfWeek = CalculateDayOfWeek(SelectedDate);
            scheduleHandler.LoadData();
        }
        // 🔹 요일 계산
        private string CalculateDayOfWeek(int date)
        {
            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            return days[(date - 1) % 7]; // 1일부터 시작하므로 (date - 1)
        }


        public Dictionary<string, int> FilterSetEditedFriendshipCondition(Dictionary<string, int> friendshipCondition)
        {
            Dictionary<string, int> newFriendshipConditionEntry = friendshipCondition;
            var target = newFriendshipConditionEntry.Where(value => value.Value != 0);
            newFriendshipConditionEntry = target.ToDictionary(pair => pair.Key, pair => pair.Value);
            return newFriendshipConditionEntry;
        }



        public void DeleteScheduleEntry(string scheduleKey, ScheduleEntry entry)
        {

            ScheduleDataManager.DeleteScheduleEntry(CurrentNPC.Name, scheduleKey, entry);

        }

    }
}
