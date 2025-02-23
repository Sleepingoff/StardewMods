using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace NPCDialogues
{
    public class DialogueUI
    {

        //UI - Scroll
        public int scrollStep = 100;
        public int scrollPosition = 0;
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
        private DataManager dataManager;
        private Dictionary<string, string> originDialogues = new();
        private Dictionary<string, string> userDialogues = new();
        public DialogueUI(string npcName, DataManager dataManager)
        {

            //UI - Scroll
            int scrollBarX = (int)position.X + width - 20;
            upArrow = new ClickableTextureComponent(new Rectangle(scrollBarX, (int)position.Y, 32, 32),
                Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);

            downArrow = new ClickableTextureComponent(new Rectangle(scrollBarX, (int)position.Y + height - 32, 32, 32),
                Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
            //UI - viewport
            viewport = new Rectangle((int)position.X, (int)position.Y, width, height);
            //Data
            this.npc = Game1.getCharacterFromName(npcName);
            this.dataManager = dataManager;
        }

        public void InitUI()
        {
            (originDialogues, userDialogues) = dataManager.GetDialogues(npc.Name);
        }

        public void Draw(SpriteBatch b)
        {
            // 🔹 스크롤 버튼 그리기
            upArrow.draw(b);
            downArrow.draw(b);

            b.End();
            b.GraphicsDevice.ScissorRectangle = viewport;
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState() { ScissorTestEnable = true });

            //drawing here


            b.End();
            b.Begin();
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