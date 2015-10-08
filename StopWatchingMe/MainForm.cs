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
        private Thread _hidingThread;
        private bool _isHiding;
        private IntPtr _selectedWindowHandler = IntPtr.Zero;
        private const int ALT = 0xA4;
        private const int EXTENDEDKEY = 0x1;
        private const int KEYUP = 0x2;

        public MainForm()
        {
            InitializeComponent();
            notifyIcon.Icon = Icon;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            timer.Start();
            mainFormBindingSource.DataSource = _processes;
            processesListBox.DisplayMember = "MainWindowTitle";
            processesListBox.ValueMember = "MainWindowHandle";
            processesListBox.DataSource = mainFormBindingSource;
        }

        private void UpdateProcessesList()
        {
            _processes.Clear();
            var processes = Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).ToList();
            _processes.AddRange(processes);
            mainFormBindingSource.ResetBindings(false);
            if (_selectedWindowHandler != IntPtr.Zero && _processes.All(p => p.MainWindowHandle != _selectedWindowHandler))
            {
                _isHiding = false;
                _selectedWindowHandler = IntPtr.Zero;
                selectedWindowLabel.Text = "Selected window:";
                MessageBox.Show("Selected window has been closed! Hiding stopped!");
            }
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
                            if (IsIconic(_selectedWindowHandler))
                            {
                                ShowWindow(_selectedWindowHandler, ShowWindowCommands.Restore);
                            }

                            // Simulate a key press
                            keybd_event((byte) ALT, 0x45, EXTENDEDKEY | 0, 0);

                            // Simulate a key release
                            keybd_event((byte) ALT, 0x45, EXTENDEDKEY | KEYUP, 0);
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


        private void btnStart_Click(object sender, EventArgs e)
        {
            if (_selectedWindowHandler == IntPtr.Zero)
            {
                MessageBox.Show("Please, select a window");
                return;
            }
            if (!_isHiding)
            {
                StartHidingThread();
            }
            else
            {
                StopHiding();
            }
        }

        private void StopHiding()
        {
            btnStart.Text = "Start";
            _isHiding = false;
            _hidingThread.Join();
        }

        private void StartHidingThread()
        {
            btnStart.Text = "Stop";
            _isHiding = true;
            _hidingThread = new Thread(StartHiding);
            _hidingThread.Start();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isHiding = false;
            if (_hidingThread != null && _hidingThread.IsAlive)
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

        #region user32.dll imports

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        #endregion

        private void timer_Tick(object sender, EventArgs e)
        {
            UpdateProcessesList();
        }

        private void processesListBox_DoubleClick(object sender, EventArgs e)
        {
            if (processesListBox != null)
            {
                _selectedWindowHandler = (IntPtr) processesListBox.SelectedValue;
                selectedWindowLabel.Text = " Selected window: " + processesListBox.Text;
            }
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