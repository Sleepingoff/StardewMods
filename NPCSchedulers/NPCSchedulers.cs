using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using NPCSchedulers.UI;
using Microsoft.Xna.Framework;
using NPCSchedulers.API;

namespace NPCSchedulers
{

    public class ModConfig
    {
        public SButton Key { get; set; } = SButton.F12;
        public bool Immediately { get; set; } = true;
        public bool DayStated { get; set; } = true;
        public bool NotApply { get; set; } = false;
        public bool All { get; set; } = true;
        public bool Selected { get; set; } = false;
        // NPC별 스케줄 키 저장소
        public Dictionary<string, Dictionary<string, bool>> NpcScheduleKeys { get; set; } = new();

        // 특정 NPC의 스케줄 키 추가 메서드
        public void AddNpcScheduleKey(string npc, string key, bool value = true)
        {
            if (!NpcScheduleKeys.ContainsKey(npc))
            {
                NpcScheduleKeys.Add(npc, new Dictionary<string, bool>() { { key, value } });

            }
            else
            {
                NpcScheduleKeys[npc][key] = value;
            }
        }

        // 특정 NPC의 특정 스케줄 키 삭제
        public void RemoveNpcScheduleKey(string npc, string key)
        {
            if (NpcScheduleKeys.ContainsKey(npc) && NpcScheduleKeys[npc].ContainsKey(key))
            {
                NpcScheduleKeys[npc].Remove(key);
                if (NpcScheduleKeys[npc].Count == 0)
                {
                    NpcScheduleKeys.Remove(npc);
                }
            }
        }

        // 특정 NPC의 특정 스케줄 키 값 업데이트
        public void UpdateNpcScheduleKey(string npc, string key, bool value)
        {
            if (NpcScheduleKeys.ContainsKey(npc))
            {
                NpcScheduleKeys[npc][key] = value;
            }
        }

        // 특정 NPC의 스케줄 키 목록 가져오기
        public Dictionary<string, bool> GetNpcSchedules(string npc)
        {
            return NpcScheduleKeys.ContainsKey(npc) ? NpcScheduleKeys[npc] : null;
        }
    }
    public class ModEntry : Mod
    {
        public ModConfig Config = new();
        private SchedulePage schedulePage;
        private bool isProfileMenuOpen = false;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            RegisterEvents();
            PatchMethods();
        }
        public override object GetApi()
        {
            return new INPCSchedulers();
        }
        private void RegisterEvents()
        {
            Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            Helper.Events.Display.MenuChanged += OnMenuChanged;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
        }
        private void PatchMethods()
        {
            Harmony harmony = new Harmony(ModManifest.UniqueID);

            MethodInfo targetDrawMethod = AccessTools.Method(typeof(ProfileMenu), "draw", new System.Type[] { typeof(SpriteBatch) });
            harmony.Patch(targetDrawMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(DrawPostfix)));

            // 🔹 ProfileMenu의 클릭 이벤트 패치
            MethodInfo targetClickMethod = AccessTools.Method(typeof(ProfileMenu), "receiveLeftClick");
            harmony.Patch(targetClickMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(ReceiveLeftClick)));

            // 🔹 ProfileMenu의 클릭 이벤트 패치
            MethodInfo targetHeldMethod = AccessTools.Method(typeof(ProfileMenu), "leftClickHeld");
            harmony.Patch(targetHeldMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(LeftClickHeld)));

            // 🔹 클릭 해제 이벤트 (선택 해제 등 필요할 경우)
            MethodInfo releaseClickMethod = AccessTools.Method(typeof(ProfileMenu), "releaseLeftClick");
            harmony.Patch(releaseClickMethod, postfix: new HarmonyMethod(typeof(ModEntry), nameof(LeftClickReleased)));
        }

        public static bool ReceiveLeftClick(int x, int y)
        {
            Instance.schedulePage?.LeftClick(x, y);
            return !SchedulePage.IsOpen;
        }
        private static double clickHoldTime = 0; // 클릭 지속 시간 (초)
        private static bool isHoldingClick = false;
        private static readonly double requiredHoldTime = 0.3; // 최소 0.3초 이상 클릭해야 동작

        public static void LeftClickHeld(int x, int y)
        {
            if (!isHoldingClick)
            {
                isHoldingClick = true;
                clickHoldTime = Game1.currentGameTime.TotalGameTime.TotalSeconds; // 클릭한 순간 기록
            }

            double elapsedTime = Game1.currentGameTime.TotalGameTime.TotalSeconds - clickHoldTime;

            if (elapsedTime >= requiredHoldTime)
            {
                Instance.schedulePage?.LeftHeld(x, y);
            }
        }
        public static void LeftClickReleased()
        {
            isHoldingClick = false;
        }

        public static bool DrawPostfix(SpriteBatch b)
        {
            if (!SchedulePage.IsOpen) return true;

            Instance.schedulePage?.Draw(b);

            return false;
        }
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e)
        {
            if (schedulePage != null)
            {
                schedulePage.ScrollWheelAction(e.Delta);
            }

        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            GameLocation gameLocation = Game1.currentLocation;

            NPC npcAtCursor = gameLocation.isCharacterAtTile(Game1.currentCursorTile);
            if (npcAtCursor == null) npcAtCursor = gameLocation.isCharacterAtTile(new Vector2(Game1.currentCursorTile.X, Game1.currentCursorTile.Y + 1));
            if (e.Button == Config.Key && npcAtCursor != null)
            {
                // 새 프로필 및 스케줄 편집 메뉴 열기
                var allSocialEntries = new List<SocialPage.SocialEntry>(); // You need to populate this list as required
                Game1.activeClickableMenu = new ProfileMenu(new SocialPage.SocialEntry(npcAtCursor, Game1.player.friendshipData[npcAtCursor.Name], npcAtCursor.GetData()), allSocialEntries);
                schedulePage = new SchedulePage(npcAtCursor.Name);
                schedulePage.ToggleSchedulePage((ProfileMenu)Game1.activeClickableMenu);
            }

            //e 혹은 esc를 누르면 스케줄러 창 닫기

            if (SchedulePage.IsOpen)
            {
                if (!isProfileMenuOpen)
                {
                    SchedulePage.Close();
                }
            }
            if (!isProfileMenuOpen || !(Game1.activeClickableMenu is ProfileMenu profileMenu)) return;

            int x = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.X);
            int y = (int)Utility.ModifyCoordinateForUIScale(e.Cursor.ScreenPixels.Y);

            if (SchedulePage.IsOpenPage(x, y))
            {
                schedulePage.ToggleSchedulePage(profileMenu);
            }
            else if (SchedulePage.IsOpenFriendshipList(x, y))
            {
                schedulePage.ToggleFriendshipList();
            }
            else if (SchedulePage.IsOpenMailList(x, y))
            {
                schedulePage.ToggleMailList();
            }


        }
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (Config.DayStated)
            {
                List<string> NPCList = ScheduleDataManager.GetAllNPCListByUser();
                foreach (string npcName in NPCList)
                {
                    ScheduleDataManager.ApplyScheduleToNPC(npcName, Config);
                }
            }
        }
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is ProfileMenu profileMenu)
            {
                SchedulePage.DrawButton(Game1.spriteBatch);
            }
        }
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.OldMenu is ProfileMenu)
                RegisterConfig();
        }
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            isProfileMenuOpen = Game1.activeClickableMenu is ProfileMenu;
            if (isProfileMenuOpen)
            {
                SchedulePage.CreateScheduleButton((ProfileMenu)Game1.activeClickableMenu);
                ProfileMenu profileMenu = (ProfileMenu)Game1.activeClickableMenu;
                if (schedulePage == null || schedulePage.npcName != profileMenu.Current.Character.Name)
                {
                    schedulePage = new SchedulePage(profileMenu.Current.Character.Name);
                }
            }

        }
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            ScheduleDataManager.LoadAllSchedules();
        }


        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            RegisterConfig();
        }

        public void RegisterConfig()
        {
            var configMenu = Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (configMenu == null)
            {
                Monitor.Log("Generic Mod Config Menu not found.", LogLevel.Warn);

                return;
            }

            configMenu.Unregister(this.ModManifest);

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Helper.ReadConfig<ModConfig>(),
                save: () => this.Helper.WriteConfig(this.Config)
            );
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("Config.Key").Default("Key"),
                getValue: () => this.Config.Key,
                setValue: value => this.Config.Key = value
            );
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => Helper.Translation.Get("Config.When").Default("When Apply Schedule")
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("Config.Immediately").Default("Immediately"),
                getValue: () => this.Config.Immediately,
                setValue: value => this.Config.Immediately = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("Config.DayStated").Default("DayStated"),
                getValue: () => this.Config.DayStated,
                setValue: value => this.Config.DayStated = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("Config.NotApply").Default("NotApply"),
                getValue: () => this.Config.NotApply,
                setValue: value => this.Config.NotApply = value
                );

            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => Helper.Translation.Get("Config.What").Default("What Schedule (not for 'NotApply')")
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("Config.All").Default("All"),
                getValue: () => this.Config.All,
                setValue: value => this.Config.All = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("Config.Selected").Default("Selected"),
                getValue: () => this.Config.Selected,
                setValue: value => this.Config.Selected = value
            );
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => Helper.Translation.Get("Config.Select").Default("Select Schedule")
            );
            var characters = Utility.getAllCharacters();
            foreach (var character in characters)
            {
                var userKeys = ScheduleDataManager.GetEditedScheduleKeys(character.Name);
                configMenu.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => character.Name
                );
                foreach (string userKey in userKeys)
                {
                    this.Config.AddNpcScheduleKey(character.Name, userKey, Config.All);
                    configMenu.AddBoolOption(mod: this.ModManifest,
                        name: () => userKey,
                        getValue: () => this.Config.GetNpcSchedules(character.Name).Values.First(),
                        setValue: value => this.Config.UpdateNpcScheduleKey(character.Name, userKey, value)
                    );
                }

                if (Config.Immediately)
                {
                    ScheduleDataManager.ApplyScheduleToNPC(character.Name, Config);
                }
            }
        }
        public static ModEntry Instance { get; private set; }
    }

}
