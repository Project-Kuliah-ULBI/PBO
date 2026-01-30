using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SiJabarApp
{
    partial class Chatbot
    {
        // Komponen UI
        private System.ComponentModel.IContainer components = null;
        private FlowLayoutPanel chatPanel;
        private TextBox txtInput;
        private Panel header;
        private Button btnSend;
        private Button btnClear;
        private Label title;
        private Panel pnlInput;

        // Warna & Style
        private Color colorUser = Color.FromArgb(220, 248, 198);
        private Color colorBot = Color.FromArgb(245, 245, 245);

        // DLL Import untuk Drag Window
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // Method standard Designer (Jangan dihapus jika sudah ada)
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Text = "Chatbot";
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(480, 600);
            this.BackColor = Color.White;
        }

        // Method Manual UI kita (Pengganti InitUI lama)
        private void InitCustomUI()
        {
            // 1. Header Area
            header = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(39, 174, 96) };
            header.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); };

            title = new Label
            {
                Text = "SiJabar Waste Assistant ♻️",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(15, 20),
                AutoSize = true
            };

            btnClear = new Button
            {
                Text = "🗑",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Size = new Size(35, 35),
                Location = new Point(430, 15),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += btnClear_Click; // Event handler ada di Chatbot.cs

            header.Controls.Add(title);
            header.Controls.Add(btnClear);
            this.Controls.Add(header);

            // 2. Input Area
            pnlInput = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(240, 240, 240), Padding = new Padding(10) };

            txtInput = new TextBox { Width = 380, Font = new Font("Segoe UI", 11), BorderStyle = BorderStyle.FixedSingle, Location = new Point(15, 25) };
            txtInput.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { SendAction(); e.SuppressKeyPress = true; } // SendAction di Chatbot.cs
            };

            btnSend = new Button
            {
                Text = "➤",
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(45, 45),
                Location = new Point(410, 15),
                Cursor = Cursors.Hand
            };
            btnSend.Click += (s, e) => SendAction(); // SendAction di Chatbot.cs

            pnlInput.Controls.Add(txtInput);
            pnlInput.Controls.Add(btnSend);
            this.Controls.Add(pnlInput);

            // 3. Chat Panel
            chatPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10)
            };
            this.Controls.Add(chatPanel);
            chatPanel.BringToFront();
        }

        // Helper untuk Menambah Bubble Chat
        private void AddBubble(string text, bool isUser)
        {
            if (chatPanel.InvokeRequired)
            {
                chatPanel.Invoke(new Action(() => AddBubble(text, isUser)));
                return;
            }

            Panel row = new Panel { Width = chatPanel.ClientSize.Width - 30, AutoSize = true, Padding = new Padding(0, 5, 0, 5) };
            Label lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10),
                BackColor = isUser ? colorUser : colorBot,
                Padding = new Padding(10),
                MaximumSize = new Size(300, 0),
                AutoSize = true,
                TextAlign = isUser ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft
            };

            // Logika posisi bubble
            row.Controls.Add(lbl);
            lbl.Location = isUser ? new Point(row.Width - lbl.PreferredWidth - 10, 0) : new Point(10, 0);

            chatPanel.Controls.Add(row);
            ScrollBottom();
        }

        private void ScrollBottom()
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)delegate {
                    chatPanel.AutoScrollPosition = new Point(0, chatPanel.VerticalScroll.Maximum);
                    chatPanel.PerformLayout();
                });
            }
        }
    }
}