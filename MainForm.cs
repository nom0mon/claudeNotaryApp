using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace LegalOfficeApp
{
    public partial class MainForm : Form
    {
        // Set to true when user clicks Sign Out (vs closing the window)
        public static bool RestartAfterSignOut { get; set; } = false;

        private readonly Color Navy      = Color.FromArgb(10, 26, 107);
        private readonly Color NavyHover = Color.FromArgb(20, 40, 130);
        private readonly Color BgGray    = Color.FromArgb(240, 240, 240);

        private Panel  panelMenu;
        private Label  logo;
        private Button btnNavIcon, btnDashboard, btnSubmission, btnTracking, btnLogs;
        private Button btnSignOut, btnAddStaff;
        private Button _activeBtn;

        private readonly string[] _fullText = {
            "   Dashboard",
            "   Document Submission",
            "   Submission Tracking",
            "   Activity Logs"
        };
        private readonly string[] _iconOnly = { "⊞", "↑", "⊟", "↺" };

        private Panel panelContent;
        private Label lblPageTitle;

        private DashboardControl  ucDashboard;
        private SubmissionControl ucSubmission;
        private TrackingControl   ucTracking;
        private LogsControl       ucLogs;

        public MainForm()
        {
            InitializeComponent();
            BuildLayout();
            OpenPage(ucDashboard, btnDashboard, "DASHBOARD");
        }

        private void BuildLayout()
        {
            Text          = "Legal Office – Calamba City";
            Size          = new Size(1280, 760);
            MinimumSize   = new Size(960, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = BgGray;
            Font          = new Font("Segoe UI", 9f);

            ucDashboard  = new DashboardControl  { Dock = DockStyle.Fill };
            ucSubmission = new SubmissionControl  { Dock = DockStyle.Fill };
            ucTracking   = new TrackingControl    { Dock = DockStyle.Fill };
            ucLogs       = new LogsControl        { Dock = DockStyle.Fill };

            BuildContentArea();
            BuildSidebar();
        }

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
            var lblRole = new Label
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

            btnDashboard .Click += (s, e) => OpenPage(ucDashboard,  btnDashboard,  "DASHBOARD");
            btnSubmission.Click += (s, e) => OpenPage(ucSubmission, btnSubmission, "NOTARIAL BOOK SUBMISSION");
            btnTracking  .Click += (s, e) => OpenPage(ucTracking,   btnTracking,   "SUBMISSION TRACKING");
            btnLogs      .Click += (s, e) => OpenPage(ucLogs,       btnLogs,       "ACTIVITY LOGS");

            btnTracking.Visible = isAdmin;
            btnLogs    .Visible = isAdmin;

            // Bottom buttons
            btnSignOut = BottomBtn("   Sign Out", Navy);
            btnSignOut.Click += SignOut_Click;

            btnAddStaff = BottomBtn("   + Add Staff / Admin", Color.FromArgb(20, 40, 130));
            btnAddStaff.Visible = isAdmin;
            btnAddStaff.Click  += AddStaff_Click;

            var divider = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 1,
                BackColor = Color.FromArgb(40, 255, 255, 255)
            };

            panelMenu.Controls.Add(btnSignOut);
            panelMenu.Controls.Add(btnAddStaff);
            panelMenu.Controls.Add(divider);
            panelMenu.Controls.Add(btnLogs);
            panelMenu.Controls.Add(btnTracking);
            panelMenu.Controls.Add(btnSubmission);
            panelMenu.Controls.Add(btnDashboard);
            panelMenu.Controls.Add(lblRole);
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

            string roleTag = SessionManager.IsAdmin ? " [Admin]" : " [Staff]";
            var lblUser = new Label
            {
                Text      = $"👤  {SessionManager.Current?.FullName ?? "User"}{roleTag}",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Right,
                Width     = 220,
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

        private void OpenPage(UserControl page, Button btn, string title)
        {
            panelContent.Controls.Clear();
            panelContent.Controls.Add(page);
            lblPageTitle.Text = title;

            if (page is DashboardControl  db) db.RefreshData();
            if (page is TrackingControl   tr) tr.RefreshData();
            if (page is LogsControl       lg) lg.RefreshData();

            if (_activeBtn != null)
            {
                _activeBtn.BackColor = Navy;
                _activeBtn.ForeColor = Color.FromArgb(200, 200, 200);
            }
            _activeBtn    = btn;
            btn.BackColor = Color.FromArgb(20, 40, 130);
            btn.ForeColor = Color.White;
        }

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
            if (panelContent.Controls.Count > 0 && panelContent.Controls[0] is LogsControl lg)
                lg.RefreshData();
        }

        private void CollapseMenu()
        {
            bool expanded = panelMenu.Width > 80;
            Button[] navBtns = { btnDashboard, btnSubmission, btnTracking, btnLogs };

            if (expanded)
            {
                panelMenu.Width     = 56;
                logo.Visible        = false;
                btnAddStaff.Visible = false;

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
                panelMenu.Width     = 210;
                logo.Visible        = true;
                btnAddStaff.Visible = SessionManager.IsAdmin;

                for (int i = 0; i < navBtns.Length; i++)
                {
                    navBtns[i].Text      = _fullText[i];
                    navBtns[i].Font      = new Font("Segoe UI", 9.5f);
                    navBtns[i].TextAlign = ContentAlignment.MiddleLeft;
                    navBtns[i].Padding   = new Padding(14, 0, 0, 0);
                }
                btnSignOut.Text      = "   Sign Out";
                btnSignOut.Font      = new Font("Segoe UI", 9.5f);
                btnSignOut.TextAlign = ContentAlignment.MiddleLeft;
                btnSignOut.Padding   = new Padding(14, 0, 0, 0);

                btnNavIcon.TextAlign = ContentAlignment.MiddleLeft;
                btnNavIcon.Padding   = new Padding(14, 0, 0, 0);
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;
            ResumeLayout(false);
        }
    }
}