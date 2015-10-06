using System.Windows.Forms;

namespace StopWatchingMe
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            Application.Run(new MainForm());
            Application.Exit();
        }
    }
}
