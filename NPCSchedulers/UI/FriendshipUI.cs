using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCSchedulers.Store;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace NPCSchedulers.UI
{
    public class FriendshipUI : UIBase
    {
        private Vector2 heartDisplayPosition;
        private OptionsSlider heartSlider;  // 🔹 기존 하트 슬라이더
        private NPC villager;     // 🔹 모든 마을 NPC 목록
        public int Height = 20;
        public FriendshipUI(Vector2 position, string npcName, int FriendshipLevel)
        {
            heartDisplayPosition = new Vector2(position.X + 100, position.Y + Height);

            // 🔹 기존 하트 슬라이더 유지
            heartSlider = new OptionsSlider("", 0, (int)heartDisplayPosition.X + 100, (int)heartDisplayPosition.Y - 25);
            heartSlider.value = (int)(FriendshipLevel * 99f / 14); // 🔥 초기값 설정

            // 🔹 마을 NPC 목록 불러오기
            villager = Game1.getCharacterFromName(npcName);
        }

        public override bool Draw(SpriteBatch b)
        {
            if (!IsVisible) return true;

            int friendshipLevel = (int)(heartSlider.value / 99f * 14);

            // 🔹 하트 UI (기존 코드 유지)
            drawNPCSlotHeart(heartDisplayPosition, b, friendshipLevel);

            heartSlider.draw(b, 0, 0);
            Texture2D portraitTexture = villager.Portrait;
            // 🔥 첫 번째 초상화만 잘라서 가져오기
            Rectangle sourceRect = new Rectangle(0, 0, 64, 64); // (X:0, Y:0) → 첫 번째 초상화
            if (portraitTexture == null)
            {
                portraitTexture = villager.Sprite.Texture;
            }
            b.Draw(
                portraitTexture,
                new Rectangle((int)heartDisplayPosition.X, (int)heartDisplayPosition.Y - 50, 64, 64), // 화면에 표시될 위치
                sourceRect, // 🔥 잘라낸 부분만 그리기
                Color.White
            );
            return false;
        }

        public override void LeftClick(int x, int y)
        {
            // 🔹 하트 슬라이더 클릭 감지
            if (heartSlider.bounds.Contains(x, y))
            {
                heartSlider.receiveLeftClick(x, y);
                int newHeartLevel = (int)((heartSlider.value / 99.0f) * 14); // 🔥 슬라이더 값 -> 하트 값 변환

                UIStateManager.Instance.SetEditedFriendshipCondition(villager.Name, newHeartLevel);
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
        public int Height = 25;
        public FriendshipTargetUI(Vector2 position)
        {
            this.position = position;
        }

        public override bool Draw(SpriteBatch b)
        {
            if (!IsVisible) return true;

            Dictionary<string, int> conditions = UIStateManager.Instance.EditedFriendshipCondition;
            var filteredCondition = UIStateManager.Instance.FilterSetEditedFriendshipCondition(conditions);
            int yOffset = 0;

            foreach (var condition in filteredCondition)
            {
                var npcName = condition.Key;
                var friendshipLevel = condition.Value;

                // 🔹 선택된 NPC 및 호감도 수치 텍스트 표시
                b.DrawString(Game1.smallFont, $"{npcName} >= {friendshipLevel}", new Vector2(position.X, (int)position.Y + yOffset), Color.Gray);
                yOffset += 30;
            }
            Height = yOffset;

            return false;
        }

        public override void LeftClick(int x, int y)
        {

        }
    }
    public class FriendshipListUI : ListUI
    {
        List<FriendshipUI> friendshipUIs = new();
        List<string> villagers = new();
        public FriendshipListUI(Vector2 position) : base(position, 400, 400)
        {
            villagers = Utility.getAllCharacters().Where(npc => npc.IsVillager).Select(npc => npc.Name).ToList();
            UpdateFriendshipUI();
        }

        public void UpdateFriendshipUI()
        {
            friendshipUIs.Clear();
            var EditedFriendshipCondition = UIStateManager.Instance.EditedFriendshipCondition;

            int yOffset = 0;
            foreach (var npc in villagers)
            {
                int level = 0;
                if (EditedFriendshipCondition.ContainsKey(npc))
                {
                    level = EditedFriendshipCondition[npc];
                    yOffset += 90;
                }
                var detailDisplayPosition = new Vector2(position.X, position.Y + yOffset - scrollPosition);
                friendshipUIs.Add(new FriendshipUI(detailDisplayPosition, npc, level));
            }
            SetMaxScrollPosition(yOffset, viewport.Height);
        }

        public override bool Draw(SpriteBatch b)
        {
            NPC currentNPC = UIStateManager.Instance.CurrentNPC;
            SpriteText.drawStringWithScrollCenteredAt(b, currentNPC.Name, viewport.Center.X, viewport.Top - 60);

            base.Draw(b);

            foreach (var friendshipUI in friendshipUIs)
            {
                friendshipUI.Draw(b);

            }

            return base.DrawEnd(b);

        }
        public override void LeftClick(int x, int y)
        {
            if (upArrow.containsPoint(x, y))
            {
                Scroll(-1);
                UpdateFriendshipUI();
            }
            else if (downArrow.containsPoint(x, y))
            {
                Scroll(1);
                UpdateFriendshipUI();
            }
            else
            {

                foreach (var friendshipUI in friendshipUIs)
                {
                    friendshipUI.LeftClick(x, y);

                }
            }
        }
    }
}