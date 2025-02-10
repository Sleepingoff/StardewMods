using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using NPCSchedulers.Store;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Events;

namespace NPCSchedulers.UI
{
    public class ScheduleDateUI : ListUI
    {
        private UIStateManager uiStateManager;
        private OptionsSlider dateSlider;
        private ClickableTextureComponent leftButton;
        private ClickableTextureComponent rightButton;
        private static ScheduleListUI scheduleListUI;
        public ScheduleDateUI(Vector2 position, UIStateManager uiStateManager) : base(position, (int)position.X + 500, (int)position.Y + 700)
        {
            this.uiStateManager = uiStateManager;
            this.position = new Vector2(position.X + 100, position.Y);
            // 🔹 날짜 슬라이더 초기화 (0~99 범위를 1~28 날짜로 변환)

            var (_, date) = uiStateManager.GetCurrentDate();
            dateSlider = new OptionsSlider("", 0, (int)this.position.X + 300, (int)this.position.Y);
            dateSlider.value = (date - 1) * 99 / 27;

            // 🔹 계절 변경 좌우 버튼 초기화
            leftButton = new ClickableTextureComponent(
                new Rectangle((int)this.position.X, (int)this.position.Y - 50, 32, 32),
                Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f);

            rightButton = new ClickableTextureComponent(
                new Rectangle((int)this.position.X + 450, (int)this.position.Y - 50, 32, 32),
                Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f);
            var scheduleListUIDisplayPosition = new Vector2(position.X, position.Y);
            scheduleListUI = new ScheduleListUI(scheduleListUIDisplayPosition, uiStateManager);

        }

        public override bool Draw(SpriteBatch b)
        {
            if (!IsVisible) return true;

            // 🔹 좌우 버튼 그리기
            leftButton.draw(b);
            rightButton.draw(b);


            // 🔹 날짜 슬라이더 그리기
            dateSlider.draw(b, 0, 0);


            var (season, date) = uiStateManager.GetCurrentDate();
            // 🔹 현재 선택된 날짜 텍스트 표시
            b.DrawString(Game1.smallFont, $"{date}",
                         new Vector2(position.X + 250, position.Y - 10), Color.Brown);

            b.DrawString(Game1.smallFont, $"{season}",
                    new Vector2(position.X + 200, position.Y - 40), Color.Brown);
            // 🔹 스케줄 리스트 UI 렌더링
            scheduleListUI?.Draw(b);
            return false;
        }

        public override void Scroll(int direction)
        {
            scheduleListUI?.Scroll(direction);
        }
        public override void LeftHeld(int x, int y)
        {
            if (dateSlider.bounds.Contains(x, y)) { dateSlider.leftClickHeld(x, y); UpdateSlider(0); }
            scheduleListUI?.LeftHeld(x, y);
        }
        public override void LeftClick(int x, int y)
        {
            if (!IsVisible) return;

            // 🔹 좌우 버튼 클릭 감지 → 날짜 변경
            if (leftButton.containsPoint(x, y))
            {
                UpdateSlider(-1);
            }
            else if (rightButton.containsPoint(x, y))
            {
                UpdateSlider(1);
            }
            else if (dateSlider.bounds.Contains(x, y))
            {
                dateSlider.receiveLeftClick(x, y);
                UpdateSlider(0);
            }
            scheduleListUI?.LeftClick(x, y);
        }

        private void UpdateSlider(int direction)
        {
            int newDate = (int)(dateSlider.value / 99.0f * 27) + 1;
            uiStateManager.SetCurrentDate((direction, newDate));
            var scheduleListUIDisplayPosition = new Vector2(position.X - 100, position.Y);
            scheduleListUI = new ScheduleListUI(scheduleListUIDisplayPosition, uiStateManager);
        }


    }

}