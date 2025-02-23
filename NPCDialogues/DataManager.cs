using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;

namespace NPCDialogues
{
    public class DataManager
    {
        private string DataPath;
        private string UserPath;
        private IModHelper Helper;

        public Dictionary<string, Dictionary<string, string>> npc_Dialogues = new();
        public Dictionary<string, Dictionary<string, string>> npc_DialoguesByUser = new();
        public DataManager(IModHelper helper)
        {
            Helper = helper;
            DataPath = Path.Combine(helper.DirectoryPath, "dialogues_data.json");
            UserPath = Path.Combine(helper.DirectoryPath, "dialogues.json");
            InitData();
        }
        public void InitData()
        {
            npc_Dialogues.Clear();

            var characters = Utility.getAllCharacters();
            foreach (NPC character in characters)
            {
                if (!character.IsVillager) continue;
                if (npc_Dialogues.ContainsKey(character.Name))
                {
                    npc_Dialogues[character.Name] = character.Dialogue;
                }
                else
                {
                    npc_Dialogues.Add(character.Name, character.Dialogue);
                }
            }

            SaveOriginalDialogues();
        }
        public void LoadData()
        {

            npc_DialoguesByUser.Clear();

            string originContents = LoadFileContents(DataPath);
            var parsedOriginData = ParseFileContents(originContents) as Dictionary<string, Dictionary<string, string>>;
            if (parsedOriginData != null)
            {
                foreach (var npcEntry in parsedOriginData)
                {
                    npc_Dialogues[npcEntry.Key] = npcEntry.Value;
                }
            }

            string fileContents = LoadFileContents(UserPath);
            var parsedData = ParseFileContents(fileContents) as Dictionary<string, Dictionary<string, string>>;

            if (parsedData != null)
            {
                foreach (var npcEntry in parsedData)
                {
                    npc_DialoguesByUser[npcEntry.Key] = npcEntry.Value;
                }
            }
        }
        protected static string LoadFileContents(string filePath)
        {
            if (!File.Exists(filePath)) return string.Empty;

            return File.ReadAllText(filePath);
        }
        public object GetDialogue(string npcName, string key)
        {
            if (npc_Dialogues.ContainsKey(npcName) && npc_Dialogues[npcName] is
            Dictionary<string, string> npcData)
            {
                return npcData.ContainsKey(key) ? npcData[key] : null;
            }
            return null;
        }

        public HashSet<string> GetDialogueKeys(string npcName)
        {
            if (npc_Dialogues.ContainsKey(npcName) && npc_Dialogues[npcName] is Dictionary<string, string> npcData)
            {
                return new HashSet<string>(npcData.Keys);
            }
            return new HashSet<string>();
        }

        public void SaveOriginalDialogues()
        {
            string json = JsonConvert.SerializeObject(npc_Dialogues, Formatting.Indented);

            // 파일 저장
            File.WriteAllText(DataPath, json);
        }

        public void SaveUserDialogues()
        {
            string json = JsonConvert.SerializeObject(npc_DialoguesByUser, Formatting.Indented);

            // 파일 저장
            File.WriteAllText(UserPath, json);
        }
        protected object ParseFileContents(string fileContents)
        {
            return string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, Dictionary<string, string>>()
                : JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContents)
                  ?? new Dictionary<string, Dictionary<string, string>>();
        }
    }
}