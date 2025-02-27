using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Characters;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace NPCDialogues
{
    public class DialogueEditMenu : IClickableMenu
    {
        private string npcName;
        private string dialogueKey;
        private KeyValuePair<string, string> dialogue = new();
        private List<string> pages = new();
        private int currentPage;
        private string currentText;
        private string currentPortrait; //대사에 들어가야 함
        private List<string> currentPortraitText = new();

        // UI 영역 (DialogueBox 스타일)
        private CustomTextBox inputBox;
        private Rectangle dialogueBoxRect;
        private Rectangle portraitBoxRect;
        private Rectangle addButtonRect;
        private Rectangle saveButtonRect;
        private Rectangle backButtonRect;
        private ClickableComponent prevDialogueButton;
        private ClickableComponent nextDialogueButton;

        private ClickableComponent prevPortraitButton;
        private ClickableComponent nextPortraitButton;
        private TextBox keyInputBox;
        private ClickableComponent gotoKeyButton;
        private bool isNewKey = false;
        private List<Texture2D> portraitList = new();
        private int currentPortraitIndex = 0; //다른 외형을 보게 할 뿐, 대사에 영향 x
        public DialogueEditMenu(string npcName, KeyValuePair<string, string> dialogue)
            : base(0, 0, Game1.graphics.GraphicsDevice.Viewport.Width, Game1.graphics.GraphicsDevice.Viewport.Height, showUpperRightCloseButton: false)
        {
            this.npcName = npcName;
            this.dialogueKey = dialogue.Key;
            this.dialogue = dialogue;
            // 기존 대사는 "#$b" 구분자로 페이지 분리
            pages = dialogue.Value.Split("#$b#").ToList();
            pages = pages.SelectMany(page => page.Split("#$e#").Select((p, index) => index == page.Split("#$e#").Length - 1 ? p : p + "#$e#")).ToList();

            if (pages.Count == 0)
                pages.Add("");
            currentPortraitText.Add("$0");
            currentPage = 0;
            currentText = pages[currentPage];
            ParsePortraitToken();
            NPC npc = Game1.getCharacterFromName(npcName);
            string[] list ={
             "",   "Beach", "Winter"
            };


            portraitList.Clear();
            foreach (var item in list)
            {

                try
                {
                    string assetName = npc.Portrait.Name.Split("_").First();
                    Texture2D portrait = Game1.content.Load<Texture2D>(string.IsNullOrEmpty(item) ? assetName : assetName + "_" + item);
                    portraitList.Add(portrait);
                }
                catch (Exception)
                {
                    //do nothing
                }

            }


            // DialogueBox와 유사한 스타일: 고정 크기 1200×384, 중앙 배치(수평), 하단에 배치
            int boxWidth = 1100;
            int boxHeight = 384;

            // 초상화 영역: 대화상자 오른쪽에 배치 (예: 전체 너비의 20% 정도)
            int portraitWidth = (int)(boxWidth * 0.3f) + 20;

            int x = (int)Utility.getTopLeftPositionForCenteringOnScreen(boxWidth + portraitWidth, boxHeight).X;
            int y = Game1.uiViewport.Height - boxHeight - 64;
            width = boxWidth;
            height = boxHeight;
            dialogueBoxRect = new Rectangle(x, y, width, height);
            portraitBoxRect = new Rectangle(dialogueBoxRect.Right, dialogueBoxRect.Y, portraitWidth, boxHeight);
            prevPortraitButton = new ClickableComponent(new Rectangle(portraitBoxRect.Center.X - (32 * 4 + 10), dialogueBoxRect.Y + boxHeight - 50, 32, 32), "prev");
            nextPortraitButton = new ClickableComponent(new Rectangle(portraitBoxRect.Center.X - (32 * 4 + 10) + 32 + 20, dialogueBoxRect.Y + boxHeight - 50, 32, 32), "next");
            keyInputBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Color.Black)
            {
                Width = 500,
                Height = 80,
                Text = dialogue.Key
            };
            keyInputBox.X = dialogueBoxRect.X + 50;
            keyInputBox.Y = dialogueBoxRect.Top + 20;

            gotoKeyButton = new ClickableComponent(new Rectangle(keyInputBox.X + keyInputBox.Width + 10, keyInputBox.Y, 40, 40), "search");
            // 대사 편집창 우측 하단에 추가 버튼 (32×32)
            addButtonRect = new Rectangle(dialogueBoxRect.Right - 40, dialogueBoxRect.Top + 40, 32, 32);

            // 초상화 박스 우측 상단에 저장 버튼 (32×32)
            saveButtonRect = new Rectangle(portraitBoxRect.Right - 50, portraitBoxRect.Y - 50, 32, 32);
            backButtonRect = new Rectangle(portraitBoxRect.Right - 50 - 32 - 20, portraitBoxRect.Y - 50, 32, 32);
            // 좌측/우측 버튼: 대화 페이지 이동 (DialogueBox 스타일의 하단 좌우 여백 고려)
            prevDialogueButton = new ClickableComponent(new Rectangle(dialogueBoxRect.Center.X + 400, dialogueBoxRect.Top + 50, 32, 32), "<");
            nextDialogueButton = new ClickableComponent(new Rectangle(dialogueBoxRect.Center.X + 400 + 60, dialogueBoxRect.Top + 50, 32, 32), ">");
            // TextBox 생성 및 커스터마이징
            int textBoxX = dialogueBoxRect.X + 20;
            int textBoxY = dialogueBoxRect.Y + 100;
            int textBoxWidth = dialogueBoxRect.Width - 40;
            inputBox = new CustomTextBox(null, null, Game1.dialogueFont, Color.Black);
            inputBox.X = textBoxX;
            inputBox.Y = textBoxY;
            inputBox.Width = textBoxWidth;
            string wrappedText = Game1.parseText(currentText, Game1.dialogueFont, dialogueBoxRect.Width - 40);
            inputBox.Text = wrappedText;
            inputBox.Selected = false; // 초기엔 포커스 없음


        }
        public override void update(GameTime time)
        {
            base.update(time);
            // TextBox가 포커스 상태라면 업데이트하여 내부 텍스트를 처리
            if (inputBox.Selected)
            {
                inputBox.Update();
                currentText = inputBox.Text;
                ParsePortraitToken();
            }
        }


        public override void draw(SpriteBatch b)
        {

            // 어두운 배경 오버레이
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);

            // 대화상자 배경 그리기 (DialogueBox와 동일한 텍스처 박스 사용)
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                dialogueBoxRect.X, dialogueBoxRect.Y, dialogueBoxRect.Width, dialogueBoxRect.Height, Color.White, 1f, false);
            // 페이지 인디케이터 표시
            string pageIndicator = $"Page {currentPage + 1} / {pages.Count}";
            Vector2 indicatorSize = Game1.smallFont.MeasureString(pageIndicator);
            b.DrawString(Game1.smallFont, pageIndicator, new Vector2(dialogueBoxRect.X + dialogueBoxRect.Width - indicatorSize.X - 10, dialogueBoxRect.Y + 10), Color.Black);


            inputBox.Draw(b);
            keyInputBox.Draw(b);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(80, 0, 16, 16),
                     keyInputBox.X + keyInputBox.Width + 10, keyInputBox.Y, 40, 40, Color.White, 2.5f, false);

            if (isNewKey) b.Draw(Game1.mouseCursors, new Vector2(keyInputBox.X + keyInputBox.Width + 10 + 50, keyInputBox.Y), new Rectangle(315, 408, 26, 12), Color.White, 0f, new Vector2(0, 0), 3f, SpriteEffects.None, 0f);

            // 추가 버튼 그리기
            b.Draw(Game1.mouseCursors, addButtonRect, new Rectangle(175, 378, 16, 16), Color.White);

            // 초상화 영역 그리기
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                portraitBoxRect.X, portraitBoxRect.Y, portraitBoxRect.Width, portraitBoxRect.Height, Color.White, 1f, false);

            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
           portraitBoxRect.Center.X - (32 * 4 + 10), portraitBoxRect.Y + 40, 64 * 4 + 20, 64 * 4 + 20, Color.White, 1f, false);

            NPC npc = Game1.getCharacterFromName(npcName);
            var portraitRect = Game1.getSourceRectForStandardTileSheet(npc.Portrait, Convert.ToInt32(currentPortrait.Substring(1)));

            if (portraitList.Count > 0 && portraitList.Count > currentPortraitIndex) b.Draw(portraitList[currentPortraitIndex], new Vector2(portraitBoxRect.Center.X - (32 * 4 + 10), portraitBoxRect.Y + 50), portraitRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.99f);
            b.DrawString(Game1.smallFont, "<", new Vector2(prevPortraitButton.bounds.X, prevPortraitButton.bounds.Y), Color.Black);
            b.DrawString(Game1.smallFont, ">", new Vector2(nextPortraitButton.bounds.X, nextPortraitButton.bounds.Y), Color.Black);
            b.DrawString(Game1.smallFont, currentPortrait, new Vector2(portraitBoxRect.Right - 70, portraitBoxRect.Y + portraitBoxRect.Height - 50), Color.Black);
            //Draw(Texture2D texture, Vector2 position, Rectangle ? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
            // 저장 버튼 그리기
            b.Draw(Game1.mouseCursors, new Vector2(saveButtonRect.X, saveButtonRect.Y), new Rectangle(127, 256, 64, 64), Color.White, 0f, new Vector2(16, 16), 1f, SpriteEffects.None, 0f);
            //뒤로가기 버튼 그리기
            b.Draw(Game1.mouseCursors, new Vector2(backButtonRect.X, backButtonRect.Y), new Rectangle(335, 493, 16, 16), Color.White, 0f, new Vector2(8, 4), 4f, SpriteEffects.None, 0f);
            //페이지 이동 버튼 그리기
            b.Draw(Game1.mouseCursors, new Vector2(prevDialogueButton.bounds.X, prevDialogueButton.bounds.Y), new Rectangle(480, 96, 32, 32), Color.White, 0f, new Vector2(16, 16), 1f, SpriteEffects.None, 0f);
            b.Draw(Game1.mouseCursors, new Vector2(nextDialogueButton.bounds.X, nextDialogueButton.bounds.Y), new Rectangle(480 - 32, 96, 32, 32), Color.White, 0f, new Vector2(16, 16), 1f, SpriteEffects.None, 0f);


            // 기본 닫기 버튼 등
            base.draw(b);

            Game1.activeClickableMenu?.drawMouse(b, ignore_transparency: true);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {   // TextBox의 영역을 클릭하면 포커스 전환
            Rectangle inputBounds = new Rectangle(inputBox.X, inputBox.Y, inputBox.Width, inputBox.Height);

            Rectangle portraitBounds = new Rectangle(portraitBoxRect.X, portraitBoxRect.Y + 40, portraitBoxRect.Width, 64 * 4 + 20);


            Rectangle keyInputBoxBounds = new Rectangle(keyInputBox.X, keyInputBox.Y, keyInputBox.Width, keyInputBox.Height);

            if (inputBounds.Contains(x, y))
            {
                inputBox.SelectMe();
                Game1.playSound("smallSelect");
            }
            else
            {
                inputBox.Selected = false;

            }

            // 추가 버튼 클릭: 새 페이지 추가
            if (addButtonRect.Contains(x, y))
            {
                pages.Add("");
                currentPortraitText.Add("$0");
                currentPage = pages.Count - 1;
                currentText = "";
                string wrappedText = Game1.parseText(currentText, Game1.smallFont, dialogueBoxRect.Width - 40);
                inputBox.Text = wrappedText;
                Game1.playSound("smallSelect");
            }
            // 저장 버튼 클릭: 편집한 대사 저장 후 메뉴 종료
            else if (saveButtonRect.Contains(x, y))
            {
                pages = pages.Select(p =>
                    p.Replace("\n", "")
                    .Replace("\r", "")
                )
                .ToList();
                string newDialogue = "";

                for (int i = 0; i < pages.Count; i++)
                {
                    var page = pages[i];
                    var portraitIndex = currentPortraitText.Count - 1 >= i ? currentPortraitText[i] : "";
                    if (string.IsNullOrEmpty(page) || string.IsNullOrWhiteSpace(page)) continue;

                    if (page.Trim().EndsWith("#$e#") || page.Trim().EndsWith("#$b#"))
                    {
                        newDialogue += page.Trim();
                    }
                    else
                    {
                        newDialogue += page.Trim() + "#$b#";
                    }
                    //newDialogue의 마지막 "#$b#" 또는  "#$e#" 앞에 portraitIndex 삽입
                    string[] markers = { "#$b#", "#$e#" };

                    foreach (var marker in markers)
                    {
                        int position = newDialogue.LastIndexOf(marker);
                        if (position != -1)
                        {
                            newDialogue = newDialogue.Insert(position, portraitIndex);
                            break;
                        }
                    }

                }
                newDialogue = newDialogue.Trim().Trim("#$b#".ToCharArray());
                if (DataManager.npc_DialoguesByUser.ContainsKey(npcName))
                {
                    if (DataManager.npc_DialoguesByUser[npcName].ContainsKey(dialogueKey))
                        DataManager.npc_DialoguesByUser[npcName][dialogueKey] = newDialogue;
                    else
                        DataManager.npc_DialoguesByUser[npcName].Add(dialogueKey, newDialogue);
                }
                else
                {
                    DataManager.npc_DialoguesByUser[npcName] = new Dictionary<string, string>() { { dialogueKey, newDialogue } };
                }
                DataManager.SaveUserDialogues();
                Game1.playSound("money");
                currentPortraitText.Clear();
                exitThisMenu();
            }
            // 좌측 버튼 클릭: 이전 대사 페이지로 이동
            else if (prevDialogueButton.bounds.Contains(x + 16, y + 16))
            {
                if (currentPage > 0)
                {
                    currentPage--;
                    currentText = pages[currentPage];

                    ParsePortraitToken();
                    string wrappedText = Game1.parseText(currentText, Game1.smallFont, dialogueBoxRect.Width - 40);
                    inputBox.Text = wrappedText;
                    Game1.playSound("shwip");
                }
            }
            // 우측 버튼 클릭: 다음 대사 페이지로 이동
            else if (nextDialogueButton.bounds.Contains(x + 16, y + 16))
            {
                if (currentPage < pages.Count - 1)
                {
                    currentPage++;
                    currentText = pages[currentPage];
                    ParsePortraitToken();
                    string wrappedText = Game1.parseText(currentText, Game1.smallFont, dialogueBoxRect.Width - 40);
                    inputBox.Text = wrappedText;
                    Game1.playSound("shwip");
                }
            }

            else if (portraitBounds.Contains(x, y))
            {
                if (int.TryParse(currentPortrait.Split("$").Last(), out int portraitNum))
                {
                    int nextPortraitNum = portraitNum + 1;
                    NPC npc = Game1.getCharacterFromName(npcName);

                    int portraitHeight = portraitList[currentPortraitIndex].GetActualHeight();
                    var portraitRect = Game1.getSourceRectForStandardTileSheet(npc.Portrait, portraitNum);
                    int portraitCount = portraitHeight / portraitRect.Height * 2 - 1;
                    if (portraitNum >= portraitCount)
                    {
                        nextPortraitNum = 0;
                    }
                    currentPortrait = "$" + nextPortraitNum;
                    if (currentPortraitText.Count > currentPage)
                    {
                        currentPortraitText[currentPage] = currentPortrait;
                    }
                    else
                    {
                        currentPortraitText.Insert(currentPage, currentPortrait);
                    }
                }
            }

            else if (prevPortraitButton.containsPoint(x, y))
            {

                currentPortraitIndex--;
                if (currentPortraitIndex < 0)
                {
                    currentPortraitIndex = portraitList.Count - 1;
                }
                if (int.TryParse(currentPortrait.Split("$").Last(), out int portraitNum))
                {
                    int portraitHeight = portraitList[currentPortraitIndex].GetActualHeight();
                    var portraitRect = Game1.getSourceRectForStandardTileSheet(portraitList[currentPortraitIndex], portraitNum);
                    int portraitCount = portraitHeight / portraitRect.Height * 2 - 1;
                    if (portraitNum > portraitCount)
                    {
                        currentPortrait = "$" + 0;
                        if (currentPortraitText.Count > currentPage)
                        {
                            currentPortraitText[currentPage] = currentPortrait;
                        }
                        else
                        {
                            currentPortraitText.Insert(currentPage, currentPortrait);
                        }
                    }
                }
            }
            else if (nextPortraitButton.containsPoint(x, y))
            {

                currentPortraitIndex++;
                if (portraitList.Count <= currentPortraitIndex)
                {
                    currentPortraitIndex = 0;
                }
                if (int.TryParse(currentPortrait.Split("$").Last(), out int portraitNum))
                {
                    int portraitHeight = portraitList[currentPortraitIndex].GetActualHeight();
                    var portraitRect = Game1.getSourceRectForStandardTileSheet(portraitList[currentPortraitIndex], portraitNum);
                    int portraitCount = portraitHeight / portraitRect.Height * 2 - 1;
                    if (portraitNum > portraitCount)
                    {
                        currentPortrait = "$" + 0;
                        if (currentPortraitText.Count > currentPage)
                        {
                            currentPortraitText[currentPage] = currentPortrait;
                        }
                        else
                        {
                            currentPortraitText.Insert(currentPage, currentPortrait);
                        }
                    }
                }
            }

            else if (keyInputBoxBounds.Contains(x, y))
            {
                keyInputBox.SelectMe();
            }
            else if (gotoKeyButton.containsPoint(x, y))
            {
                var (origin, user) = DataManager.GetDialogues(npcName);

                isNewKey = !origin.ContainsKey(dialogueKey) && !user.ContainsKey(dialogueKey);
                dialogueKey = keyInputBox.Text.Trim();
                dialogue = user.ContainsKey(dialogueKey) ? new KeyValuePair<string, string>(dialogueKey, user[dialogueKey]) : origin.ContainsKey(dialogueKey) ? new KeyValuePair<string, string>(dialogueKey, origin[dialogueKey]) : new KeyValuePair<string, string>(dialogueKey, "");
                pages = dialogue.Value.Split("#$b#").ToList();
                pages = pages.SelectMany(page => page.Split("#$e#").Select((p, index) => index == page.Split("#$e#").Length - 1 ? p : p + "//end")).ToList();

                if (pages.Count == 0)
                    pages.Add("");
                currentPage = 0;
                currentText = pages[currentPage];
                currentPortraitText.Clear();
                currentPortraitText.Add("$0");
                ParsePortraitToken();
                inputBox.Text = dialogue.Value;
            }
            else if (backButtonRect.Contains(x, y))
            {
                exitThisMenu();
            }
            base.receiveLeftClick(x, y, playSound);
        }


        public override void receiveKeyPress(Keys key)
        {

            if (inputBox.Selected)
            {
                switch (key)
                {
                    case Keys.Right:
                        inputBox.RecieveSpecialInput(key);
                        break;
                    case Keys.Left:
                        inputBox.RecieveSpecialInput(key);
                        break;

                    case Keys.Up:
                        inputBox.RecieveSpecialInput(key);
                        break;
                    case Keys.Down:
                        inputBox.RecieveSpecialInput(key);
                        break;
                }
            }
            base.receiveKeyPress(key);
        }
        /// <summary>
        /// currentText 내의 토큰을 분석하여 currentPortrait 필드를 업데이트합니다.
        /// 토큰 예:
        ///   $0           → neutral (기본값, 또는 $neutral)
        ///   $1 또는 $h   → happy
        ///   $2 또는 $s   → sad
        ///   $3 또는 $u   → unique
        ///   $4 또는 $l   → love
        ///   $5 또는 $a   → angry
        ///   $<id>       → custom portrait
        /// </summary>
        private void ParsePortraitToken()
        {
            // 기본값 설정 (토큰이 없으면 중립)
            currentPortrait = "$0";
            if (string.IsNullOrEmpty(currentText))
                return;

            int tokenIndex = currentText.IndexOf('$');


            // 표준 토큰 처리
            if (currentText.Contains("$h") || currentText.Contains("$1"))
            {
                currentPortrait = "$1";
                currentText = currentText.Replace("$h", "").Replace("$1", "");
                if (currentPortraitText.Count > currentPage)
                {
                    currentPortraitText[currentPage] = currentPortrait;
                }
                else
                {
                    currentPortraitText.Insert(currentPage, currentPortrait);
                }
            }
            if (currentText.Contains("$s") || currentText.Contains("$2"))
            {
                currentPortrait = "$2";
                currentText = currentText.Replace("$s", "").Replace("$2", "");
                if (currentPortraitText.Count > currentPage)
                {
                    currentPortraitText[currentPage] = currentPortrait;
                }
                else
                {
                    currentPortraitText.Insert(currentPage, currentPortrait);
                }
            }
            if (currentText.Contains("$u") || currentText.Contains("$3"))
            {
                currentPortrait = "$3";
                currentText = currentText.Replace("$u", "").Replace("$3", "");
                if (currentPortraitText.Count > currentPage)
                {
                    currentPortraitText[currentPage] = currentPortrait;
                }
                else
                {
                    currentPortraitText.Insert(currentPage, currentPortrait);
                }
            }
            if (currentText.Contains("$l") || currentText.Contains("$4"))
            {
                currentPortrait = "$4";
                currentText = currentText.Replace("$l", "").Replace("$4", "");
                if (currentPortraitText.Count > currentPage)
                {
                    currentPortraitText[currentPage] = currentPortrait;
                }
                else
                {
                    currentPortraitText.Insert(currentPage, currentPortrait);
                }
            }
            if (currentText.Contains("$a") || currentText.Contains("$5"))
            {
                currentPortrait = "$5";
                currentText = currentText.Replace("$a", "").Replace("$5", "");
                if (currentPortraitText.Count > currentPage)
                {
                    currentPortraitText[currentPage] = currentPortrait;
                }
                else
                {
                    currentPortraitText.Insert(currentPage, currentPortrait);
                }
            }
            // $0 또는 명시적으로 $neutral인 경우
            if (currentText.Contains("$0") || currentText.Contains("$neutral"))
            {
                currentPortrait = "$0";
                currentText = currentText.Replace("$0", "").Replace("$neutral", "");
                if (currentPortraitText.Count > currentPage)
                {
                    currentPortraitText[currentPage] = currentPortrait;
                }
                else
                {
                    currentPortraitText.Insert(currentPage, currentPortrait);
                }
            }

            // 만약 위 토큰들이 없다면, '$' 다음에 숫자들이 연속되는 경우 처리합니다.
            int numDigits = 0;
            if (tokenIndex != -1)
                for (int i = tokenIndex + 1; i < currentText.Length && char.IsDigit(currentText[i]); i++)
                {
                    numDigits++;
                }
            if (numDigits > 0)
            {
                // 예: "$12" → currentPortrait = "$12"
                string numericToken = currentText.Substring(tokenIndex, numDigits + 1);
                currentPortrait = numericToken;
                currentText = currentText.Replace(numericToken, "");
                if (currentPortraitText.Count > currentPage)
                {
                    currentPortraitText[currentPage] = currentPortrait;
                }
                else
                {
                    currentPortraitText.Insert(currentPage, currentPortrait);
                }
            }

            pages[currentPage] = currentText;
        }

    }
}
