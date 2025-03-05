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
        public Vector2 position;
        private UIStateManager uiStateManager;
        public static ScheduleEditUI scheduleEditUI;
        private string scheduleKey;
        private string gotoKey;
        private OptionsTextBox gotoTextBox;
        private ClickableTextureComponent checkGotoButton;
        private List<ScheduleEntry> entries; // 🔥 여러 개의 상세 일정 포함
        private FriendshipTargetUI friendshipTargetUI;
        private MailTargetUI mailTargetUI;
        private Rectangle scheduleBox;
        public Rectangle Bounds => scheduleBox;
        public int Height = 80;
        public ScheduleUI(Vector2 position, string scheduleKey, UIStateManager uiStateManager)
        {
            this.position = position;
            this.uiStateManager = uiStateManager;
            this.scheduleKey = scheduleKey;
            uiStateManager.SetScheduleKey(scheduleKey);
            this.entries = uiStateManager.GetScheduleEntries(scheduleKey);
            // 🔹 스케줄 박스 크기 설정    
            friendshipTargetUI = new FriendshipTargetUI(new Vector2((int)position.X, (int)position.Y + 60), scheduleKey, uiStateManager);
            mailTargetUI = new MailTargetUI(new Vector2((int)position.X, (int)position.Y + 60 + friendshipTargetUI.Height + 60), scheduleKey, uiStateManager);

            UpdateScheduleEntries();
            gotoTextBox = new OptionsTextBox("GOTO", gotoKey != null ? gotoKey : "", 300);
            Height = entries.Count * Height + mailTargetUI.Height + friendshipTargetUI.Height + 250 + (uiStateManager.IsEditMode ? 600 : 0);
            scheduleBox = new Rectangle((int)position.X, (int)position.Y, 600, Height);
            checkGotoButton = new ClickableTextureComponent("check", new Rectangle(gotoTextBox.bounds.X + 300 + 50, gotoTextBox.bounds.Y, 64, 64), "", "save GOTO  scheduleKey", Game1.mouseCursors, new Rectangle(175, 378, 16, 16), 3f);
        }
        private void UpdateScheduleEntries()
        {
            this.entries = uiStateManager.GetScheduleEntries(scheduleKey);
            this.gotoKey = uiStateManager.GetGotoKey(scheduleKey);
        }
        public override bool Draw(SpriteBatch b)
        {
            if (!IsVisible) return true;
            gotoTextBox.bounds = new Rectangle((int)position.X + 200, (int)position.Y + 40 + friendshipTargetUI.Height, 300, 50);

            UpdateScheduleEntries();
            Height = entries.Count * 80 + mailTargetUI.Height + friendshipTargetUI.Height + 300;


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
            int yOffset = scheduleBox.Y + 60;

            SpriteText.drawString(b, $"{scheduleKey}", (int)titleDisplayPosition.X, (int)titleDisplayPosition.Y, layerDepth: 0.1f, color: keyColor);

            gotoTextBox.draw(b, (int)titleDisplayPosition.X, yOffset);
            checkGotoButton.bounds.X = gotoTextBox.bounds.X + gotoTextBox.bounds.Width + 50;
            checkGotoButton.bounds.Y = gotoTextBox.bounds.Y;
            checkGotoButton.draw(b);
            yOffset += 50;
            friendshipTargetUI.position = new Vector2((int)position.X, yOffset);
            friendshipTargetUI.Draw(b);
            yOffset += friendshipTargetUI.Height;
            mailTargetUI.position = new Vector2((int)position.X, yOffset);
            mailTargetUI.Draw(b);
            yOffset += mailTargetUI.Height;

            // 🔹 여러 개의 상세 스케줄 출력 (각 항목마다 삭제 버튼 포함)

            Vector2 detailDisplayPosition = new Vector2(scheduleBox.X, 0);
            foreach (var entry in entries)
            {
                Rectangle detailDisplay = new Rectangle((int)detailDisplayPosition.X, (int)detailDisplayPosition.Y + yOffset, scheduleBox.Width, 80);
                DrawBorder(b, detailDisplay, 3, Color.Brown);

                // 🔹 기존 UI 스타일 유지 (반투명 박스 + 테두리)
                b.Draw(Game1.staminaRect, detailDisplay, new Rectangle(0, 0, 1, 1), Color.SandyBrown * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 0.8f);


                string scheduleText = $"{entry.Time}: {entry.Location} / {entry.Action ?? "None"}";
                b.DrawString(Game1.smallFont, scheduleText, new Vector2(scheduleBox.X + 10, yOffset + 10), Color.Black);
                b.DrawString(Game1.smallFont, entry.Talk ?? "None", new Vector2(scheduleBox.X + 10, yOffset + 40), Color.Black);

                entry.SetBounds(detailDisplay.X, detailDisplay.Y, detailDisplay.Width, detailDisplay.Height);

                // 🔹 개별 삭제 버튼 추가
                ClickableTextureComponent deleteButton = new ClickableTextureComponent(
                    new Rectangle(scheduleBox.Right - 40, yOffset + 10, 32, 32),
                    Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 2f);
                deleteButton.draw(b);
                // 🔹 편집 UI가 활성화되었으면 렌더링
                if (uiStateManager.IsEditMode && uiStateManager.EditedScheduleKey == entry.Key && scheduleEditUI != null)
                {
                    scheduleEditUI.position = new Vector2(entry.Bounds.bounds.X, entry.Bounds.bounds.Y + 80);
                    scheduleEditUI?.Draw(b);
                    yOffset += 600;
                    Height += 600;
                }

                yOffset += 100; // 🔹 각 스케줄 간격 유지
            }
            scheduleBox = new Rectangle((int)position.X, (int)position.Y, 600, Height);
            return false;
        }
        public override void LeftHeld(int x, int y)
        {
            if (uiStateManager.IsEditMode)
            {
                scheduleEditUI?.LeftHeld(x, y);
            }
        }
        public override void LeftClick(int x, int y)
        {

            if (!IsVisible) return;

            // 🔹 개별 스케줄 삭제 버튼 클릭 감지
            int yOffset = scheduleBox.Y + 60;

            if (scheduleEditUI != null && uiStateManager.IsEditMode)
            {
                scheduleEditUI?.LeftClick(x, y);
            }
            if (gotoTextBox.bounds.Contains(x, y)) gotoTextBox.textBox.SelectMe();

            if (checkGotoButton != null && checkGotoButton.bounds.Contains(x, y))
            {
                string newGotoKey = gotoTextBox.textBox.Text;
                bool hasKey = uiStateManager.HasKeyInAllScheduleDataWithCurrentNPC(newGotoKey);
                if (newGotoKey == scheduleKey || newGotoKey == null)
                {
                    Game1.addHUDMessage(new HUDMessage($"can't apply GOTO key like {newGotoKey}", 2));
                }
                else if (!hasKey && newGotoKey != "")
                {
                    //존재하지 않는 키 추가에 대한 경고
                    Game1.addHUDMessage(new HUDMessage($"The key '{newGotoKey}' does not exist in the current NPC's schedule data.", 2));
                }
                else
                {
                    uiStateManager.SetScheduleKey(scheduleKey);
                    uiStateManager.SetGotoKey(newGotoKey);
                    uiStateManager.SetScheduleDataByList(entries);
                }
            }

            yOffset += mailTargetUI?.Height ?? 50 + 60;
            yOffset += friendshipTargetUI?.Height ?? 50 + 50;
            Vector2 detailDisplayPosition = new Vector2(position.X, 0);
            foreach (var entry in entries)
            {
                Rectangle detailDisplay = new Rectangle((int)detailDisplayPosition.X, (int)detailDisplayPosition.Y + yOffset + 50, 600, 80);

                Rectangle deleteButtonBounds = new Rectangle(detailDisplay.Right - 40, detailDisplay.Y, 32, 32);
                if (deleteButtonBounds.Contains(x, y))
                {
                    // 🔹 삭제 요청
                    uiStateManager.SetScheduleKey(scheduleKey);
                    uiStateManager.DeleteScheduleEntry(scheduleKey, entry);
                    UpdateScheduleEntries();
                    return;
                }

                if (entry.Contains(x, y))
                {
                    uiStateManager.SetScheduleKey(scheduleKey);
                    uiStateManager.ToggleEditMode(entry.Key);
                    if (uiStateManager.IsEditMode && uiStateManager.EditedScheduleKey == entry.Key)
                    {
                        scheduleEditUI = new ScheduleEditUI(new Vector2(entry.Bounds.bounds.X, entry.Bounds.bounds.Y + 80), entry.Key, entry, uiStateManager);
                        yOffset += 600;
                    }

                }
                entry.SetBounds(detailDisplay.X, detailDisplay.Y, detailDisplay.Width, detailDisplay.Height);
                yOffset += 100;

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
        private ClickableTextureComponent originButton;
        public ScheduleListUI(Vector2 position, UIStateManager uiStateManager) : base(position, 700, 450)
        {
            this.uiStateManager = uiStateManager;
            originButton = new ClickableTextureComponent(
                           new Rectangle((int)viewport.Right - 80, (int)viewport.Y, 16, 16),
                           Game1.mouseCursors, new Rectangle(240, 192, 16, 16), 2f);
            InitSchedules();
        }
        private void InitSchedules()
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
        private void UpdateSchedules()
        {
            int yOffset = 0;

            foreach (var ui in scheduleEntries)
            {
                var scheduleUIDisplayPosition = new Vector2(position.X, position.Y + yOffset - scrollPosition);
                ui.position = scheduleUIDisplayPosition;
                yOffset += ui.Height;
            }
            SetMaxScrollPosition(yOffset, viewport.Height);
        }

        public override bool Draw(SpriteBatch b)
        {
            originButton?.draw(b);

            b.DrawString(Game1.smallFont, uiStateManager.GetCurrentFilter(), new Vector2(position.X + viewport.Width - 100, viewport.Top), Color.Gray * 0.5f);

            foreach (var scheduleUI in scheduleEntries)
            {
                base.Draw(b);

                scheduleUI?.Draw(b);
                UpdateSchedules();
                base.DrawEnd(b);
                if (uiStateManager.IsEditMode) ScheduleEditUI.DrawTooltip(b, ScheduleUI.scheduleEditUI);
            }

            return false;
        }
        public override void Scroll(int direction)
        {
            base.Scroll(direction);
        }
        public override void LeftHeld(int x, int y)
        {
            foreach (var scheduleUI in scheduleEntries)
            {
                scheduleUI.LeftHeld(x, y);
            }

        }
        public override void LeftClick(int x, int y)
        {
            if (originButton.containsPoint(x, y))
            {
                uiStateManager.ToggleScheduleVersion();
            }
            if (upArrow.containsPoint(x, y))
            {
                InitSchedules();
            }
            if (downArrow.containsPoint(x, y))
            {
                InitSchedules();
            }
            foreach (var scheduleUI in scheduleEntries)
            {
                if (scheduleUI.Bounds.Contains(x, y))
                {
                    scheduleUI.LeftClick(x, y);
                }
            }

        }
    }

}
