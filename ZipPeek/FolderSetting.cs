using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZipPeek
{
    public partial class FolderSetting : UserControl
    {
        public static bool FailedSkip = true;
        public static int ExistsFileOption = 0; // 0: Skip, 1: Overwrite, 2: Message.
        public static int SubfolderOption = 1; // 0: No, 1: Yes, 2: Message.

        public FolderSetting()
        {
            InitializeComponent();

            failedSkip.CheckedChanged += (s, e) => { FailedSkip = failedSkip.Checked; };

            existSkip.CheckedChanged += (s, e) => { if (existSkip.Checked) ExistsFileOption = 0; };
            existDownload.CheckedChanged += (s, e) => { if (existDownload.Checked) ExistsFileOption = 1; };
            existMessage.CheckedChanged += (s, e) => { if (existMessage.Checked) ExistsFileOption = 2; };

            subfolderNo.CheckedChanged += (s, e) => { if (subfolderNo.Checked) SubfolderOption = 0; };
            subfolderYes.CheckedChanged += (s, e) => { if (subfolderYes.Checked) SubfolderOption = 1; };
            subfolderMessage.CheckedChanged += (s, e) => { if (subfolderMessage.Checked) SubfolderOption = 2; };
        }

        #region Border Radius
        private readonly int borderRadius = 20;

        private GraphicsPath GetRoundedPath()
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, borderRadius, borderRadius, 180, 90);
            path.AddArc(Width - borderRadius, 0, borderRadius, borderRadius, 270, 90);
            path.AddArc(Width - borderRadius, Height - borderRadius, borderRadius, borderRadius, 0, 90);
            path.AddArc(0, Height - borderRadius, borderRadius, borderRadius, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.Region = new Region(GetRoundedPath());
        }
        #endregion

    }
}
