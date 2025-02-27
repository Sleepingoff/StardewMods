using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;

namespace NPCDialogues
{
    public class DialogueUI
    {

        //UI - Scroll
        public int scrollStep = 100;
        public static int scrollPosition = 0;
        protected int maxScrollPosition = 0;

        //UI - viewport
        private int width = 100;
        private int height = 100;
        private Vector2 position;
        public Rectangle viewport;
        protected ClickableTextureComponent upArrow;
        protected ClickableTextureComponent downArrow;

        //Data
        public NPC npc;
        private UIManager uiManager;
        private Dictionary<string, string> originDialogues = new();
        private Dictionary<string, string> userDialogues = new();

        public Dictionary<string, string> AllDialogues
        {
            get
            {
                return originDialogues.Concat(userDialogues)
                              .GroupBy(d => d.Key)
                              .ToDictionary(g => g.Key, g => g.Last().Value);
            }
        }
        public DialogueUI(string npcName, UIManager uiManager)
        {
            int x = (int)uiManager.currentMenu?.xPositionOnScreen + 500;
            int y = (int)uiManager.currentMenu?.yPositionOnScreen + 100;
            this.position = new Vector2(x, y);
            //UI - Scroll

            this.width = (int)uiManager.currentMenu?.width - 500 - 100;
            this.height = (int)uiManager.currentMenu?.height - 200 - 50;


            int scrollBarX = (int)position.X + width - 20;
            upArrow = new ClickableTextureComponent(new Rectangle(scrollBarX, (int)position.Y, 32, 32),
                Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);

            downArrow = new ClickableTextureComponent(new Rectangle(scrollBarX, (int)position.Y + height - 32, 32, 32),
                Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
            //UI - viewport

            viewport = new Rectangle((int)position.X, (int)position.Y, width - 50, height);
            //Data
            this.npc = Game1.getCharacterFromName(npcName);
            this.uiManager = uiManager;

            InitUI();

        }

        public void InitUI()
        {
            (originDialogues, userDialogues) = DataManager.GetDialogues(npc.Name);

        }

        public void Draw(SpriteBatch b)
        {
            b.End();
            b.GraphicsDevice.ScissorRectangle = viewport;
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState() { ScissorTestEnable = true });

            //drawing here
            int yOffset = 300;
            Vector2 detailDisplayPosition = new Vector2(viewport.X, 0);
            foreach (var dialogues in AllDialogues)
            {
                if (string.IsNullOrEmpty(dialogues.Value)) continue;
                bool edited = userDialogues.ContainsKey(dialogues.Key);
                Rectangle detailDisplay = new Rectangle((int)detailDisplayPosition.X, yOffset - scrollPosition, viewport.Width, 80);
                DrawBorder(b, detailDisplay, 3, Color.Brown);

                // 🔹 기존 UI 스타일 유지 (반투명 박스 + 테두리)
                b.Draw(Game1.staminaRect, detailDisplay, new Rectangle(0, 0, 1, 1), Color.SandyBrown * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 0.8f);



                string dialogue = Game1.parseText(dialogues.Value, Game1.smallFont, detailDisplay.Width).Split("\n").First();

                b.DrawString(Game1.smallFont, dialogues.Key, new Vector2(detailDisplay.X + 10, detailDisplay.Y + 10), edited ? Color.DarkBlue : Color.CadetBlue);
                b.DrawString(Game1.smallFont, dialogue, new Vector2(detailDisplay.X + 10, detailDisplay.Y + 40), Color.Black);

                // 🔹 개별 추가 버튼 추가
                ClickableTextureComponent editButton = new ClickableTextureComponent(
                        new Rectangle(viewport.Right - 80, detailDisplay.Y + 10, 32, 32),
                        Game1.mouseCursors, new Rectangle(175, 378, 16, 16), 2f);
                editButton.draw(b);
                // 🔹 개별 삭제 버튼 추가
                ClickableTextureComponent deleteButton = new ClickableTextureComponent(
                    new Rectangle(viewport.Right - 30, detailDisplay.Y + 15, 32, 32),
                    Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 2f);
                deleteButton.draw(b);
                yOffset += 100; // 🔹 각 스케줄 간격 유지
            }

            b.End();
            b.Begin();
            SetMaxScrollPosition(yOffset, viewport.Height);
            // 🔹 스크롤 버튼 그리기
            upArrow.draw(b);
            downArrow.draw(b);
        }
        public void OnMouseWheel(int direction)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            direction = direction > 0 ? -1 : 1;
            int x = (int)Utility.ModifyCoordinateForUIScale(mouseX);
            int y = (int)Utility.ModifyCoordinateForUIScale(mouseY);
            if (viewport.Contains(x, y))
            {
                Scroll(direction);
            }
        }
        public void OnClickDetails(object sender, ButtonPressedEventArgs e)
        {
            if (!uiManager.isOpen) return;
            int x = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.X);
            int y = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.Y);

            if (upArrow.containsPoint(x, y))
            {
                Scroll(-1);
            }
            else if (downArrow.containsPoint(x, y))
            {
                Scroll(1);
            }
            else if (viewport.Contains(x, y))
            {

                int yOffset = 300;
                Vector2 detailDisplayPosition = new Vector2(viewport.X, 0);

                foreach (var dialogue in AllDialogues)
                {
                    if (string.IsNullOrEmpty(dialogue.Value)) continue;
                    Rectangle detailDisplay = new Rectangle((int)detailDisplayPosition.X, yOffset - scrollPosition, viewport.Width - 20, 80);
                    Rectangle editButtonBounds = new Rectangle(viewport.Right - 80, detailDisplay.Y + 10, 32, 32);
                    Rectangle deleteButtonBounds = new Rectangle(viewport.Right - 30, detailDisplay.Y + 15, 32, 32);
                    if (deleteButtonBounds.Contains(x, y))
                    {
                        DataManager.DeleteDialogue(npc.Name, dialogue.Key);

                    }
                    else if (editButtonBounds.Contains(x, y))
                    {
                        uiManager.EditDialogue(npc.Name, dialogue);
                    }
                    else if (detailDisplay.Contains(x, y))
                    {
                        uiManager.ShowDialogue(npc.Name, dialogue);
                    }

                    yOffset += 100; // 🔹 각 스케줄 간격 유지
                }
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
        public void Scroll(int direction)
        {
            scrollPosition = MathHelper.Clamp(scrollPosition + direction * scrollStep, 0, maxScrollPosition);
        }

        public void SetMaxScrollPosition(int contentHeight, int viewportHeight)
        {
            maxScrollPosition = Math.Max(0, contentHeight - viewportHeight);
            scrollPosition = Math.Min(scrollPosition, maxScrollPosition);
        }

    }
}