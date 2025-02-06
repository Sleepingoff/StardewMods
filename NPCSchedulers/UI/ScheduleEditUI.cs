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
        private ClickableTextureComponent saveButton;
        private ClickableTextureComponent cancelButton;

        private OptionsSlider locationSlider;
        private OptionsSlider directionSlider;
        private OptionsSlider actionSlider;
        private OptionsTextBox timeTextBox;
        private OptionsTextBox xTextBox;
        private OptionsTextBox yTextBox;
        private OptionsTextBox talkTextBox;
        private static List<string> locationOptions = new List<string>();
        private static List<string> directionOptions = new() { "^", ">", "V", "<" };
        private static Dictionary<string, List<string>> actionOptions = new Dictionary<string, List<string>>();
        public ScheduleEditUI(Vector2 position, string scheduleKey, ScheduleEntry entry, UIStateManager uiStateManager)
        {
            this.uiStateManager = uiStateManager;
            this.position = position;
            this.scheduleKey = scheduleKey;
            this.entry = entry;
            InitializeOptions();
            GenerateScheduleOptions(ModEntry.Instance.Helper.Translation);
        }


        public static void InitializeOptions()
        {
            // 🔥 모든 장소 불러오기
            locationOptions = Game1.locationData.Select(loc => loc.Key).ToList();
            locationOptions.Sort();
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
                    foreach (var entry in npc.Schedule.Values)
                    {
                        string action = entry.endOfRouteBehavior;

                        // 🔥 Null 또는 빈 문자열일 경우 "None"으로 처리
                        if (string.IsNullOrEmpty(action))
                        {
                            action = "None";
                        }

                        // 🔥 중복 방지 후 추가
                        if (!actionOptions[npc.Name].Contains(action))
                        {
                            actionOptions[npc.Name].Add(action);
                        }
                    }
                }
            }
        }
        private List<OptionsElement> GenerateScheduleOptions(ITranslationHelper i18n)
        {
            string currentNPC = uiStateManager.CurrentNPC.Name;

            if (entry == null) return new List<OptionsElement>();
            Rectangle editBox = new Rectangle((int)position.X, (int)position.Y, 400, 260);
            int offsetX = editBox.X + 10 + 200;
            int offsetY = editBox.Y + 10;

            timeTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.Time"), entry.Time.ToString());
            offsetY += 50;

            //locationOptions
            locationSlider = new OptionsSlider("", 0, offsetX, 0);
            locationSlider.bounds.Width = 400;
            locationSlider.value = (int)(locationOptions.IndexOf(entry.Location) / (float)locationOptions.Count * 99);
            offsetY += 50;
            xTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.XCoordinate"), entry.X.ToString());
            offsetY += 50;
            yTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.YCoordinate"), entry.Y.ToString());
            offsetY += 50;
            directionSlider = new OptionsSlider("", 0, offsetX, 0);
            directionSlider.value = (int)(entry.Direction / 4f * 99);
            offsetY += 50;

            actionSlider = new OptionsSlider("", 0, offsetX, 0);
            actionSlider.bounds.Width = 400;
            actionSlider.value = (int)(actionOptions[currentNPC].IndexOf(entry.Action) / (float)actionOptions[currentNPC].Count * 99);
            offsetY += 50;
            talkTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.Talk"), entry.Talk);
            offsetY += 50;
            // 🔹 저장 및 취소 버튼
            saveButton = new ClickableTextureComponent(new Rectangle((int)position.X + 120, offsetY, 32, 32),
                Game1.mouseCursors, new Rectangle(128, 256, 64, 64), 1f);
            cancelButton = new ClickableTextureComponent(new Rectangle((int)position.X, offsetY, 32, 32),
                Game1.mouseCursors, new Rectangle(192, 256, 64, 64), 1f);

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
            b.DrawString(Game1.smallFont, i18n.Get("ScheduleUI.Location"), new Vector2(offsetX, offsetY - 10), Color.Black);
            locationSlider.draw(b, 0, 0);
            locationSlider.bounds.Y = offsetY + 10;
            index = Math.Clamp((int)(locationSlider.value / 99f * locationOptions.Count), 0, locationOptions.Count - 1);
            b.DrawString(Game1.smallFont, locationOptions[index], new Vector2(offsetX, offsetY + 10), Color.Gray);
            offsetY += 50;

            // b.DrawString(Game1.smallFont, "X:", new Vector2(offsetX, offsetY - 15), Color.Black);
            xTextBox.bounds = new Rectangle(offsetX, offsetY, 400, xTextBox.bounds.Height);
            xTextBox.draw(b, offsetX, offsetY);
            offsetY += 50;

            // b.DrawString(Game1.smallFont, "Y:", new Vector2(offsetX, offsetY - 15), Color.Black);
            yTextBox.bounds = new Rectangle(offsetX, offsetY, 400, yTextBox.bounds.Height);
            yTextBox.draw(b, offsetX, offsetY);
            offsetY += 50;

            b.DrawString(Game1.smallFont, i18n.Get("ScheduleUI.Direction"), new Vector2(offsetX, offsetY - 10), Color.Black);
            directionSlider.draw(b, 0, 0);
            directionSlider.bounds.Y = offsetY + 10;
            index = Math.Clamp((int)Math.Round(directionSlider.value / 99f * directionOptions.Count), 0, directionOptions.Count - 1);
            b.DrawString(Game1.smallFont, directionOptions[index], new Vector2(offsetX, offsetY + 10), Color.Gray);
            offsetY += 50;

            b.DrawString(Game1.smallFont, i18n.Get("ScheduleUI.Action"), new Vector2(offsetX, offsetY - 10), Color.Black);
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
            saveButton.bounds = new Rectangle(editBox.X + editBox.Width - 80, offsetY, 64, 64);
            cancelButton.bounds = new Rectangle(editBox.X + editBox.Width - 140, offsetY, 64, 64);
            saveButton.draw(b);
            cancelButton.draw(b);

            return false;
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
            // 🔹 저장 버튼 클릭 → 변경사항 반영
            if (saveButton.containsPoint(x, y))
            {
                Console.WriteLine("button");
                ApplyChanges();
                uiStateManager.ToggleEditMode(null);
            }

            // 🔹 취소 버튼 클릭 → 편집 모드 종료
            if (cancelButton.containsPoint(x, y))
            {
                uiStateManager.ToggleEditMode(null);
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
            ScheduleEntry updatedEntry = new ScheduleEntry(scheduleKey, newTime, newLocation, newX, newY, directionIndex, newAction, newTalk);
            string key = scheduleKey.Split('/')[0];
            // 🔹 `UIStateManager`를 통해 스케줄 업데이트
            uiStateManager.SetScheduleDataByEntry(updatedEntry, key);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rectangle, int thickness, Color color)
        {
            Texture2D pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y + rectangle.Height - thickness, rectangle.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + rectangle.Width - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }
    }



}
