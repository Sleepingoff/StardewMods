using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCSchedulers.Store;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NPCSchedulers.UI
{

    public class ScheduleEditUI : UIBase
    {
        private Vector2 position;
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
        private static Dictionary<string, List<string>> actionOptions = new Dictionary<string, List<string>>();
        public ScheduleEditUI(Vector2 position, string scheduleKey, ScheduleEntry entry)
        {
            this.position = position;
            this.scheduleKey = scheduleKey;
            this.entry = entry;
            GenerateScheduleOptions(ModEntry.Instance.Helper.Translation);

        }


        public static void InitializeOptions()
        {
            // 🔥 모든 장소 불러오기
            locationOptions = Game1.locationData.Select(loc => loc.Key).ToList();

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
            string currentNPC = UIStateManager.Instance.CurrentNPC.Name;

            if (entry == null) return new List<OptionsElement>();
            //locationOptions
            locationSlider = new OptionsSlider(i18n.Get("ScheduleUI.Location"), 0);
            locationSlider.bounds.Width = 400;
            locationSlider.value = (int)(locationOptions.IndexOf(entry.Location) / (float)locationOptions.Count * 99);

            directionSlider = new OptionsSlider(i18n.Get("ScheduleUI.Direction"), 0);
            directionSlider.value = (int)(entry.Direction / 4f * 99);


            actionSlider = new OptionsSlider(i18n.Get("ScheduleUI.Action"), 0);
            actionSlider.bounds.Width = 400;

            actionSlider.value = (int)(actionOptions[currentNPC].IndexOf(entry.Action) / (float)actionOptions[currentNPC].Count * 99);

            timeTextBox = new OptionsTextBox("Time", entry.Time.ToString());
            xTextBox = new OptionsTextBox("X", entry.X.ToString());
            yTextBox = new OptionsTextBox("Y", entry.Y.ToString());
            talkTextBox = new OptionsTextBox("Action", entry.Action);

            // 🔹 저장 및 취소 버튼
            saveButton = new ClickableTextureComponent(new Rectangle((int)position.X + 120, (int)position.Y + 100, 32, 32),
                Game1.mouseCursors, new Rectangle(128, 256, 64, 64), 1f);
            cancelButton = new ClickableTextureComponent(new Rectangle((int)position.X, (int)position.Y + 100, 32, 32),
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
            if (!IsVisible) return true;
            string currentNPC = UIStateManager.Instance.CurrentNPC.Name;

            // 🔹 배경 박스
            Rectangle editBox = new Rectangle((int)position.X, (int)position.Y, 400, 260);
            b.Draw(Game1.staminaRect, editBox, new Rectangle(0, 0, 1, 1), Color.DarkSlateGray * 0.8f, 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
            DrawBorder(b, editBox, 2, Color.White);

            int offsetX = editBox.X + 10;
            int offsetY = editBox.Y + 10;

            // 🔹 입력 필드 직접 배치 (위치 계산 반영)
            b.DrawString(Game1.smallFont, "Time:", new Vector2(offsetX, offsetY - 15), Color.Black);
            timeTextBox.draw(b, offsetX, offsetY);
            timeTextBox.bounds = new Rectangle(offsetX + 200, offsetY, 400, timeTextBox.bounds.Height);
            offsetY += 50;

            // 🔹 기존 `foreach`에서 하던 위치 계산을 그대로 적용
            b.DrawString(Game1.smallFont, "Location:", new Vector2(offsetX, offsetY - 15), Color.Black);
            locationSlider.bounds = new Rectangle(offsetX + 200, offsetY, locationSlider.bounds.Width, locationSlider.bounds.Height);
            locationSlider.draw(b, offsetX + 200, offsetY);
            offsetY += 50;

            b.DrawString(Game1.smallFont, "X:", new Vector2(offsetX, offsetY - 15), Color.Black);
            xTextBox.draw(b, offsetX, offsetY);
            xTextBox.bounds = new Rectangle(offsetX + 200, offsetY, 400, xTextBox.bounds.Height);
            offsetY += 50;

            b.DrawString(Game1.smallFont, "Y:", new Vector2(offsetX, offsetY - 15), Color.Black);
            yTextBox.draw(b, offsetX, offsetY);
            yTextBox.bounds = new Rectangle(offsetX + 200, offsetY, 400, yTextBox.bounds.Height);
            offsetY += 50;

            b.DrawString(Game1.smallFont, "Direction:", new Vector2(offsetX, offsetY - 15), Color.Black);
            directionSlider.bounds = new Rectangle(offsetX + 200, offsetY, directionSlider.bounds.Width, directionSlider.bounds.Height);
            directionSlider.draw(b, offsetX + 200, offsetY);
            offsetY += 50;

            b.DrawString(Game1.smallFont, "Action:", new Vector2(offsetX, offsetY - 15), Color.Black);
            actionSlider.bounds = new Rectangle(offsetX + 200, offsetY, actionSlider.bounds.Width, actionSlider.bounds.Height);
            actionSlider.draw(b, offsetX + 200, offsetY);
            offsetY += 50;

            b.DrawString(Game1.smallFont, "Talk:", new Vector2(offsetX, offsetY - 15), Color.Black);
            talkTextBox.bounds = new Rectangle(offsetX + 200, offsetY, 400, talkTextBox.bounds.Height);
            talkTextBox.draw(b, offsetX + 200, offsetY);

            // 🔹 저장 및 취소 버튼 유지
            saveButton.bounds = new Rectangle(editBox.X + editBox.Width - 80, editBox.Y + editBox.Height - 40, saveButton.bounds.Width, saveButton.bounds.Height);
            cancelButton.bounds = new Rectangle(editBox.X + editBox.Width - 140, editBox.Y + editBox.Height - 40, cancelButton.bounds.Width, cancelButton.bounds.Height);
            saveButton.draw(b);
            cancelButton.draw(b);

            return false;
        }


        public override void LeftClick(int x, int y)
        {
            if (!IsVisible) return;

            // 🔹 저장 버튼 클릭 → 변경사항 반영
            if (saveButton.containsPoint(x, y))
            {
                ApplyChanges();
                UIStateManager.Instance.ToggleEditMode(null);
            }

            // 🔹 취소 버튼 클릭 → 편집 모드 종료
            if (cancelButton.containsPoint(x, y))
            {
                UIStateManager.Instance.ToggleEditMode(null);
            }
        }

        private void ApplyChanges()
        {
            string currentNPC = UIStateManager.Instance.CurrentNPC.Name;
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

            // 🔹 `UIStateManager`를 통해 스케줄 업데이트
            UIStateManager.Instance.UpdateScheduleEntry(scheduleKey, updatedEntry);
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
