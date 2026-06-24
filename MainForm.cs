using System;
using System.Drawing;
using System.Windows.Forms;
using CG.Web.MegaApiClient;


namespace LegalOfficeApp
{
    public partial class MainForm : Form
    {
        private readonly MegaApiClient client = new MegaApiClient();
        private readonly Color Navy      = Color.FromArgb(10, 26, 107);
        private readonly Color NavyHover = Color.FromArgb(20, 40, 130);
        private readonly Color BgGray    = Color.FromArgb(240, 240, 240);

        private MegaService mega = new MegaService();
        private Panel  panelMenu;
        private Label  logo;
        private Button btnNavIcon, btnDashboard, btnSubmission, btnTracking, btnLogs, btnSignOut;
        private Button _activeBtn;

        // Full text and icon-only text for each nav button
        private readonly string[] _fullText = {
            "   Dashboard",
            "   Notarial Submission",
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

        private async void mainMenu_Load(object sender, EventArgs e)
        {
            try
            {
                await mega.LoginAsync(
                    "gplopez@ccc.edu.ph",
                    "megaPass20*"
                );

                MessageBox.Show("Connected to MEGA!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void BuildLayout()
        {
            Text          = "Legal Office – Calamba City";
            Size          = new Size(1280, 760);
            MinimumSize   = new Size(960, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = BgGray;
            Font          = new Font("Segoe UI", 9f);

            this.Icon = new Icon("C:\\Users\\Admin\\source\\repos\\NotaryApp\\resources\\logo.ico");
            // Instantiate pages first
            ucDashboard  = new DashboardControl  { Dock = DockStyle.Fill };
            ucSubmission = new SubmissionControl  { Dock = DockStyle.Fill };
            ucTracking   = new TrackingControl    { Dock = DockStyle.Fill };
            ucLogs       = new LogsControl        { Dock = DockStyle.Fill };

            // WinForms docking rule:
            //   Controls are docked in REVERSE z-order (last added = processed first).
            //   So: add Fill/content controls FIRST (they are processed last, taking leftover space).
            //       add Left/Right/Top sidebar controls LAST (processed first, reserving their strip).
            BuildContentArea();  // adds panelContent (Fill) and topBar (Top) — processed LAST
            BuildSidebar();      // adds panelMenu (Left) — processed FIRST, strips its width before Fill runs
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
            // panelMenu is added to this.Controls at the END of this method
            // so it is processed FIRST by the dock engine (reserves its left strip
            // before the Fill panel takes the remainder).

            // Hamburger at top
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

            // Logo
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

            // Nav buttons
            btnDashboard  = NavBtn(_fullText[0]);
            btnSubmission = NavBtn(_fullText[1]);
            btnTracking   = NavBtn(_fullText[2]);
            btnLogs       = NavBtn(_fullText[3]);
            btnSignOut    = NavBtn("   Sign Out");
            btnSignOut.Dock = DockStyle.Bottom;

            btnDashboard .Click += (s, e) => OpenPage(ucDashboard,  btnDashboard,  "DASHBOARD");
            btnSubmission.Click += (s, e) => OpenPage(ucSubmission, btnSubmission, "NOTARIAL BOOK SUBMISSION");
            btnTracking  .Click += (s, e) => OpenPage(ucTracking,   btnTracking,   "SUBMISSION TRACKING");
            btnLogs      .Click += (s, e) => OpenPage(ucLogs,       btnLogs,       "ACTIVITY LOGS");
            btnSignOut   .Click += (s, e) => { if (Confirm("Sign out?")) Application.Exit(); };

            // Children: Bottom first, then Top items in reverse visual order
            panelMenu.Controls.Add(btnSignOut);
            panelMenu.Controls.Add(btnLogs);
            panelMenu.Controls.Add(btnTracking);
            panelMenu.Controls.Add(btnSubmission);
            panelMenu.Controls.Add(btnDashboard);
            panelMenu.Controls.Add(logo);
            panelMenu.Controls.Add(btnNavIcon);

            // Add sidebar to form LAST — dock engine processes higher z-order controls first,
            // so adding last = processed first = strips its 210px before Fill takes the rest.
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

        // ── Content area ─────────────────────────────────────────────
        private void BuildContentArea()
        {
            // Top bar
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
            var lblUser = new Label
            {
                Text      = "👤  Admin Juan",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Right,
                Width     = 160,
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 16, 0)
            };
            var border = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(220, 220, 220) };
            topBar.Controls.AddRange(new Control[] { border, lblUser, lblPageTitle });

            // Content panel — Fill must be added AFTER sidebar in Controls
            panelContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BgGray,
                Padding   = new Padding(16)
            };

            // Add Fill first, then Top.
            // WinForms dock order: last-added control has lowest z-order = processed first by dock engine.
            // We want: Top bar processed after Fill so it sits above it.
            // Both content controls must be in Controls BEFORE the sidebar (added in BuildSidebar)
            // so the sidebar (Left) is processed before them and strips its width correctly.
            this.Controls.Add(panelContent);  // Fill — processed last, takes remaining space
            this.Controls.Add(topBar);        // Top  — processed before Fill, takes top strip
        }

        // ── Navigation ───────────────────────────────────────────────
        private void OpenPage(UserControl page, Button btn, string title)
        {
            panelContent.Controls.Clear();
            panelContent.Controls.Add(page);
            lblPageTitle.Text = title;

            if (_activeBtn != null)
            {
                _activeBtn.BackColor = Navy;
                _activeBtn.ForeColor = Color.FromArgb(200, 200, 200);
            }
            _activeBtn           = btn;
            btn.BackColor        = Color.FromArgb(20, 40, 130);
            btn.ForeColor        = Color.White;
        }

        // ── Collapse / Expand ─────────────────────────────────────────
        private void CollapseMenu()
        {
            bool expanded = panelMenu.Width > 80;
            Button[] navBtns = { btnDashboard, btnSubmission, btnTracking, btnLogs };

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
                btnSignOut.Text      = "   Sign Out";
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
