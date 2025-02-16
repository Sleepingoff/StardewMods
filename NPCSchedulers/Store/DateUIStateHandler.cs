using System.Collections.Generic;
using StardewValley;

namespace NPCSchedulers.Store
{
    public class DateUIStateHandler : BaseUIStateHandler<(string, int)>
    {
        public static readonly List<string> seasons = new() { "Spring", "Summer", "Fall", "Winter", "Rain", "Festival" };
        public static string selectedSeason = "Spring";
        private int selectedDate = 1;

        public DateUIStateHandler(string npcName, string scheduleKey) : base(npcName, scheduleKey)
        {
            InitData();
        }
        public override void InitData()
        {
            selectedSeason = Game1.currentSeason;
            selectedDate = Game1.dayOfMonth;
        }
        public override (string, int) GetData()
        {
            return (selectedSeason, selectedDate);
        }

        public override void SaveData((string, int) data)
        {
            var (season, date) = data;
            selectedSeason = season;
            selectedDate = date;
        }

        public override void UpdateData((string, int) data)
        {
            var (seasonDirection, date) = data;

            int direction = 0;
            switch (seasonDirection)
            {
                case "next":
                    direction = 1; break;
                case "prev":
                    direction = -1; break;
                default: break;
            }
            int currentIndex = seasons.IndexOf(selectedSeason);
            int nextIndex = (currentIndex + direction) % seasons.Count;
            if (nextIndex < 0) nextIndex += seasons.Count;

            SaveData((seasons[nextIndex], date));
        }

        public override void DeleteData((string, int) data)
        {
            // 날짜를 삭제하는 개념은 없으므로 삭제 로직은 비워둠
        }
        // 🔹 요일 계산
        public static string CalculateDayOfWeek(int date)
        {
            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            return days[(date - 1) % 7]; // 1일부터 시작하므로 (date - 1)
        }


    }
}
