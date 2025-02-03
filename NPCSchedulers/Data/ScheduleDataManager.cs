using System.Collections.Generic;

namespace NPCSchedulers.DATA
{
    public class ScheduleDataManager
    {
        private static OriginalScheduleData originalSchedule = new OriginalScheduleData();
        private static UserScheduleData userSchedule = new UserScheduleData();

        /// <summary>
        /// 두 개의 데이터를 로드
        /// </summary>
        public static void LoadAllSchedules()
        {
            originalSchedule.LoadData();
            userSchedule.LoadData();
        }

        /// <summary>
        /// 기존 데이터와 유저 데이터 비교하여 편집 중인 키 찾기
        /// </summary>
        public static HashSet<string> GetEditedScheduleKeys(string npcName)
        {
            HashSet<string> editedKeys = new HashSet<string>();
            HashSet<string> originalKeys = originalSchedule.GetScheduleKeys(npcName);
            HashSet<string> userKeys = userSchedule.GetScheduleKeys(npcName);

            foreach (string key in userKeys)
            {
                if (originalKeys.Contains(key))
                {
                    editedKeys.Add(key);
                }
            }
            return editedKeys;
        }
    }
}
