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

        private static UIStateManager uiStateManager;
        private static ScheduleListUI scheduleListUI;

        private static ScheduleDateUI scheduleDateUI;
        private static FriendshipListUI friendshipListUI;

        private static Rectangle scheduleButton;
        private static Rectangle friendshipButton;
        public static void ToggleSchedulePage(ProfileMenu profileMenu)
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
        public static void ToggleFriendshipList()
        {
            isOpenFriendshipList = !isOpenFriendshipList;
            if (!isOpenFriendshipList) return;
            var displayPosition = UIStateManager.GetMenuPosition();
            var friendshipListUIDisplayPosition = new Vector2(displayPosition.X, displayPosition.Y + 100);
            friendshipListUI = new FriendshipListUI(friendshipListUIDisplayPosition, uiStateManager);
        }
        public static bool IsOpen => isOpen;

        public static void Open(NPC npc)
        {
            uiStateManager = new UIStateManager(npc.Name);
            var displayPosition = UIStateManager.GetMenuPosition();
            var scheduleListUIDisplayPosition = new Vector2(displayPosition.X + 500, displayPosition.Y + 200);
            var scheduleDateUIDisplayPosition = new Vector2(displayPosition.X, displayPosition.Y);

            scheduleListUI = new ScheduleListUI(scheduleListUIDisplayPosition, uiStateManager);
            scheduleDateUI = new ScheduleDateUI(scheduleDateUIDisplayPosition, uiStateManager); // 날짜 UI 위치

            isOpen = true;
        }

        public static void Close()
        {
            isOpen = false;
            scheduleListUI = null;
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

            NPC nPC = uiStateManager.CurrentNPC;

            if (nPC == null) return true;

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

            // 🔹 편집 UI가 활성화되었으면 렌더링
            if (isOpenFriendshipList) friendshipListUI?.Draw(b);
            else
            {

                b.Draw((Game1.timeOfDay >= 1900) ? Game1.nightbg : Game1.daybg, characterSpriteDrawPosition, Color.White);
                animatedSprite.draw(b, screenPosition, 0.8f);

                // NPC 이름 출력
                SpriteText.drawStringWithScrollCenteredAt(b, nPC.displayName, (int)characterNamePosition.X, (int)characterNamePosition.Y);
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
            }

            SpriteText.drawStringWithScrollCenteredAt(b, "Today's Schedule",
                                                       itemDisplayRect.Center.X, itemDisplayRect.Top);
            // 🔹 날짜 UI 그리기
            scheduleDateUI?.Draw(b);

            // 🔹 스케줄 리스트 UI 렌더링
            scheduleListUI?.Draw(b);

            Game1.activeClickableMenu?.drawMouse(b, ignore_transparency: true);

            return false;
        }

        /// <summary>
        /// 버튼에 대한 툴팁을 그리는 함수
        /// </summary>
        public static void DrawTooltip(SpriteBatch b, string text, Rectangle bounds)
        {
            int x = (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseX());
            int y = (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseY());
            // 1️⃣ 마우스가 영역 안에 있는지 확인
            if (bounds.Contains(x, y))
            {
                // 2️⃣ 툴팁 위치 계산
                int tooltipX = x + 20;  // 마우스 오른쪽에 표시
                int tooltipY = y + 20;  // 마우스 아래쪽에 표시

                // 3️⃣ 화면 경계를 벗어나지 않도록 조정
                if (tooltipX + bounds.Width > Game1.viewport.Width)
                    tooltipX -= bounds.Width;  // 오른쪽 경계 벗어나면 왼쪽으로 이동

                if (tooltipY + bounds.Height > Game1.viewport.Height)
                    tooltipY -= bounds.Height;  // 아래쪽 경계 벗어나면 위로 이동

                // 4️⃣ 툴팁 배경 & 텍스트 그리기
                IClickableMenu.drawHoverText(b, text, Game1.smallFont, tooltipX, tooltipY);
            }
        }

        // 🔹 클릭 이벤트 처리 추가
        public override void LeftClick(int x, int y)
        {
            if (!isOpen) return;

            friendshipListUI?.LeftClick(x, y);
            scheduleDateUI?.LeftClick(x, y);
            scheduleListUI?.LeftClick(x, y);
        }

        public override void LeftHeld(int x, int y)
        {
            friendshipListUI?.LeftHeld(x, y);
            scheduleDateUI?.LeftHeld(x, y);
            scheduleListUI?.LeftHeld(x, y);
        }
        public void DragAction(CursorMovedEventArgs e)
        {
            // scheduleDateUI.DragAction(e);
            // scheduleListUI.DragAction(e);
        }
        public void ScrollWheelAction(int direction)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            direction = direction > 0 ? -1 : 1;
            int x = (int)Utility.ModifyCoordinateForUIScale(mouseX);
            int y = (int)Utility.ModifyCoordinateForUIScale(mouseY);
            if (IsMouseOverUIElement(scheduleListUI, x, y))
            {
                scheduleListUI?.Scroll(direction);
            }
            else if (isOpenFriendshipList && IsMouseOverUIElement(friendshipListUI, x, y))
            {
                friendshipListUI?.Scroll(direction);
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
            int buttonY = menu.yPositionOnScreen + 50;

            scheduleButton = new Rectangle(buttonX, buttonY, 64, 64);
            friendshipButton = new Rectangle(buttonX + 64, buttonY, 64, 64);
        }
        private static void DrawDialogButton(SpriteBatch b, Rectangle bounds, string text, bool disable = false)
        {
            float alpha = disable ? 0.5f : 1.0f; // 🔹 비활성화 상태면 50% 투명도

            // 다이얼로그 박스 배경
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                bounds.X - 10, bounds.Y - 10, bounds.Width + 20, bounds.Height + 20,
                Color.White * alpha, 1f, false // 🔹 Opacity 적용
            );


            // 버튼 텍스트 (비활성화일 경우 회색으로 표시)
            Utility.drawTextWithShadow(
                b, text, Game1.smallFont,
                new Vector2(bounds.X + bounds.Width / 2 - Game1.smallFont.MeasureString(text).X / 2, bounds.Y + bounds.Height + 5),
                disable ? Color.Gray * alpha : Color.Black
            );
        }
        public static void DrawButton(SpriteBatch b)
        {
            DrawDialogButton(b, scheduleButton, "Scheduler");
            DrawDialogButton(b, friendshipButton, "<3", isOpen);
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
