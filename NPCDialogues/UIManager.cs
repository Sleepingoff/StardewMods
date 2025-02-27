using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Characters;
using StardewValley.Menus;

namespace NPCDialogues
{
    public class UIManager
    {
        public NPC npc;
        public ProfileMenu currentMenu;
        public bool isOpen;
        public bool IsOpen
        {
            get
            {
                return isOpen;
            }
            set
            {
                isOpen = value;
                if (isOpen && this != null)
                {
                    NPC nPC = currentMenu?.Current.Character as NPC;
                    this.dialogueUI = new DialogueUI(nPC.Name, this);
                }
                else this.dialogueUI = null;
            }
        }

        public bool isShowPreview;
        private DialogueUI dialogueUI;
        private IModHelper Helper;

        private Rectangle dialogueButton = Rectangle.Empty;
        public UIManager(IModHelper helper, string npcName)
        {
            Helper = helper;
            this.npc = Game1.getCharacterFromName(npcName);
            CreateScheduleButton();
        }
        public void CreateScheduleButton()
        {
            if (!(Game1.activeClickableMenu is ProfileMenu)) return;
            ProfileMenu menu = (ProfileMenu)Game1.activeClickableMenu;
            this.currentMenu = menu;
            int buttonX = menu.xPositionOnScreen + menu.width - 80 - 200;
            int buttonY = menu.yPositionOnScreen + 650;

            dialogueButton = new Rectangle(buttonX, buttonY, 200, 32);
        }
        public void OnScroll(int delta)
        {
            dialogueUI?.OnMouseWheel(delta);
        }
        public void OnClickButton(object sender, ButtonPressedEventArgs e)
        {

            float x = Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.X);
            float y = Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.Y);
            if (e.Button == SButton.MouseLeft && dialogueButton.Contains(x, y))
            {
                IsOpen = !IsOpen;
                if (IsOpen)
                {
                    Helper.Events.Input.ButtonPressed -= dialogueUI.OnClickDetails;
                    Helper.Events.Input.ButtonPressed += dialogueUI.OnClickDetails;
                }
            }
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
            if (!IsOpen) return true;

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
            Rectangle backGrounds = new Rectangle(itemDisplayRect.X + 50, itemDisplayRect.Y, itemDisplayRect.Width - 100, itemDisplayRect.Height - 50);
            b.Draw(Game1.staminaRect, backGrounds, Color.AntiqueWhite);
            // 다이얼로그 박스 배경
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                itemDisplayRect.X + 20, itemDisplayRect.Y - 20, itemDisplayRect.Width - 50, itemDisplayRect.Height - 30,
                Color.White, 1f, false
            );

            SpriteText.drawStringWithScrollCenteredAt(b, nPC.Name + "'s Dialogues",
                                                       itemDisplayRect.Center.X, itemDisplayRect.Top + 10);
            dialogueUI?.Draw(b);

            Game1.activeClickableMenu?.drawMouse(b, ignore_transparency: true);

            return false;
        }


        public void EditDialogue(string npcName, KeyValuePair<string, string> dialogue)
        {
            IsOpen = false;
            this.dialogueUI = new DialogueUI(npcName, this);
            Game1.activeClickableMenu = new DialogueEditMenu(npcName, dialogue);

        }

        //미리보기
        public void ShowDialogue(string npcName, KeyValuePair<string, string> dialogue)
        {
            IsOpen = false;
            Farmer who = Game1.player;
            NPC speaker = Game1.getCharacterFromName(npcName);
            if (!who.friendshipData.TryGetValue(speaker.Name, out var value))
            {
                Game1.addHUDMessage(new HUDMessage("retry after talking", 2));
                return;
            }
            bool isAlreadyTalkedToday = value.TalkedToToday;
            value.TalkedToToday = false;

            speaker.CurrentDialogue.Clear();
            speaker.CurrentDialogue.Push(new Dialogue(speaker, dialogue.Key, dialogue.Value.Replace("#$e#", "#$b#")));
            isShowPreview = true;
            Game1.drawDialogue(speaker);

            value.TalkedToToday = isAlreadyTalkedToday;

        }
    }
}