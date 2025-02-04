
using Microsoft.Xna.Framework;
using NPCSchedulers.DATA;
using StardewValley;

namespace NPCSchedulers.Store
{
    public class UIStateManager
    {
        public bool IsSchedulePageOpen { get; private set; } = false;
        public bool IsEditMode { get; private set; } = false;
        public NPC CurrentNPC { get; private set; } = null;
        public string ScheduleKey { get; private set; } = null;
        public string EditedScheduleKey { get; private set; } = null;

        private readonly FriendshipUIStateHandler friendshipHandler;
        private readonly DateUIStateHandler dateHandler;
        public UIStateManager(string npcName)
        {
            NPC npc = Game1.getCharacterFromName(npcName);
            SetCurrentNpc(npc);
            friendshipHandler = new FriendshipUIStateHandler(CurrentNPC.Name, ScheduleKey);
            dateHandler = new DateUIStateHandler(CurrentNPC.Name, ScheduleKey);
        }

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
                friendshipHandler.GetData();
        }

        public NPC GetCurrentNpc()
        {
            return CurrentNPC;
        }
        public void SetCurrentNpc(NPC npc)
        {
            CurrentNPC = npc;
        }


    }
}
