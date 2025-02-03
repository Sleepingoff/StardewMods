using Microsoft.Xna.Framework;
using StardewValley.Menus;
namespace NPCSchedulers.DATA
{


    public class ScheduleEntry
    {
        public int Time;  // 🔥 속성이 아닌 필드로 변경
        public string Key;
        public string Location;
        public int X;
        public int Y;
        public int Direction;
        public string Action;
        public string Talk;
        private ClickableComponent bounds; // 🔹 필드로 변경

        public ClickableComponent Bounds => bounds; // 🔹 읽기 전용 속성 유지

        public ScheduleEntry(string key, int time, string location, int x, int y, int direction, string action, string talk)
        {
            Key = key;
            Time = time;
            Location = location;
            X = x;
            Y = y;
            Direction = direction;
            Action = action;
            Talk = talk;
            // 🔹 기본값으로 빈 영역 설정 (추후 UI에서 변경)
            bounds = new ClickableComponent(new Rectangle(0, 0, 0, 0), "");
        }

        // 🔹 UI에서 위치를 동기화하기 위한 메서드
        public void SetBounds(int x, int y, int width, int height)
        {
            bounds.bounds.X = x;
            bounds.bounds.Y = y;
            bounds.bounds.Width = width;
            bounds.bounds.Height = height;

        }

        // 🔹 클릭된 좌표가 현재 스케줄 영역에 포함되는지 확인
        public bool Contains(int x, int y)
        {
            return bounds.bounds.Contains(x, y);
        }

        // 🔹 데이터 수정 메서드 추가
        public void SetTime(int newTime) => Time = newTime;
        public void SetLocation(string newLocation) => Location = newLocation;
        public void SetCoordinates(int newX, int newY)
        {
            X = newX;
            Y = newY;
        }
        public void SetDirection(int newDirection) => Direction = newDirection;
        public void SetAction(string newAction) => Action = newAction;
        public void SetTalk(string talk) => Talk = talk;
    }
}


