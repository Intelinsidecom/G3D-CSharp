using System.Drawing;
using System.Windows.Forms;

namespace G3D.Direct3D11
{
    public sealed class D3D11Control : Control
    {
        public D3D11Control()
        {
            SetStyle(ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }
    }
}
