using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using NPCSchedulers.Store;

namespace NPCSchedulers.UI
{
    public class ScheduleDateUI : UIBase
    {
        private UIStateManager uiStateManager;
        private Vector2 position;
        private OptionsSlider dateSlider;
        private ClickableTextureComponent leftButton;
        private ClickableTextureComponent rightButton;

        public ScheduleDateUI(Vector2 position, UIStateManager uiStateManager)
        {
            this.uiStateManager = uiStateManager;
            this.position = new Vector2(position.X + 600, position.Y + 150);
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

            return false;
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
        }

        private void UpdateSlider(int direction)
        {
            int newDate = (int)(dateSlider.value / 99.0f * 27) + 1;
            uiStateManager.SetCurrentDate((direction, newDate));
        }


    }

}