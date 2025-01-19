using System;
using UnityEngine;
using UnityEditor;
using System.Globalization;
using System.IO;
using System.Text;

namespace net.koodaa.UnityDiary.Editor
{
    [InitializeOnLoad]
    internal class Diary
    {
        static readonly DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
        static Diary()
        {
            if (!UseDiary()) // Checking is the diary feature is enabled
                return;

            // Unregistering and registering the event to avoid multiple registration
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.quitting += OnEditorQuitting;
            EditorApplication.focusChanged -= OnEditorFocusChanged;
            EditorApplication.focusChanged += OnEditorFocusChanged;

            var currentWeekDate = DateTime.Today;
            Calendar cal = dfi.Calendar;
            int currentWeek = cal.GetWeekOfYear(currentWeekDate, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

            // Initializing the diary file if it does not exist
            InitFile(currentWeekDate);

            // Writing the open record
            WriteOpenRecord();
        }

        private static void OnEditorFocusChanged(bool obj)
        {
            var currentWeekDate = DateTime.Today;
            Calendar cal = dfi.Calendar;
            int currentWeek = cal.GetWeekOfYear(currentWeekDate, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            int previousWeekNumber = EditorPrefs.GetInt("DiaryWeekNumber", 0);
            if (previousWeekNumber != currentWeek)
            {
                // Ask if the user has written a diary record
                var result = EditorUtility.DisplayDialog("Diary", "Week has been changed\nDo you want to open diary?", "Yes", "No");
                if (result)
                {
                    // Open the diary record
                    EditorApplication.ExecuteMenuItem("Diary/Last week");
                }
                EditorPrefs.SetInt("DiaryWeekNumber", currentWeek);
            }
        }

        private static void OnEditorQuitting()
        {
            WriteCloseRecord();
        }

        [MenuItem("Diary/Current week", priority = 1)]
        public static void OpenDiary()
        {
            var date = DateTime.Today;
            string file = GetDiaryFilePath(date);
            if (!File.Exists(file))
                InitFile(date);
            OpenTextFile(file);
        }

        [MenuItem("Diary/Last week", priority = 10)]
        public static void OpenDiaryLastWeek()
        {
            var date = DateTime.Today.AddDays(-7);
            string file = GetDiaryFilePath(date);
            if (!File.Exists(file))
                InitFile(date);
            OpenTextFile(file);
        }

        [MenuItem("Diary/Open Diary folder", priority = 100)]
        public static void OpenDiaryFolder()
        {
            EditorUtility.RevealInFinder(GetDiaryFilePath(DateTime.Today));
        }

        private static void InitFile(DateTime date)
        {
            string diaryFile = GetDiaryFilePath(date);
            if (File.Exists(diaryFile))
                return;

            Calendar cal = dfi.Calendar;
            int week = cal.GetWeekOfYear(date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

            StringBuilder sb = new StringBuilder();
            var monday = GetLastMonday(date);
            var sunday = GetNextSunday(date);

            sb.AppendLine("# " + monday.ToString("yyyy-MM-dd") + " - " + sunday.ToString("yyyy-MM-dd"));
            sb.AppendLine("Week " + week);
            sb.AppendLine(Application.companyName + " - " + Application.productName);
            sb.AppendLine("---");
            sb.AppendLine("");

            File.WriteAllText(diaryFile, sb.ToString());
        }

        private static void WriteOpenRecord()
        {
            if (SessionState.GetBool("DiaryOpened", false))
                return;
            SessionState.SetBool("DiaryOpened", true);

            string diaryFile = GetDiaryFilePath(DateTime.Now);
            string record = Environment.NewLine + "- " + DateTime.Now.ToString("g") + " - Unity Opened";
            File.AppendAllText(diaryFile, record);
        }

        private static void WriteCloseRecord()
        {
            string diaryFile = GetDiaryFilePath(DateTime.Now);
            string record = Environment.NewLine + "- " + DateTime.Now.ToString("g") + " - Unity Closed";
            File.AppendAllText(diaryFile, record);
        }

        /// <summary>
        /// Checking if the diary feature is enabled
        /// </summary>
        /// <returns></returns>
        private static bool UseDiary()
        {
            if (EditorPrefs.HasKey("DiaryUse"))
            {
                return EditorPrefs.GetBool("DiaryUse");
            }
            else
            {
                var result = EditorUtility.DisplayDialog("Diary", "Do you want to use diary?", "Yes", "No");
                EditorPrefs.SetBool("DiaryUse", result);
                return result;
            }
        }

        public static string GetDiaryFilePath(DateTime date)
        {
            Calendar cal = dfi.Calendar;
            int week = cal.GetWeekOfYear(date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

            // Get os documents folder
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // Getting the company and product name
            string company = Application.companyName.Replace(":", "-");
            string product = Application.productName.Replace(":", "-");

            // Building the folder path and filename
            string folder = Path.Combine(documentsPath, Application.companyName, Application.productName);
            string filename = "Diary " + date.Year.ToString() + "w" + week.ToString() + ".txt";

            Directory.CreateDirectory(folder);
            return Path.Combine(folder, filename);
        }

        public static void OpenTextFile(string file)
        {
            if (!File.Exists(file))
            {
                File.Create(file).Dispose();
            }
            EditorUtility.OpenWithDefaultApp(file);
        }

        static DateTime GetLastMonday(DateTime date)
        {
            int daysToSubtract = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysToSubtract < 0)
            {
                daysToSubtract += 7; // Wrap around to the previous Monday
            }
            return date.AddDays(-daysToSubtract);
        }

        static DateTime GetNextSunday(DateTime date)
        {
            int daysToAdd = (int)DayOfWeek.Sunday - (int)date.DayOfWeek;
            if (daysToAdd < 0)
            {
                daysToAdd += 7; // Wrap around to the next Sunday
            }
            return date.AddDays(daysToAdd);
        }
    }
}
