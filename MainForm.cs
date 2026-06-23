using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public partial class MainForm : Form
    {
        // ── Colors ──────────────────────────────────────────────
        private readonly Color Navy      = Color.FromArgb(10, 26, 107);
        private readonly Color NavyHover = Color.FromArgb(20, 40, 130);
        private readonly Color ActiveHL  = Color.FromArgb(245, 200, 66);   // gold left-border
        private readonly Color BgGray    = Color.FromArgb(240, 240, 240);

        // ── Sidebar controls ────────────────────────────────────
        private Panel  panelMenu;
        private Panel  navIconPanel;
        private Label  logo;
        private Button btnNavIcon, btnDashboard, btnSubmission, btnTracking, btnLogs, btnSignOut;
        private Button _activeBtn;

        // ── Content panel ───────────────────────────────────────
        private Panel  panelContent;
        private Label  lblPageTitle;

        // ── Pages (UserControls) ─────────────────────────────────
        private DashboardControl    ucDashboard;
        private SubmissionControl   ucSubmission;
        private TrackingControl     ucTracking;
        private LogsControl         ucLogs;

        public MainForm()
        {
            InitializeComponent();
            BuildLayout();
            OpenPage(ucDashboard, btnDashboard, "DASHBOARD");
        }

        // ════════════════════════════════════════════════════════
        //  LAYOUT
        // ════════════════════════════════════════════════════════
        private void BuildLayout()
        {
            this.Text            = "Legal Office – Calamba City";
            this.Size            = new Size(1280, 720);
            this.MinimumSize     = new Size(900, 600);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = BgGray;
            this.Font            = new Font("Segoe UI", 9f, FontStyle.Regular);

            BuildSidebar();
            BuildContentArea();

            // Instantiate pages
            ucDashboard  = new DashboardControl  { Dock = DockStyle.Fill };
            ucSubmission = new SubmissionControl  { Dock = DockStyle.Fill };
            ucTracking   = new TrackingControl    { Dock = DockStyle.Fill };
            ucLogs       = new LogsControl        { Dock = DockStyle.Fill };
        }

        // ── Sidebar ─────────────────────────────────────────────
        private void BuildSidebar()
        {
            panelMenu = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 220,
                BackColor = Navy
            };
            this.Controls.Add(panelMenu);

            // Hamburger / nav icon row
            navIconPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Navy
            };
            btnNavIcon = NavBtn("≡", null);
            btnNavIcon.Font   = new Font("Segoe UI", 16f, FontStyle.Bold);
            btnNavIcon.Dock   = DockStyle.Fill;
            btnNavIcon.Click += (s, e) => CollapseMenu();
            navIconPanel.Controls.Add(btnNavIcon);
            panelMenu.Controls.Add(navIconPanel);

            // Logo
            logo = new Label
            {
                Text      = "LEGAL OFFICE\nCALAMBA CITY",
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Dock      = DockStyle.Top,
                Height    = 90,
                Padding   = new Padding(10)
            };
            panelMenu.Controls.Add(logo);

            // Nav buttons
            btnDashboard  = NavBtn("  ☰  Dashboard",        "DASHBOARD");
            btnSubmission = NavBtn("  ↑  Notarial Submission", "SUBMISSION");
            btnTracking   = NavBtn("  ⊞  Submission Tracking", "TRACKING");
            btnLogs       = NavBtn("  ↺  Activity Logs",    "LOGS");
            btnSignOut    = NavBtn("  →  Sign Out",          null);

            btnDashboard .Click += (s, e) => OpenPage(ucDashboard,  btnDashboard,  "DASHBOARD");
            btnSubmission.Click += (s, e) => OpenPage(ucSubmission, btnSubmission, "NOTARIAL BOOK SUBMISSION");
            btnTracking  .Click += (s, e) => OpenPage(ucTracking,   btnTracking,   "SUBMISSION TRACKING");
            btnLogs      .Click += (s, e) => OpenPage(ucLogs,       btnLogs,       "ACTIVITY LOGS");
            btnSignOut   .Click += (s, e) => { if (Confirm("Sign out?")) Application.Exit(); };

            // Sign-out at bottom
            btnSignOut.Dock = DockStyle.Bottom;

            panelMenu.Controls.Add(btnSignOut);
            panelMenu.Controls.Add(btnLogs);
            panelMenu.Controls.Add(btnTracking);
            panelMenu.Controls.Add(btnSubmission);
            panelMenu.Controls.Add(btnDashboard);
        }

        private Button NavBtn(string text, string tag)
        {
            var btn = new Button
            {
                Text      = text,
                Tag       = text,           // store original text for expand
                ForeColor = Color.FromArgb(210, 210, 210),
                BackColor = Navy,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Top,
                Height    = 46,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(14, 0, 0, 0),
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 9.5f)
            };
            btn.FlatAppearance.BorderSize     = 0;
            btn.FlatAppearance.MouseOverBackColor = NavyHover;
            btn.MouseEnter += (s, e) => { if (btn != _activeBtn) btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { if (btn != _activeBtn) btn.ForeColor = Color.FromArgb(210, 210, 210); };
            return btn;
        }

        // ── Content area ────────────────────────────────────────
        private void BuildContentArea()
        {
            // Top bar
            var topBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.White
            };

            lblPageTitle = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Dock      = DockStyle.Left,
                Width     = 400,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0)
            };

            var lblUser = new Label
            {
                Text      = "👤  Admin Juan",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Right,
                Width     = 150,
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 16, 0)
            };

            var topBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(220, 220, 220) };
            topBar.Controls.AddRange(new Control[] { topBorder, lblUser, lblPageTitle });

            // Content panel
            panelContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BgGray,
                Padding   = new Padding(16)
            };

            this.Controls.Add(panelContent);
            this.Controls.Add(topBar);
        }

        // ════════════════════════════════════════════════════════
        //  NAVIGATION
        // ════════════════════════════════════════════════════════
        private void OpenPage(UserControl page, Button btn, string title)
        {
            // Swap page
            panelContent.Controls.Clear();
            panelContent.Controls.Add(page);

            // Update title
            lblPageTitle.Text = title;

            // Highlight active nav button
            if (_activeBtn != null)
            {
                _activeBtn.BackColor = Navy;
                _activeBtn.ForeColor = Color.FromArgb(210, 210, 210);
                _activeBtn.FlatAppearance.BorderColor = Navy;
            }
            _activeBtn           = btn;
            btn.BackColor        = Color.FromArgb(20, 40, 130);
            btn.ForeColor        = Color.White;
            btn.FlatAppearance.BorderSize  = 0;
        }

        // ════════════════════════════════════════════════════════
        //  COLLAPSE / EXPAND MENU
        // ════════════════════════════════════════════════════════
        private void CollapseMenu()
        {
            bool isExpanded = panelMenu.Width > 100;

            if (isExpanded)
            {
                panelMenu.Width  = 60;
                logo.Visible     = false;

                foreach (Button b in new[] { btnDashboard, btnSubmission, btnTracking, btnLogs, btnSignOut })
                {
                    b.Text        = "";
                    b.Padding     = new Padding(0);
                    b.TextAlign   = ContentAlignment.MiddleCenter;
                    b.Height      = 46;
                }
            }
            else
            {
                panelMenu.Width  = 220;
                logo.Visible     = true;

                foreach (Button b in new[] { btnDashboard, btnSubmission, btnTracking, btnLogs, btnSignOut })
                {
                    b.Text      = b.Tag?.ToString() ?? "";
                    b.Padding   = new Padding(14, 0, 0, 0);
                    b.TextAlign = ContentAlignment.MiddleLeft;
                }
            }
        }

        // ════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════
        private bool Confirm(string msg) =>
            MessageBox.Show(msg, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode       = AutoScaleMode.Font;
            this.ResumeLayout(false);
        }
    }
}
