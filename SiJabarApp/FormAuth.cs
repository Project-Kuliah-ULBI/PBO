using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using SiJabarApp.helper;

namespace SiJabarApp
{
    public partial class FormAuth : Form
    {
        // --- DLL IMPORT (Drag Window) ---
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // --- VARIABEL UI UTAMA ---
        private Panel panelLogin;
        private Panel panelRegister;
        private Panel panelOverlay;
        private Button btnClose;
        private Button btnMinimize;

        // --- VARIABEL INPUT & DATABASE --- 
        private TextBox txtRegName, txtRegEmail, txtRegPass;
        private TextBox txtLogEmail, txtLogPass;
        private MongoHelper dbHelper;

        // --- VARIABEL OVERLAY ---
        private Label lblOverlayTitle;
        private Label lblOverlayDesc;
        private Button btnOverlayAction;

        // --- LOGIKA ANIMASI ---
        private System.Windows.Forms.Timer animationTimer;
        private bool isShowRegister = false;
        private int slideSpeed = 50;

        // --- TEMA WARNA HIJAU (EMERALD GREEN via StyleHelper) ---
        private Color primaryColor = StyleHelper.PrimaryColor;
        private Color secondaryColor = StyleHelper.SecondaryColor;
        private Color inputBgColor = Color.FromArgb(240, 240, 240);

        // --- UKURAN FORM BESAR ---
        private int panelWidth = 500;
        private int formHeight = 650;

        public FormAuth()
        {
            InitializeComponent();

            // PAKSA UKURAN BESAR & SETUP FORM
            this.ClientSize = new Size(panelWidth * 2, formHeight);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            this.Text = "SiJabar App";
            this.BackColor = Color.White;

            try
            {
                dbHelper = new MongoHelper();
            }
            catch { }

            SetupUI();
            this.MouseDown += Form_MouseDown;
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void SetupUI()
        {
            // Panel Register (Kanan Awalnya)
            panelRegister = CreateFormPanel("Buat Akun", "Gunakan email untuk pendaftaran", "DAFTAR", true);
            panelRegister.Location = new Point(panelWidth, 0);
            this.Controls.Add(panelRegister);

            // Panel Login (Kiri Awalnya)
            panelLogin = CreateFormPanel("Masuk", "atau gunakan akun anda", "MASUK", false);
            panelLogin.Location = new Point(0, 0);
            this.Controls.Add(panelLogin);

            // Panel Overlay (Penutup Geser)
            panelOverlay = new Panel
            {
                Size = new Size(panelWidth, formHeight),
                Location = new Point(panelWidth, 0)
            };
            panelOverlay.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(panelOverlay.ClientRectangle, primaryColor, secondaryColor, 45F))
                {
                    e.Graphics.FillRectangle(brush, panelOverlay.ClientRectangle);
                }
            };
            panelOverlay.MouseDown += Form_MouseDown;
            this.Controls.Add(panelOverlay);
            panelOverlay.BringToFront();

            SetupOverlayContent();
            SetupWindowButtons();

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 10;
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void SetupWindowButtons()
        {
            btnClose = new Button
            {
                Text = "×",
                Font = new Font("Arial", 28, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = secondaryColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(60, 60),
                Location = new Point(this.Width - 60, 0),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            btnClose.Click += (s, e) => { Application.Exit(); };
            this.Controls.Add(btnClose);
            btnClose.BringToFront();

            btnMinimize = new Button
            {
                Text = "−",
                Font = new Font("Arial", 28, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = secondaryColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(60, 60),
                Location = new Point(this.Width - 120, 0),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 255, 255, 255);
            btnMinimize.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };
            this.Controls.Add(btnMinimize);
            btnMinimize.BringToFront();
        }

        private void UpdateWindowButtonColors()
        {
            if (isShowRegister)
            {
                btnClose.BackColor = Color.White;
                btnClose.ForeColor = Color.DimGray;
                btnMinimize.BackColor = Color.White;
                btnMinimize.ForeColor = Color.DimGray;
            }
            else
            {
                btnClose.BackColor = secondaryColor;
                btnClose.ForeColor = Color.White;
                btnMinimize.BackColor = secondaryColor;
                btnMinimize.ForeColor = Color.White;
            }
        }

        private void SetupOverlayContent()
        {
            int contentHeight = 80 + 80 + 55 + 60;
            int startY = (formHeight - contentHeight) / 2;

            lblOverlayTitle = new Label
            {
                Text = "Halo, Teman!",
                ForeColor = Color.White,
                Font = new Font("Georgia", 32, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(panelWidth, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, startY),
                BackColor = Color.Transparent
            };
            lblOverlayTitle.MouseDown += Form_MouseDown;
            panelOverlay.Controls.Add(lblOverlayTitle);

            lblOverlayDesc = new Label
            {
                Text = "Masukkan detail pribadi Anda dan mulailah perjalanan bersama kami",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14),
                AutoSize = false,
                Size = new Size(400, 80),
                TextAlign = ContentAlignment.TopCenter,
                BackColor = Color.Transparent
            };
            lblOverlayDesc.Location = new Point((panelWidth - 400) / 2, startY + 80);
            lblOverlayDesc.MouseDown += Form_MouseDown;
            panelOverlay.Controls.Add(lblOverlayDesc);

            btnOverlayAction = new Button
            {
                Text = "DAFTAR",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(200, 55),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            btnOverlayAction.FlatAppearance.BorderColor = Color.White;
            btnOverlayAction.FlatAppearance.BorderSize = 2;
            btnOverlayAction.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 255, 255, 255);
            btnOverlayAction.Location = new Point((panelWidth - 200) / 2, startY + 190);

            btnOverlayAction.Click += (s, e) => { animationTimer.Start(); };
            panelOverlay.Controls.Add(btnOverlayAction);
        }

        private Panel CreateFormPanel(string title, string subtitle, string btnText, bool isRegister)
        {
            Panel p = new Panel { Size = new Size(panelWidth, formHeight), BackColor = Color.White };
            p.MouseDown += Form_MouseDown;

            int inputCount = isRegister ? 3 : 2;
            int gap = 25;
            int inputH = 55;
            int titleH = 80;
            int subH = 30;
            int totalContentHeight = titleH + 10 + subH + 40 + (inputCount * (inputH + gap)) + 30 + 55;
            int currentY = (formHeight - totalContentHeight) / 2;

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = primaryColor,
                AutoSize = false,
                Size = new Size(panelWidth, titleH),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, currentY)
            };
            currentY += titleH + 10;

            Label lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(panelWidth, subH),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, currentY)
            };
            currentY += subH + 40;

            p.Controls.Add(lblTitle);
            p.Controls.Add(lblSub);

            if (isRegister)
            {
                var nameInput = CreateStylishInput(p, "Nama", currentY);
                txtRegName = nameInput.Item2;
                p.Controls.Add(nameInput.Item1);
                currentY += (inputH + gap);

                var emailInput = CreateStylishInput(p, "Email", currentY);
                txtRegEmail = emailInput.Item2;
                p.Controls.Add(emailInput.Item1);
                currentY += (inputH + gap);

                var passInput = CreateStylishInput(p, "Password", currentY, true);
                txtRegPass = passInput.Item2;
                p.Controls.Add(passInput.Item1);
                currentY += (inputH + gap + 15);
            }
            else
            {
                var emailInput = CreateStylishInput(p, "Email", currentY);
                txtLogEmail = emailInput.Item2;
                p.Controls.Add(emailInput.Item1);
                currentY += (inputH + gap);

                var passInput = CreateStylishInput(p, "Password", currentY, true);
                txtLogPass = passInput.Item2;
                p.Controls.Add(passInput.Item1);
                currentY += (inputH + gap + 15);
            }

            Button btn = new Button
            {
                Text = btnText,
                BackColor = primaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(220, 55),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Location = new Point((panelWidth - btn.Width) / 2, currentY);

            if (isRegister) btn.Click += BtnActionRegister_Click;
            else btn.Click += BtnActionLogin_Click;

            p.Controls.Add(btn);
            return p;
        }

        // --- AKSI REGISTER ---
        private void BtnActionRegister_Click(object sender, EventArgs e)
        {
            if (dbHelper == null) dbHelper = new MongoHelper();

            if (string.IsNullOrWhiteSpace(txtRegName.Text) || txtRegName.Text == "Nama" ||
                string.IsNullOrWhiteSpace(txtRegEmail.Text) || txtRegEmail.Text == "Email" ||
                string.IsNullOrWhiteSpace(txtRegPass.Text) || txtRegPass.Text == "Password")
            {
                MessageBox.Show("Mohon lengkapi semua data!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string msg;
            this.Cursor = Cursors.WaitCursor;
            bool success = dbHelper.RegisterUser(txtRegName.Text, txtRegEmail.Text, txtRegPass.Text, out msg);
            this.Cursor = Cursors.Default;

            if (success)
            {
                MessageBox.Show(msg, "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Auto switch ke Login view
                animationTimer.Start();
                // Reset form
                txtRegName.Text = "Nama"; txtRegName.ForeColor = Color.Gray;
                txtRegEmail.Text = "Email"; txtRegEmail.ForeColor = Color.Gray;
                txtRegPass.Text = "Password"; txtRegPass.ForeColor = Color.Gray; txtRegPass.UseSystemPasswordChar = false;
            }
            else
            {
                MessageBox.Show(msg, "Gagal Registrasi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- AKSI LOGIN (PENTING!) ---
        private void BtnActionLogin_Click(object sender, EventArgs e)
        {
            if (dbHelper == null) dbHelper = new MongoHelper();

            string msg, username, userId, role;

            // Memanggil Helper: Mengirim Email & Pass, Menerima Msg, Username, UserId, Role
            bool success = dbHelper.LoginUser(txtLogEmail.Text, txtLogPass.Text, out msg, out username, out userId, out role);

            if (success)
            {
                MessageBox.Show($"Selamat Datang, {username}!", "Login Sukses");

                // BUKA DASHBOARD UTAMA (MENGIRIM 3 PARAMETER)
                MainForm dashboard = new MainForm(userId, username, role);
                dashboard.Show();

                this.Hide(); // Sembunyikan form auth
            }
            else
            {
                MessageBox.Show(msg, "Login Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Tuple<Panel, TextBox> CreateStylishInput(Panel parentPanel, string placeholder, int y, bool isPassword = false)
        {
            int inputWidth = 380;
            int inputHeight = 55;

            Panel pContainer = new Panel
            {
                Size = new Size(inputWidth, inputHeight),
                BackColor = inputBgColor,
                Location = new Point((parentPanel.Width - inputWidth) / 2, y),
                Padding = new Padding(0)
            };

            TextBox tb = new TextBox
            {
                Text = placeholder,
                BorderStyle = BorderStyle.None,
                BackColor = inputBgColor,
                Font = new Font("Segoe UI", 13),
                Width = inputWidth - 40,
                ForeColor = Color.Gray
            };

            tb.Location = new Point(20, (inputHeight - tb.Height) / 2);

            // Logic Placeholder
            tb.Enter += (s, e) => {
                if (tb.Text == placeholder)
                {
                    tb.Text = "";
                    tb.ForeColor = Color.Black;
                    if (isPassword) tb.UseSystemPasswordChar = true;
                }
            };
            tb.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    if (isPassword) tb.UseSystemPasswordChar = false;
                    tb.Text = placeholder;
                    tb.ForeColor = Color.Gray;
                }
            };

            pContainer.Controls.Add(tb);
            return Tuple.Create(pContainer, tb);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!isShowRegister)
            {
                if (panelOverlay.Left > 0)
                {
                    panelOverlay.Left -= slideSpeed;
                }
                else
                {
                    animationTimer.Stop();
                    isShowRegister = true;
                    UpdateOverlayContent();
                    UpdateWindowButtonColors();
                }
            }
            else
            {
                if (panelOverlay.Left < panelWidth)
                {
                    panelOverlay.Left += slideSpeed;
                }
                else
                {
                    animationTimer.Stop();
                    isShowRegister = false;
                    UpdateOverlayContent();
                    UpdateWindowButtonColors();
                }
            }
        }

        private void UpdateOverlayContent()
        {
            if (isShowRegister)
            {
                lblOverlayTitle.Font = new Font("Georgia", 24, FontStyle.Bold);
                lblOverlayTitle.Text = "Selamat Datang!";
                lblOverlayDesc.Text = "Untuk tetap terhubung, silakan login info pribadi Anda";
                btnOverlayAction.Text = "MASUK";
            }
            else
            {
                lblOverlayTitle.Font = new Font("Georgia", 32, FontStyle.Bold);
                lblOverlayTitle.Text = "Halo, Teman!";
                lblOverlayDesc.Text = "Masukkan detail pribadi Anda dan mulailah perjalanan bersama kami";
                btnOverlayAction.Text = "DAFTAR";
            }
        }
    }
}