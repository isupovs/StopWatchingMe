using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace StopWatchingMe
{
    public partial class MainForm : Form
    {
        private readonly List<Process> _processes = new List<Process>();
        private bool _alreadyConnected;
        private readonly Thread _hidingThread;
        private bool _isHiding;
        private IntPtr _selectedWindowHandler = IntPtr.Zero;

        public MainForm()
        {
            InitializeComponent();
            _hidingThread = new Thread(StartHiding);
            notifyIcon.Icon = Icon;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var processes = Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).ToList();
            _processes.AddRange(processes);
            processesListBox.DisplayMember = "MainWindowTitle";
            processesListBox.ValueMember = "MainWindowHandle";
            processesListBox.DataSource = _processes;
        }

        private void StartHiding()
        {
            while (_isHiding)
            {
                if (IsPortInUse(5900))
                {
                    if (!_alreadyConnected)
                    {
                        Console.WriteLine("Connected");
                        if (_selectedWindowHandler != IntPtr.Zero)
                        {
                            ShowWindow(_selectedWindowHandler, ShowWindowCommands.ShowMaximized);
                            SetForegroundWindow(_selectedWindowHandler);
                        }
                        _alreadyConnected = true;
                    }
                }
                else
                {
                    if (_alreadyConnected)
                    {
                        Console.WriteLine("Disconnected");
                        _alreadyConnected = false;
                    }
                }
                Thread.Sleep(500);
            }
        }

        private static bool IsPortInUse(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            var c = tcpConnInfoArray.FirstOrDefault(r => r.LocalEndPoint.Port == port && r.State == TcpState.Established);
            if (c != null)
            {
                return true;
            }
            return false;
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!_isHiding)
            {
                btnStart.Text = "Stop";
                _isHiding = true;
                _hidingThread.Start();
            }
            else
            {
                btnStart.Text = "Start";
                _isHiding = false;
            }
        }

        private void processesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedWindowHandler = (IntPtr)processesListBox.SelectedValue;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isHiding = false;
            if (_hidingThread.IsAlive)
            {
                _hidingThread.Join();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

    }

    internal enum ShowWindowCommands
    {
        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        Hide = 0,

        /// <summary>
        /// Activates and displays a window. If the window is minimized or
        /// maximized, the system restores it to its original size and position.
        /// An application should specify this flag when displaying the window
        /// for the first time.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        ShowMinimized = 2,

        /// <summary>
        /// Maximizes the specified window.
        /// </summary>
        Maximize = 3, // is this the right value?

        /// <summary>
        /// Activates the window and displays it as a maximized window.
        /// </summary>      
        ShowMaximized = 3,

        /// <summary>
        /// Displays a window in its most recent size and position. This value
        /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
        /// the window is not activated.
        /// </summary>
        ShowNoActivate = 4,

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        Show = 5,

        /// <summary>
        /// Minimizes the specified window and activates the next top-level
        /// window in the Z order.
        /// </summary>
        Minimize = 6,

        /// <summary>
        /// Displays the window as a minimized window. This value is similar to
        /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
        /// window is not activated.
        /// </summary>
        ShowMinNoActive = 7,

        /// <summary>
        /// Displays the window in its current size and position. This value is
        /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
        /// window is not activated.
        /// </summary>
        ShowNA = 8,

        /// <summary>
        /// Activates and displays the window. If the window is minimized or
        /// maximized, the system restores it to its original size and position.
        /// An application should specify this flag when restoring a minimized window.
        /// </summary>
        Restore = 9,

        /// <summary>
        /// Sets the show state based on the SW_* value specified in the
        /// STARTUPINFO structure passed to the CreateProcess function by the
        /// program that started the application.
        /// </summary>
        ShowDefault = 10,

        /// <summary>
        ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
        /// that owns the window is not responding. This flag should only be
        /// used when minimizing windows from a different thread.
        /// </summary>
        ForceMinimize = 11
    }
}