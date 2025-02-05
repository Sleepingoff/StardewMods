using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCSchedulers.DATA;
using NPCSchedulers.Store;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace NPCSchedulers.UI
{

    public class ScheduleUI : UIBase
    {
        private UIStateManager uiStateManager;
        private string scheduleKey;
        private List<ScheduleEntry> entries; // 🔥 여러 개의 상세 일정 포함
        private Dictionary<string, int> friendshipConditionEntry;

        private FriendshipTargetUI friendshipTargetUI;
        private Rectangle scheduleBox;
        public int Height = 80;
        public ScheduleUI(Vector2 position, string scheduleKey, UIStateManager uiStateManager)
        {
            this.uiStateManager = uiStateManager;
            this.scheduleKey = scheduleKey;
            this.entries = uiStateManager.GetScheduleEntries(scheduleKey);
            friendshipConditionEntry = uiStateManager.GetFriendshipCondition();
            // 🔹 스케줄 박스 크기 설정
            scheduleBox = new Rectangle((int)position.X, (int)position.Y, 600, Height);
            friendshipTargetUI = new FriendshipTargetUI(new Vector2((int)position.X, (int)position.Y));
            Height = entries.Count * Height + friendshipConditionEntry.Count * 60 + 100;
        }

        public override bool Draw(SpriteBatch b)
        {
            if (!IsVisible) return true;

            Vector2 titleDisplayPosition = new Vector2(scheduleBox.X + 10, scheduleBox.Y);
            // 🔹 스케줄 키 표시
            Color keyColor = Color.SandyBrown;

            foreach (var key in uiStateManager.GetEditedScheduleKeyList())
            {
                if (key == scheduleKey)
                {
                    keyColor = Color.Blue;
                    break;
                }
            }
            SpriteText.drawString(b, $"{scheduleKey}", (int)titleDisplayPosition.X, (int)titleDisplayPosition.Y, layerDepth: 0.1f, color: keyColor);

            int yOffset = scheduleBox.Y + 60;
            friendshipTargetUI.Draw(b);
            yOffset += friendshipTargetUI.Height;
            // 🔹 여러 개의 상세 스케줄 출력 (각 항목마다 삭제 버튼 포함)

            Vector2 detailDisplayPosition = new Vector2(scheduleBox.X, 0);
            foreach (var entry in entries)
            {
                Rectangle detailDisplay = new Rectangle((int)detailDisplayPosition.X, (int)detailDisplayPosition.Y + yOffset, scheduleBox.Width, scheduleBox.Height);
                DrawBorder(b, detailDisplay, 3, Color.Brown);

                // 🔹 기존 UI 스타일 유지 (반투명 박스 + 테두리)
                b.Draw(Game1.staminaRect, detailDisplay, new Rectangle(0, 0, 1, 1), Color.SandyBrown * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 0.8f);


                string scheduleText = $"{entry.Time}: {entry.Location} / {entry.Action}";
                b.DrawString(Game1.smallFont, scheduleText, new Vector2(scheduleBox.X + 10, yOffset + 10), Color.Black);
                // 🔹 개별 삭제 버튼 추가
                ClickableTextureComponent deleteButton = new ClickableTextureComponent(
                    new Rectangle(scheduleBox.Right - 40, yOffset + 10, 32, 32),
                    Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 2f);
                deleteButton.draw(b);


                yOffset += 100; // 🔹 각 스케줄 간격 유지
            }
            return false;
        }

        public override void LeftClick(int x, int y)
        {
            if (!IsVisible) return;


            // 🔹 개별 스케줄 삭제 버튼 클릭 감지
            int yOffset = scheduleBox.Y + 60;
            yOffset += friendshipTargetUI.Height;
            foreach (var entry in entries)
            {
                Rectangle deleteButtonBounds = new Rectangle(scheduleBox.Right - 40, yOffset + 10, 32, 32);
                if (deleteButtonBounds.Contains(x, y))
                {
                    // 🔹 삭제 요청
                    uiStateManager.DeleteScheduleEntry(scheduleKey, entry);
                    return;
                }
                yOffset += scheduleBox.Height;

            }


        }


        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rectangle, int thickness, Color color)
        {
            Texture2D pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // 상단
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            // 하단
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y + rectangle.Height - thickness, rectangle.Width, thickness), color);
            // 좌측
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            // 우측
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + rectangle.Width - thickness, rectangle.Y, thickness, rectangle.Height), color);

            // 🔹 모서리 대각선 추가 (기존 코드에서 유지)
            int cornerLength = 10;

            for (int i = 0; i < cornerLength; i++)
            {
                spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 1 + i, rectangle.Bottom - cornerLength + i - 2, 2, 2), color);
                spriteBatch.Draw(pixel, new Rectangle(rectangle.Right + i - 1 - cornerLength, rectangle.Bottom - i - 2, 2, 2), color);
            }

            for (int i = 0; i < cornerLength; i++)
            {
                spriteBatch.Draw(pixel, new Rectangle(rectangle.Right + i - 1 - cornerLength, rectangle.Y + i, 2, 2), color);
                spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 1 + i, rectangle.Y + cornerLength - i, 2, 2), color);
            }
        }

    }

    public class ScheduleListUI : ListUI
    {
        private UIStateManager uiStateManager;
        private List<ScheduleUI> scheduleEntries = new List<ScheduleUI>();
        public ScheduleListUI(Vector2 position, UIStateManager uiStateManager) : base(position, 700, 500)
        {
            this.uiStateManager = uiStateManager;
            UpdateSchedules();
        }

        private void UpdateSchedules()
        {
            var entries = uiStateManager.GetSchedule();
            scheduleEntries.Clear(); // 🔹 기존 리스트 초기화
            int yOffset = 0;
            foreach (var entry in entries)
            {
                var scheduleUIDisplayPosition = new Vector2(position.X, position.Y + yOffset - scrollPosition);
                var scheduleUi = new ScheduleUI(scheduleUIDisplayPosition, entry.Key, uiStateManager);
                scheduleEntries.Add(scheduleUi);
                yOffset += scheduleUi.Height;
            }

            SetMaxScrollPosition(yOffset, viewport.Height);
        }

        public override bool Draw(SpriteBatch b)
        {
            base.Draw(b);
            UpdateSchedules();
            // 🔹 `foreach`문 제거 → `ScheduleUI` 리스트를 그대로 렌더링
            foreach (var scheduleUI in scheduleEntries)
            {
                scheduleUI.Draw(b);
            }

            return base.DrawEnd(b);
        }


        public override void LeftClick(int x, int y)
        {
            if (upArrow.containsPoint(x, y))
            {
                Scroll(-1);
                UpdateSchedules();
            }
            else if (downArrow.containsPoint(x, y))
            {
                Scroll(1);
                UpdateSchedules();
            }
            else
            {
                foreach (var scheduleUI in scheduleEntries)
                {
                    scheduleUI.LeftClick(x, y);
                }
            }
        }
    }

}
