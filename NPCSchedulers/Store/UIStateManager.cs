
using Microsoft.Xna.Framework;
using NPCSchedulers.DATA;
using NPCSchedulers.Type;
using NPCSchedulers.UI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Network;

namespace NPCSchedulers.Store
{
    public class UIStateManager
    {
        #region field
        public bool IsSchedulePageOpen { get; private set; } = false;
        // 수정 모드 적용 여부
        public bool IsEditMode { get; private set; } = false;
        //현재 보여지는 List UI
        public string CurrentListUI { get; private set; } = "character";
        public NPC CurrentNPC { get; private set; } = null;

        //현재 수정 중인 스케줄 키
        public string ScheduleKey { get; private set; } = null;

        //현재 수정 중인 상세 스케줄 키
        public string EditedScheduleKey { get; private set; } = null;

        //유저가 수정한 스케줄 키 리스트
        public HashSet<string> EditedScheduleKeyList { get; }

        //현재 날짜를 기준으로 만들어진 스케줄 딕셔너리
        public ScheduleDataType ScheduleData { get; private set; } = null;

        public Dictionary<string, bool> mailCondition { get; private set; } = new();
        private Dictionary<string, FriendshipUIStateHandler> friendshipHandler;
        private Dictionary<string, MailUIStateHandler> mailHandler;
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
            mailHandler = new();



            InitScheduleData();

            //리스트마다 수행
            foreach (var key in ScheduleDataManager.GetFinalSchedule(CurrentNPC.Name).Keys)
            {
                friendshipHandler[key] = new FriendshipUIStateHandler(CurrentNPC.Name, key);
                mailHandler[key] = new MailUIStateHandler(CurrentNPC.Name, key);
                ScheduleData = ScheduleDataManager.GetAllScheduleByKey(npcName, key);
                if (ScheduleData.ContainsKey(key))
                {
                    mailHandler[key].SetGotoKey(ScheduleData[key].Item4);
                }
            }
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
            //SchedulePage.IsOpen == false이면 IsEditMode = false

            IsEditMode = scheduleKey != null && !IsEditMode;
            if (!SchedulePage.IsOpen) IsEditMode = false;



            if (IsEditMode && ScheduleKey != null)
            {
                EditedScheduleKey = scheduleKey;
                friendshipHandler[ScheduleKey].GetData();
            }
            else InitScheduleData();
        }

        public void ToggleListUI(string uiName = null)
        {
            CurrentListUI = uiName;
            //리스트마다 수행
            foreach (var key in ScheduleDataManager.GetFinalSchedule(CurrentNPC.Name).Keys)
            {
                friendshipHandler[key] = new FriendshipUIStateHandler(CurrentNPC.Name, key);
                mailHandler[key] = new MailUIStateHandler(CurrentNPC.Name, key);
            }
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

        public void InitDate()
        {
            dateHandler.InitData();
        }
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
        public Dictionary<string, int> GetFriendshipCondition(string scheduleKey = null)
        {
            return scheduleKey == null && ScheduleKey == null ? new() : friendshipHandler[scheduleKey ?? ScheduleKey].GetData();
        }

        //key: npcName, value: heartLevel
        public void SetFriendshipCondition(string name, int level)
        {
            if (ScheduleKey == null) return;
            Dictionary<string, int> data = new Dictionary<string, int> { { name, level } };
            friendshipHandler[ScheduleKey].UpdateData(data);
            SetScheduleData();
        }
        #endregion

        #region mail
        //v0.0.3 + 메일관련 UIStateManager 추가
        public Dictionary<string, bool> GetMailCondition(string scheduleKey = null)
        {
            return scheduleKey == null && ScheduleKey == null ? new() : mailHandler[scheduleKey ?? ScheduleKey].GetData();
        }
        public Dictionary<string, string> GetMailList(string scheduleKey = null)
        {
            return ScheduleKey == null ? new() : mailHandler[ScheduleKey].GetMailList();
        }

        public void SetMailCondition(string mailKey, bool condition)
        {
            if (ScheduleKey == null) return;
            Dictionary<string, bool> data = new Dictionary<string, bool> { { mailKey, condition } };
            mailHandler[ScheduleKey].UpdateData(data);
            SetScheduleData();
        }
        public bool GetReceivedMail(string mailKey)
        {
            return MailUIStateHandler.HasReceivedAllMail(mailKey);
        }

        public string GetGotoKey(string scheduleKey = null)
        {
            if (ScheduleKey == null || !ScheduleData.ContainsKey(scheduleKey ?? ScheduleKey)) return null;
            return mailHandler[scheduleKey ?? ScheduleKey].GetGotoKey() ?? ScheduleData[scheduleKey ?? ScheduleKey].Item4;
        }

        public void SetGotoKey(string newGotoKey)
        {
            if (ScheduleKey == null) return;
            mailHandler[ScheduleKey].SetGotoKey(newGotoKey);
            SetScheduleData();
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

            scheduleEntries.RemoveWhere(e => e.Key == entry.Key);


            SetScheduleDataByList(scheduleEntries);
        }

        #endregion
        //스케줄 관련

        public void InitScheduleData()
        {
            //? issue 생길 수도 있음 창이 닫혔을 때 날짜가 초기화되지 않아서 없는 키 찾을 가능성 있음
            // InitDate();
            var (season, date) = GetCurrentDate();
            ScheduleData = ScheduleDataManager.GetFilteredSchedule(CurrentNPC.Name, season, date);

        }

        public bool HasKeyInAllScheduleDataWithCurrentNPC(string key)
        {
            return ScheduleDataManager.GetAllScheduleKeys(CurrentNPC.Name).Contains(key);

        }



        /// <summary>
        /// 스케줄 리스트 반환
        /// </summary>

        public ScheduleDataType GetSchedule()
        {
            var (season, date) = GetCurrentDate();
            ScheduleData = ScheduleDataManager.GetFilteredSchedule(CurrentNPC.Name, season, date);
            return ScheduleData;
        }

        /// <summary>
        /// 스케줄 리스트 반환
        /// </summary>

        public List<ScheduleEntry> GetScheduleEntries(string scheduleKey = null)
        {
            if (ScheduleKey != null && ScheduleData.ContainsKey(scheduleKey ?? ScheduleKey))
            {
                // ✅ 새로운 리스트로 반환하여 원본 데이터 유지 방지
                return ScheduleData[scheduleKey ?? ScheduleKey].Item2;
            }
            return new List<ScheduleEntry>() { new ScheduleEntry(scheduleKey, 610, "Town", 0, 0, 2, "None", "None") };
        }

        /// <summary>
        /// 상세 스케줄 생성
        /// </summary>
        public ScheduleEntry CreateScheduleDataByKey(string scheduleDetailKey, ScheduleEntry data = null)
        {
            ScheduleEntry initData = data;
            if (initData == null)
            {
                initData = new ScheduleEntry(scheduleDetailKey, 610, "Town", 0, 0, 2, null, null);
            }

            return initData;
        }
        public void SetScheduleKey(string newScheduleKey)
        {
            ScheduleKey = newScheduleKey;
        }
        public void SetScheduleDataByEntry(ScheduleEntry newEntry, string scheduleKey = null)
        {
            ScheduleKey = scheduleKey ?? ScheduleKey;
            List<ScheduleEntry> scheduleEntries = GetScheduleEntries(scheduleKey);
            bool isIncludesSameTime = scheduleEntries.Any(entry => entry.Time == newEntry.Time);

            //v0.0.2 + 동시간대 기존 다른 스케줄을 지우기
            if (isIncludesSameTime)
            {
                var sameScheduleEntries = scheduleEntries.Where(entry => entry.Time == newEntry.Time).ToList();
                foreach (var entry in sameScheduleEntries)
                {
                    scheduleEntries.RemoveWhere(entry => entry.Time == newEntry.Time);
                }
            }
            //스케줄 추가 및 시간 순 정렬
            newEntry.Key += 99;
            scheduleEntries.Add(newEntry);
            scheduleEntries.Sort((a, b) => a.Time.CompareTo(b.Time));
            SetScheduleDataByList(scheduleEntries);
        }
        public void SetScheduleDataByList(List<ScheduleEntry> newEntries)
        {
            var friendship = GetFriendshipCondition();
            var friendshipEntry = new FriendshipConditionEntry(CurrentNPC.Name, ScheduleKey, friendship);
            var mail = GetMailCondition();
            var mailEntry = MailUIStateHandler.FilterData(mail).Select(m => m.Key).ToList();
            var gotoKey = GetGotoKey();

            // ✅ 기존 중복 시간(Time) 제거 로직 추가
            var filteredEntries = newEntries
                .GroupBy(entry => entry.Time) // 시간 기준으로 그룹화
                .Select(group => group.Last()) // 가장 최신(마지막) 데이터만 유지
                .OrderBy(entry => entry.Time) // 정렬 유지
                .ToList();
            ScheduleData[ScheduleKey] = (friendshipEntry, filteredEntries, mailEntry, gotoKey);
            ScheduleDataManager.SaveUserSchedule(CurrentNPC.Name, ScheduleKey, ScheduleData);
        }
        public void SetScheduleData()
        {
            var friendship = GetFriendshipCondition();
            var friendshipEntry = new FriendshipConditionEntry(CurrentNPC.Name, ScheduleKey, friendship);
            // 깊은 복사를 수행하여 기존 ScheduleEntry 객체를 수정하지 않도록 함
            if (!ScheduleData.ContainsKey(ScheduleKey)) ScheduleData.Add(ScheduleKey, new());
            var newEntries = ScheduleData[ScheduleKey].Item2
                .Select(entry => entry) // 생성자를 이용한 깊은 복사
                .ToList();

            var mail = GetMailCondition();
            var mailEntry = MailUIStateHandler.FilterData(mail).Select(m => m.Key).ToList();
            var gotoKey = GetGotoKey();
            ScheduleData[ScheduleKey] = (friendshipEntry, new List<ScheduleEntry>(newEntries), mailEntry, gotoKey);

            ScheduleDataManager.SaveUserSchedule(CurrentNPC.Name, ScheduleKey, ScheduleData);
        }
        public void SetScheduleDataByKey(string key, FriendshipConditionEntry friendshipConditionEntry = null, List<ScheduleEntry> newSchedule = null, List<string> mailEntry = null, string gotoKey = null)
        {

            if (friendshipConditionEntry == null)
            {
                friendshipConditionEntry = new FriendshipConditionEntry(CurrentNPC.Name, ScheduleKey, new());
            }

            if (newSchedule == null)
            {
                newSchedule = new();
            }

            if (mailEntry == null)
            {
                mailEntry = new();
            }
            if (!ScheduleData.ContainsKey(key))
            {
                ScheduleData.Add(key, (friendshipConditionEntry, newSchedule, mailEntry, gotoKey));
            }
            else
            {
                ScheduleData[key] = (friendshipConditionEntry, new List<ScheduleEntry>(newSchedule), mailEntry, gotoKey);
            }
            ScheduleDataManager.SaveUserSchedule(CurrentNPC.Name, key, ScheduleData);

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
