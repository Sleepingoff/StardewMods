using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NPCDialogues
{
    public class DataManager
    {
        private static string DataPath;
        private static string UserPath;

        public static Dictionary<string, Dictionary<string, string>> npc_Dialogues = new();
        public static Dictionary<string, Dictionary<string, string>> npc_DialoguesByUser = new();

        public static void InitData(IModHelper helper)
        {
            DataPath = Path.Combine(helper.DirectoryPath, "dialogues_data.json");
            UserPath = Path.Combine(helper.DirectoryPath, "dialogues.json");
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
        public static void LoadData()
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
        public static string LoadFileContents(string filePath)
        {
            if (!File.Exists(filePath)) return string.Empty;

            return File.ReadAllText(filePath);
        }
        public static object GetDialogue(string npcName, string key)
        {
            if (npc_Dialogues.ContainsKey(npcName) && npc_Dialogues[npcName] is
            Dictionary<string, string> npcData)
            {
                return npcData.ContainsKey(key) ? npcData[key] : null;
            }
            return null;
        }
        public static (Dictionary<string, string>, Dictionary<string, string>) GetDialogues(string npcName)
        {
            LoadData();
            Dictionary<string, string> originDialogues = new();
            Dictionary<string, string> userDialogues = new();
            if (npc_Dialogues.ContainsKey(npcName))
            {
                originDialogues = npc_Dialogues[npcName];
            }
            if (npc_DialoguesByUser.ContainsKey(npcName))
            {
                userDialogues = npc_DialoguesByUser[npcName];
            }
            return (originDialogues, userDialogues);
        }

        public HashSet<string> GetDialogueKeys(string npcName)
        {
            if (npc_Dialogues.ContainsKey(npcName) && npc_Dialogues[npcName] is Dictionary<string, string> npcData)
            {
                return new HashSet<string>(npcData.Keys);
            }
            return new HashSet<string>();
        }

        public static void SaveOriginalDialogues()
        {
            string json = JsonConvert.SerializeObject(npc_Dialogues, Formatting.Indented);

            // 파일 저장
            File.WriteAllText(DataPath, json);
        }

        public static void SaveUserDialogues()
        {
            string json = JsonConvert.SerializeObject(npc_DialoguesByUser, Formatting.Indented);

            // 파일 저장
            File.WriteAllText(UserPath, json);
            LoadData();
        }
        protected static object ParseFileContents(string fileContents)
        {
            return string.IsNullOrWhiteSpace(fileContents)
                ? new Dictionary<string, Dictionary<string, string>>()
                : JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContents)
                  ?? new Dictionary<string, Dictionary<string, string>>();
        }

        public static void DeleteDialogue(string npcName, string key)
        {
            if (npc_DialoguesByUser.ContainsKey(npcName))
            {
                if (npc_DialoguesByUser[npcName].ContainsKey(key))
                {
                    npc_DialoguesByUser[npcName][key] = "";
                }
                else
                {
                    npc_DialoguesByUser[npcName].Add(key, "");
                }
            }
            else
            {
                npc_DialoguesByUser[npcName] = new() { { key, "" } };
            }

            SaveUserDialogues();

        }

        public static void ApplyDialogueToAll()
        {
            var characters = Utility.getAllCharacters();
            foreach (NPC character in characters)
            {
                if (!character.IsVillager) continue;
                if (npc_DialoguesByUser.ContainsKey(character.Name))
                    ApplyDialogueToNPC(character.Name, npc_DialoguesByUser[character.Name]);
            }
        }

        public static void ApplyDialogueToNPC(string npcName, Dictionary<string, string> dialogue)
        {
            NPC npc = Game1.getCharacterFromName(npcName);
            if (npc == null) { Console.WriteLine("cannot find NPC"); return; }
            foreach (var (key, value) in dialogue)
            {
                if (string.IsNullOrEmpty(value))
                {
                    npc.Dialogue.Remove(key);
                    Console.WriteLine($"remove dialogues({key}) / {npcName}");
                }
                else
                {
                    npc.Dialogue[key] = value;
                    Console.WriteLine($"apply dialogues({key}) / {npcName}");
                }
            }
        }

    }
}
