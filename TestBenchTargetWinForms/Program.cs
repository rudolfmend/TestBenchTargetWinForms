using System;
using System.Threading;
using System.Windows.Forms;
using System.Globalization;

namespace TestBenchTargetWinForms
{
    internal static class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

        private static string mutexName = "com.rudolfmendzezof.testbenchtarget";
        private static Mutex mutex;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Nastavenie anglickej kultúry globálne pre všetky vlákna
            CultureInfo ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = ci;
            Thread.CurrentThread.CurrentCulture = ci;

            // Nastavenie predvolenej kultúry pre všetky nové vlákna
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;

            bool createdNew;
            mutex = new Mutex(true, mutexName, out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("The application is already running.", "TestBench Target", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                SetProcessDpiAwarenessContext(-4);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false); 
                Application.Run(new Form1()); // here application ends 
            }
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }
        }
    }
}
