using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System;
using System.Globalization;
using static HolidayCalendarAPI.HolidayCalendarAPI;

namespace HolidayCalendarAPI
{
    public class HolidayCalendarAPI
    {
        const int max_length = 2000;

        public string FunctionName => nameof(HolidayCalendarAPI);// API名
        public string Description => "Create a calendar with holiday information for the specified period.";// APIの説明（LLMが理解できる内容）
        public object Function
        {
            get
            {
                return new
                {
                    name = FunctionName,// API名
                    description = Description,// APIの説明
                    parameters = new
                    {
                        type = "object",// object
                        properties = new
                        {
                            // 必要な引数を列挙する
                            startDate = new
                            {
                                type = "string",// 引数の型
                                description = "start date (yyyy-mm-dd)"// 引数の説明（LLMが理解できる内容）
                            },
                            endDate = new
                            {
                                type = "string",// 引数の型
                                description = "end date (yyyy-mm-dd)"// 引数の説明（LLMが理解できる内容）
                            },
                        },
                        required = new[] { "startDate", "endDate" }// 最低限必要な引数
                    },
                };
            }
        }

        public async Task<string> ExecAsync(Action<string> addContent, string param, Func<string, bool> confirm)
        {
            string result;
            var paramData = JsonConvert.DeserializeObject<dynamic>(param);
            if (paramData == null)
            {
                result = "param error.";
                addContent(result);// LLMに送る
            }
            else
            {
                result = "**Get calendar...**";// 処理中メッセージ
                if (param != null)
                {
                    try
                    {
                        string calendarString = "";

                        string startDateString = paramData.startDate;
                        string endDateString = paramData.endDate;

                        startDateString = startDateString.Trim();
                        endDateString = endDateString.Trim();

                        bool startParseSuccess = DateTime.TryParse(startDateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDate);
                        bool endParseSuccess = DateTime.TryParse(endDateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDate);

                        if (startParseSuccess && endParseSuccess)
                        {
                            var calendar = await GenerateCalendarAsync(startDate, endDate);

                            foreach (var dayInfo in calendar)
                            {
                                calendarString += $"Date: {dayInfo.Date}({dayInfo.DayOfWeek}), Holiday: {dayInfo.Holiday}  " + Environment.NewLine;
                            }
                            if (calendarString.Length > max_length)
                            {
                                calendarString = calendarString.ToString().Substring(0, max_length);
                            }

                            calendarString += "**Saturdays and Sundays are non-business days (holidays).**";
                        }
                        else
                        {
                            if (!startParseSuccess)
                            {
                                calendarString += "Failed to parse start date.";
                            }
                            if (!endParseSuccess)
                            {
                                calendarString += "Failed to parse end date.";
                            }
                        }

                        addContent(calendarString);// LLMに送る
                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                        addContent(result);// LLMに送る
                    }
                }
            }
            return result;
        }

        public class DayInfo
        {
            public string? Date { get; set; }
            public string? DayOfWeek { get; set; }
            public string? Holiday { get; set; }
        }

        static async Task<List<DayInfo>> GenerateCalendarAsync(DateTime startDate, DateTime endDate)
        {
            HttpClient httpClient = new HttpClient();
            var allHolidays = new Dictionary<string, string>();

            for (int year = startDate.Year; year <= endDate.Year; year++)
            {
                string url = $"https://holidays-jp.github.io/api/v1/{year}/date.json";
                string jsonResponse = await httpClient.GetStringAsync(url);
                var holidays = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);
                if (holidays != null)
                {
                    allHolidays = allHolidays.Concat(holidays).ToDictionary(x => x.Key, x => x.Value);
                }
            }

            List<DayInfo> calendar = new List<DayInfo>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayInfo = new DayInfo
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    DayOfWeek = date.DayOfWeek.ToString()
                };

                if (date.DayOfWeek == DayOfWeek.Saturday)
                {
                    dayInfo.Holiday = "土曜日";
                }
                else if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    dayInfo.Holiday = "日曜日";
                }

                if (allHolidays.ContainsKey(date.ToString("yyyy-MM-dd")))
                {
                    dayInfo.Holiday = allHolidays[date.ToString("yyyy-MM-dd")];
                }

                calendar.Add(dayInfo);
            }

            return calendar;
        }
    }
}