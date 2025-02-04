using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace NPCSchedulers.UI
{
    /// <summary>
    /// 입력 필드처럼 동작하는 커스텀 UI 요소 (클릭하면 TitleTextInputMenu가 열림)
    /// </summary>
    public class OptionsTextBox : OptionsElement
    {
        public readonly TextBox textBox;
        private readonly ClickableComponent clickableArea;

        public string _label;
        public OptionsTextBox(string label, string value, int width = 200) : base("", new Rectangle(0, 0, width, 50), 0)
        {
            this._label = label;
            textBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Color.Black)
            {
                Width = width,
                Height = 50,
                Text = value
            };

            clickableArea = new ClickableComponent(new Rectangle(0, 0, width, 50), label);
        }

        public override void receiveLeftClick(int x, int y)
        {
            base.receiveLeftClick(x, y);


            textBox.SelectMe();
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu clickableMenu = null)
        {
            base.draw(b, slotX, slotY);

            textBox.X = slotX + 200;
            textBox.Y = slotY;
            clickableArea.bounds.X = textBox.X;
            clickableArea.bounds.Y = textBox.Y;
            clickableArea.bounds.Width = textBox.Width;
            clickableArea.bounds.Height = textBox.Height;
            // 🔥 라벨을 위쪽에 작은 폰트로 표시
            Vector2 labelPosition = new Vector2(textBox.X - 200, textBox.Y);
            b.DrawString(Game1.smallFont, _label, labelPosition, Game1.textColor);
            textBox.Draw(b, false);
        }

        public bool ContainsPoint(int x, int y)
        {
            return clickableArea.containsPoint(x, y);
        }
    }


    /// <summary>
    /// 커스텀 드롭다운 구현 (기본 OptionsDropDown을 확장)
    /// </summary>
    /// <summary>
    /// 게임 내 옵션과 연동되는 커스텀 드롭다운
    /// </summary>
    public class CustomOptionsDropDown : OptionsElement
    {
        private readonly List<string> optionsList;
        private readonly Func<int> getValue;
        private readonly Action<int> setValue;
        private ClickableComponent clickableArea;
        private bool isExpanded;

        private static readonly Rectangle DropDownBGSource = new Rectangle(433, 451, 3, 3);
        private static readonly Rectangle DropDownButtonSource = new Rectangle(437, 450, 10, 11);
        public Rectangle dropDownBounds;

        public CustomOptionsDropDown(string label, List<string> options, Func<int> getValue, Action<int> setValue)
            : base("")
        {
            this.getValue = getValue;
            this.setValue = setValue;
            this.isExpanded = false;
            this.optionsList = options ?? new List<string> { "None" };

            // 🔥 드롭다운 클릭 영역 설정
            clickableArea = new ClickableComponent(new Rectangle(0, 0, 200, 40), label);
        }

        public override void receiveLeftClick(int x, int y)
        {
            base.receiveLeftClick(x, y);

            if (isExpanded && dropDownBounds.Contains(x, y))
            {
                int index = (y - dropDownBounds.Y) / clickableArea.bounds.Height;
                if (index >= 0 && index < optionsList.Count)
                {
                    setValue(index);
                    Game1.playSound("drumkit6");
                }
                isExpanded = false;
            }
            if (clickableArea.containsPoint(x, y))
            {
                isExpanded = !isExpanded;
                Game1.playSound("shwip");
            }
        }
        public void Close()
        {
            OptionsDropDown.selected = null;

            isExpanded = false;
        }
        public bool IsActive()
        {
            return isExpanded;
        }
        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu clickableMenu = null)
        {
            base.draw(b, slotX, slotY);
            // 🔥 드롭다운의 개별 위치 설정
            bounds.X = slotX;
            bounds.Y = slotY;
            bounds.Width = 200;
            bounds.Height = 40;
            clickableArea.bounds.X = slotX;
            clickableArea.bounds.Y = slotY;

            dropDownBounds = new Rectangle(clickableArea.bounds.X, clickableArea.bounds.Y + clickableArea.bounds.Height, clickableArea.bounds.Width, optionsList.Count * clickableArea.bounds.Height);

            //라벨 표시
            Vector2 labelPosition = new Vector2(slotX, slotY - 20);
            b.DrawString(Game1.smallFont, label, labelPosition, Game1.textColor);
            // 🔥 기본 드롭다운 박스
            IClickableMenu.drawTextureBox(
                b, Game1.mouseCursors, DropDownBGSource,
                clickableArea.bounds.X, clickableArea.bounds.Y, clickableArea.bounds.Width - 48, clickableArea.bounds.Height,
                Color.White, 4f, drawShadow: false
            );

            // 🔥 현재 선택된 옵션 표시
            int selectedIndex = getValue();
            if (optionsList.Count <= selectedIndex || selectedIndex == -1)
            {
                selectedIndex = 0;
            }

            b.DrawString(Game1.smallFont, optionsList[selectedIndex], new Vector2(clickableArea.bounds.X + 4, clickableArea.bounds.Y + 8), Game1.textColor);

            // 🔥 드롭다운 버튼 아이콘
            b.Draw(Game1.mouseCursors, new Vector2(clickableArea.bounds.X + clickableArea.bounds.Width - 48, clickableArea.bounds.Y), DropDownButtonSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);

            // 🔥 드롭다운이 열려 있을 경우, 옵션 리스트 표시
            if (isExpanded)
            {
                IClickableMenu.drawTextureBox(
                    b, Game1.mouseCursors, DropDownBGSource,
                    dropDownBounds.X, dropDownBounds.Y, dropDownBounds.Width, dropDownBounds.Height,
                    Color.White, 4f, drawShadow: false, 0.98f
                );

                for (int i = 0; i < optionsList.Count; i++)
                {
                    Rectangle optionBounds = new Rectangle(dropDownBounds.X, dropDownBounds.Y + i * clickableArea.bounds.Height, dropDownBounds.Width, clickableArea.bounds.Height);

                    if (i == selectedIndex)
                    {
                        b.Draw(Game1.staminaRect, optionBounds, new Rectangle(0, 0, 1, 1), Color.Wheat, 0f, Vector2.Zero, SpriteEffects.None, 0.98f);
                    }
                    b.DrawString(Game1.smallFont, optionsList[i], new Vector2(optionBounds.X + 4, optionBounds.Y + 8), Game1.textColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.98f);
                }


            }
        }

        public bool ContainsPoint(int x, int y)
        {
            return clickableArea.containsPoint(x, y);
        }
    }

    public class IconSlider : OptionsSlider
    {
        private readonly List<string> npcs;
        public int selectedIndex = 0;

        public IconSlider(List<string> npcList)
            : base("", 0)
        {
            this.npcs = npcList;
        }

        public override void receiveLeftClick(int x, int y)
        {
            selectedIndex = (selectedIndex + 1) % npcs.Count;
            Game1.playSound("shwip");
        }

        public override void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu clickableMenu = null)
        {
            base.draw(b, slotX, slotY);

            bounds.X = slotX;
            bounds.Y = slotY;
            bounds.Width = 64;
            bounds.Height = 64;
            // 선택된 NPC 얼굴 표시 (첫 번째 초상화만 가져오기)
            NPC selectedNPC = Game1.getCharacterFromName(npcs[selectedIndex]);
            Texture2D portraitTexture = selectedNPC.Portrait;
            // 🔥 첫 번째 초상화만 잘라서 가져오기
            Rectangle sourceRect = new Rectangle(0, 0, 64, 64); // (X:0, Y:0) → 첫 번째 초상화
            if (portraitTexture == null)
            {
                portraitTexture = selectedNPC.Sprite.Texture;
            }
            b.Draw(
                portraitTexture,
                new Rectangle(bounds.X, bounds.Y, 64, 64), // 화면에 표시될 위치
                sourceRect, // 🔥 잘라낸 부분만 그리기
                Color.White
            );
        }

        public string GetSelectedNPC()
        {
            return npcs[selectedIndex];
        }
    }

}