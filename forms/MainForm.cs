using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Reflection;

namespace LegalOfficeApp
{
    public partial class MainForm : Form
    {
        private readonly Color Navy      = Color.FromArgb(10, 26, 107);
        private readonly Color NavyHover = Color.FromArgb(20, 40, 130);
        private readonly Color BgGray    = Color.FromArgb(240, 240, 240);

        private Panel  panelMenu;
        private Label  logo;
        private Button btnNavIcon, btnDashboard, btnSubmission, btnTracking,
                       btnLogs, btnUsers, btnScheduling, btnSignOut;
        private Button _activeBtn;

        private readonly string[] _fullText = {
            "⊞   Dashboard",
            "↑   Document Submission",
            "⊟   Submission Tracking",
            "↺   Activity Logs",
            "👤   User Management",
            "📅   Scheduling"           // index 5
        };
        private readonly string[] _iconOnly = { "⊞", "↑", "⊟", "↺", "👤", "📅" };

        private Panel  panelContent;
        private Label  lblPageTitle;

        private DashboardControl      ucDashboard;
        private SubmissionControl     ucSubmission;
        private TrackingControl       ucTracking;
        private LogsControl           ucLogs;
        private UserManagementControl ucUsers;
        private SchedulingControl     ucScheduling;

        public MainForm()
        {
            InitializeComponent();
            SchedulerService.Instance.Start();
            ApplyIcon();
            BuildLayout();
            OpenPage(ucDashboard, btnDashboard, "DASHBOARD");
        }


        private bool _isSigningOut = false;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isSigningOut)
            {
                SchedulerService.Instance.Stop();
                base.OnFormClosing(e);
                return; // let it close cleanly, no exit prompt
            }

            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (!Confirm("Exit the application?"))
                {
                    e.Cancel = true;
                    return;
                }
                SchedulerService.Instance.Stop();
                Application.Exit();
            }
        }

        private void ApplyIcon()
        {
            string exeDir  = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = {
                Path.Combine(exeDir, "logo.ico"),
                Path.Combine(exeDir, "app.ico"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "logo.ico"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "app.ico"),
            };

            foreach (var p in paths)
            {
                try
                {
                    string full = Path.GetFullPath(p);
                    if (File.Exists(full))
                    {
                        this.Icon          = new Icon(full);
                        this.ShowInTaskbar = true;
                        return;
                    }
                }
                catch { }
            }

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = asm.GetManifestResourceStream(name)!;
                        this.Icon          = new Icon(stream);
                        this.ShowInTaskbar = true;
                        return;
                    }
                }
            }
            catch { }
        }

        private void BuildLayout()
        {
            Text          = "Legal Office – Calamba City";
            Size          = new Size(1280, 760);
            MinimumSize   = new Size(960, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = BgGray;
            Font          = new Font("Segoe UI", 9f);

            ucDashboard  = new DashboardControl      { Dock = DockStyle.Fill };
            ucSubmission = new SubmissionControl      { Dock = DockStyle.Fill };
            ucTracking   = new TrackingControl        { Dock = DockStyle.Fill };
            ucLogs       = new LogsControl            { Dock = DockStyle.Fill };
            ucUsers      = new UserManagementControl  { Dock = DockStyle.Fill };
            ucScheduling = new SchedulingControl      { Dock = DockStyle.Fill };

            BuildContentArea();
            BuildSidebar();
        }

        // ── Sidebar ──────────────────────────────────────────────
        private void BuildSidebar()
        {
            panelMenu = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 210,
                BackColor = Navy
            };

            btnNavIcon = new Button
            {
                Text      = "≡",
                Dock      = DockStyle.Top,
                Height    = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = Navy,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(14, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            btnNavIcon.FlatAppearance.BorderSize         = 0;
            btnNavIcon.FlatAppearance.MouseOverBackColor = NavyHover;
            btnNavIcon.Click += (s, e) => CollapseMenu();

            logo = new Label
            {
                Text      = "LEGAL OFFICE\nCALAMBA CITY",
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Dock      = DockStyle.Top,
                Height    = 80,
                Padding   = new Padding(8)
            };

            btnDashboard  = NavBtn(_fullText[0]);
            btnSubmission = NavBtn(_fullText[1]);
            btnTracking   = NavBtn(_fullText[2]);
            btnLogs       = NavBtn(_fullText[3]);
            btnUsers      = NavBtn(_fullText[4]);
            btnScheduling = NavBtn(_fullText[5]);
            btnSignOut    = NavBtn("→   Sign Out");

            btnSignOut.Dock = DockStyle.Bottom;

            btnDashboard .Click += (s, e) => OpenPage(ucDashboard,  btnDashboard,  "DASHBOARD");
            btnSubmission.Click += (s, e) => OpenPage(ucSubmission,  btnSubmission, "NOTARIAL BOOK SUBMISSION");
            btnTracking  .Click += (s, e) => OpenPage(ucTracking,    btnTracking,   "SUBMISSION TRACKING");
            btnLogs      .Click += (s, e) => OpenPage(ucLogs,        btnLogs,       "ACTIVITY LOGS");
            btnUsers     .Click += (s, e) => OpenPage(ucUsers,       btnUsers,      "USER MANAGEMENT");
            btnScheduling.Click += (s, e) => OpenPage(ucScheduling,  btnScheduling, "SCHEDULING");
            btnSignOut   .Click += (s, e) =>
            {
                 if (!Confirm("Sign out?")) return;
                    _isSigningOut = true;
                    SessionManager.Logout();
                    this.Close();
            };

            // Admin-only controls
            btnUsers.Visible = SessionManager.IsAdmin;

            // Add controls — order matters (DockStyle.Top stacks bottom-up)
            panelMenu.Controls.Add(btnSignOut);
            panelMenu.Controls.Add(btnScheduling);
            if (SessionManager.IsAdmin)
                panelMenu.Controls.Add(btnUsers);
            panelMenu.Controls.Add(btnLogs);
            panelMenu.Controls.Add(btnTracking);
            panelMenu.Controls.Add(btnSubmission);
            panelMenu.Controls.Add(btnDashboard);
            panelMenu.Controls.Add(logo);
            panelMenu.Controls.Add(btnNavIcon);

            this.Controls.Add(panelMenu);
        }

        private Button NavBtn(string text)
        {
            var btn = new Button
            {
                Text      = text,
                Tag       = text,
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Navy,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Top,
                Height    = 46,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(14, 0, 0, 0),
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 9.5f)
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = NavyHover;
            btn.MouseEnter += (s, e) => { if (btn != _activeBtn) btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { if (btn != _activeBtn) btn.ForeColor = Color.FromArgb(200, 200, 200); };
            return btn;
        }

        // ── Content area ─────────────────────────────────────────
        private void BuildContentArea()
        {
            var topBar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.White };

            lblPageTitle = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Dock      = DockStyle.Left,
                Width     = 500,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0)
            };

            string roleBadge = SessionManager.IsAdmin ? "  [Admin]" : "  [Staff]";
            var lblUser = new Label
            {
                Text      = $"👤  {SessionManager.Current?.FullName ?? "User"}{roleBadge}",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Right,
                Width     = 200,
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 16, 0)
            };

            var border = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(220, 220, 220)
            };
            topBar.Controls.AddRange(new Control[] { border, lblUser, lblPageTitle });

            panelContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BgGray,
                Padding   = new Padding(16)
            };

            this.Controls.Add(panelContent);
            this.Controls.Add(topBar);
        }

        // ── Navigation ────────────────────────────────────────────
        private void OpenPage(UserControl page, Button btn, string title)
        {
            panelContent.Controls.Clear();
            panelContent.Controls.Add(page);
            lblPageTitle.Text = title;

            if (page is DashboardControl      db) db.RefreshData();
            if (page is TrackingControl       tr) tr.RefreshData();
            if (page is LogsControl           lg) lg.RefreshData();
            if (page is UserManagementControl um) um.RefreshData();
            if (page is SchedulingControl     sc) sc.RefreshData();

            if (_activeBtn != null)
            {
                _activeBtn.BackColor = Navy;
                _activeBtn.ForeColor = Color.FromArgb(200, 200, 200);
            }
            _activeBtn    = btn;
            btn.BackColor = Color.FromArgb(20, 40, 130);
            btn.ForeColor = Color.White;
        }

        // ── Collapse / Expand sidebar ─────────────────────────────
        private void CollapseMenu()
        {
            bool expanded = panelMenu.Width > 80;

            // Build nav button array based on role
            var navBtns = SessionManager.IsAdmin
                ? new[] { btnDashboard, btnSubmission, btnTracking, btnLogs, btnUsers, btnScheduling }
                : new[] { btnDashboard, btnSubmission, btnTracking, btnLogs, btnScheduling };

            if (expanded)
            {
                panelMenu.Width = 56;
                logo.Visible    = false;
                for (int i = 0; i < navBtns.Length; i++)
                {
                    navBtns[i].Text      = _iconOnly[i];
                    navBtns[i].Font      = new Font("Segoe UI", 14f);
                    navBtns[i].TextAlign = ContentAlignment.MiddleCenter;
                    navBtns[i].Padding   = new Padding(0);
                }
                btnSignOut.Text      = "→";
                btnSignOut.Font      = new Font("Segoe UI", 14f);
                btnSignOut.TextAlign = ContentAlignment.MiddleCenter;
                btnSignOut.Padding   = new Padding(0);
                btnNavIcon.TextAlign = ContentAlignment.MiddleCenter;
                btnNavIcon.Padding   = new Padding(0);
            }
            else
            {
                panelMenu.Width = 210;
                logo.Visible    = true;
                for (int i = 0; i < navBtns.Length; i++)
                {
                    navBtns[i].Text      = _fullText[i];
                    navBtns[i].Font      = new Font("Segoe UI", 9.5f);
                    navBtns[i].TextAlign = ContentAlignment.MiddleLeft;
                    navBtns[i].Padding   = new Padding(14, 0, 0, 0);
                }
                btnSignOut.Text      = "→   Sign Out";
                btnSignOut.Font      = new Font("Segoe UI", 9.5f);
                btnSignOut.TextAlign = ContentAlignment.MiddleLeft;
                btnSignOut.Padding   = new Padding(14, 0, 0, 0);
                btnNavIcon.TextAlign = ContentAlignment.MiddleLeft;
                btnNavIcon.Padding   = new Padding(14, 0, 0, 0);
            }
        }

        private bool Confirm(string msg) =>
            MessageBox.Show(msg, "Confirm", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ResumeLayout(false);
        }
    }
}