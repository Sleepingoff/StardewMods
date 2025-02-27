
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace NPCDialogues;

public class CustomTextBox : IKeyboardSubscriber
{
    protected Texture2D _textBoxTexture;

    protected Texture2D _caretTexture;

    protected SpriteFont _font;

    protected Color _textColor;

    public bool numbersOnly;

    public int textLimit = -1;

    public bool limitHeight = true;

    public int maxHeight = -1;

    private string _text = "";

    private int _caretIndex = 0; // 현재 커서 위치를 저장합니다.
    private bool _selected;

    public SpriteFont Font => _font;
    private int _height = -1;

    public Color TextColor => _textColor;

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height
    {
        get
        {
            return _height;
        }
        set
        {
            _height = value;

        }
    }

    public bool PasswordBox { get; set; }

    public string Text
    {
        get
        {
            return _text;
        }
        set
        {
            _text = value;
            if (_text == null)
            {
                _text = "";
            }
            if (_text != "")
            {
                _text = Utility.FilterDirtyWordsIfStrictPlatform(_text);
                _text = _text.Replace("\n", "");
                _caretIndex = Math.Clamp(_caretIndex, 0, _text.Length);
            }
        }
    }

    //
    // 요약:
    //     Displayed as the title for virtual keyboards.
    public string TitleText { get; set; }

    public bool Selected
    {
        get
        {
            return _selected;
        }
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;
            if (_selected)
            {
                Game1.keyboardDispatcher.Subscriber = this;
                return;
            }

            if (Game1.keyboardDispatcher.Subscriber == this)
            {
                Game1.keyboardDispatcher.Subscriber = null;
            }
        }
    }

    public event TextBoxEvent OnEnterPressed;

    public event TextBoxEvent OnTabPressed;

    public event TextBoxEvent OnBackspacePressed;

    public CustomTextBox(Texture2D textBoxTexture, Texture2D caretTexture, SpriteFont font, Color textColor)
    {
        _caretTexture = caretTexture;
        _font = font;
        _textColor = textColor;
        _textBoxTexture = textBoxTexture;
        Height = (int)font.MeasureString("Ay").Y + 10;

        if (textBoxTexture != null)
        {
            Width = textBoxTexture.Width;
            Height = textBoxTexture.Height;
        }



    }

    public void SelectMe()
    {
        Selected = true;
        _caretIndex = Text.Length;
    }

    public void Update()
    {
        Point value = new Point(Game1.getMouseX(), Game1.getMouseY());
        if (new Rectangle(X, Y, Width, Height).Contains(value))
        {
            Selected = true;
            // _caretIndex
        }
        else
        {
            Selected = false;
        }


    }

    public virtual void Draw(SpriteBatch spriteBatch, bool drawShadow = true)
    {
        bool flag = Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000.0 >= 500.0;
        string text = Text;
        string wrappedText = Game1.parseText(text, this.Font, Width - 4);
        text = wrappedText;
        Vector2 vector = _font.MeasureString(text);
        if (limitHeight && maxHeight > 0) Height = Math.Clamp((int)vector.Y + 50, Height, maxHeight); else Height = Math.Max((int)vector.Y + 50, 100);

        if (PasswordBox)
        {
            text = "";
            for (int i = 0; i < Text.Length; i++)
            {
                text += "•";
            }
        }

        if (_textBoxTexture != null)
        {
            spriteBatch.Draw(_textBoxTexture, new Rectangle(X, Y, 16, Height), new Rectangle(0, 0, 16, Height), Color.White);
            spriteBatch.Draw(_textBoxTexture, new Rectangle(X + 16, Y, Width - 32, Height), new Rectangle(16, 0, 4, Height), Color.White);
            spriteBatch.Draw(_textBoxTexture, new Rectangle(X + Width - 16, Y, 16, Height), new Rectangle(_textBoxTexture.Bounds.Width - 16, 0, 16, Height), Color.White);
        }
        else
        {
            Game1.drawDialogueBox(X - 32, Y - 112 + 10, Width + 70, Height + 100, speaker: false, drawOnlyBox: true);
        }

        // 다중 줄을 고려한 캐럿 위치 계산
        // 현재 커서까지의 텍스트를 줄별로 분리하고, 마지막 줄의 수평 오프셋을 구함.
        string textBeforeCaret = text.Substring(0, Math.Min(_caretIndex, text.Length));
        string[] linesBeforeCaret = textBeforeCaret.Split('\n');
        int currentLineIndex = linesBeforeCaret.Length - 1;
        string currentLineText = linesBeforeCaret[currentLineIndex];
        Vector2 currentLineSize = _font.MeasureString(currentLineText);

        // 캐럿 X 좌표는 텍스트 박스 X+여백(16)에서 시작해, 현재 줄의 너비만큼 이동한 후 약간의 오프셋(2) 추가
        int caretX = X + 16 + (int)currentLineSize.X + (int)_font.MeasureString("Ay").X;

        // 캐럿 Y 좌표는 텍스트 박스 Y+여백(8)에서 시작하고, 현재 줄 인덱스에 따른 줄 높이만큼 이동
        int lineHeight = (int)_font.MeasureString("Ay").Y;
        int caretY = Y + 8 + currentLineIndex * lineHeight;

        // 캐럿 깜빡임: flag가 true일 때만 캐럿 그리기
        if (flag && Selected)
        {
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(caretX, caretY, 2, lineHeight), _textColor);
        }

        if (drawShadow)
        {
            Utility.drawTextWithShadow(spriteBatch, text, _font, new Vector2(X + 16, Y + ((_textBoxTexture != null) ? 12 : 8)), _textColor);
        }
        else
        {
            spriteBatch.DrawString(_font, text, new Vector2(X + 16, Y + ((_textBoxTexture != null) ? 12 : 8)), _textColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.99f);
        }
    }

    public virtual void RecieveTextInput(char inputChar)
    {
        if (!Selected || (numbersOnly && !char.IsDigit(inputChar)) || (textLimit != -1 && Text.Length >= textLimit))
        {
            return;
        }

        if (Game1.gameMode != 3)
        {
            switch (inputChar)
            {
                case '+':
                    Game1.playSound("slimeHit");
                    break;
                case '*':
                    Game1.playSound("hammer");
                    break;
                case '=':
                    Game1.playSound("coin");
                    break;
                case '<':
                    Game1.playSound("crystal", 0);
                    break;
                case '$':
                    Game1.playSound("money");
                    break;
                case '"':
                    return;
                default:
                    Game1.playSound("cowboy_monsterhit");
                    break;
            }
        }

        Text = Text.Insert(_caretIndex, inputChar.ToString());
        _caretIndex++;
    }

    public virtual void RecieveTextInput(string text)
    {
        int result = -1;
        if (Selected && (!numbersOnly || int.TryParse(text, out result)) && (textLimit == -1 || Text.Length < textLimit))
        {
            Text = Text.Insert(_caretIndex, text);
            _caretIndex += text.Length;
        }
    }

    public virtual void RecieveCommandInput(char command)
    {
        if (!Selected)
        {
            return;
        }
        switch (command)
        {

            case '\b':
                if (Text.Length <= 0)
                {
                    break;
                }
                if (_caretIndex > 0 && Text.Length > 0)
                {
                    Text = Text.Remove(_caretIndex - 1, 1);
                    _caretIndex--;
                }
                if (Game1.gameMode != 3)
                {
                    Game1.playSound("tinyWhip");
                }

                break;
            case '\r':
                // 엔터키 입력 시 현재 커서 위치에 줄바꿈 문자 삽입
                Text = Text.Insert(_caretIndex, "\n");
                _caretIndex++; // 커서 위치를 새 줄 바로 뒤로 이동
                if (Game1.gameMode != 3)
                {
                    Game1.playSound("coin");
                }

                break;
        }
    }

    public virtual void RecieveSpecialInput(Keys key)
    {
        if (!Selected)
        {
            return;
        }

        switch (key)
        {
            case Keys.Right:
                if (_caretIndex < Text.Length)
                {
                    _caretIndex++;
                    if (Game1.gameMode != 3)
                    {
                        Game1.playSound("shwip");
                    }
                }
                break;
            case Keys.Left:
                if (_caretIndex > 0)
                {
                    _caretIndex--;
                    if (Game1.gameMode != 3)
                    {
                        Game1.playSound("shwip"); // 원하는 효과음 사용
                    }
                }
                break;
            case Keys.Up:
                {
                    string wrappedText = Game1.parseText(Text, this.Font, Width - 4);
                    // 현재 커서까지의 텍스트를 줄별로 분리
                    string textBeforeCaret = wrappedText.Substring(0, Math.Min(_caretIndex, wrappedText.Length));
                    string[] caretLines = textBeforeCaret.Split('\n');
                    int currentLineIndex = caretLines.Length - 1;
                    // 윗줄이 있는 경우
                    if (currentLineIndex > 0)
                    {
                        // 현재 줄의 수평 오프셋(픽셀)
                        float currentOffset = _font.MeasureString(caretLines.Last()).X;
                        // 전체 텍스트를 줄별로 분리
                        string[] allLines = wrappedText.Split('\n');
                        string prevLine = allLines[currentLineIndex - 1];
                        int newPos = 0;
                        float bestDiff = float.MaxValue;
                        // 이전 줄에서, 현재 줄 오프셋과 가장 가까운 위치 찾기
                        for (int i = 0; i <= prevLine.Length; i++)
                        {
                            float width = _font.MeasureString(prevLine.Substring(0, i)).X;
                            float diff = Math.Abs(width - currentOffset);
                            if (diff < bestDiff)
                            {
                                bestDiff = diff;
                                newPos = i;
                            }
                        }
                        // 새로운 커서 인덱스는 이전 줄 시작 위치 + newPos
                        int newCaretIndex = 0;
                        for (int i = 0; i < currentLineIndex - 1; i++)
                        {
                            // 각 줄의 길이 + 줄바꿈 문자(1)를 포함
                            newCaretIndex += allLines[i].Length + 1;
                        }
                        newCaretIndex += newPos;
                        _caretIndex = newCaretIndex;
                        if (Game1.gameMode != 3)
                        {
                            Game1.playSound("shwip");
                        }
                    }
                }
                break;
            case Keys.Down:
                {
                    string wrappedText = Game1.parseText(Text, this.Font, Width - 4);
                    string[] allLines = wrappedText.Split('\n');
                    string textBeforeCaret = wrappedText.Substring(0, Math.Min(_caretIndex, wrappedText.Length));
                    string[] caretLines = textBeforeCaret.Split('\n');
                    int currentLineIndex = caretLines.Length - 1;
                    // 아래줄이 있는 경우
                    if (currentLineIndex < allLines.Length - 1)
                    {
                        // 현재 줄의 수평 오프셋 계산
                        float currentOffset = _font.MeasureString(caretLines.Last()).X;
                        string nextLine = allLines[currentLineIndex + 1];
                        int newPos = 0;
                        float bestDiff = float.MaxValue;
                        // 아래 줄에서 현재 오프셋과 가장 근접한 위치 찾기
                        for (int i = 0; i <= nextLine.Length; i++)
                        {
                            float width = _font.MeasureString(nextLine.Substring(0, i)).X;
                            float diff = Math.Abs(width - currentOffset);
                            if (diff < bestDiff)
                            {
                                bestDiff = diff;
                                newPos = i;
                            }
                        }
                        // 새로운 커서 인덱스는 위의 줄들 길이(줄바꿈 포함) + newPos
                        int newCaretIndex = 0;
                        for (int i = 0; i <= currentLineIndex; i++)
                        {
                            newCaretIndex += allLines[i].Length + 1;
                        }
                        newCaretIndex += newPos;
                        _caretIndex = newCaretIndex;
                        if (Game1.gameMode != 3)
                        {
                            Game1.playSound("shwip");
                        }
                    }
                }
                break;
        }
    }

    public void Hover(int x, int y)
    {
        if (x > X && x < X + Width && y > Y && y < Y + Height)
        {
            Game1.SetFreeCursorDrag();
        }
    }
}
