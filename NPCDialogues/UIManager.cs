using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Characters;
using StardewValley.Menus;

namespace NPCDialogues
{
    public class UIManager
    {
        public NPC npc;

        private Rectangle dialogueButton = Rectangle.Empty;
        public UIManager(string npcName)
        {
            this.npc = Game1.getCharacterFromName(npcName);
            CreateScheduleButton();
        }
        public void CreateScheduleButton()
        {
            if (!(Game1.activeClickableMenu is ProfileMenu)) return;
            ProfileMenu menu = (ProfileMenu)Game1.activeClickableMenu;
            int buttonX = menu.xPositionOnScreen + menu.width - 100;
            int buttonY = menu.yPositionOnScreen + 650;

            dialogueButton = new Rectangle(buttonX, buttonY, 64, 32);
        }
        private void DrawDialogButton(SpriteBatch b, Rectangle bounds, string text, bool disable = false)
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
        public void DrawButton(SpriteBatch b)
        {
            if (dialogueButton == Rectangle.Empty) return;
            DrawDialogButton(b, dialogueButton, "Dialogues");
        }
        public bool Draw(SpriteBatch b)
        {

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

            rectangle.Y += 32;
            rectangle.Height -= 32;

            rectangle.Y += Game1.daybg.Height + 32;
            rectangle.Height -= Game1.daybg.Height + 32;

            rectangle.Y += 96;
            rectangle.Height -= 96;
            NPC nPC = ((ProfileMenu)Game1.activeClickableMenu)?.Current.Character as NPC;

            if (nPC == null) return true;

            CharacterData data = nPC.GetData();
            string text = "Characters/" + nPC.getTextureName();


            SpriteText.drawStringWithScrollCenteredAt(b, nPC.Name + "'s Dialogues",
                                                       itemDisplayRect.Center.X, itemDisplayRect.Top);


            Game1.activeClickableMenu?.drawMouse(b, ignore_transparency: true);

            return false;
        }

    }
}