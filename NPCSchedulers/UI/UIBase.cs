using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace NPCSchedulers.UI
{
    public abstract class UIBase
    {
        public bool IsVisible { get; private set; } = true;

        public void Show() => IsVisible = true;
        public void Hide() => IsVisible = false;
        public void ToggleVisibility() => IsVisible = !IsVisible;

        public abstract bool Draw(SpriteBatch b);

        public virtual void LeftHeld(int x, int y) { }
        public virtual void LeftClick(int x, int y) { }
        public virtual void Update(GameTime gameTime) { }


    }
    public abstract class ListUI : UIBase
    {

        public int scrollStep = 100;
        public int scrollPosition = 0;
        protected int maxScrollPosition = 0;
        protected Vector2 position;
        protected List<UIBase> elements = new List<UIBase>(); // 🔹 리스트 UI에 추가될 요소들
        public Rectangle viewport { get; protected set; }
        protected ClickableTextureComponent upArrow;
        protected ClickableTextureComponent downArrow;

        public ListUI(Vector2 position, int width, int height)
        {
            this.position = position;
            viewport = new Rectangle((int)position.X, (int)position.Y, width, height);

            // 🔹 스크롤 버튼 UI 생성
            int scrollBarX = (int)position.X + width - 20;
            upArrow = new ClickableTextureComponent(new Rectangle(scrollBarX, (int)position.Y, 32, 32),
                Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);

            downArrow = new ClickableTextureComponent(new Rectangle(scrollBarX, (int)position.Y + height - 32, 32, 32),
                Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
        }

        public override bool Draw(SpriteBatch b)
        {
            if (!IsVisible) return true;
            // 🔹 스크롤 버튼 그리기
            upArrow.draw(b);
            downArrow.draw(b);

            b.End();
            b.GraphicsDevice.ScissorRectangle = viewport;
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState() { ScissorTestEnable = true });

            return false;
        }


        public bool DrawEnd(SpriteBatch b)
        {
            b.End();
            b.Begin();

            return false;
        }
        public override void LeftClick(int x, int y)
        {

        }

        public void Scroll(int direction)
        {
            scrollPosition = MathHelper.Clamp(scrollPosition + direction * scrollStep, 0, maxScrollPosition);
        }

        public void SetMaxScrollPosition(int contentHeight, int viewportHeight)
        {
            maxScrollPosition = Math.Max(0, contentHeight - viewportHeight);
        }

        public void AddElement(UIBase element)
        {
            elements.Add(element);
        }
    }

}