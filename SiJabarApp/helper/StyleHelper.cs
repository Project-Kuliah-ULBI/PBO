using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace SiJabarApp.helper
{
    public static class StyleHelper
    {
        // --- PALET WARNA (EMERALD GREEN THEME) ---
        public static readonly Color PrimaryColor = Color.FromArgb(46, 204, 113);    // Emerald Green
        public static readonly Color SecondaryColor = Color.FromArgb(39, 174, 96);   // Darker Green
        public static readonly Color AccentColor = Color.FromArgb(52, 152, 219);     // Blue (untuk variasi)
        public static readonly Color DangerColor = Color.FromArgb(231, 76, 60);      // Red (Delete/Close)
        public static readonly Color WarningColor = Color.FromArgb(241, 196, 15);    // Yellow
        
        public static readonly Color BackgroundColor = Color.FromArgb(243, 244, 246); // Light Gray (Modern BG)
        public static readonly Color CardBackgroundColor = Color.White;
        public static readonly Color TextColor = Color.FromArgb(44, 62, 80);          // Dark Blue-Gray
        public static readonly Color TextLightColor = Color.FromArgb(127, 140, 141);  // Gray

        // --- FONT ---
        public static Font FontTitle = new Font("Segoe UI", 16, FontStyle.Bold);
        public static Font FontSubtitle = new Font("Segoe UI", 12, FontStyle.Regular);
        public static Font FontBody = new Font("Segoe UI", 10, FontStyle.Regular);
        public static Font FontBold = new Font("Segoe UI", 10, FontStyle.Bold);

        // --- BUTTON STYLING ---
        public static void StyleButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = bgColor;
            btn.ForeColor = textColor;
            btn.Font = FontBold;
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bgColor);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(bgColor);
        }

        public static void StyleSecondaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = TextLightColor;
            btn.BackColor = Color.White;
            btn.ForeColor = TextColor;
            btn.Font = FontBody;
            btn.Cursor = Cursors.Hand;
        }

        // --- DATAGRIDVIEW STYLING ---
        public static void StyleGridView(DataGridView grid)
        {
            grid.BackgroundColor = CardBackgroundColor;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.EnableHeadersVisualStyles = false;

            // Header
            grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryColor;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = FontBold;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            grid.ColumnHeadersHeight = 50;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            // Rows
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = TextColor;
            grid.DefaultCellStyle.Font = FontBody;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 250, 240); // Light Green Selection
            grid.DefaultCellStyle.SelectionForeColor = TextColor;
            grid.DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            grid.RowTemplate.Height = 50; // Lebih tinggi biar enak dilihat
            
            // Alternating Rows
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
        }

        // --- TEXTBOX STYLING ---
        public static void StyleInput(Control ctrl)
        {
            ctrl.BackColor = Color.White;
            ctrl.Font = FontBody;
            if (ctrl is TextBox tb)
            {
                tb.BorderStyle = BorderStyle.FixedSingle;
            }
        }
    }
}
