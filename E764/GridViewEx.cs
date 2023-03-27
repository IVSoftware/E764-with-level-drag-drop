using DevExpress.XtraExport.Helpers;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using System;
using System.Data;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace E764
{
    enum GroupDragDropState
    {
        None,
        Down,
        Drag,
        Drop,
    }
    internal class GridViewEx : GridView
    {
        public GridViewEx()
        {
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            CustomDrawGroupRow += OnCustomDrawGroupRow;
            DataSourceChanged += OnDataSourceChanged;
            DisableCurrencyManager = true; // Use this setting from sample code.
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

        /// <summary>
        /// Disable automatic sorting. 
        /// </summary>
        protected virtual void OnDataSourceChanged(object sender, EventArgs e)
        {
            foreach (GridColumn column in Columns)
            {
                column.SortMode = ColumnSortMode.Custom;
            }
            ExpandGroupLevel(1);
        }

        protected virtual void OnCustomColumnSort(object sender, CustomColumnSortEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Disallow collapses during drag operations
        /// </summary>
        protected virtual void OnGroupRowCollapsing(object sender, RowAllowEventArgs e)
        {
            e.Allow = GroupDragDropState.Equals(GroupDragDropState.None);
        }
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

        internal DragFeedback _dragFeedbackLabel { get; } = new DragFeedback
        {
            Name = "DragFeedback",
            Visible = false,
            BackColor = Color.White,
        };

        #region P R O P E R T I E S
        public GridGroupRowInfo DragRowInfo { get; set; }

        public GridGroupRowInfo CurrentGroupRowInfo { get; set; }

    public bool DropBelow
    {
        get => _dropBelow;
        set
        {
            if (!Equals(_dropBelow, value))
            {
                _dropBelow = value;
    #if true
                // "Minimal redraw" version
                RefreshRow(CurrentGroupRowInfo.RowHandle);
    #else
                // But if drawing artifacts are present, refresh
                // the entire control surface instead.
                GridControl.Refresh();
    #endif
            }
        }
    }
    bool _dropBelow = false;

        GroupDragDropState GroupDragDropState
        {
            get => _groupDragDropState;
            set
            {
                if (!Equals(_groupDragDropState, value))
                {
                    _groupDragDropState = value;
                    OnGroupDragDropStateChanged();
                    Debug.WriteLine($"{GroupDragDropState}");
                }
            }
        }
        GroupDragDropState _groupDragDropState = default(GroupDragDropState);
        #endregion P R O P E R T I E S

        private bool _isGroupRow;
        private bool _isExpanded;
    }
}
