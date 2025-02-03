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
    public class SchedulePage
    {
        private static string[] seasons = { "Spring", "Summer", "Fall", "Winter", "Rain", "Festival", "Marriage" };
        private static List<string> villagers = new List<string>();
        private static int currentSeasonIndex = 0;
        private static int selectedDate = 1;
        private static OptionsSlider heartBox;
        private IconSlider heartTarget;
        private static int clickedHeart = 0;
        private static Vector2 heartDisplayPosition = new Vector2();
        private ClickableTextureComponent leftSeasonButton;
        private ClickableTextureComponent rightSeasonButton;
        private ClickableTextureComponent addScheduleButton;

        private ClickableTextureComponent resetButton;
        private OptionsSlider dateSlider;

        public static Dictionary<string, (FriendshipConditionEntry, List<ScheduleEntry>)> scheduleEntries;
        public static NPC currentNPC;
        private static ScheduleUI scheduleUI;
        public static string isOpenScheduleEditUI = null;
        public SchedulePage(NPC npc, IClickableMenu clickableMenu)
        {
            currentNPC = npc;
            scheduleEntries = ScheduleUI.LoadNPCSchedules(npc);
            // 🔹 계절 선택 버튼 생성
            leftSeasonButton = new ClickableTextureComponent(
                new Rectangle(clickableMenu.xPositionOnScreen + 550, clickableMenu.yPositionOnScreen + 100, 32, 32), Game1.mouseCursors,
                new Rectangle(352, 495, 12, 11), 4f);

            rightSeasonButton = new ClickableTextureComponent(
                new Rectangle(clickableMenu.xPositionOnScreen + 1100, clickableMenu.yPositionOnScreen + 100, 32, 32), Game1.mouseCursors,
                new Rectangle(365, 495, 12, 11), 4f);

            addScheduleButton = new ClickableTextureComponent(
                  new Rectangle(clickableMenu.xPositionOnScreen + 1100, clickableMenu.yPositionOnScreen + 200, 32, 32),
                  Game1.mouseCursors, new Rectangle(267, 257, 16, 16), 4f);

            resetButton = new ClickableTextureComponent(
                   new Rectangle(clickableMenu.xPositionOnScreen + 1164, clickableMenu.yPositionOnScreen + 200, 32, 32),  // 위치와 크기 (적절히 조정)
                   Game1.mouseCursors,
                   new Rectangle(128 + 128, 256, 16, 16),  // 🔥 새로운 버튼 이미지 (적절한 위치 설정)
                   1f
               );

            // 🔹 날짜 증가/감소 버튼 추가
            // 🔹 슬라이더 바 & 핸들 추가
            dateSlider = new OptionsSlider("Date", 0, clickableMenu.xPositionOnScreen + 850, clickableMenu.yPositionOnScreen + 150);
            dateSlider.value = (selectedDate + 1) * 99 / 27;
            //하트 위에 그릴 클릭 박스
            heartDisplayPosition = new Vector2(clickableMenu.xPositionOnScreen + 100, clickableMenu.yPositionOnScreen + 450);


            heartTarget = new IconSlider(villagers);

            // 🔥 현재 NPC의 인덱스 찾기
            int index = villagers.IndexOf(npc.Name);
            heartTarget.selectedIndex = index;

            heartBox = new OptionsSlider("", 0, (int)heartDisplayPosition.X + 100, (int)heartDisplayPosition.Y - 25);
            heartBox.value = clickedHeart * 99 / 14;

            UpdateSchedule();
        }

        public static void Initialize()
        {
            villagers = new List<string>();

            foreach (var character in Utility.getAllCharacters())
            {
                if (character is NPC npc && npc.IsVillager)
                {
                    villagers.Add(npc.Name);
                }
            }

            villagers.Sort(); // 이름순 정렬
        }
        private static void drawNPCSlotHeart(Vector2 heartDisplayPosition, SpriteBatch b, int clickedHeart)
        {
            for (int hearts = 0; hearts < 14; hearts++) // 최대 14 하트
            {
                Color color = (hearts < clickedHeart) ? Color.Red : (Color.White * 0.0f);
                int x = (hearts < clickedHeart) ? 211 : 218;
                if (hearts < 10)
                {
                    b.Draw(Game1.mouseCursors,
                          new Vector2(heartDisplayPosition.X + hearts * 32, heartDisplayPosition.Y),
                          new Rectangle(x, 428, 7, 6),
                          color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);

                }
                // 🔥 10개 이상이면 두 번째 줄로 배치
                else
                {
                    b.Draw(Game1.mouseCursors,
                           new Vector2(heartDisplayPosition.X + ((hearts - 10) * 32), heartDisplayPosition.Y + 32f),
                           new Rectangle(x, 428, 7, 6),
                           color, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
                }
            }
        }

        private void UpdateDateFromSlider()
        {
            float sliderValue = dateSlider.value;
            selectedDate = 1 + (int)((sliderValue - 1) / 99.0 * 27);
            UpdateSchedule();
        }
        public static void UpdateSchedule()
        {
            if (currentNPC == null) return;

            // ✅ 기존 스케줄을 유지한 채 최신 데이터를 불러오기
            scheduleEntries = ScheduleUI.LoadNPCSchedules(currentNPC, seasons[currentSeasonIndex], selectedDate);

        }


        //todo: 저장하고 가져오기 위에 있는 거 써도 되는데 scrollPosition을 초기화하는게 맘에 걸림
        public void receiveLeftClick(int x, int y, bool playSound = true)
        {

            if (leftSeasonButton.bounds.Contains(x, y))
            {
                currentSeasonIndex = (currentSeasonIndex - 1 + seasons.Length) % seasons.Length;
                UpdateSchedule();
                Game1.playSound("shwip");
            }
            else if (rightSeasonButton.containsPoint(x, y))
            {
                currentSeasonIndex = (currentSeasonIndex + 1) % seasons.Length;
                UpdateSchedule();
                Game1.playSound("shwip");
            }
            else if (addScheduleButton.bounds.Contains(x, y))
            {
                Console.WriteLine("[DEBUG] Adding New Schedule...");

                string newKey = $"{seasons[currentSeasonIndex].ToLower()}_{selectedDate}";
                ScheduleEntry newEntry = new ScheduleEntry($"{newKey}/{scheduleEntries.Count}", 600, "Farm", 0, 0, 2, "None", "None");

                scheduleEntries[newKey] = (new FriendshipConditionEntry(currentNPC.Name, newKey, new Dictionary<string, int>()), new List<ScheduleEntry> { newEntry });

                ScheduleManager.SaveSchedule(currentNPC.Name, seasons[currentSeasonIndex], selectedDate, scheduleEntries);
                UpdateSchedule();

                Game1.playSound("coin"); // 클릭 효과음 추가
            }
            else if (dateSlider.bounds.Contains(x, y))
            {
                dateSlider.receiveLeftClick(x, y);
                UpdateDateFromSlider();
            }
            else if (upArrow != null && upArrow.bounds.Contains(x, y))
            {
                Scroll(-scrollStep); // 🔼 위로 스크롤
                Game1.playSound("shwip");
            }
            else if (downArrow != null && downArrow.bounds.Contains(x, y))
            {
                Scroll(scrollStep); // 🔽 아래로 스크롤
                Game1.playSound("shwip");
            }
            else if (heartBox.bounds.Contains(x, y))
            {
                heartBox.receiveLeftClick(x, y);
                clickedHeart = (int)((heartBox.value - 1) / 99.0 * 14);
                string npcName = heartTarget.GetSelectedNPC();
                if (scheduleUI != null)
                {
                    scheduleUI.UpdateFriendshipCondition(npcName, clickedHeart);
                }
            }
            else if (heartTarget.bounds.Contains(x, y))
            {
                heartTarget.receiveLeftClick(x, y);
            }
            else if (scheduleUI != null && isOpenScheduleEditUI != null)
            {
                scheduleUI.HandleClick(x, y);
            }

            if (scheduleEntries == null || scheduleEntries.Count == 0) return;
            foreach (var entry in scheduleEntries)
            {
                string scheduleKey = entry.Key; // 🔹 scheduleEntries의 키


                foreach (var text in entry.Value.Item2)
                { // 🔹 삭제 버튼 위치 조정
                    Rectangle deleteButtonBounds = new Rectangle(text.Bounds.bounds.Right - 40, text.Bounds.bounds.Top + 10, 32, 32);
                    if (deleteButtonBounds.Contains(x, y))
                    {
                        Console.WriteLine($"[DEBUG] Deleting Schedule: {text.Key}");

                        entry.Value.Item2.Remove(text);

                        // 🔥 만약 해당 키의 리스트가 비어 있다면 키 자체도 삭제
                        if (entry.Value.Item2.Count == 0)
                        {
                            scheduleEntries.Remove(entry.Key);
                        }

                        ScheduleManager.SaveSchedule(currentNPC.Name, seasons[currentSeasonIndex], selectedDate, scheduleEntries);
                        return;
                    }
                    if (text.Contains(x, y))
                    {
                        // 🔥 클릭한 스케줄을 수정하는 UI 추가
                        scheduleUI = new ScheduleUI(currentNPC, scheduleEntries, text, seasons[currentSeasonIndex], selectedDate);
                        if (isOpenScheduleEditUI == null)
                        {
                            isOpenScheduleEditUI = text.Key;
                        }
                        else
                        {
                            isOpenScheduleEditUI = null;
                        }
                        UpdateScrollSize();

                        FriendshipConditionEntry condition = entry.Value.Item1;
                        // 🔹 우정 조건이 있는 경우 모든 조건 표시
                        if (condition != null && condition.Condition.Count > 0)
                        {

                            List<KeyValuePair<string, int>> conditionList = condition.Condition.ToList(); // 🔥 컬렉션 복사 후 순회
                            foreach (var cond in conditionList)
                            {
                                int friendship = cond.Value; // 🔹 필요한 호감도 값

                                clickedHeart = (int)((friendship - 1) / 99.0 * 14);
                                heartBox.value = clickedHeart * 99 / 14;
                            }

                        }

                        return;
                    }
                }



            }

        }

        public void receiveScrollWheelAction(int direction)
        {
            int scrollAmount = direction > 0 ? -scrollStep : scrollStep;
            Scroll(scrollAmount);
        }


        // 🔹 스크롤 관련 변수 (클래스 필드)
        private static int scrollPosition = 0;  // 현재 스크롤 위치
        private static int scrollSize = 0;      // 스크롤 가능한 크기
        private static int maxScrollPosition = 0; // 최대 스크롤 가능 위치
        private static int scrollStep = 100;      // 한 번에 이동할 거리


        static ClickableTextureComponent upArrow;
        static ClickableTextureComponent downArrow;




        private static void Scroll(int offset)
        {
            if (NeedsScrollBar())
            {
                scrollPosition = MathHelper.Clamp(scrollPosition + offset * maxScrollPosition / scrollSize, 0, scrollSize);
            }
        }
        private static bool NeedsScrollBar()
        {
            return scrollSize > 0;
        }

        private static void DrawScrollUI(SpriteBatch b, Rectangle itemDisplayRect)
        {
            int scrollBarWidth = 24;
            int scrollArrowSize = 44;
            int ScrollNum = 64;

            Rectangle scrollBarRunner = new Rectangle(itemDisplayRect.Right + 48, itemDisplayRect.Top + ScrollNum, scrollBarWidth, itemDisplayRect.Height - ScrollNum * 2);

            upArrow = new ClickableTextureComponent(
                new Rectangle(scrollBarRunner.Center.X - scrollArrowSize / 2, scrollBarRunner.Top - 16 - scrollArrowSize, scrollArrowSize, scrollArrowSize),
                Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);

            downArrow = new ClickableTextureComponent(
                new Rectangle(scrollBarRunner.Center.X - scrollArrowSize / 2, scrollBarRunner.Bottom + 16, scrollArrowSize, scrollArrowSize),
                Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);


            upArrow.draw(b);
            downArrow.draw(b);
        }
        public void Draw(SpriteBatch b, int xPositionOnScreen, int yPositionOnScreen, int width, int height)
        {
            UpdateScrollSize();
            heartTarget.draw(b, (int)heartDisplayPosition.X, (int)heartDisplayPosition.Y - 50);
            drawNPCSlotHeart(heartDisplayPosition, b, clickedHeart);
            heartBox.draw(b, 0, 0);
            // 🔹 계절 선택 버튼
            leftSeasonButton.draw(b);
            rightSeasonButton.draw(b);
            addScheduleButton.draw(b);

            // 🔹 현재 계절 표시
            SpriteText.drawStringHorizontallyCenteredAt(b, seasons[currentSeasonIndex], xPositionOnScreen + 350, yPositionOnScreen);

            // 🔹 날짜 선택 슬라이더
            dateSlider.draw(b, 0, 0);
            SpriteText.drawStringHorizontallyCenteredAt(b, selectedDate.ToString(), xPositionOnScreen + 750, yPositionOnScreen);

            // 🔹 스케줄 목록 표시
            Rectangle viewport = new Rectangle(xPositionOnScreen, yPositionOnScreen + 100, width, height - 150);
            int yOffset = viewport.Top - scrollPosition;

            b.End();
            b.GraphicsDevice.ScissorRectangle = viewport;
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState() { ScissorTestEnable = true });
            // 🔥 모든 스케줄 데이터를 로드
            ScheduleDataManager.LoadAllSchedules();
            HashSet<string> editedScheduleKeys = ScheduleDataManager.GetEditedScheduleKeys(currentNPC.Name);

            if (scheduleEntries != null && scheduleEntries.Count > 0)
            {
                foreach (var entry in scheduleEntries)
                {
                    string scheduleKey = entry.Key;
                    List<ScheduleEntry> scheduleList = entry.Value.Item2;
                    FriendshipConditionEntry condition = entry.Value.Item1;

                    Color keyColor = editedScheduleKeys.Contains(scheduleKey) ? Color.Blue : Color.White;

                    SpriteText.drawString(b, $"{scheduleKey}", viewport.Left + 30, yOffset, layerDepth: 0.1f, color: keyColor);
                    yOffset += 40;

                    // 🔹 우정 조건이 있는 경우 모든 조건 표시
                    if (condition != null && condition.Condition.Count > 0)
                    {
                        foreach (var cond in condition.Condition)
                        {
                            string npcName = cond.Key;  // 🔹 조건이 적용되는 NPC 이름
                            int friendship = cond.Value; // 🔹 필요한 호감도 값
                                                         // 🔥 편집된 스케줄이 있으면 파란색으로 표시

                            // 💙 해당 스케줄이 적용되기 위한 조건 표시
                            b.DrawString(Game1.smallFont, $"🖤 {npcName} >= {friendship}",
                                new Vector2(viewport.Left + 30, yOffset), Color.Gray);
                            yOffset += 30;
                        }
                    }
                    foreach (var scheduleEntry in scheduleList)
                    {
                        if (scheduleEntry == null) continue;
                        // 🏠 일반 스케줄 항목
                        string scheduleText = $"{scheduleEntry.Time}: {scheduleEntry.Location} / {scheduleEntry.Action}";

                        // 🔥 박스 스타일 추가
                        Rectangle scheduleBox = new Rectangle(viewport.Left + 20, yOffset, width - 60, 80);

                        // 🔥 반투명 배경 박스 그리기
                        b.Draw(Game1.staminaRect, scheduleBox, new Rectangle(0, 0, 1, 1), Color.SandyBrown * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 0.8f);
                        DrawBorder(b, scheduleBox, 3, Color.Brown);  // 테두리 두께 3px

                        // 🔹 텍스트 출력 (박스 내부)
                        b.DrawString(Game1.smallFont, scheduleText, new Vector2(scheduleBox.X + 10, scheduleBox.Y + 10), Color.Black);
                        b.DrawString(Game1.smallFont, $"Talk: {scheduleEntry.Talk}", new Vector2(scheduleBox.X + 10, scheduleBox.Y + 40), Color.Black);


                        // 🔹 박스 클릭 가능하도록 좌표 설정
                        scheduleEntry.SetBounds(scheduleBox.X, scheduleBox.Y, scheduleBox.Width, scheduleBox.Height);

                        yOffset += 90;



                        // 🔥 편집 UI가 열려 있으면 박스 내부에 표시
                        if (isOpenScheduleEditUI == scheduleEntry.Key)
                        {
                            Vector2 editUIPosition = new Vector2(scheduleBox.X + 10, scheduleBox.Y + scheduleBox.Height + 10);



                            scheduleUI.Draw(b, editUIPosition);
                            yOffset += 600;
                        }
                        else
                        {
                            // 🔹 삭제 버튼 추가
                            // 🔹 삭제 버튼 위치 조정
                            ClickableTextureComponent deleteButton = new ClickableTextureComponent(
                                new Rectangle(scheduleBox.Right - 40, scheduleBox.Y + 10, 32, 32),
                                Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 2f);

                            deleteButton.draw(b);
                        }
                    }

                    yOffset += 100;
                }
                // 🔹 스크롤 UI 처리
                if (scheduleEntries != null)
                {
                    maxScrollPosition = viewport.Height;
                    scrollStep = 100;
                }
                DrawScrollUI(b, new Rectangle(viewport.Left - 100, viewport.Top, viewport.Width, viewport.Height));
            }

            b.End();
            b.Begin();
        }// 🔥 박스 테두리 직접 그리는 함수
        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rectangle, int thickness, Color color)
        {
            Texture2D pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            // 상단
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            // 하단
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y + rectangle.Height - thickness, rectangle.Width, thickness), color);
            // 좌측
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            // 우측
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + rectangle.Width - thickness, rectangle.Y, thickness, rectangle.Height), color);

            // 🔹 모서리 대각선 추가
            int cornerLength = 10;  // 대각선 길이

            // 하단
            for (int i = 0; i < cornerLength; i++)
            {
                spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 1 + i, rectangle.Bottom - cornerLength + i - 2, 2, 2), color);
                spriteBatch.Draw(pixel, new Rectangle(rectangle.Right + i - 1 - cornerLength, rectangle.Bottom - i - 2, 2, 2), color);
            }

            // 상단
            for (int i = 0; i < cornerLength; i++)
            {
                spriteBatch.Draw(pixel, new Rectangle(rectangle.Right + i - 1 - cornerLength, rectangle.Y + i, 2, 2), color);
                spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 1 + i, rectangle.Y + cornerLength - i, 2, 2), color);
            }
        }
        private void UpdateScrollSize()
        {
            int baseEntryHeight = 90; // 기본 스케줄 항목 높이
            int extraPadding = 150; // 여백 추가
            int editUIHeight = 600; // 편집 UI가 나타날 때 추가되는 높이

            scrollSize = 500;
            int yOffset = scrollSize;

            foreach (var entry in scheduleEntries)
            {
                yOffset += baseEntryHeight;


            }// 🔥 편집 UI가 열려있는 경우 해당 항목의 높이를 추가
            if (isOpenScheduleEditUI != null)
            {
                yOffset += editUIHeight;
            }
            scrollSize = yOffset + extraPadding;
        }

    }
}
