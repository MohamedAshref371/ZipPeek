using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ZipPeek
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += (sender, e) => LogError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => LogError((Exception)e.ExceptionObject);

            try
            {
                Mutex mutex = new Mutex(true, Path.GetFullPath("Download").Replace(":", "").Replace("\\", ""), out bool createdNew);
                if (createdNew)
                {
                    Application.Run(new Form1());
                    mutex.ReleaseMutex();
                }
                else MessageBox.Show("You have already opened the program.");
            }
            catch (Exception ex)
            {
                LogError(ex);
                MessageBox.Show("An unexpected error occurred; the program will close.");
            }
        }

        public static void LogError(Exception ex, bool inTryCatch = false)
            => File.AppendAllText("Errors.txt", $"{DateTime.Now}{(inTryCatch ? "  -  Inside Custom Try-Catch Block" : "")}\n{ex.Message}\n{ex.StackTrace}\n------------------\n\n");
    }
}
