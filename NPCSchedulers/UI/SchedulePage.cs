using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCSchedulers.Store;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Characters;
using StardewValley.Menus;

namespace NPCSchedulers.UI
{
    public class SchedulePage : UIBase
    {
        private static bool isOpen = false;
        private static bool isOpenFriendshipList = false;
        private static bool isOpenMailList = false;

        private UIStateManager uiStateManager;


        private static ScheduleDateUI scheduleDateUI;
        private static FriendshipListUI friendshipListUI;
        private static MailListUI mailListUI;
        public string npcName;
        private static Rectangle scheduleButton;
        private static Rectangle friendshipButton;
        private static Rectangle mailButton;

        //스케줄페이지 인스턴스 생성
        public static SchedulePage Instance { get; private set; }
        public SchedulePage(string npcName)
        {
            this.npcName = npcName;
            uiStateManager = new UIStateManager(npcName);
            Instance = this;
        }
        public void ToggleSchedulePage(ProfileMenu profileMenu)
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                NPC npc = profileMenu.Current.Character as NPC;
                if (npc != null)
                {
                    Open(npc);
                }
            }
        }
        public void ToggleFriendshipList()
        {
            uiStateManager.ToggleListUI("");
            isOpenFriendshipList = uiStateManager.IsEditMode && !isOpenFriendshipList;

            if (!isOpenFriendshipList) return;

            isOpenMailList = false;
            uiStateManager.ToggleListUI("friendship");

            var displayPosition = UIStateManager.GetMenuPosition();
            var friendshipListUIDisplayPosition = new Vector2(displayPosition.X, displayPosition.Y + 100);
            friendshipListUI = new FriendshipListUI(friendshipListUIDisplayPosition, uiStateManager);
        }
        public void ToggleMailList()
        {
            uiStateManager.ToggleListUI("");
            isOpenMailList = uiStateManager.IsEditMode && !isOpenMailList;

            if (!isOpenMailList) return;
            isOpenFriendshipList = false;
            uiStateManager.ToggleListUI("mail");
            var displayPosition = UIStateManager.GetMenuPosition();
            var friendshipListUIDisplayPosition = new Vector2(displayPosition.X, displayPosition.Y + 100);

            mailListUI = new MailListUI(friendshipListUIDisplayPosition, uiStateManager);
        }
        public static bool IsOpen => isOpen;

        public void Open(NPC npc)
        {

            uiStateManager.InitDate();
            var displayPosition = UIStateManager.GetMenuPosition();

            var scheduleDateUIDisplayPosition = new Vector2(displayPosition.X + 500, displayPosition.Y + 150);


            scheduleDateUI = new ScheduleDateUI(scheduleDateUIDisplayPosition, uiStateManager); // 날짜 UI 위치

            isOpen = true;
        }

        public static void Close()
        {
            isOpen = false;
            Instance.uiStateManager.ToggleEditMode();
        }

        public override bool Draw(SpriteBatch b)
        {
            if (!isOpen) return true;

            if (!Game1.options.showClearBackgrounds)
            {
                b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            }
            int width = Game1.activeClickableMenu.width;
            int height = Game1.activeClickableMenu.height;


            int xPositionOnScreen = Game1.activeClickableMenu.xPositionOnScreen;
            int yPositionOnScreen = Game1.activeClickableMenu.yPositionOnScreen;
            int x = xPositionOnScreen + 64 - 12;
            int y = yPositionOnScreen + IClickableMenu.borderWidth;

            Rectangle rectangle = new Rectangle(x, y, 400, 720 - IClickableMenu.borderWidth * 2);
            Rectangle itemDisplayRect = new Rectangle(x, y, 1204, 720 - IClickableMenu.borderWidth * 2);

            itemDisplayRect.X += rectangle.Width;
            itemDisplayRect.Width -= rectangle.Width;
            Rectangle characterStatusDisplayBox = new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            rectangle.Y += 32;
            rectangle.Height -= 32;

            Vector2 characterSpriteDrawPosition = new Vector2(rectangle.X + (rectangle.Width - Game1.nightbg.Width) / 2, rectangle.Y);

            rectangle.Y += Game1.daybg.Height + 32;
            rectangle.Height -= Game1.daybg.Height + 32;
            Vector2 characterNamePosition = new Vector2(rectangle.Center.X, rectangle.Top);
            rectangle.Y += 96;
            rectangle.Height -= 96;
            if (uiStateManager == null) return true;
            NPC nPC = uiStateManager.CurrentNPC;

            if (nPC == null) return true;
            if (nPC.Birthday_Season != null && Utility.getSeasonNumber(nPC.Birthday_Season) >= 0)
            {
                rectangle.Y += 48;
                rectangle.Height -= 48;
                rectangle.Y += 64;
                rectangle.Height -= 64;
            }
            CharacterData data = nPC.GetData();
            string text = "Characters/" + nPC.getTextureName();
            Texture2D letterTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\letterBG");
            AnimatedSprite animatedSprite = new AnimatedSprite(text, 0, data?.Size.X ?? 16, data?.Size.Y ?? 32);
            Vector2 screenPosition = new Vector2(characterSpriteDrawPosition.X + (float)((Game1.daybg.Width - animatedSprite.SpriteWidth * 4) / 2), characterSpriteDrawPosition.Y + 32f + (float)((32 - animatedSprite.SpriteHeight) * 4));

            b.Draw(letterTexture, new Vector2(xPositionOnScreen + width / 2, yPositionOnScreen + height / 2), new Rectangle(0, 0, 320, 180), Color.White, 0f, new Vector2(160f, 90f), 4f, SpriteEffects.None, 0.86f);

            Game1.DrawBox(characterStatusDisplayBox.X, characterStatusDisplayBox.Y, characterStatusDisplayBox.Width, characterStatusDisplayBox.Height);



            //기본 UI

            // ClickableTextureComponent nextCharacterButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 32 - 48, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
            // {
            //     myID = 0,
            //     name = "Next Char",
            //     upNeighborID = -99998,
            //     downNeighborID = -99998,
            //     leftNeighborID = -99998,
            //     rightNeighborID = -99998,
            //     region = 500
            // };
            // ClickableTextureComponent previousCharacterButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 32, yPositionOnScreen - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
            // {
            //     myID = 0,
            //     name = "Previous Char",
            //     upNeighborID = -99998,
            //     downNeighborID = -99998,
            //     leftNeighborID = -99998,
            //     rightNeighborID = -99998,
            //     region = 500
            // };
            // previousCharacterButton.bounds.X = (int)characterSpriteDrawPosition.X - 64 - previousCharacterButton.bounds.Width / 2;
            // previousCharacterButton.bounds.Y = (int)characterSpriteDrawPosition.Y + Game1.nightbg.Height / 2 - previousCharacterButton.bounds.Height / 2;
            // nextCharacterButton.bounds.X = (int)characterSpriteDrawPosition.X + Game1.nightbg.Width + 64 - nextCharacterButton.bounds.Width / 2;
            // nextCharacterButton.bounds.Y = (int)characterSpriteDrawPosition.Y + Game1.nightbg.Height / 2 - nextCharacterButton.bounds.Height / 2;



            //     List<ClickableTextureComponent> clickableTextureComponents = new List<ClickableTextureComponent>
            // {
            //     previousCharacterButton,
            //     nextCharacterButton,
            // };
            //     foreach (ClickableTextureComponent clickableTextureComponent in clickableTextureComponents)
            //     {
            //         clickableTextureComponent.draw(b);
            //     }

            if (isOpenFriendshipList) friendshipListUI?.Draw(b);
            else if (isOpenMailList) mailListUI?.Draw(b);
            else
            {

                b.Draw((Game1.timeOfDay >= 1900) ? Game1.nightbg : Game1.daybg, characterSpriteDrawPosition, Color.White);
                animatedSprite.draw(b, screenPosition, 0.8f);

                // NPC 이름 출력
                SpriteText.drawStringWithScrollCenteredAt(b, nPC.displayName, (int)characterNamePosition.X, (int)characterNamePosition.Y);
            }
            SpriteText.drawStringWithScrollCenteredAt(b, nPC.Name + "'s Schedule",
                                                       itemDisplayRect.Center.X, itemDisplayRect.Top);
            // 🔹 날짜 UI 그리기
            scheduleDateUI?.Draw(b);



            Game1.activeClickableMenu?.drawMouse(b, ignore_transparency: true);

            return false;
        }

        /// <summary>
        /// 버튼에 대한 툴팁을 그리는 함수
        /// </summary>
        public static void DrawTooltip(SpriteBatch b, string text, Rectangle bounds)
        {
            int x = Game1.getMouseX();
            int y = Game1.getMouseY();
            // 1️⃣ 마우스가 영역 안에 있는지 확인

            if (bounds.Contains(x, y))
            {
                // 2️⃣ 툴팁 위치 계산
                int tooltipX = 20;  // 마우스 오른쪽에 표시
                int tooltipY = 20;  // 마우스 아래쪽에 표시

                // 3️⃣ 화면 경계를 벗어나지 않도록 조정
                if (tooltipX + bounds.Width > Game1.viewport.Width)
                    tooltipX -= bounds.Width;  // 오른쪽 경계 벗어나면 왼쪽으로 이동

                if (tooltipY + bounds.Height > Game1.viewport.Height)
                    tooltipY -= bounds.Height;  // 아래쪽 경계 벗어나면 위로 이동

                // 4️⃣ 툴팁 배경 & 텍스트 그리기
                IClickableMenu.drawHoverText(b, text, Game1.smallFont, tooltipX, tooltipY, boxScale: 0.5f);
            }
        }

        // 🔹 클릭 이벤트 처리 추가
        public override void LeftClick(int x, int y)
        {
            if (!isOpen) return;

            if (isOpenFriendshipList) friendshipListUI?.LeftClick(x, y);
            if (isOpenMailList) mailListUI?.LeftClick(x, y);
            scheduleDateUI?.LeftClick(x, y);

        }

        public override void LeftHeld(int x, int y)
        {
            if (isOpenFriendshipList) friendshipListUI?.LeftHeld(x, y);
            if (isOpenMailList) mailListUI?.LeftHeld(x, y);
            scheduleDateUI?.LeftHeld(x, y);

        }
        public void ScrollWheelAction(int direction)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            direction = direction > 0 ? -1 : 1;
            int x = (int)Utility.ModifyCoordinateForUIScale(mouseX);
            int y = (int)Utility.ModifyCoordinateForUIScale(mouseY);
            if (IsMouseOverUIElement(scheduleDateUI, x, y))
            {
                scheduleDateUI?.Scroll(direction);
            }
            else if (isOpenFriendshipList && IsMouseOverUIElement(friendshipListUI, x, y))
            {
                friendshipListUI?.Scroll(direction);
            }
            else if (isOpenMailList && IsMouseOverUIElement(mailListUI, x, y))
            {
                mailListUI?.Scroll(direction);
            }
        }
        private bool IsMouseOverUIElement(ListUI uiElement, int mouseX, int mouseY)
        {
            return uiElement != null && new Rectangle(
                (int)uiElement.viewport.X, (int)uiElement.viewport.Y,
                uiElement.viewport.Width, uiElement.viewport.Height
            ).Contains(mouseX, mouseY);
        }


        // 🔹 "스케줄" 버튼 생성 및 렌더링
        public static void CreateScheduleButton(ProfileMenu menu)
        {
            int buttonX = menu.xPositionOnScreen + 480;
            int buttonY = menu.yPositionOnScreen + 650;

            scheduleButton = new Rectangle(buttonX, buttonY, 180, 32);
            friendshipButton = new Rectangle(buttonX + 190, buttonY, 64, 32);
            mailButton = new Rectangle(buttonX + 190 + 74, buttonY, 64, 32);
        }
        private static void DrawDialogButton(SpriteBatch b, Rectangle bounds, string text, bool disable = false)
        {
            b.End();
            b.Begin();
            float alpha = disable ? 0.5f : 1.0f; // 🔹 비활성화 상태면 50% 투명도

            // 다이얼로그 박스 배경
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                bounds.X - 10, bounds.Y - 10, bounds.Width + 20, bounds.Height + 20,
                Color.White * alpha, 1f, false
            );


            // 버튼 텍스트 (비활성화일 경우 회색으로 표시)
            Utility.drawTextWithShadow(
                b, text, Game1.smallFont,
                new Vector2(bounds.X + bounds.Width / 2 - Game1.smallFont.MeasureString(text).X / 2, bounds.Y),
                disable ? Color.Gray * alpha : Color.Black
            );
        }
        public static void DrawButton(SpriteBatch b)
        {
            if (Instance == null) return;
            DrawDialogButton(b, scheduleButton, "Scheduler");

            DrawDialogButton(b, friendshipButton, "<3", !isOpen && (Instance.uiStateManager == null || !Instance.uiStateManager.IsEditMode));
            DrawDialogButton(b, mailButton, "@", !isOpen && (Instance.uiStateManager == null || !Instance.uiStateManager.IsEditMode));
        }
        public static bool IsOpenMailList(int x, int y)
        {
            return mailButton.Contains(x, y);
        }
        public static bool IsOpenFriendshipList(int x, int y)
        {
            return friendshipButton.Contains(x, y);
        }
        public static bool IsOpenPage(int x, int y)
        {
            return scheduleButton.Contains(x, y);
        }


    }
}
