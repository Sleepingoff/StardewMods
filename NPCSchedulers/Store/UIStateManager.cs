
using Microsoft.Xna.Framework;
using NPCSchedulers.DATA;
using NPCSchedulers.Type;
using StardewValley;

namespace NPCSchedulers.Store
{
    public class UIStateManager
    {
        #region field
        public bool IsSchedulePageOpen { get; private set; } = false;
        public bool IsEditMode { get; private set; } = false;
        public NPC CurrentNPC { get; private set; } = null;

        //현재 수정 중인 스케줄 키
        public string ScheduleKey { get; private set; } = null;

        //현재 수정 중인 상세 스케줄 키
        public string EditedScheduleKey { get; private set; } = null;

        //유저가 수정한 스케줄 키 리스트
        public HashSet<string> EditedScheduleKeyList { get; }

        //현재 날짜를 기준으로 만들어진 스케줄 딕셔너리
        public ScheduleDataType ScheduleData { get; private set; } = null;
        private Dictionary<string, FriendshipUIStateHandler> friendshipHandler;
        private readonly DateUIStateHandler dateHandler;
        private string filter { get; set; } = "all";
        #endregion
        #region .ctor
        public UIStateManager(string npcName)
        {
            NPC npc = Game1.getCharacterFromName(npcName);
            SetCurrentNpc(npc);

            dateHandler = new DateUIStateHandler(CurrentNPC.Name, ScheduleKey);
            // 🔥 scheduleKey마다 friendshipHandler를 개별적으로 관리해야 함
            friendshipHandler = new Dictionary<string, FriendshipUIStateHandler>();

            foreach (var key in ScheduleDataManager.GetFinalSchedule(CurrentNPC.Name).Keys)
            {
                friendshipHandler[key] = new FriendshipUIStateHandler(CurrentNPC.Name, key);
            }
            InitScheduleData();
        }
        #endregion
        public static Rectangle GetMenuPosition()
        {
            var currentMenu = Game1.activeClickableMenu;
            if (currentMenu == null) return new Rectangle(0, 0, 0, 0);
            return new Rectangle(currentMenu.xPositionOnScreen, currentMenu.yPositionOnScreen, currentMenu.width, currentMenu.height);
        }

        // 🔹 스케줄 페이지 열고 닫기
        public void ToggleEditMode(string scheduleKey = null)
        {
            IsEditMode = scheduleKey != null;
            EditedScheduleKey = IsEditMode ? scheduleKey : null;
            if (IsEditMode && ScheduleKey != null)
                friendshipHandler[ScheduleKey].GetData();
            else InitScheduleData();
        }
        public void ToggleScheduleVersion()
        {
            filter = filter == "all" ? "user" : filter == "user" ? "origin" : filter == "origin" ? "all" : "user";
            var (season, date) = GetCurrentDate();
            ScheduleData = ScheduleDataManager.GetFilteredSchedule(CurrentNPC.Name, season, date, filter);
        }
        public string GetCurrentFilter()
        {
            return filter;
        }

        #region  npc
        public NPC GetCurrentNpc()
        {
            return CurrentNPC;
        }

        //각 npc 별로 UIStateManager를 새로 생성하는 방식이므로 현재 npc를 설정하는 함수를 public에서 private로 변경
        private void SetCurrentNpc(NPC npc)
        {
            CurrentNPC = npc;
        }

        #endregion

        #region  date
        //string: season ex) Spring, int: date ex) 1
        public (string, int) GetCurrentDate()
        {
            return dateHandler.GetData();
        }
        //string: season ex) Spring, int: date ex) 1
        public void SetCurrentDate((int, int) data)
        {
            var (direction, date) = data;

            string seasonDirection = "current";
            switch (direction)
            {
                case -1:
                    seasonDirection = "prev"; break;

                case 1:

                    seasonDirection = "next"; break;

                default: break;

            }
            dateHandler.UpdateData((seasonDirection, date));
            InitScheduleData();
        }
        #endregion

        #region friendship

        //key: npcName, value: heartLevel
        public Dictionary<string, int> GetFriendshipCondition()
        {
            if (ScheduleKey == null) return new();
            return friendshipHandler[ScheduleKey].GetData();
        }

        //key: npcName, value: heartLevel
        public void SetFriendshipCondition(string name, int level)
        {
            if (ScheduleKey == null) return;
            Dictionary<string, int> data = new Dictionary<string, int> { { name, level } };
            friendshipHandler[ScheduleKey].UpdateData(data);
            InitScheduleData();
        }
        #endregion
        #region  schedule

        #region  edit
        public HashSet<string> GetEditedScheduleKeyList()
        {
            return ScheduleDataManager.GetEditedScheduleKeys(CurrentNPC.Name);
        }

        public void DeleteScheduleEntry(string scheduleKey, ScheduleEntry entry)
        {
            List<ScheduleEntry> scheduleEntries = GetScheduleEntries(scheduleKey);

            scheduleEntries.Remove(entry);

            SetScheduleDataByList(scheduleEntries);
        }

        #endregion
        //스케줄 관련

        public void InitScheduleData()
        {
            var (season, date) = GetCurrentDate();
            ScheduleData = ScheduleDataManager.GetFilteredSchedule(CurrentNPC.Name, season, date);
        }
        /// <summary>
        /// 스케줄 리스트 반환
        /// </summary>

        public ScheduleDataType GetSchedule()
        {
            return ScheduleData;
        }

        /// <summary>
        /// 스케줄 리스트 반환
        /// </summary>

        public List<ScheduleEntry> GetScheduleEntries(string key = null)
        {
            return ScheduleData[key ?? ScheduleKey].Item2;
        }

        /// <summary>
        /// 상세 스케줄 생성
        /// </summary>
        public ScheduleEntry CreateScheduleDataByKey(string scheduleDetailKey, ScheduleEntry data = null)
        {
            ScheduleEntry initData = data;
            if (initData == null)
            {
                initData = new ScheduleEntry(scheduleDetailKey, 610, "Town", 0, 0, 2, "None", "None");
            }

            return initData;
        }
        public void SetScheduleKey(string newScheduleKey)
        {
            ScheduleKey = newScheduleKey;
            GetFriendshipCondition();
        }
        public void SetScheduleDataByEntry(ScheduleEntry newEntry, string scheduleKey = null)
        {
            ScheduleKey = scheduleKey;
            List<ScheduleEntry> scheduleEntries = GetScheduleEntries(scheduleKey);
            bool isIncludesSameTime = scheduleEntries.Any(entry => entry.Time == newEntry.Time);

            //v0.0.2 + 동시간대 기존 다른 스케줄을 지우기
            if (isIncludesSameTime)
            {
                var sameScheduleEntry = scheduleEntries.Where(entry => entry.Time == newEntry.Time);
                foreach (var entry in sameScheduleEntry)
                {
                    scheduleEntries.Remove(entry);
                }
            }
            //스케줄 추가 및 시간 순 정렬
            scheduleEntries.Add(newEntry);
            scheduleEntries.Sort((a, b) => a.Time.CompareTo(b.Time));
            SetScheduleDataByList(scheduleEntries);
        }
        public void SetScheduleDataByList(List<ScheduleEntry> newEntries)
        {
            var friendship = GetFriendshipCondition();
            var friendshipEntry = new FriendshipConditionEntry(CurrentNPC.Name, ScheduleKey, friendship);
            ScheduleData[ScheduleKey] = (friendshipEntry, newEntries);
            InitScheduleData();
            ScheduleDataManager.SaveUserSchedule(CurrentNPC.Name, ScheduleKey, friendshipEntry, newEntries);
        }

        public void SetScheduleDataByKey(string key, FriendshipConditionEntry friendshipConditionEntry = null, List<ScheduleEntry> newSchedule = null)
        {

            if (friendshipConditionEntry == null)
            {
                friendshipConditionEntry = new FriendshipConditionEntry(CurrentNPC.Name, ScheduleKey, new());
            }

            if (newSchedule == null)
            {
                newSchedule = new();
            }

            if (!ScheduleData.ContainsKey(key))
            {
                ScheduleData.Add(key, (friendshipConditionEntry, newSchedule));
            }
            else
            {
                ScheduleData[key] = (friendshipConditionEntry, newSchedule);
            }
            InitScheduleData();
            ScheduleDataManager.SaveUserSchedule(CurrentNPC.Name, key, friendshipConditionEntry, newSchedule);

        }


        /// <summary>
        /// 상세 스케줄 하나를 반환
        /// </summary>
        public ScheduleEntry GetScheduleDetailByKey(string scheduleDetailKey)
        {
            ScheduleEntry target = null;

            List<ScheduleEntry> entries = GetScheduleEntries();
            foreach (ScheduleEntry entry in entries)
            {
                if (entry.Key == scheduleDetailKey)
                {
                    target = entry;
                    break;
                }
            }
            return target;
        }
        /// <summary>
        /// 상세 스케줄 하나를 수정
        /// 시간이 다르면 새로운 스케줄로 등록
        /// 시간이 같으면 기존 스케줄 수정
        /// </summary>
        /// <param name="scheduleDetailKey">기존 상세 키</param>
        /// <param name="data">새로운 데이터</param>
        public void SetScheduleDetailByKey(string scheduleDetailKey, ScheduleEntry data)
        {
            ScheduleEntry detailSchedule = GetScheduleDetailByKey(scheduleDetailKey);
            List<ScheduleEntry> scheduleEntries = GetScheduleEntries();
            if (detailSchedule != null && detailSchedule.Time == data.Time)
            {
                scheduleEntries.Remove(detailSchedule);
            }
            scheduleEntries.Add(data);
            SetScheduleDataByList(scheduleEntries);
        }
        #endregion
    }
}
