using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCSchedulers.DATA;
using NPCSchedulers.Store;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace NPCSchedulers.UI
{

    public class ScheduleEditUI : UIBase
    {
        private UIStateManager uiStateManager;
        public Vector2 position;
        private ScheduleEntry entry;
        private string scheduleKey;
        private ClickableComponent saveButton;
        private ClickableComponent cancelButton;
        private ClickableComponent nextButton;
        private ClickableComponent prevButton;
        private OptionsSlider locationSlider;
        private OptionsSlider directionSlider;
        private OptionsSlider actionSlider;
        private OptionsTextBox timeTextBox;
        private OptionsTextBox xTextBox;
        private OptionsTextBox yTextBox;
        private OptionsTextBox talkTextBox;

        private static Dictionary<char, List<string>> optionsByLetter = new Dictionary<char, List<string>>();
        private static List<char> availableLetters = new List<char>();
        private static char currentLetter = 'A';
        private static string selectedOption;
        private static List<string> locationOptions = new List<string>();
        private static List<string> directionOptions = new() { "^", ">", "V", "<" };
        private static Dictionary<string, List<string>> actionOptions = new Dictionary<string, List<string>>();
        public ScheduleEditUI(Vector2 position, string scheduleKey, ScheduleEntry entry, UIStateManager uiStateManager)
        {
            this.uiStateManager = uiStateManager;
            this.position = position;
            this.scheduleKey = scheduleKey;
            this.entry = entry;
            InitializeOptions(entry);
            GenerateScheduleOptions(ModEntry.Instance.Helper.Translation);
        }


        public static void InitializeOptions(ScheduleEntry entry)
        {
            optionsByLetter.Clear();
            // 🔥 모든 장소 불러오기
            locationOptions = Game1.locationData.Select(loc => loc.Key).ToList();
            locationOptions.Sort();

            foreach (var option in locationOptions)
            {
                char firstChar = char.ToUpper(option[0]);
                if (!char.IsLetter(firstChar)) firstChar = '_'; // 숫자 및 특수문자 그룹

                if (!optionsByLetter.ContainsKey(firstChar))
                    optionsByLetter[firstChar] = new List<string>();

                optionsByLetter[firstChar].Add(option);
            }

            availableLetters = optionsByLetter.Keys.OrderBy(c => c).ToList();
            char currentChar = char.ToUpper(entry.Location[0]);
            currentLetter = currentChar;

            directionOptions = new() { "back", "right", "front", "left" };
            // 🔥 모든 NPC별 액션 불러오기
            actionOptions.Clear();

            foreach (var npc in Utility.getAllCharacters()) // 모든 NPC 가져오기
            {
                if (!actionOptions.ContainsKey(npc.Name))
                {
                    actionOptions[npc.Name] = new List<string>();
                }

                // 🔥 기본값 "None" 추가 (항상 리스트가 최소 1개 이상의 요소를 가지도록)
                if (actionOptions[npc.Name].Count == 0)
                {
                    actionOptions[npc.Name].Add("None");
                }

                if (npc.Schedule != null)
                {
                    foreach (var value in npc.Schedule.Values)
                    {
                        string action = value.endOfRouteBehavior;

                        // 🔥 중복 방지 후 추가
                        if (!actionOptions[npc.Name].Contains(action ?? "None"))
                        {
                            actionOptions[npc.Name].Add(action);
                        }
                    }
                }
            }
        }
        private void ChangeLetter(int direction)
        {
            int currentIndex = availableLetters.IndexOf(currentLetter);
            int newIndex = (currentIndex + direction) % availableLetters.Count;

            if (newIndex < 0)
            {
                newIndex = availableLetters.Count - 1;
            }

            if (newIndex >= 0 && newIndex < availableLetters.Count)
            {
                currentLetter = availableLetters[newIndex];
                locationSlider.value = 0; // 그룹 변경 시 슬라이더 초기화
            }
        }
        private void UpdateIndexFromSlider()
        {
            if (optionsByLetter.ContainsKey(currentLetter))
            {
                var currentOptions = optionsByLetter[currentLetter];

                int selectedIndex = (int)((locationSlider.value / 99.0) * (currentOptions.Count - 1));
                selectedIndex = Math.Clamp(selectedIndex, 0, currentOptions.Count - 1);

                selectedOption = currentOptions[selectedIndex];
            }
        }
        private List<OptionsElement> GenerateScheduleOptions(ITranslationHelper i18n)
        {
            string currentNPC = uiStateManager.CurrentNPC.Name;

            if (entry == null) return new List<OptionsElement>();
            Rectangle editBox = new Rectangle((int)position.X, (int)position.Y, 400, 260);
            int offsetX = editBox.X + 10 + 200;
            int offsetY = editBox.Y + 10;

            timeTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.Time").Default("Time"), entry.Time.ToString() ?? "");
            offsetY += 50;

            //locationOptions
            locationSlider = new OptionsSlider("", 0, offsetX, 0);
            locationSlider.bounds.Width = 250;
            locationSlider.value = Math.Clamp((int)(optionsByLetter[currentLetter].IndexOf(entry.Location) / (float)optionsByLetter[currentLetter].Count * 99), 0, optionsByLetter[currentLetter].Count - 1);

            nextButton = new ClickableComponent(new Rectangle(offsetX + 250 + 100, offsetY, 16, 32), ">");
            prevButton = new ClickableComponent(new Rectangle(offsetX + 250, offsetY, 16, 32), "<");

            offsetY += 50;
            xTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.XCoordinate").Default("X"), entry.X.ToString() ?? "");
            offsetY += 50;
            yTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.YCoordinate").Default("Y"), entry.Y.ToString() ?? "");
            offsetY += 50;
            directionSlider = new OptionsSlider("", 0, offsetX, 0);
            directionSlider.value = (int)(entry.Direction / 4f * 99);
            offsetY += 50;

            actionSlider = new OptionsSlider("", 0, offsetX, 0);
            actionSlider.bounds.Width = 400;
            //v0.0.3 + fix: actionOptions[currentNPC] 범위 초과 오류 수정
            actionSlider.value = Math.Clamp((int)(actionOptions[currentNPC].IndexOf(entry.Action) / (float)actionOptions[currentNPC].Count * 99), 0, actionOptions[currentNPC].Count - 1);
            offsetY += 50;
            talkTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.Talk").Default("Talk"), entry.Talk ?? "");
            offsetY += 50;
            // 🔹 저장 및 취소 버튼
            // 🔹 저장 및 취소 버튼 (텍스트 버튼)
            cancelButton = new ClickableComponent(new Rectangle((int)position.X + 100, offsetY - 250, 200, 64), i18n.Get("button.cancel").Default("Cancel"));
            saveButton = new ClickableComponent(new Rectangle((int)position.X + 200, offsetY - 250, 200, 64), i18n.Get("button.save").Default("Save"));

            return new List<OptionsElement> {
                timeTextBox,
                locationSlider,
                xTextBox,
                yTextBox,
                directionSlider,
                actionSlider,
               talkTextBox
            };
        }
        public override bool Draw(SpriteBatch b)
        {
            var i18n = ModEntry.Instance.Helper.Translation;
            UpdateIndexFromSlider();
            // 🔹 배경 박스
            Rectangle editBox = new Rectangle((int)position.X, (int)position.Y, 400, 260);
            int offsetX = editBox.X + 10;
            int offsetY = editBox.Y + 10;
            int index = 0;
            // 🔹 입력 필드 직접 배치 (위치 계산 반영)
            // b.DrawString(Game1.smallFont, "Time:", new Vector2(offsetX, offsetY - 15), Color.Black);
            timeTextBox.bounds = new Rectangle(offsetX, offsetY, 400, timeTextBox.bounds.Height);
            timeTextBox.draw(b, offsetX, offsetY);
            offsetY += 50;
            // 🔹 기존 `foreach`에서 하던 위치 계산을 그대로 적용
            b.DrawString(Game1.smallFont, i18n.Get("ScheduleUI.Location").Default("Location"), new Vector2(offsetX, offsetY - 10), Color.Black);
            locationSlider.draw(b, 0, 0);
            locationSlider.bounds.Y = offsetY + 10;
            nextButton.bounds.Y = offsetY + 10;
            nextButton.bounds.X = offsetX + 200 + 250 + 100;
            prevButton.bounds.Y = offsetY + 10;
            prevButton.bounds.X = offsetX + 200 + 250 + 50;

            b.DrawString(Game1.smallFont, currentLetter.ToString(), new Vector2(offsetX + 200 + 250 + 75, offsetY + 10), Color.Gray);
            b.DrawString(Game1.smallFont, "<", new Vector2(prevButton.bounds.X, offsetY + 10), Color.Black);
            b.DrawString(Game1.smallFont, ">", new Vector2(nextButton.bounds.X, offsetY + 10), Color.Black);
            index = Math.Clamp(((int)(locationSlider.value / 99f * (optionsByLetter[currentLetter].Count))), 0, optionsByLetter[currentLetter].Count - 1);
            b.DrawString(Game1.smallFont, optionsByLetter[currentLetter][index], new Vector2(offsetX, offsetY + 10), Color.Gray);
            offsetY += 50;

            // b.DrawString(Game1.smallFont, "X:", new Vector2(offsetX, offsetY - 15), Color.Black);
            xTextBox.bounds = new Rectangle(offsetX, offsetY, 400, xTextBox.bounds.Height);
            xTextBox.draw(b, offsetX, offsetY);
            offsetY += 50;

            // b.DrawString(Game1.smallFont, "Y:", new Vector2(offsetX, offsetY - 15), Color.Black);
            yTextBox.bounds = new Rectangle(offsetX, offsetY, 400, yTextBox.bounds.Height);
            yTextBox.draw(b, offsetX, offsetY);
            offsetY += 50;

            b.DrawString(Game1.smallFont, i18n.Get("ScheduleUI.Direction").Default("Direction"), new Vector2(offsetX, offsetY - 10), Color.Black);
            directionSlider.draw(b, 0, 0);
            directionSlider.bounds.Y = offsetY + 10;
            index = Math.Clamp((int)Math.Round(directionSlider.value / 99f * directionOptions.Count), 0, directionOptions.Count - 1);
            b.DrawString(Game1.smallFont, directionOptions[index], new Vector2(offsetX, offsetY + 10), Color.Gray);
            offsetY += 50;

            b.DrawString(Game1.smallFont, i18n.Get("ScheduleUI.Action").Default("Action"), new Vector2(offsetX, offsetY - 10), Color.Black);
            actionSlider.draw(b, 0, 0);
            actionSlider.bounds.Y = offsetY + 10;
            string npcName = uiStateManager.CurrentNPC.Name;
            int value = actionSlider.value;
            index = Math.Clamp((int)(value / 99f * actionOptions[npcName].Count), 0, actionOptions[npcName].Count - 1);
            b.DrawString(Game1.smallFont, actionOptions[npcName][index], new Vector2(offsetX, offsetY + 10), Color.Gray);
            offsetY += 50;


            // b.DrawString(Game1.smallFont, "Talk:", new Vector2(offsetX, offsetY - 15), Color.Black);
            talkTextBox.bounds = new Rectangle(offsetX, offsetY, 400, talkTextBox.bounds.Height);
            talkTextBox.textBox.Width = 400;
            talkTextBox.draw(b, offsetX, offsetY);
            offsetY += 50;
            // 🔹 저장 및 취소 버튼 유지
            saveButton.bounds = new Rectangle(editBox.Center.X + 100, offsetY + 50, 200, 64);
            cancelButton.bounds = new Rectangle(editBox.Center.X - 100, offsetY + 50, 200, 64);
            // 배경 색상 설정 (버튼 느낌 강조)
            Color buttonColor = Color.Gray;
            Color textColor = Color.White;
            // 🔹 저장 버튼 렌더링
            b.Draw(Game1.menuTexture, saveButton.bounds, new Rectangle(0, 256, 64, 64), buttonColor);
            Utility.drawTextWithShadow(b, saveButton.name, Game1.smallFont, new Vector2(saveButton.bounds.X + 50, saveButton.bounds.Y + 8), textColor);

            // 🔹 취소 버튼 렌더링
            b.Draw(Game1.menuTexture, cancelButton.bounds, new Rectangle(0, 256, 64, 64), buttonColor);
            Utility.drawTextWithShadow(b, cancelButton.name, Game1.smallFont, new Vector2(cancelButton.bounds.X + 30, cancelButton.bounds.Y + 8), textColor);




            return false;
        }
        public static void DrawTooltip(SpriteBatch b, ScheduleEditUI instance)
        {
            var i18n = ModEntry.Instance.Helper.Translation;
            SchedulePage.DrawTooltip(b, i18n.Get("tooltip.Time").Default("Enter time (format: HHMM, only number)"), instance.timeTextBox.bounds);
            SchedulePage.DrawTooltip(b, i18n.Get("tooltip.Location").Default("Select a location by moving the slider."), instance.locationSlider.bounds);
            SchedulePage.DrawTooltip(b, i18n.Get("tooltip.Coordinate").Default("Enter X and Y coordinates for precise positioning. (format: only number)"), instance.xTextBox.bounds);
            SchedulePage.DrawTooltip(b, i18n.Get("tooltip.Coordinate").Default("Enter X and Y coordinates for precise positioning. (format: only number)"), instance.yTextBox.bounds);
            SchedulePage.DrawTooltip(b, i18n.Get("tooltip.Direction").Default("Choose the NPC's facing direction using the slider."), instance.directionSlider.bounds);
            SchedulePage.DrawTooltip(b, i18n.Get("tooltip.Action").Default("Select an action for the NPC using the slider."), instance.actionSlider.bounds);
            SchedulePage.DrawTooltip(b, i18n.Get("tooltip.Talk").Default("Enter the dialogue NPC will say upon arrival."), instance.talkTextBox.bounds);
        }

        public override void LeftHeld(int x, int y)
        {

            if (locationSlider.bounds.Contains(x, y)) locationSlider.leftClickHeld(x, y);
            if (actionSlider.bounds.Contains(x, y)) actionSlider.leftClickHeld(x, y);
            if (directionSlider.bounds.Contains(x, y)) directionSlider.leftClickHeld(x, y);
        }
        public override void LeftClick(int x, int y)
        {
            if (timeTextBox.ContainsPoint(x, y)) timeTextBox.textBox.SelectMe();
            if (xTextBox.ContainsPoint(x, y)) xTextBox.textBox.SelectMe();
            if (yTextBox.ContainsPoint(x, y)) yTextBox.textBox.SelectMe();
            if (talkTextBox.ContainsPoint(x, y)) talkTextBox.textBox.SelectMe();
            if (locationSlider.bounds.Contains(x, y)) locationSlider.receiveLeftClick(x, y);
            if (actionSlider.bounds.Contains(x, y)) actionSlider.receiveLeftClick(x, y);
            if (directionSlider.bounds.Contains(x, y)) directionSlider.receiveLeftClick(x, y);

            if (nextButton.bounds.Contains(x, y)) { ChangeLetter(1); }
            if (prevButton.bounds.Contains(x, y)) { ChangeLetter(-1); }
            // 🔹 저장 버튼 클릭 → 변경사항 반영
            if (saveButton.containsPoint(x, y))
            {
                ApplyChanges();
                uiStateManager.ToggleEditMode(null);
                Game1.playSound("smallSelect");
            }

            // 🔹 취소 버튼 클릭 → 편집 모드 종료
            if (cancelButton.containsPoint(x, y))
            {
                uiStateManager.ToggleEditMode(null);
                Game1.playSound("smallSelect");
            }
        }

        private void ApplyChanges()
        {
            string currentNPC = uiStateManager.CurrentNPC.Name;
            // 🔹 입력된 값 가져오기
            int newTime = int.TryParse(timeTextBox.textBox.Text, out int t) ? t : entry.Time;
            int newX = int.TryParse(xTextBox.textBox.Text, out int x) ? x : entry.X;
            int newY = int.TryParse(yTextBox.textBox.Text, out int y) ? y : entry.Y;

            int actionIndex = Math.Clamp((int)(actionSlider.value / 99f * actionOptions[currentNPC].Count), 0, actionOptions[currentNPC].Count - 1);
            string newAction = actionOptions[currentNPC][actionIndex];

            int locationIndex = Math.Clamp((int)(locationSlider.value / 99f * locationOptions.Count), 0, locationOptions.Count - 1);
            string newLocation = locationOptions[locationIndex];

            int directionIndex = Math.Clamp((int)(directionSlider.value / 99f * 4), 0, 3);

            string newTalk = talkTextBox.textBox.Text;
            // 🔹 새 스케줄 엔트리 생성
            ScheduleEntry updatedEntry = new ScheduleEntry(scheduleKey, newTime, newLocation, newX, newY, directionIndex, newAction ?? "", newTalk ?? "");
            string key = scheduleKey.Split('/')[0];
            // 🔹 `UIStateManager`를 통해 스케줄 업데이트
            uiStateManager.SetScheduleDataByEntry(updatedEntry, key);
        }

    }



}
