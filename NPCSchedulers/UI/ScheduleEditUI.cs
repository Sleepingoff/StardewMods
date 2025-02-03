using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCSchedulers.DATA;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System.Collections.Generic;

namespace NPCSchedulers.UI
{
    public class ScheduleUI
    {
        private NPC currentNPC;
        private List<OptionsElement> entryBounds;
        // 🔹 입력 필드 및 드롭다운 추가
        private static string timeValue = ""; // 입력값 저장
        private static string xValue = "";
        private static string yValue = "";
        private static string talkValue = "";

        private static List<string> locationOptions = new List<string>();
        private static Dictionary<string, List<string>> actionOptions = new Dictionary<string, List<string>>();

        private static ScheduleEntry currentScheduleEntry; // scheduleEntry를 private 필드로 유지

        private ClickableTextureComponent saveButton;
        private ClickableTextureComponent cancelButton;

        private string targetSeason = null;
        private int targetDate = 1;

        private List<ScheduleEntry> scheduleEntries;
        private FriendshipConditionEntry friendshipConditionEntry;

        public ScheduleUI(NPC npc, Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> targetData, ScheduleEntry entry, string targetSeason, int targetDate)
        {
            this.targetSeason = targetSeason;
            this.targetDate = targetDate;
            currentNPC = npc;
            currentScheduleEntry = entry;
            timeValue = entry.Time.ToString();
            xValue = entry.X.ToString();
            yValue = entry.Y.ToString();
            talkValue = entry.Talk;
            friendshipConditionEntry = targetData[entry.Key.Split('/')[0]].Item1;
            scheduleEntries = targetData[entry.Key.Split('/')[0]].Item2;
            entryBounds = GenerateScheduleOptions(ModEntry.Instance.Helper.Translation);

            saveButton = new ClickableTextureComponent(
              new Rectangle(0, 0, 80, 40),
              Game1.mouseCursors, new Rectangle(128, 256, 64, 64), 1f
            );

            cancelButton = new ClickableTextureComponent(
                new Rectangle(0, 0, 80, 40),
                Game1.mouseCursors, new Rectangle(128 + 64, 256, 64, 64), 1f
            );
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
        public static string GetDayOfWeek(int day)
        {
            if (day < 1 || day > 28)
                return "Invalid day"; // 1~28 범위를 벗어난 경우

            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            return days[(day - 1) % 7]; // 1일부터 시작하므로 (day - 1)
        }

        public static Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> LoadNPCSchedules(NPC npc, string season = "Spring", int date = -1)
        {

            if (date == -1)
            {
                date = Game1.dayOfMonth;
            }

            string dayOfWeek = GetDayOfWeek(date);
            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> scheduleEntries = ScheduleManager.GetNPCSchedule(npc, season, date, dayOfWeek);

            return scheduleEntries;
        }
        private OptionsSlider locationSlider;
        private OptionsSlider directionSlider;
        private OptionsSlider actionSlider;
        private OptionsTextBox timeTextBox;
        private OptionsTextBox xTextBox;
        private OptionsTextBox yTextBox;
        private OptionsTextBox talkTextBox;
        private List<OptionsElement> GenerateScheduleOptions(ITranslationHelper i18n)
        {

            if (currentScheduleEntry == null) return new List<OptionsElement>();
            //locationOptions
            locationSlider = new OptionsSlider(i18n.Get("ScheduleUI.Location"), 0);
            locationSlider.bounds.Width = 400;
            locationSlider.value = (int)(locationOptions.IndexOf(currentScheduleEntry.Location) / (float)locationOptions.Count * 99);

            directionSlider = new OptionsSlider(i18n.Get("ScheduleUI.Direction"), 0);
            directionSlider.value = (int)(currentScheduleEntry.Direction / 4f * 99);


            actionSlider = new OptionsSlider(i18n.Get("ScheduleUI.Action"), 0);
            actionSlider.bounds.Width = 400;
            //actionOptions[currentNPC.Name]
            actionSlider.value = (int)(actionOptions[currentNPC.Name].IndexOf(currentScheduleEntry.Action) / (float)actionOptions[currentNPC.Name].Count * 99);

            timeTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.Time"), ref timeValue);
            xTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.XCoordinate"), ref xValue);
            yTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.YCoordinate"), ref yValue);
            talkTextBox = new OptionsTextBox(i18n.Get("ScheduleUI.Talk"), ref talkValue, 400);

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
        public void Draw(SpriteBatch b, Vector2 position)
        {
            int offsetX = (int)position.X;
            int offsetY = (int)position.Y;
            foreach (var element in entryBounds)
            {
                if (element is OptionsTextBox optionsTextBox)
                {
                    b.DrawString(Game1.smallFont, optionsTextBox.label, new Vector2(offsetX, offsetY - 15), Color.Black);
                    element.draw(b, offsetX, offsetY);
                    element.bounds.X = offsetX + 200;
                    element.bounds.Y = offsetY;
                    element.bounds.Width = 400;
                }
                else if (element is OptionsSlider slider)
                {
                    slider.bounds.Y = offsetY;
                    slider.bounds.X = offsetX + 200;
                }
                offsetY += 60;
            }


            Vector2 labelOffset = new Vector2(actionSlider.bounds.X, actionSlider.bounds.Bottom);
            actionSlider.labelOffset = new Vector2(-actionSlider.bounds.Width - 200, 0);
            int index = (int)Math.Round((actionSlider.value / 99.0f) * actionOptions[currentNPC.Name].Count);
            index = Math.Clamp(index, 0, actionOptions[currentNPC.Name].Count - 1); // 🔥 범위 제한
            b.DrawString(Game1.smallFont, actionOptions[currentNPC.Name][index], labelOffset, Color.Black);
            actionSlider.draw(b, 0, 0);

            labelOffset = new Vector2(directionSlider.bounds.X, directionSlider.bounds.Bottom);
            directionSlider.labelOffset = new Vector2(-directionSlider.bounds.Width - 200, 0);
            index = (int)Math.Round((directionSlider.value / 99.0f) * 4);
            index = Math.Clamp(index, 0, 3); // 🔥 범위 제한
            b.DrawString(Game1.smallFont, index.ToString(), labelOffset, Color.Black);
            directionSlider.draw(b, 0, 0);

            labelOffset = new Vector2(locationSlider.bounds.X, locationSlider.bounds.Bottom);
            locationSlider.labelOffset = new Vector2(-locationSlider.bounds.Width - 200, 0);
            index = (int)Math.Round((locationSlider.value / 99.0f) * locationOptions.Count);
            index = Math.Clamp(index, 0, locationOptions.Count - 1); // 🔥 범위 제한
            b.DrawString(Game1.smallFont, locationOptions[index], labelOffset, Color.Black);
            locationSlider.draw(b, 0, 0);

            Vector2 buttonPosition = new Vector2(position.X, position.Y + 350);
            DrawEditUI(b, buttonPosition);

        }

        private void DrawEditUI(SpriteBatch b, Vector2 position)
        {
            int editX = (int)position.X + 500;
            int editY = (int)position.Y + 100;

            // 🔹 UI 요소 배치
            saveButton.bounds.X = editX + 64 + 10;
            saveButton.bounds.Y = editY;
            cancelButton.bounds.X = editX;
            cancelButton.bounds.Y = editY;
            saveButton.draw(b);
            cancelButton.draw(b);
        }
        public ScheduleEntry GetScheduleEntry()
        {
            // 🔥 기존 시간 값 저장
            int oldTime = currentScheduleEntry.Time;

            // 🔥 새로운 시간 입력값 가져오기
            int.TryParse(timeTextBox.textBox.Text, out int newTime);

            // 🔥 기존 시간과 입력된 시간이 다르면 새로운 스케줄 추가
            bool isTimeChanged = oldTime != newTime;

            int index = (int)Math.Round((locationSlider.value / 99.0f) * locationOptions.Count);
            string location = locationOptions[index];
            int.TryParse(xTextBox.textBox.Text, out int x);
            int.TryParse(yTextBox.textBox.Text, out int y);
            int direction = directionSlider.value;
            index = (int)Math.Round((actionSlider.value / 99.0f) * actionOptions[currentNPC.Name].Count);
            string action = actionOptions[currentNPC.Name][index];
            string talk = talkTextBox.textBox.Text;

            if (isTimeChanged)
            {
                // 🔥 시간이 변경되었다면 새로운 스케줄 생성
                string newKey = $"{targetSeason.ToLower()}_{targetDate}/{newTime * 999}";

                return new ScheduleEntry(newKey, newTime, location, x, y, direction, action, talk);
            }
            else
            {
                // 🔥 시간이 변경되지 않았다면 기존 스케줄 업데이트
                currentScheduleEntry.SetTime(newTime);
                currentScheduleEntry.SetLocation(location);
                currentScheduleEntry.SetCoordinates(x, y);
                currentScheduleEntry.SetDirection(direction);
                currentScheduleEntry.SetAction(action);
                currentScheduleEntry.SetTalk(talk);

                return currentScheduleEntry;
            }
        }

        public void UpdateFriendshipCondition(string selectedNPC, int newHeartLevel)
        {
            if (friendshipConditionEntry != null)
            {

                friendshipConditionEntry.SetCondition(selectedNPC, newHeartLevel);

            }
        }

        public bool HandleClick(int x, int y)
        {
            foreach (var element in entryBounds)
            {
                if (element.bounds.Contains(x, y))
                {
                    element.receiveLeftClick(x, y);
                }

            }

            if (saveButton.containsPoint(x, y))
            {
                this.SaveSchedule();
                currentScheduleEntry = null;
                SchedulePage.isOpenScheduleEditUI = null; // 수정 UI 닫기
                return true;
            }
            else if (cancelButton.containsPoint(x, y))
            {
                currentScheduleEntry = null;
                SchedulePage.isOpenScheduleEditUI = null; // 수정 UI 닫기
                return true;
            }
            return false;

        }
        private void SaveSchedule()
        {
            if (currentScheduleEntry == null) return;

            ScheduleEntry scheduleEntry = GetScheduleEntry();

            List<ScheduleEntry> updatedScheduleList = new List<ScheduleEntry>(scheduleEntries);
            // 🔥 기존 키가 있으면 삭제 후 추가
            updatedScheduleList.RemoveAll(e => e.Key == scheduleEntry.Key);
            updatedScheduleList.Add(scheduleEntry);

            Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> newScheduleData = new Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)>();

            foreach (var entry in updatedScheduleList)
            {
                string scheduleKey = entry.Key;

                if (!newScheduleData.ContainsKey(scheduleKey))
                {
                    newScheduleData[scheduleKey] = (friendshipConditionEntry, new List<ScheduleEntry>());
                }

                newScheduleData[scheduleKey].Item2.Add(entry);
            }

            ScheduleManager.SaveSchedule(currentNPC.Name, targetSeason, targetDate, newScheduleData);
            ScheduleManager.ApplyScheduleToNPC(currentNPC.Name);
            // ✅ 저장 후 UI 업데이트
            SchedulePage.UpdateSchedule();

            // ✅ 수정 UI 닫기
            SchedulePage.isOpenScheduleEditUI = null;
        }


    }
}
