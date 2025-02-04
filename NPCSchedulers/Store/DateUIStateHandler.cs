using System.Collections.Generic;

namespace NPCSchedulers.Store
{
    public class DateUIStateHandler : BaseUIStateHandler<(string, int)>
    {
        private static readonly List<string> seasons = new() { "Spring", "Summer", "Fall", "Winter", "Rain", "Festival" };
        private string selectedSeason = "Spring";
        private int selectedDate = 1;

        public DateUIStateHandler(string npcName, string scheduleKey) : base(npcName, scheduleKey)
        {
        }
        public override void InitData()
        {
            selectedSeason = "Spring";
            selectedDate = 1;
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
            var (season, date) = data;
            int currentIndex = seasons.IndexOf(selectedSeason);
            int index = seasons.IndexOf(season);
            int direction = index - currentIndex;
            int nextIndex = (index + direction) % seasons.Count;
            if (nextIndex < 0) nextIndex += seasons.Count;

            SaveData((seasons[nextIndex], date));
        }

        public override void DeleteData((string, int) data)
        {
            // 날짜를 삭제하는 개념은 없으므로 삭제 로직은 비워둠
        }


    }
}
