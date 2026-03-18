using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StardewMultibyteLanguagesInput
{
    public class MainForm : Form
    {
        private TextBox inputField;
        private bool isClosing = false;

        // Windows APIの定義
        // Windows API definitions
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const byte VK_T = 0x54;
        private const byte KEYEVENTF_KEYUP = 0x0002;

        public MainForm()
        {
            //this.Text = "SV日本語入力補助";
            this.Text = "SV Multibyte Language Input Support";
            
            this.Size = new System.Drawing.Size(400, 80);
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.TopMost = true; 
            this.StartPosition = FormStartPosition.CenterScreen;

            inputField = new TextBox { 
                Dock = DockStyle.Fill,
                Multiline = false,
                Font = new System.Drawing.Font("MS UI Gothic", 12)
            };
            
            inputField.KeyDown += OnKeyDown;
            inputField.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) e.Handled = true; };
            this.Controls.Add(inputField);

            this.Activated += (s, e) => { 
                if(!isClosing) {
                    this.Opacity = 1.0;
                    // 強制的に最前面へ
                    // Bring to the front
                    SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                }
            };
            this.Deactivate += (s, e) => { if(!isClosing) this.Opacity = 0.5; };
            this.FormClosing += (s, e) => { isClosing = true; };
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; 
                string text = inputField.Text;
                if (string.IsNullOrWhiteSpace(text)) return;

                // 1. クリップボードにコピー
                // 1. Copy to clipboard
                Clipboard.SetText(text);

                // 2. ウィンドウを検索
                // 2. Search for a window
                IntPtr svHandle = IntPtr.Zero;
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.MainWindowTitle.Contains("Stardew Valley") && p.MainWindowTitle.Contains("running SMAPI"))
                    {
                        svHandle = p.MainWindowHandle;
                        break;
                    }
                }

                if (svHandle != IntPtr.Zero)
                {
                    // 3. ゲームを最前面にする
                    // 3. Bring the game to the foreground
                    SetForegroundWindow(svHandle);

                    // フォーカスが移るのを待つ（ここが短いと貼り付けに失敗します）
                    // Wait for the focus to shift (if this interval is too short, the paste operation will fail)
                    System.Threading.Thread.Sleep(300); 

                    // キー定数の定義
                    // Definition of key constants
                    const byte VK_CONTROL = 0x11;
                    const byte VK_V = 0x56;
                    const byte VK_RETURN = 0x0D;

                    // 4. 【貼り付けシーケンス】
                    // 修飾キーを一度クリア
                    // 4. [Paste Sequence]
                    // Clear modifier keys once
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0); 
                    System.Threading.Thread.Sleep(50);

                    // Ctrl + V を実行
                    // Press Ctrl + V
                    keybd_event(VK_CONTROL, 0, 0, 0); // Ctrl Down
                    keybd_event(VK_V, 0, 0, 0);       // V Down
                    System.Threading.Thread.Sleep(50);
                    keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0); // V Up
                    keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0); // Ctrl Up

                    // 貼り付けが反映されるのをわずかに待つ
                    // Wait a moment for the paste to take effect
                    System.Threading.Thread.Sleep(150);

                    // 5. 送信 (Enter)
                    // 5. Send (Enter)
                    keybd_event(VK_RETURN, 0, 0, 0); // Enter Down
                    keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0); // Enter Up

                    inputField.Clear();
                }
                else
                {
                    //MessageBox.Show("ゲームが見つかりませんでした。");
                    MessageBox.Show("The game window could not be found.");
                }
            }
        }



        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
