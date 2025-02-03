using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using System.Reflection;
using StardewValley.GameData.Characters;
using NPCSchedulers.UI;
using System.Numerics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace NPCSchedulers
{

    public class ModEntry : Mod
    {
        private Harmony harmony;
        private static List<ScheduleEntry> scheduleEntries = new List<ScheduleEntry>();
        private static bool isScheduleViewOpen = false;
        private ClickableTextureComponent scheduleButton;
        private bool isProfileMenuOpen = false;
        private static SchedulePage schedulePage;
        private static List<List<OptionsElement>> optionsElements = new List<List<OptionsElement>>();
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            harmony = new Harmony(ModManifest.UniqueID);

            RegisterEvents();
            PatchMethods();
        }

        private void PatchMethods()
        {
            MethodInfo targetDrawMethod = AccessTools.Method(typeof(ProfileMenu), "draw", new Type[] { typeof(SpriteBatch) });
            harmony.Patch(targetDrawMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(DrawPostfix)));

            MethodInfo targetClickMethod = AccessTools.Method(typeof(ProfileMenu), "receiveLeftClick");
            harmony.Patch(targetClickMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(ReceiveLeftClickPostfix)));

            // MethodInfo cursorMovedMethod = AccessTools.Method(typeof(ProfileMenu), "receiveCursorMoved");
            // harmony.Patch(cursorMovedMethod, postfix: new HarmonyMethod(typeof(ModEntry), nameof(ReceiveCursorMovedPostfix)));

            MethodInfo releaseClickMethod = AccessTools.Method(typeof(ProfileMenu), "releaseLeftClick");
            harmony.Patch(releaseClickMethod, postfix: new HarmonyMethod(typeof(ModEntry), nameof(ReleaseLeftClickPostfix)));
        }

        private void RegisterEvents()
        {
            Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;

        }
        public static void ReleaseLeftClickPostfix(int x, int y)
        {
            if (isScheduleViewOpen && schedulePage != null)
            {
                // schedulePage.receiveReleaseClick(x, y);
            }
        }
        public static void ReceiveLeftClickPostfix(int x, int y)
        {
            if (schedulePage == null) return;
            schedulePage.receiveLeftClick(x, y);
            if (optionsElements.Count == 0) return;

            foreach (var scheduleOption in optionsElements)
            {
                foreach (var option in scheduleOption)
                {
                    if (option is CustomOptionsDropDown dropdown && (dropdown.bounds.Contains(x, y) || dropdown.IsActive() && dropdown.dropDownBounds.Contains(x, y)))
                    {
                        dropdown.receiveLeftClick(x, y);
                        return;
                    }
                    if (option is OptionsTextBox textBox && textBox.ContainsPoint(x, y))
                    {
                        textBox.receiveLeftClick(x, y);
                        return;
                    }
                }

            }


        }


        public static bool DrawPostfix(SpriteBatch b, ProfileMenu __instance)
        {
            if (!isScheduleViewOpen) return true;
            if (!Game1.options.showClearBackgrounds)
            {
                b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            }
            int width = Game1.activeClickableMenu.width;
            int height = Game1.activeClickableMenu.height;
            NPC nPC = __instance.Current.Character as NPC;
            if (nPC == null) return true;

            int xPositionOnScreen = Game1.activeClickableMenu.xPositionOnScreen;
            int yPositionOnScreen = Game1.activeClickableMenu.yPositionOnScreen;
            int x = xPositionOnScreen + 64 - 12;
            int y = yPositionOnScreen + IClickableMenu.borderWidth;
            Rectangle rectangle = new Rectangle(x, y, 400, 720 - IClickableMenu.borderWidth * 2);
            Rectangle itemDisplayRect = new Rectangle(x, y, 1204, 720 - IClickableMenu.borderWidth * 2);
            Rectangle characterSpriteBox = new Rectangle(xPositionOnScreen + 64 - 12 + (400 - Game1.nightbg.Width) / 2, yPositionOnScreen + IClickableMenu.borderWidth, Game1.nightbg.Width, Game1.nightbg.Height);

            itemDisplayRect.X += rectangle.Width;
            itemDisplayRect.Width -= rectangle.Width;
            Rectangle characterStatusDisplayBox = new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            rectangle.Y += 32;
            rectangle.Height -= 32;

            Vector2 characterSpriteDrawPosition = new Vector2(rectangle.X + (rectangle.Width - Game1.nightbg.Width) / 2, rectangle.Y);

            rectangle.Y += Game1.daybg.Height + 32;
            rectangle.Height -= Game1.daybg.Height + 32;
            Vector2 characterNamePosition = new Vector2(rectangle.Center.X, rectangle.Top);
            rectangle.Y += 96;
            rectangle.Height -= 96;

            if (nPC.Birthday_Season != null && Utility.getSeasonNumber(nPC.Birthday_Season) >= 0)
            {
                rectangle.Y += 48;
                rectangle.Height -= 48;
                rectangle.Y += 64;
                rectangle.Height -= 64;
            }
            CharacterData data = nPC.GetData();
            string text = "Characters/" + nPC.getTextureName();
            Texture2D letterTexture = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\letterBG");
            AnimatedSprite animatedSprite = new AnimatedSprite(text, 0, data?.Size.X ?? 16, data?.Size.Y ?? 32);
            b.Draw(letterTexture, new Vector2(xPositionOnScreen + width / 2, yPositionOnScreen + height / 2), new Rectangle(0, 0, 320, 180), Color.White, 0f, new Vector2(160f, 90f), 4f, SpriteEffects.None, 0.86f);
            Vector2 screenPosition = new Vector2(characterSpriteDrawPosition.X + (float)((Game1.daybg.Width - animatedSprite.SpriteWidth * 4) / 2), characterSpriteDrawPosition.Y + 32f + (float)((32 - animatedSprite.SpriteHeight) * 4));

            Game1.DrawBox(characterStatusDisplayBox.X, characterStatusDisplayBox.Y, characterStatusDisplayBox.Width, characterStatusDisplayBox.Height);
            b.Draw((Game1.timeOfDay >= 1900) ? Game1.nightbg : Game1.daybg, characterSpriteDrawPosition, Color.White);
            animatedSprite.draw(b, screenPosition, 0.8f);

            // NPC 이름 출력
            SpriteText.drawStringWithScrollCenteredAt(b, nPC.displayName, (int)characterNamePosition.X, (int)characterNamePosition.Y);

            SpriteText.drawStringWithScrollCenteredAt(b, "Today's Schedule",
                                                      itemDisplayRect.Center.X, itemDisplayRect.Top);

            Vector2 startVector = new Vector2(characterStatusDisplayBox.Width + characterStatusDisplayBox.X, itemDisplayRect.Top + 60);
            // 🔥 각 스케줄을 UI로 렌더링

            if (schedulePage != null)
            {
                schedulePage.Draw(b, (int)startVector.X, (int)startVector.Y, itemDisplayRect.Width, itemDisplayRect.Height);
            }

            //기본 UI

            ClickableTextureComponent nextCharacterButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 32 - 48, yPositionOnScreen + height - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f)
            {
                myID = 0,
                name = "Next Char",
                upNeighborID = -99998,
                downNeighborID = -99998,
                leftNeighborID = -99998,
                rightNeighborID = -99998,
                region = 500
            };
            ClickableTextureComponent previousCharacterButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + 32, yPositionOnScreen - 32 - 64, 48, 44), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f)
            {
                myID = 0,
                name = "Previous Char",
                upNeighborID = -99998,
                downNeighborID = -99998,
                leftNeighborID = -99998,
                rightNeighborID = -99998,
                region = 500
            };
            previousCharacterButton.bounds.X = (int)characterSpriteDrawPosition.X - 64 - previousCharacterButton.bounds.Width / 2;
            previousCharacterButton.bounds.Y = (int)characterSpriteDrawPosition.Y + Game1.nightbg.Height / 2 - previousCharacterButton.bounds.Height / 2;
            nextCharacterButton.bounds.X = (int)characterSpriteDrawPosition.X + Game1.nightbg.Width + 64 - nextCharacterButton.bounds.Width / 2;
            nextCharacterButton.bounds.Y = (int)characterSpriteDrawPosition.Y + Game1.nightbg.Height / 2 - nextCharacterButton.bounds.Height / 2;



            List<ClickableTextureComponent> clickableTextureComponents = new List<ClickableTextureComponent>
            {
                previousCharacterButton,
                nextCharacterButton,
            };
            foreach (ClickableTextureComponent clickableTextureComponent in clickableTextureComponents)
            {
                clickableTextureComponent.draw(b);
            }
            return false;

        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Game1.activeClickableMenu != null)
            {
                isProfileMenuOpen = Game1.activeClickableMenu.GetType().Name == "ProfileMenu";
                if (isProfileMenuOpen)
                {
                    CreateScheduleButton(Game1.activeClickableMenu);
                    ProfileMenu profileMenu = (ProfileMenu)Game1.activeClickableMenu;
                    if (isScheduleViewOpen)
                    {
                        // 🔥 모든 스케줄 불러오기
                        NPC npc = Game1.getCharacterFromName(profileMenu.Current.InternalName);
                        if (npc != null && (schedulePage == null || SchedulePage.currentNPC.Name != npc.Name))
                        {

                            schedulePage = new SchedulePage(npc, profileMenu);

                        }
                    }
                }
            }
        }
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e)
        {
            if (isScheduleViewOpen && schedulePage != null)
            {
                schedulePage.receiveScrollWheelAction(e.Delta);
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!isProfileMenuOpen || scheduleButton == null) return;
            if (e.Button == SButton.MouseLeft && Game1.activeClickableMenu is ProfileMenu profileMenu)
            {
                ScheduleUI.InitializeOptions();
                SchedulePage.Initialize();

                int x = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.X);
                int y = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.Y);

                Rectangle rectangle = scheduleButton.bounds;
                if (rectangle.Contains(x, y))
                {
                    isScheduleViewOpen = !isScheduleViewOpen; // UI 토글

                }

            }
        }
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            // 현재 활성화된 메뉴가 ProfileMenu인지 확인
            if (Game1.activeClickableMenu is ProfileMenu profileMenu && scheduleButton != null)
            {
                scheduleButton.draw(Game1.spriteBatch);

            }
        }

        public void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            ScheduleManager.SaveScheduleData();
        }
        private void CreateScheduleButton(IClickableMenu menu)
        {
            int buttonX = menu.xPositionOnScreen + 80; // ProfileMenu의 상대 좌표
            int buttonY = menu.yPositionOnScreen + 80;

            scheduleButton = new ClickableTextureComponent(
                new Rectangle(buttonX, buttonY, 64, 64), // 크기 확대
                Game1.mouseCursors,
                new Rectangle(16, 368, 16, 16), // 버튼 텍스처 위치
                4f);
        }
        private static void DrawScheduleUI(SpriteBatch b, ScheduleUI scheduleUI, Vector2 vector)
        {

            int startX = (int)vector.X + 10;  // UI 위치 조정
            int startY = (int)vector.Y;  // 적절한 높이 조정


            foreach (var scheduleOption in optionsElements)
            {
                foreach (var option in scheduleOption)
                {
                    // UI 요소 그리기
                    option.draw(b, startX, startY);
                    startX += 250; // 간격 조정
                }
                startY += 80;
                startX = (int)vector.X + 10;
            }



        }


        public static ModEntry Instance { get; private set; }

    }
}
