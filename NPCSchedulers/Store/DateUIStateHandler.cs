using System.Collections.Generic;

namespace NPCSchedulers.Store
{
    public class DateUIStateHandler : IUIStateHandler<int>
    {
        private static readonly List<string> seasons = new() { "Spring", "Summer", "Fall", "Winter" };

        public void LoadData()
        {
            // 이미 UIStateManager에서 상태를 유지하므로 별도 데이터 로딩 없음
        }

        public void SaveData(int date)
        {
            UIStateManager.Instance.SetSelectedDate(date);
        }

        public void UpdateData(int date)
        {
            UIStateManager.Instance.SetSelectedDate(date);
        }

        public void DeleteData(int date)
        {
            // 날짜를 삭제하는 개념은 없으므로 삭제 로직은 비워둠
        }

        public void ChangeSeason(int direction)
        {
            int index = seasons.IndexOf(UIStateManager.Instance.SelectedSeason);
            int nextIndex = (index + direction) % seasons.Count;
            if (nextIndex < 0) nextIndex += seasons.Count;
            UIStateManager.Instance.SetSelectedSeason(seasons[nextIndex]);
        }
    }
}
