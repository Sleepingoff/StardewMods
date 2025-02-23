using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace NPCDialogues
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var characters = Utility.getAllCharacters();
            foreach (var character in characters)
            {
                //todo : 캐릭터별 다이어로그 저장하기
                // character.Dialogue
            }
        }
    }
}
