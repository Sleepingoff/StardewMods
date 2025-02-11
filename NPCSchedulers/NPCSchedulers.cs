using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using NPCSchedulers.UI;
using NPCSchedulers.DATA;

namespace NPCSchedulers
{
    public class ModEntry : Mod
    {
        private SchedulePage schedulePage;
        private bool isProfileMenuOpen = false;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            RegisterEvents();
            PatchMethods();
        }

        private void RegisterEvents()
        {
            Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
        }
        private void PatchMethods()
        {
            Harmony harmony = new Harmony(ModManifest.UniqueID);

            MethodInfo targetDrawMethod = AccessTools.Method(typeof(ProfileMenu), "draw", new System.Type[] { typeof(SpriteBatch) });
            harmony.Patch(targetDrawMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(DrawPostfix)));

            // 🔹 ProfileMenu의 클릭 이벤트 패치
            MethodInfo targetClickMethod = AccessTools.Method(typeof(ProfileMenu), "receiveLeftClick");
            harmony.Patch(targetClickMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(ReceiveLeftClick)));

            // 🔹 ProfileMenu의 클릭 이벤트 패치
            MethodInfo targetHeldMethod = AccessTools.Method(typeof(ProfileMenu), "leftClickHeld");
            harmony.Patch(targetHeldMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(LeftClickHeld)));

            // 🔹 클릭 해제 이벤트 (선택 해제 등 필요할 경우)
            MethodInfo releaseClickMethod = AccessTools.Method(typeof(ProfileMenu), "releaseLeftClick");
            harmony.Patch(releaseClickMethod, postfix: new HarmonyMethod(typeof(ModEntry), nameof(LeftClickReleased)));
        }

        public static bool ReceiveLeftClick(int x, int y)
        {
            Instance.schedulePage?.LeftClick(x, y);
            return !SchedulePage.IsOpen;
        }
        private static double clickHoldTime = 0; // 클릭 지속 시간 (초)
        private static bool isHoldingClick = false;
        private static readonly double requiredHoldTime = 0.3; // 최소 0.3초 이상 클릭해야 동작

        public static void LeftClickHeld(int x, int y)
        {
            if (!isHoldingClick)
            {
                isHoldingClick = true;
                clickHoldTime = Game1.currentGameTime.TotalGameTime.TotalSeconds; // 클릭한 순간 기록
            }

            double elapsedTime = Game1.currentGameTime.TotalGameTime.TotalSeconds - clickHoldTime;

            if (elapsedTime >= requiredHoldTime)
            {
                Instance.schedulePage?.LeftHeld(x, y);
            }
        }
        public static void LeftClickReleased()
        {
            isHoldingClick = false;
        }

        public static bool DrawPostfix(SpriteBatch b)
        {
            if (!SchedulePage.IsOpen) return true;

            Instance.schedulePage?.Draw(b);

            return false;
        }
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e)
        {
            if (schedulePage != null)
            {
                schedulePage.ScrollWheelAction(e.Delta);
            }

        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {

            //e 혹은 esc를 누르면 스케줄러 창 닫기

            if (SchedulePage.IsOpen)
            {
                if (e.Button == SButton.Escape || e.Button == SButton.E)
                {
                    SchedulePage.Close();
                }
            }
            if (!isProfileMenuOpen || !(Game1.activeClickableMenu is ProfileMenu profileMenu)) return;

            int x = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.X);
            int y = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.Y);

            if (SchedulePage.IsOpenPage(x, y))
            {
                schedulePage.ToggleSchedulePage(profileMenu);
            }
            else if (SchedulePage.IsOpenFriendshipList(x, y))
            {
                schedulePage.ToggleFriendshipList();
            }
            else if (SchedulePage.IsOpenMailList(x, y))
            {

                schedulePage.ToggleMailList();
            }


        }
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            List<string> NPCList = ScheduleDataManager.GetAllNPCListByUser();
            foreach (string npcName in NPCList)
            {
                ScheduleDataManager.ApplyScheduleToNPC(npcName);
            }
        }
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is ProfileMenu profileMenu)
            {
                SchedulePage.DrawButton(Game1.spriteBatch);
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            isProfileMenuOpen = Game1.activeClickableMenu is ProfileMenu;
            if (isProfileMenuOpen)
            {
                SchedulePage.CreateScheduleButton((ProfileMenu)Game1.activeClickableMenu);
                ProfileMenu profileMenu = (ProfileMenu)Game1.activeClickableMenu;
                if (schedulePage == null || schedulePage.npcName != profileMenu.Current.Character.Name) schedulePage = new SchedulePage(profileMenu.Current.Character.Name);
            }

        }
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            ScheduleDataManager.LoadAllSchedules();
        }

        public static ModEntry Instance { get; private set; }
    }

}
