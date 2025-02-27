using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace NPCDialogues
{
    public class ModEntry : Mod
    {
        private UIManager uiManager;

        private bool isProfileMenuOpen;

        private ProfileMenu lastOpenedProfileMenu;
        public override void Entry(IModHelper helper)
        {

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Display.MenuChanged += OnMenuChanged;

            Helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
        }
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e)
        {
            if (uiManager != null)
            {
                uiManager.OnScroll(e.Delta);
            }

        }


        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // 예제로 첫번째 NPC를 선택하여 UIManager를 초기화합니다.
            DataManager.InitData(Helper);
        }
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is ProfileMenu profileMenu)
            {
                isProfileMenuOpen = true;
                if (uiManager != null)
                {
                    Helper.Events.Input.ButtonPressed -= uiManager.OnClickButton;
                    Helper.Events.Input.ButtonPressed += uiManager.OnClickButton;

                }
                if (uiManager == null || uiManager.npc.Name != profileMenu.Current.Character.Name)
                {
                    uiManager = new UIManager(Helper, profileMenu.Current.Character.Name);

                }
                lastOpenedProfileMenu = profileMenu;
            }

        }


        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            //uiManager가 할당되어 있을 때, 프로필 메뉴가 열리면
            if (e.NewMenu is ProfileMenu && uiManager != null)
            {
                uiManager.CreateScheduleButton();
                uiManager.currentMenu = (ProfileMenu)e.NewMenu;
                lastOpenedProfileMenu = null;
                isProfileMenuOpen = true;
            }
            //프로필 메뉴가 닫힐 때
            if (e.OldMenu is ProfileMenu profileMenu)
            {
                DataManager.ApplyDialogueToAll();
                lastOpenedProfileMenu = profileMenu;
            }
            //게임메뉴가 열릴 때 상태 초기화
            if (e.NewMenu is GameMenu)
            {
                isProfileMenuOpen = false;
                uiManager = null;
                lastOpenedProfileMenu = null;
            }

            //기본 상태 + 마지막 메뉴가 편집모드일 때, 혹은 미리보기를 보여줬을 때
            bool flag = isProfileMenuOpen && uiManager != null && lastOpenedProfileMenu != null;
            bool isNewMenuDialogueEditMenu = e.OldMenu is DialogueEditMenu;
            bool isShowPreview = (uiManager?.isShowPreview ?? false) && e.OldMenu is DialogueBox dialogueBox && dialogueBox.characterDialogue.isDialogueFinished();
            flag = flag && (isNewMenuDialogueEditMenu || isShowPreview);
            if (flag)
            {
                uiManager.IsOpen = true;
                uiManager.isShowPreview = false;
                Game1.activeClickableMenu = lastOpenedProfileMenu;

            }
            //메뉴가 닫힐 때
            if (e.NewMenu == null)
            {
                if (uiManager == null) return;
                uiManager.IsOpen = false;
            }

        }
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is ProfileMenu profileMenu)
            {
                uiManager?.DrawButton(e.SpriteBatch);
                if (uiManager.IsOpen)
                {
                    uiManager.Draw(e.SpriteBatch);

                }
            }

        }
    }
}
