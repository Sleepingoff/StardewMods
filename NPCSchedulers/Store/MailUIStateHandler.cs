using System.Collections.Generic;
using NPCSchedulers.DATA;
using StardewValley;

namespace NPCSchedulers.Store
{
    public class MailUIStateHandler : BaseUIStateHandler<Dictionary<string, bool>>
    {
        private Dictionary<string, bool> mailConditions;
        private List<string> mailKeys = new();
        private static Dictionary<string, string> mails = new();

        public MailUIStateHandler(string npcName, string scheduleKey) : base(npcName, scheduleKey)
        {
            InitData();
        }

        public override void InitData()
        {
            if (scheduleKey == null) return;
            mails = DataLoader.Mail(Game1.content);
            mailKeys = ScheduleDataManager.GetMailList(npcName, scheduleKey);
            mailConditions = mails.ToDictionary(pair => pair.Key, pair => mailKeys.Contains(pair.Key));

            Console.WriteLine(mailKeys.Count);
            foreach (var mailKey in mailKeys)
            {
                if (mailConditions.ContainsKey(mailKey))
                {
                    mailConditions.Remove(mailKey);
                    mailConditions.Add(mailKey, true);
                }
                else
                {
                    mailConditions.Add(mailKey, true);
                    mails.Add(mailKey, "");
                }
            }
        }
        public override Dictionary<string, bool> GetData()
        {
            mailKeys = ScheduleDataManager.GetMailList(npcName, scheduleKey);

            Console.WriteLine(mailKeys.Count);
            foreach (var mailKey in mailKeys)
            {
                if (mailConditions.ContainsKey(mailKey))
                {
                    mailConditions[mailKey] = true;
                }
                else
                {
                    mailConditions.Add(mailKey, true);
                }
            }
            return mailConditions;
        }

        public override void SaveData(Dictionary<string, bool> data)
        {
            mailConditions = data;
        }

        public override void UpdateData(Dictionary<string, bool> data)
        {
            var conditions = mailConditions;
            foreach (var key in conditions.Keys)
            {
                if (data.ContainsKey(key))
                {
                    bool newMail = data[key];

                    conditions[key] = newMail;

                }
            }
            SaveData(conditions);
        }

        public override void DeleteData(Dictionary<string, bool> data)
        {
            foreach (var mail in data.Keys)
            {
                mailConditions.Remove(mail);
            }
        }
        public static Dictionary<string, bool> FilterData(Dictionary<string, bool> condition)
        {
            Dictionary<string, bool> newMailCondition = condition;
            var target = newMailCondition.Where(value => value.Value);
            newMailCondition = target.ToDictionary(pair => pair.Key, pair => pair.Value);
            return newMailCondition;
        }

        public Dictionary<string, string> GetMailList()
        {
            mailKeys = ScheduleDataManager.GetMailList(npcName, scheduleKey);

            foreach (string key in mailKeys)
            {
                if (mails.ContainsKey(key)) continue;
                mails.Add(key, "");
            }
            // value가 true인 것 우선 정렬
            mails = mails.OrderByDescending(mail => mailConditions.ContainsKey(mail.Key) && mailConditions[mail.Key])
                         .ThenBy(mail => mail.Key)
                         .ToDictionary(pair => pair.Key, pair => pair.Value);
            return mails;
        }
    }
}
