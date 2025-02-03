namespace NPCSchedulers.DATA
{
    public class NPCScheduleData
    {
        /// <summary>
        /// 스케줄 키를 분류한 데이터 (예: Unknown, Date, Day of Week 등)
        /// </summary>
        public Dictionary<string, List<string>> ScheduleKeys { get; set; }

        /// <summary>
        /// NPC의 원본 스케줄 데이터 (시간과 이동 경로 포함)
        /// </summary>
        public Dictionary<string, string> RawData { get; set; }

        /// <summary>
        /// 기본 생성자: Dictionary를 초기화하여 NPE 방지
        /// </summary>
        public NPCScheduleData()
        {
            ScheduleKeys = new Dictionary<string, List<string>>();
            RawData = new Dictionary<string, string>();
        }
    }

}