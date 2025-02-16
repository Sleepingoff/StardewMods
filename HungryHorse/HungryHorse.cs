using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using Microsoft.Xna.Framework;
using StardewValley.Characters;
using StardewValley.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Buffs;

namespace HungryHorse
{
    public class ModConfig
    {
        public bool Mod { get; set; } = true;
        public int MaxStamina { get; set; } = 1000;
        public float StaminaDecreaseRate { get; set; } = 1f; // 기본 기력 감소량

        public int FriendshipPerDayAfterInteraction { get; set; } = 50;
        public int FriendshipByFood { get; set; } = 20;


    }

    public class ModEntry : Mod
    {
        private ModConfig Config = new();
        private int horseStamina;
        private int maxHorseStamina;
        private static Dictionary<int, int> horseFriendship = new(); // 말 호감도 저장
        private static Dictionary<int, bool> wasPetToday = new(); // 하루 동안 말이 쓰다듬어졌는지 저장
        private static Dictionary<int, bool> horseAteFoodToday = new(); // 오늘 말을 먹였는지 추적
        public override void Entry(IModHelper helper)
        {

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null)
            {
                Monitor.Log("Generic Mod Config Menu not found.", LogLevel.Warn);

                return;
            }

            configMenu.Unregister(this.ModManifest);
            var i18n = Helper.Translation;
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Helper.ReadConfig<ModConfig>(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => i18n.Get("Config.Mod").Default("Mod"),
                getValue: () => this.Config.Mod,
                setValue: value => { this.Config.Mod = value; this.Helper.WriteConfig(this.Config); }
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => i18n.Get("Config.MaxStamina").Default("MaxStamina"),
                getValue: () => this.Config.MaxStamina,
                setValue: value => { this.Config.MaxStamina = value; this.Helper.WriteConfig(this.Config); }
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => i18n.Get("Config.StaminaDecreaseRate").Default("StaminaDecreaseRate"),
                getValue: () => this.Config.StaminaDecreaseRate,
                setValue: value => { this.Config.StaminaDecreaseRate = value >= 1f ? value : 1f; this.Helper.WriteConfig(this.Config); },
                tooltip: () => i18n.Get("Config.RateDesc").Default("rate apply per tick. minValue: 1f")
            );
            configMenu.AddNumberOption(
               mod: this.ModManifest,
               name: () => i18n.Get("Config.FriendshipPerDayAfterInteraction").Default("FriendshipPerDayAfterInteraction"),
               getValue: () => this.Config.FriendshipPerDayAfterInteraction,
               setValue: value => { this.Config.FriendshipPerDayAfterInteraction = value; this.Helper.WriteConfig(this.Config); }
           );
            configMenu.AddNumberOption(
               mod: this.ModManifest,
               name: () => i18n.Get("Config.FriendshipByFood").Default("FriendshipByFood"),
               getValue: () => this.Config.FriendshipByFood,
               setValue: value => { this.Config.FriendshipByFood = value; this.Helper.WriteConfig(this.Config); }
           );

        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            Farmer who = Game1.player;
            GameLocation gameLocation = Game1.currentLocation;
            bool isCharacterAtTile = gameLocation.isCharacterAtTile(Game1.currentCursorTile) is Horse;
            if (isCharacterAtTile && e.Button == SButton.MouseRight)
            {
                Horse horse = (Horse)gameLocation.isCharacterAtTile(Game1.currentCursorTile);

                if (who.ActiveObject == null)
                {
                    PetHorse(horse);
                    return;
                }
                if (who.ActiveObject is StardewValley.Object food)
                {
                    int horseId = horse.id;
                    if (horseFriendship.ContainsKey(horseId))
                    {
                        FeedHorse(horse, food);
                        return;
                    }
                }

            }
        }

        bool isShowWarningMessage = false;
        bool isShowUI = false;
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            Horse horse = Game1.player?.mount;
            if (horse != null)
            {
                var i18n = Helper.Translation;
                if (!isShowUI)
                {
                    Helper.Events.Display.Rendered += OnRendered;
                    isShowUI = true;
                }
                int horseId = horse.id;

                float friendshipFactor = 1f - (horseFriendship.ContainsKey(horseId) ? (horseFriendship[horseId] / 1000f) : 0f);
                KeyboardState keyboard = Game1.GetKeyboardState();
                //키는 config에서 변경 가능
                bool isPressed = keyboard.GetPressedKeys().Contains(Keys.LeftShift);
                if (horseStamina > 0 && isPressed)
                {
                    ApplySpeedBuff();
                    horseStamina = Math.Clamp((int)(horseStamina - (Config.StaminaDecreaseRate * friendshipFactor)), 0, maxHorseStamina); // ✅ 기력 감소량 증가
                    isShowWarningMessage = false;
                }
                if (horseStamina <= 0)
                {
                    Game1.showGlobalMessage(i18n.Get("Desc.Sleep").Default("말이 뻗었습니다!"));
                    Game1.player.mount.dismount();
                }
                else if (horseStamina <= maxHorseStamina * 0.3f)
                {
                    ApplySlowBuff();
                    if (!isShowWarningMessage)
                    {
                        Game1.showGlobalMessage(i18n.Get("Desc.Tired").Default("말이 지쳐서 더 이상 달릴 수 없습니다!"));
                        isShowWarningMessage = true;
                    }
                }


            }
            else
            {
                if (isShowUI)
                {
                    Helper.Events.Display.Rendered -= OnRendered;
                    isShowUI = false;
                }

            }
            if (Game1.currentLocation is Farm farm)
            {
                foreach (NPC character in farm.characters)
                {
                    if (character is Horse) { horseStamina = Math.Clamp(horseStamina + 10, 0, maxHorseStamina); }
                }

            }
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == GameMenu.animalsTab)
            {
                AnimalPage animalPage = (AnimalPage)gameMenu.GetCurrentPage();
                var animals = animalPage.AnimalEntries;
                foreach (var entry in animals)
                {
                    if (entry.Animal is Horse horseEntry)
                    {
                        int horseId = horseEntry.id;

                        if (!horseFriendship.ContainsKey(horseId))
                        {
                            horseFriendship.Add(horseId, 0); // 기본 호감도 설정
                        }
                        if (!horseAteFoodToday.ContainsKey(horseId))
                        {
                            horseAteFoodToday.Add(horseId, false); // 기본 호감도 설정
                        }
                        var entryType = AccessTools.Inner(typeof(AnimalPage), "AnimalEntry");
                        // ✅ Horse에도 FriendshipLevel 적용 (생성자 패치 대신 후처리)
                        var friendshipField = AccessTools.DeclaredField(entryType, "FriendshipLevel");
                        var specialField = AccessTools.DeclaredField(entryType, "special");
                        var wasPet = AccessTools.DeclaredField(entryType, "WasPetYet");
                        if (friendshipField != null)
                        {
                            friendshipField.SetValue(entry, horseFriendship[horseId]);
                        }

                        // ✅ special 값 적용 (먹이 여부)
                        bool ateFood = horseAteFoodToday.ContainsKey(horseId) && horseAteFoodToday[horseId];
                        if (specialField != null)
                        {
                            specialField.SetValue(entry, ateFood ? 1 : 0);
                        }

                        if (wasPet != null)
                        {
                            wasPet.SetValue(entry, ateFood ? 2 : 0);
                        }
                    }
                }
            }
        }


        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // 아침마다 기력 회복
            maxHorseStamina = Config.MaxStamina;
            horseStamina = maxHorseStamina;


            foreach (var location in Game1.locations)
            {
                foreach (var character in location.characters)
                {
                    if (character is Horse horse)
                    {
                        int horseId = horse.id;

                        // ✅ 새로운 Horse가 생성되었는지 확인 후 초기화
                        horseFriendship[horseId] = 0; // 기본 호감도 설정

                        horseAteFoodToday[horseId] = false; // 하루 동안 먹이 주었는지 초기화
                    }
                }
            }

        }

        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            foreach (var location in Game1.locations)
            {
                foreach (var character in location.characters)
                {
                    if (character is Horse horse)
                    {
                        int horseId = horse.id;
                        if (!horseFriendship.ContainsKey(horse.id))
                        {
                            // ✅ 새로운 Horse가 생성되었는지 확인 후 초기화
                            horseFriendship[horseId] = 0; // 기본 호감도 설정
                        }
                        if (!(location is Farm) && location.characters.Contains(horse))
                        {
                            horseFriendship[horseId] = Config.FriendshipPerDayAfterInteraction * -2;
                        }
                        horseAteFoodToday[horseId] = false; // 하루 동안 먹이 주었는지 초기화
                    }
                }
            }

        }

        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            SpriteBatch spriteBatch = e.SpriteBatch;
            int hudX = 100, hudY = 300;
            // 배경 박스
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(hudX, hudY, 400, 80), Color.Black * 0.6f);

            // 기력 바 (녹색)
            int staminaBarWidth = (int)(400 * (horseStamina / (float)Config.MaxStamina)) - 20;
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(hudX + 10, hudY + 10, staminaBarWidth, 50), Color.LimeGreen);

            // 텍스트 (기력 / 호감도)
            spriteBatch.DrawString(Game1.smallFont, $"기력: {horseStamina:F0} / {Config.MaxStamina:F0}", new Vector2(hudX + 10, hudY - 20), Color.White);
        }

        /// <summary>
        /// 🐴 Animal Page (동물 관리 메뉴)에서 말 호감도 표시
        /// </summary>
        public void AnimalPagePostfix(object __instance, Character animal)
        {
            if (animal is Horse horse && horse != null)
            {
                int horseId = horse.id; // 말의 ID 가져오기
                                        // AnimalPage에서 변수 가져오기
                var animalPageType = __instance.GetType();
                var specialField = AccessTools.Field(animalPageType, "special");
                var friendshipLevelField = AccessTools.Field(animalPageType, "FriendshipLevel");

                // ✅ 우리가 설정한 horseAteFoodToday 값 적용
                bool ateFood = horseAteFoodToday[horseId];
                specialField.SetValue(animal, ateFood ? 1 : 0);

                // 호감도 값 설정
                if (horseFriendship.ContainsKey(horseId))
                {
                    friendshipLevelField.SetValue(animal, horseFriendship[horseId]);
                }
                else
                {
                    // 호감도가 등록되지 않은 말이라면 기본값 설정
                    horseFriendship[horseId] = 0; // 기본 호감도 (5칸)
                    friendshipLevelField.SetValue(animal, horseFriendship[horseId]);
                }
            }
        }


        public void PetHorse(Horse horse)
        {
            var i18n = Helper.Translation;
            int horseId = horse.id;
            if (!horseFriendship.ContainsKey(horseId))
                return;

            if (wasPetToday.ContainsKey(horseId) && wasPetToday[horseId])
            {
                return;
            }

            horseFriendship[horseId] = Math.Min(1000, horseFriendship[horseId] +
            Config.FriendshipPerDayAfterInteraction);
            wasPetToday[horseId] = true;
            Game1.showGlobalMessage(i18n.Get("Desc.Happy").Default("말이 기뻐합니다!"));
        }

        public void FeedHorse(Horse horse, StardewValley.Object food)
        {
            var i18n = Helper.Translation;
            int horseId = horse.id;
            if (!horseFriendship.ContainsKey(horseId))
                return;
            if (horseStamina >= maxHorseStamina)
            {
                Game1.showRedMessage(i18n.Get("Desc.Full").Default("말이 이미 배가 부릅니다!"));
                return;
            }
            string[] foodsId = { "Carrot", "SummerSquash", "Broccoli", "Powdermelon" };
            if (foodsId.Contains(food.ItemId))
            {
                horseStamina = Math.Min(maxHorseStamina, horseStamina + food.getHealth());
                horseFriendship[horseId] = Math.Min(1000, horseFriendship[horseId] + Config.FriendshipByFood);
                horseAteFoodToday[horseId] = true; // ✅ 먹이 주었음을 기록

                Game1.showGlobalMessage(i18n.Get("Desc.Delicious").Default("말이 맛있게 먹었습니다!"));
                Game1.player.removeItemFromInventory(food);
            }
            else
            {
                Game1.showRedMessage(i18n.Get("Desc.NotFood").Default("이건 말이 먹을 수 없는 음식입니다!"));
            }
        }

        public void ApplySpeedBuff()
        {
            var i18n = Helper.Translation;
            // ✅ 버프 효과 정의
            BuffEffects effects = new BuffEffects(); // 속도 +2
            effects.Speed.Value = 5;
            // ✅ 버프 생성
            Buff speedBuff = new Buff(
                id: "horse_speed_buff",
                source: "Horse Buff",
                displaySource: i18n.Get("Buff.Source").Default("속도 증가"),
                duration: 120, // 2분 동안 유지 (게임 시간 기준)
                effects: effects,
                isDebuff: false,
                displayName: i18n.Get("Buff.Name").Default("이동 속도 증가"),
                description: i18n.Get("Buff.Desc").Default("말을 타고 있는 동안 이동 속도가 증가합니다!")
            );

            // ✅ 플레이어에게 버프 적용
            Game1.player.applyBuff(speedBuff);
        }
        public void ApplySlowBuff()
        {
            var i18n = Helper.Translation;
            // ✅ 디버프 효과 정의
            BuffEffects effects = new BuffEffects(); // 속도 -2
            effects.Speed.Value = -1;
            // ✅ 버프 생성
            Buff slowBuff = new Buff(
                id: "horse_slow_debuff",
                source: "Slow Debuff",
                displaySource: i18n.Get("DeBuff.Source").Default("속도 감소"),
                duration: 120, // 2분 지속
                effects: effects,
                isDebuff: true, // 디버프 설정
                displayName: i18n.Get("DeBuff.Name").Default("이동 속도 감소"),
                description: i18n.Get("DeBuff.Desc").Default("이동 속도가 느려졌습니다!")
            );

            // ✅ 플레이어에게 디버프 적용
            Game1.player.applyBuff(slowBuff);
        }

    }

}
