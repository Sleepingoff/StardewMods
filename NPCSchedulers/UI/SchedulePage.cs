using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCSchedulers.DATA;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Characters;
using StardewValley.Menus;
using System.Collections.Generic;

namespace NPCSchedulers.UI
{
    public class SchedulePage : UIBase
    {
        private static bool isOpen = false;
        private static ScheduleListUI scheduleListUI;
        private static ScheduleEditUI scheduleEditUI;
        private static ScheduleDateUI scheduleDateUI;
        private static FriendshipUI friendshipUI;
        private static ClickableTextureComponent scheduleButton;
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

        public static bool IsOpen => isOpen;

        public static void Open(NPC npc)
        {
            UIStateManager.Instance.SetCurrentNpc(npc);
            var displayPosition = UIStateManager.GetMenuPosition();
            var scheduleListUIDisplayPosition = new Vector2(displayPosition.X + 500, displayPosition.Y + 250);
            var scheduleDateUIDisplayPosition = new Vector2(displayPosition.X + 600, displayPosition.Y + 150);

            scheduleListUI = new ScheduleListUI(scheduleListUIDisplayPosition);
            scheduleDateUI = new ScheduleDateUI(scheduleDateUIDisplayPosition); // 날짜 UI 위치
            friendshipUI = new FriendshipUI(0); // 기본 호감도 6으로 설정
            scheduleEditUI = null;
            isOpen = true;
        }

        public static void Close()
        {
            isOpen = false;
            scheduleListUI = null;
            scheduleEditUI = null;
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

            NPC nPC = UIStateManager.Instance.CurrentNPC;

            if (nPC == null) return true;

            int xPositionOnScreen = Game1.activeClickableMenu.xPositionOnScreen;
            int yPositionOnScreen = Game1.activeClickableMenu.yPositionOnScreen;
            int x = xPositionOnScreen + 64 - 12;
            int y = yPositionOnScreen + IClickableMenu.borderWidth;

            Rectangle rectangle = new Rectangle(x, y, 400, 720 - IClickableMenu.borderWidth * 2);
            Rectangle itemDisplayRect = new Rectangle(x, y, 1204, 720 - IClickableMenu.borderWidth * 2);
            Rectangle characterSpriteBox = new Rectangle(xPositionOnScreen + 64 - 12 + (400 - Game1.nightbg.Width) / 2, yPositionOnScreen + IClickableMenu.borderWidth, Game1.nightbg.Width, Game1.nightbg.Height);

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
            b.Draw(letterTexture, new Vector2(xPositionOnScreen + width / 2, yPositionOnScreen + height / 2), new Rectangle(0, 0, 320, 180), Color.White, 0f, new Vector2(160f, 90f), 4f, SpriteEffects.None, 0.86f);
            Vector2 screenPosition = new Vector2(characterSpriteDrawPosition.X + (float)((Game1.daybg.Width - animatedSprite.SpriteWidth * 4) / 2), characterSpriteDrawPosition.Y + 32f + (float)((32 - animatedSprite.SpriteHeight) * 4));

            Game1.DrawBox(characterStatusDisplayBox.X, characterStatusDisplayBox.Y, characterStatusDisplayBox.Width, characterStatusDisplayBox.Height);
            b.Draw((Game1.timeOfDay >= 1900) ? Game1.nightbg : Game1.daybg, characterSpriteDrawPosition, Color.White);
            animatedSprite.draw(b, screenPosition, 0.8f);

            // NPC 이름 출력
            SpriteText.drawStringWithScrollCenteredAt(b, nPC.displayName, (int)characterNamePosition.X, (int)characterNamePosition.Y);

            SpriteText.drawStringWithScrollCenteredAt(b, "Today's Schedule",
                                                      itemDisplayRect.Center.X, itemDisplayRect.Top);

            Vector2 startVector = new Vector2(characterStatusDisplayBox.Width + characterStatusDisplayBox.X, itemDisplayRect.Top + 60);

            // 🔹 날짜 UI 그리기
            scheduleDateUI?.Draw(b);

            // 🔹 우정 UI 그리기
            friendshipUI?.Draw(b);

            // 🔹 스케줄 리스트 UI 렌더링
            scheduleListUI?.Draw(b);

            // 🔹 편집 UI가 활성화되었으면 렌더링
            scheduleEditUI?.Draw(b);

            //기본 UI

            ClickableTextureComponent nextCharacterButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 32 - 48, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
            {
                myID = 0,
                name = "Next Char",
                upNeighborID = -99998,
                downNeighborID = -99998,
                leftNeighborID = -99998,
                rightNeighborID = -99998,
                region = 500
            };
            ClickableTextureComponent previousCharacterButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 32, yPositionOnScreen - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
            {
                myID = 0,
                name = "Previous Char",
                upNeighborID = -99998,
                downNeighborID = -99998,
                leftNeighborID = -99998,
                rightNeighborID = -99998,
                region = 500
            };
            previousCharacterButton.bounds.X = (int)characterSpriteDrawPosition.X - 64 - previousCharacterButton.bounds.Width / 2;
            previousCharacterButton.bounds.Y = (int)characterSpriteDrawPosition.Y + Game1.nightbg.Height / 2 - previousCharacterButton.bounds.Height / 2;
            nextCharacterButton.bounds.X = (int)characterSpriteDrawPosition.X + Game1.nightbg.Width + 64 - nextCharacterButton.bounds.Width / 2;
            nextCharacterButton.bounds.Y = (int)characterSpriteDrawPosition.Y + Game1.nightbg.Height / 2 - nextCharacterButton.bounds.Height / 2;



            List<ClickableTextureComponent> clickableTextureComponents = new List<ClickableTextureComponent>
            {
                previousCharacterButton,
                nextCharacterButton,
            };
            foreach (ClickableTextureComponent clickableTextureComponent in clickableTextureComponents)
            {
                clickableTextureComponent.draw(b);
            }
            return false;
        }
        // 🔹 클릭 이벤트 처리 추가
        public override void LeftClick(int x, int y)
        {
            if (!isOpen) return;

            scheduleDateUI?.LeftClick(x, y);
            friendshipUI?.LeftClick(x, y);
            scheduleListUI?.LeftClick(x, y);
            scheduleEditUI?.LeftClick(x, y);
        }
        public static void OpenEditUI(string scheduleKey, ScheduleEntry entry)
        {
            scheduleEditUI = new ScheduleEditUI(new Vector2(500, 300), scheduleKey, entry);
        }

        public static void CloseEditUI()
        {
            scheduleEditUI = null;
        }

        // 🔹 "스케줄" 버튼 생성 및 렌더링
        public static void CreateScheduleButton(ProfileMenu menu)
        {
            int buttonX = menu.xPositionOnScreen + 80;
            int buttonY = menu.yPositionOnScreen + 80;

            scheduleButton = new ClickableTextureComponent(
                new Rectangle(buttonX, buttonY, 64, 64),
                Game1.mouseCursors,
                new Rectangle(16, 368, 16, 16),
                4f);
        }

        public static void DrawScheduleButton(SpriteBatch b)
        {
            scheduleButton?.draw(b);
        }

        public static bool IsButtonClicked(int x, int y)
        {
            return scheduleButton != null && scheduleButton.bounds.Contains(x, y);
        }

    }
}
