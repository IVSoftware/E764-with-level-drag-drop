This answer uses a custom `GridViewEx` class inherited from `GridView` which allows the objective of **reordering grouped rows using drag & drop** to be achieved in a clean manner without a lot of code mixed in with the main form. Your post and additional comments state two requirements, and I have added a third goal to make the look and feel similar to the drag drop functionality that already exists for the records.

- Before or After Target Row
- Expanded rows stay expanded
- Drag feedback similar to row version

This custom version is simply swapped out manually in the Designer.cs file.

[![level1-above-level5][1]][1]
***

The feedback line in this example is blue for "Before Target Row" and red for "After Target Row".

[![level2-below-level6][2]][2]

***
Groups that are expanded before an operation remain in that state.

[![expanded op][3]][3]

***
**GridViewEx** - The demo project is available to [clone](https://github.com/IVSoftware/E764-with-level-drag-drop.git) from GitHub.

`GridViewEx` maintains its own `GroupDragDropState` to avoid potential conflicts with `BehaviorManager` operations. 

    enum GroupDragDropState
    {
        None,
        Down,
        Drag,
        Drop,
    }

The `Drag` state is entered if the mouse-down cursor travels more than 10 positions in any direction.

    internal class GridViewEx : GridView
    {
        public GridViewEx()
        {
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            CustomDrawGroupRow += OnCustomDrawGroupRow;
            DataSourceChanged += OnDataSourceChanged;
            DisableCurrencyManager = true; // Use this setting From sample code.
            GroupRowCollapsing += OnGroupRowCollapsing;
            CustomColumnSort += OnCustomColumnSort;
        }

        protected virtual void OnMouseDown(object sender, MouseEventArgs e)
        {
            var hittest = CalcHitInfo(e.Location);
            var screenLocation = PointToScreen(e.Location);
            _mouseDownClient = e.Location;
            _isGroupRow = hittest.RowInfo != null && hittest.RowInfo.IsGroupRow;
            if (_isGroupRow)
            {
                var gridGroupInfo = (GridGroupRowInfo)hittest.RowInfo;
                _dragFeedbackLabel.RowBounds = hittest.RowInfo.Bounds.Size;
                DragRowInfo = gridGroupInfo;
                _isExpanded = gridGroupInfo.IsGroupRowExpanded;
            }
        }

        protected virtual void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (Control.MouseButtons.Equals(MouseButtons.Left))
            {
                _mouseDeltaX = _mouseDownClient.X - e.Location.X;
                _mouseDeltaY = _mouseDownClient.Y - e.Location.Y;
                if (Math.Abs(_mouseDeltaX) > 10 || Math.Abs(_mouseDeltaY) > 10)
                {
                    GroupDragDropState = GroupDragDropState.Drag;
                }
                var hittest = CalcHitInfo(e.Location);
                if ((hittest.RowInfo == null) || hittest.RowInfo.Equals(DragRowInfo) || !hittest.RowInfo.IsGroupRow)
                {
                    CurrentGroupRowInfo = null;
                }
                else
                {
                    CurrentGroupRowInfo = (GridGroupRowInfo)hittest.RowInfo;
                    var deltaY = e.Location.Y - CurrentGroupRowInfo.Bounds.Location.Y;
                    var mid = CurrentGroupRowInfo.Bounds.Height / 2;
                    DropBelow = deltaY >= mid;
                }
            }
        }

        protected virtual void OnMouseUp(object sender, MouseEventArgs e)
        {
            switch (GroupDragDropState)
            {
                case GroupDragDropState.None:
                    break;
                case GroupDragDropState.Down:
                    GroupDragDropState = GroupDragDropState.None;
                    break;
                case GroupDragDropState.Drag:
                    GroupDragDropState = GroupDragDropState.Drop;
                    break;
                case GroupDragDropState.Drop:
                    GroupDragDropState = GroupDragDropState.None;
                    break;
            }
        }
        private Point _mouseDownClient = Point.Empty;
        private int _mouseDeltaX = 0;
        private int _mouseDeltaY = 0;
        .
        .
        .
    }

***
**Drag Feedback**

Entering the drag state takes a screenshot of the clicked row. *In order for* `Graphics.CopyFromScreen` *to work properly, the appconfig.cs has been modified to per-screen DPI awareness*.


appconfig.cs

	<?xml version="1.0"?>
	<configuration>
		<startup>	
			<supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/>
		</startup>
		<System.Windows.Forms.ApplicationConfigurationSection>
			<add key="DpiAwareness" value="PerMonitorV2" />
		</System.Windows.Forms.ApplicationConfigurationSection>
	</configuration>

GridViewEx.cs

    protected virtual void OnGroupDragDropStateChanged()
    {
        switch (GroupDragDropState)
        {
            case GroupDragDropState.None:
                break;
            case GroupDragDropState.Down:
                break;
            case GroupDragDropState.Drag:
                if (_isGroupRow)
                {
                    getRowScreenshot();
                }
                _dragFeedbackLabel.Visible = true;
                break;
            case GroupDragDropState.Drop:
                _dragFeedbackLabel.Visible = false;
                OnDrop();
                break;
            default:
                break;
        }
    }

    void getRowScreenshot()
    {
        // MUST be set to DPI AWARE in config.cs
        var ctl = GridControl;
        var screenRow = ctl.PointToScreen(DragRowInfo.Bounds.Location);
        var screenParent = ctl.TopLevelControl.Location;

        using (var srceGraphics = ctl.CreateGraphics())
        {
            var size = DragRowInfo.Bounds.Size;

            var bitmap = new Bitmap(size.Width, size.Height, srceGraphics);
            var destGraphics = Graphics.FromImage(bitmap);
            destGraphics.CopyFromScreen(screenRow.X, screenRow.Y, 0, 0, size);
            _dragFeedbackLabel.BackgroundImage = bitmap;
        }
    }

This image is assigned to the `BackgroundImage` of the `_dragFeedbackLabel` member which is a borderless form that can be drawn outside the rectangle of the main form. When visible, this form tracks the mouse cursor movement by means of a MessageFilter intercepting `WM_MOUSEMOVE` messages.

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

The row dividers are drawn by handling the `GridView.CustomDrawGroupRow` event.

    protected virtual void OnCustomDrawGroupRow(object sender, RowObjectCustomDrawEventArgs e)
    {
        if (e.Info is GridRowInfo ri)
        {
            using (var pen = new Pen(DropBelow ? Brushes.LightSalmon : Brushes.Aqua, 4F))
            {
                switch (GroupDragDropState)
                {
                    case GroupDragDropState.Drag:
                        if (CurrentGroupRowInfo != null)
                        {
                            if (ri.RowHandle == CurrentGroupRowInfo.RowHandle)
                            {
                                e.DefaultDraw();
                                int y;
                                if (DropBelow)
                                {
                                    y = ri.Bounds.Y + CurrentGroupRowInfo.Bounds.Height - 2;
                                }
                                else
                                {
                                    y = ri.Bounds.Y + 1;
                                }
                                e.Graphics.DrawLine(pen,
                                    ri.Bounds.X, y,
                                    ri.Bounds.X + ri.Bounds.Width, y);
                                e.Handled = true;
                            }
                        }
                        break;
                }
            }
        }
    }

***
**OnDrop**

The `ItemsArray` for the removed records is stashed in a dictionary to allow reassignment before adding inserting the same `DataRow` instance at a new index. Adjustments to the insert operation are made depending on the value of the `DropBelow` boolean which was set in the `MouseMove` handler.

    protected virtual void OnDrop()
    {
        var dataTable = (DataTable)GridControl.DataSource;
        if (!((DragRowInfo == null) || (CurrentGroupRowInfo == null)))
        {
            Debug.WriteLine($"{DragRowInfo.GroupValueText} {CurrentGroupRowInfo.GroupValueText}");
            var drags =
                dataTable
                .Rows
                .Cast<DataRow>()
                .Where(_ => _[CurrentGroupRowInfo.Column.FieldName]
                .Equals(DragRowInfo.EditValue)).ToArray();

            var dict = new Dictionary<DataRow, object[]>();
            foreach (var dataRow in drags)
            {
                dict[dataRow] = dataRow.ItemArray;
                dataTable.Rows.Remove(dataRow);
            }

            DataRow receiver =
                dataTable
                .Rows
                .Cast<DataRow>()
                .FirstOrDefault(_ =>
                    _[CurrentGroupRowInfo.Column.FieldName]
                    .Equals(CurrentGroupRowInfo.EditValue));
            int insertIndex;

            if (DropBelow)
            {
                receiver =
                    dataTable
                    .Rows
                    .Cast<DataRow>()
                    .LastOrDefault(_ =>
                        _[CurrentGroupRowInfo.Column.FieldName]
                        .Equals(CurrentGroupRowInfo.EditValue));

                insertIndex = dataTable.Rows.IndexOf(receiver) + 1;
            }
            else
            {
                receiver =
                    dataTable
                    .Rows
                    .Cast<DataRow>()
                    .FirstOrDefault(_ =>
                        _[CurrentGroupRowInfo.Column.FieldName]
                        .Equals(CurrentGroupRowInfo.EditValue));

                insertIndex = dataTable.Rows.IndexOf(receiver);
            }
            foreach (var dataRow in drags.Reverse())
            {
                dataRow.ItemArray = dict[dataRow];
                dataTable.Rows.InsertAt(dataRow, insertIndex);
            }
            try
            {
                var parentRowHandle = GetParentRowHandle(insertIndex);
                if (_isExpanded)
                {
                    ExpandGroupRow(parentRowHandle);
                }
                FocusedRowHandle = parentRowHandle;
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
        }
    }

  [1]: https://i.stack.imgur.com/jrsNW.png
  [2]: https://i.stack.imgur.com/wQDEb.png
  [3]: https://i.stack.imgur.com/s8dxh.png