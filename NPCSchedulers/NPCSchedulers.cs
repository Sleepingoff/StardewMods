using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using System.Reflection;
using StardewValley.GameData.Characters;
using NPCSchedulers.UI;
using System.Numerics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace NPCSchedulers
{

    public class ModEntry : Mod
    {
        private Harmony harmony;
        private static SchedulePage schedulePage;
        private bool isProfileMenuOpen = false;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            harmony = new Harmony(ModManifest.UniqueID);

            RegisterEvents();
            PatchMethods();
        }

        private void RegisterEvents()
        {
            Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
        }
        private void PatchMethods()
        {
            Harmony harmony = new Harmony(ModManifest.UniqueID);

            MethodInfo targetDrawMethod = AccessTools.Method(typeof(ProfileMenu), "draw", new Type[] { typeof(SpriteBatch) });
            harmony.Patch(targetDrawMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(DrawPostfix)));

            // 🔹 ProfileMenu의 클릭 이벤트 패치
            MethodInfo targetClickMethod = AccessTools.Method(typeof(ProfileMenu), "receiveLeftClick");
            harmony.Patch(targetClickMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(ReceiveLeftClick)));

            // // 🔹 클릭 해제 이벤트 (선택 해제 등 필요할 경우)
            // MethodInfo releaseClickMethod = AccessTools.Method(typeof(ProfileMenu), "releaseLeftClick");
            // harmony.Patch(releaseClickMethod, postfix: new HarmonyMethod(typeof(ModEntry), nameof(ReleaseLeftClickPostfix)));
        }

        public static void ReceiveLeftClick(int x, int y)
        {
            schedulePage?.LeftClick(x, y);
        }
        public static bool DrawPostfix(SpriteBatch b)
        {
            if (!SchedulePage.IsOpen) return true;

            schedulePage?.Draw(b);

            return false;
        }
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!isProfileMenuOpen || !(Game1.activeClickableMenu is ProfileMenu profileMenu)) return;

            int x = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.X);
            int y = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.Y);
            SchedulePage.CreateScheduleButton(profileMenu);
            schedulePage = new SchedulePage();
            if (SchedulePage.IsOpenPage(x, y))
            {
                SchedulePage.ToggleSchedulePage(profileMenu);
            }
            else if (SchedulePage.IsOpenFriendshipList(x, y))
            {
                SchedulePage.ToggleFriendshipList();
            }
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is ProfileMenu profileMenu)
            {
                SchedulePage.DrawScheduleButton(Game1.spriteBatch);
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            isProfileMenuOpen = Game1.activeClickableMenu is ProfileMenu;
        }

        public static ModEntry Instance { get; private set; }
    }



}
