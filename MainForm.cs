using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public partial class MainForm : Form
    {
        private readonly Color Navy      = Color.FromArgb(10, 26, 107);
        private readonly Color NavyHover = Color.FromArgb(20, 40, 130);
        private readonly Color BgGray    = Color.FromArgb(240, 240, 240);

        /// <summary>
        /// Set to true by Sign Out so Program.cs loops back to the login form.
        /// Reset to false by Program.cs after reading it.
        /// </summary>
        public static bool RestartAfterSignOut { get; set; } = false;

        private Panel  panelMenu;
        private Label  logo;
        private Button btnNavIcon, btnDashboard, btnSubmission, btnTracking, btnLogs, btnUsers, btnSignOut, btnAddStaff;
        private Label lblRole;
        private Button _activeBtn;

        // FIX (root cause #6): "User Management" added as a 5th nav entry, admin-only.
        private readonly string[] _fullText = {
            "⊞   Dashboard",
            "↑   Notarial Submission",
            "⊟   Submission Tracking",
            "↺   Activity Logs",
            "👥   User Management"
        };
        private readonly string[] _iconOnly = { "⊞", "↑", "⊟", "↺", "👥" };

        private Panel panelContent;
        private Label lblPageTitle;

        private DashboardControl       ucDashboard;
        private SubmissionControl      ucSubmission;
        private TrackingControl        ucTracking;
        private LogsControl            ucLogs;
        private UserManagementControl  ucUsers;

        public MainForm()
        {
            InitializeComponent();
            BuildLayout();
            OpenPage(ucDashboard, btnDashboard, "DASHBOARD");

            // FIX (root cause #1): previously
            //   this.Icon = new Icon("C:\Users\Admin\source\repos\NotaryApp\resources\logo.ico");
            // crashed MainForm's constructor on every machine but the original developer's —
            // meaning the app crashed immediately after every successful login. Load relative
            // to the executable and guard with File.Exists.
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
            if (File.Exists(iconPath))
                this.Icon = new Icon(iconPath);
        }

        private void BuildLayout()
        {
            Text          = "Legal Office – Calamba City";
            Size          = new Size(1280, 760);
            MinimumSize   = new Size(960, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = BgGray;
            Font          = new Font("Segoe UI", 9f);

            ucDashboard  = new DashboardControl       { Dock = DockStyle.Fill };
            ucSubmission = new SubmissionControl      { Dock = DockStyle.Fill };
            ucTracking   = new TrackingControl        { Dock = DockStyle.Fill };
            ucLogs       = new LogsControl            { Dock = DockStyle.Fill };
            ucUsers      = new UserManagementControl  { Dock = DockStyle.Fill };

            BuildContentArea();
            BuildSidebar();
        }

        // ── Sidebar ──────────────────────────────────────────────────
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

            bool isAdmin = SessionManager.IsAdmin;
            lblRole = new Label
            {
                Text      = isAdmin ? "🔑  Administrator" : "👤  Staff",
                ForeColor = isAdmin
                    ? Color.FromArgb(200, 220, 255)
                    : Color.FromArgb(180, 180, 200),
                Font      = new Font("Segoe UI", 7.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Top,
                Height    = 22
            };

            btnDashboard  = NavBtn(_fullText[0]);
            btnSubmission = NavBtn(_fullText[1]);
            btnTracking   = NavBtn(_fullText[2]);
            btnLogs       = NavBtn(_fullText[3]);
            btnUsers      = NavBtn(_fullText[4]);

            btnDashboard .Click += (s, e) => OpenPage(ucDashboard,  btnDashboard,  "DASHBOARD");
            btnSubmission.Click += (s, e) => OpenPage(ucSubmission, btnSubmission, "NOTARIAL BOOK SUBMISSION");
            btnTracking  .Click += (s, e) => OpenPage(ucTracking,   btnTracking,   "SUBMISSION TRACKING");
            btnLogs      .Click += (s, e) => OpenPage(ucLogs,       btnLogs,       "ACTIVITY LOGS");
            btnUsers     .Click += (s, e) => OpenPage(ucUsers,      btnUsers,      "USER MANAGEMENT");

            // FIX (root cause #5): Tracking and Logs were previously hidden from Staff
            // entirely (Visible = isAdmin), even though both controls already contain
            // their own Staff-appropriate behavior — TrackingControl falls back to a
            // view-only details dialog for non-admins, and LogsControl filters out
            // account-category entries for non-admins. Hiding the nav buttons made that
            // logic permanently unreachable for Staff. Both are now visible to every
            // signed-in user; the existing per-control logic still governs what they can
            // do/see once inside.
            btnTracking.Visible = true;
            btnLogs.Visible     = true;

            // User Management performs account creation/deactivation/password resets —
            // this one is correctly admin-only.
            btnUsers.Visible = isAdmin;

            // Bottom buttons
            btnAddStaff = BottomBtn("+   Add Staff / Admin", Color.FromArgb(20, 40, 130));
            btnSignOut  = BottomBtn("→   Sign Out", Navy);
            btnSignOut.Click += SignOut_Click;
            btnAddStaff.Visible = isAdmin;
            btnAddStaff.Click  += AddStaff_Click;

            var divider = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(40, 255, 255, 255)
            };

            panelMenu.Controls.Add(btnAddStaff);
            panelMenu.Controls.Add(btnSignOut);
            panelMenu.Controls.Add(divider);
            panelMenu.Controls.Add(btnUsers);
            panelMenu.Controls.Add(btnLogs);
            panelMenu.Controls.Add(btnTracking);
            panelMenu.Controls.Add(btnSubmission);
            panelMenu.Controls.Add(btnDashboard);
            panelMenu.Controls.Add(lblRole);
            panelMenu.Controls.Add(logo);
            panelMenu.Controls.Add(btnNavIcon);

            this.Controls.Add(panelMenu);
        }

        private Button BottomBtn(string text, Color bg)
        {
            var btn = new Button
            {
                Text      = text,
                Dock      = DockStyle.Bottom,
                Height    = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font      = new Font("Segoe UI", 9.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(14, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = NavyHover;
            return btn;
        }

        // ── Sign Out: set flag then close so Program.cs loops to login ──
        private void SignOut_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Sign out and return to login?", "Sign Out",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            RestartAfterSignOut = true;
            this.Close();
        }

        private void AddStaff_Click(object? sender, EventArgs e)
        {
            var dlg = new SignupForm();
            dlg.ShowDialog(this);

            // FIX: previously only refreshed LogsControl if it happened to be the open
            // page. UserManagementControl is now a reachable page too, so refresh it as
            // well if it's the one currently displayed — otherwise a newly-created account
            // wouldn't appear until the admin manually navigated away and back.
            if (panelContent.Controls.Count > 0)
            {
                if (panelContent.Controls[0] is LogsControl lg) lg.RefreshData();
                if (panelContent.Controls[0] is UserManagementControl um) um.RefreshData();
            }
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

        // ── Content area ─────────────────────────────────────────────
        private void BuildContentArea()
        {
            var topBar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.White };
            var roleTag = SessionManager.IsAdmin ? " (Administrator)" : " (Staff)";

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
            var lblUser = new Label
            {
                Text      = $"👤  {SessionManager.Current?.FullName ?? "User"}{roleTag}",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Right,
                Width     = 160,
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 16, 0)
            };
            var border = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(220, 220, 220) };
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

        // ── Navigation ───────────────────────────────────────────────
        private void OpenPage(UserControl page, Button btn, string title)
        {
            panelContent.Controls.Clear();
            panelContent.Controls.Add(page);
            lblPageTitle.Text = title;

            if (page is DashboardControl       db) db.RefreshData();
            if (page is TrackingControl        tr) tr.RefreshData();
            if (page is LogsControl            lg) lg.RefreshData();
            if (page is UserManagementControl  um) um.RefreshData();

            if (_activeBtn != null)
            {
                _activeBtn.BackColor = Navy;
                _activeBtn.ForeColor = Color.FromArgb(200, 200, 200);
            }
            _activeBtn    = btn;
            btn.BackColor = Color.FromArgb(20, 40, 130);
            btn.ForeColor = Color.White;

        }

        // ── Collapse / Expand sidebar ─────────────────────────────────
        private void CollapseMenu()
        {
            bool expanded = panelMenu.Width > 80;
            Button[] navBtns = { btnDashboard, btnSubmission, btnTracking, btnLogs, btnUsers };

if (expanded)
             {
                 panelMenu.Width = 56;
                 logo.Visible    = false;
                 lblRole.Visible = false;

                 for (int i = 0; i < navBtns.Length; i++)
                {
                    navBtns[i].Text      = _iconOnly[i];
                    navBtns[i].Font      = new Font("Segoe UI", 14f);
                    navBtns[i].TextAlign = ContentAlignment.MiddleCenter;
                    navBtns[i].Padding   = new Padding(0);
                }
                btnAddStaff.Text     = "+";
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
                 lblRole.Visible = true;

                 for (int i = 0; i < navBtns.Length; i++)
                {
                    navBtns[i].Text      = _fullText[i];
                    navBtns[i].Font      = new Font("Segoe UI", 9.5f);
                    navBtns[i].TextAlign = ContentAlignment.MiddleLeft;
                    navBtns[i].Padding   = new Padding(14, 0, 0, 0);
                }
                btnAddStaff.Text     = "+   Add Staff / Admin";
                btnSignOut.Text      = "→   Sign Out";
                btnSignOut.Font      = new Font("Segoe UI", 9.5f);
                btnSignOut.TextAlign = ContentAlignment.MiddleLeft;
                btnSignOut.Padding   = new Padding(14, 0, 0, 0);
                btnNavIcon.TextAlign = ContentAlignment.MiddleLeft;
                btnNavIcon.Padding   = new Padding(14, 0, 0, 0);
            }
        }

         private bool Confirm(string msg) =>
            MessageBox.Show(msg, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;


        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ResumeLayout(false);
        }
    }
}