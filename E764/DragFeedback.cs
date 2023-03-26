using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E764
{
    class DragFeedback : Form, IMessageFilter
    {
        const int WM_MOUSEMOVE = 0x0200;

        public DragFeedback()
        {
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            BackgroundImageLayout = ImageLayout.Stretch;
            Application.AddMessageFilter(this);
            Disposed += (sender, e) => Application.RemoveMessageFilter(this);
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (RowBounds == null)
            {
                base.SetBoundsCore(x, y, width, height, specified);
            }
            else
            {
                base.SetBoundsCore(x, y, RowBounds.Width, RowBounds.Height, specified);
            }
        }

        public bool PreFilterMessage(ref Message m)
        {
            if(MouseButtons == MouseButtons.Left && m.Msg.Equals(WM_MOUSEMOVE)) 
            {
                Location = MousePosition;
            }
            return false;
        }

        Point _mouseDownPoint = Point.Empty;
        Point _origin = Point.Empty;

        public Size RowBounds { get; internal set; }
        public new Image BackgroundImage
        {
            get => base.BackgroundImage;
            set
            {
                if((value == null) || (base.BackgroundImage == null))
                {
                    base.BackgroundImage = value;
                }
            }
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if(!Visible)
            {
                base.BackgroundImage?.Dispose(); ;
                base.BackgroundImage = null;
            }
        }
    }
}
