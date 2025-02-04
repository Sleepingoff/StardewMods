using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace NPCSchedulers.UI
{
    public class FriendshipUI : UIBase
    {
        private Vector2 heartDisplayPosition;
        private OptionsSlider heartSlider;  // 🔹 기존 하트 슬라이더
        private IconSlider npcSlider;       // 🔹 기존 NPC 선택 슬라이더
        private List<string> villagers;     // 🔹 모든 마을 NPC 목록

        public FriendshipUI(int FriendshipLevel)
        {
            var displayRect = UIStateManager.GetMenuPosition();
            heartDisplayPosition = new Vector2(displayRect.X + 100, displayRect.Y + 400);

            // 🔹 기존 하트 슬라이더 유지
            heartSlider = new OptionsSlider("", 0, (int)heartDisplayPosition.X + 100, (int)heartDisplayPosition.Y - 25);
            heartSlider.value = (int)(FriendshipLevel * 99f / 14); // 🔥 초기값 설정

            // 🔹 마을 NPC 목록 불러오기
            villagers = Utility.getAllCharacters()
                .Where(npc => npc is NPC && npc.IsVillager)
                .Select(npc => npc.Name)
                .ToList();
            villagers.Sort(); // 🔹 이름순 정렬

            // 🔹 NPC 선택용 슬라이더 초기화
            npcSlider = new IconSlider(villagers);
            // npcSlider.bounds = displayRect;
            int index = villagers.IndexOf(UIStateManager.Instance.CurrentNPC?.Name ?? "");
            npcSlider.selectedIndex = Math.Max(0, index);
        }

        public override bool Draw(SpriteBatch b)
        {
            if (!IsVisible) return true;

            int friendshipLevel = (int)(heartSlider.value / 99f * 14);

            // 🔹 하트 UI (기존 코드 유지)
            drawNPCSlotHeart(heartDisplayPosition, b, friendshipLevel);

            npcSlider.draw(b, (int)heartDisplayPosition.X, (int)heartDisplayPosition.Y - 50);
            heartSlider.draw(b, 0, 0);

            return false;
        }

        public override void LeftClick(int x, int y)
        {

            // 🔹 NPC 선택 슬라이더 클릭 감지
            if (npcSlider.bounds.Contains(x, y))
            {
                npcSlider.receiveLeftClick(x, y);
            }

            // 🔹 하트 슬라이더 클릭 감지
            if (heartSlider.bounds.Contains(x, y))
            {
                heartSlider.receiveLeftClick(x, y);
                int newHeartLevel = (int)((heartSlider.value / 99.0f) * 14); // 🔥 슬라이더 값 -> 하트 값 변환
                string selectedNpcName = villagers[npcSlider.selectedIndex];

                UIStateManager.Instance.SetEditedFriendshipCondition(selectedNpcName, newHeartLevel);
            }
        }

        private static void drawNPCSlotHeart(Vector2 heartDisplayPosition, SpriteBatch b, int clickedHeart)
        {
            for (int hearts = 0; hearts < 14; hearts++) // 최대 14 하트
            {
                Color color = (hearts < clickedHeart) ? Color.Red : (Color.White * 0.0f);
                int x = (hearts < clickedHeart) ? 211 : 218;

                if (hearts < 10)
                {
                    b.Draw(Game1.mouseCursors,
                          new Vector2(heartDisplayPosition.X + hearts * 32, heartDisplayPosition.Y),
                          new Rectangle(x, 428, 7, 6),
                          color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
                }
                else
                {
                    b.Draw(Game1.mouseCursors,
                           new Vector2(heartDisplayPosition.X + ((hearts - 10) * 32), heartDisplayPosition.Y + 32f),
                           new Rectangle(x, 428, 7, 6),
                           color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
                }
            }
        }
    }

    public class FriendshipTargetUI : UIBase
    {
        private Vector2 position;
        private int friendshipLevel;
        private IconSlider npcSlider;
        private List<string> villagers;

        public FriendshipTargetUI(int friendshipLevel, Vector2 position)
        {
            this.position = position;
            this.friendshipLevel = friendshipLevel;
        }

        public override bool Draw(SpriteBatch b)
        {
            if (!IsVisible) return true;

            Dictionary<string, int> conditions = UIStateManager.Instance.EditedFriendshipCondition;


            foreach (var condition in conditions)
            {
                var npcName = condition.Key;
                var friendshipLevel = condition.Value;

                // 🔹 선택된 NPC 및 호감도 수치 텍스트 표시
                b.DrawString(Game1.smallFont, $"🎭 {npcName} >= {friendshipLevel}", new Vector2(position.X + 200, position.Y - 70), Color.White);
            }


            return false;
        }

        public override void LeftClick(int x, int y)
        {

        }
    }

}