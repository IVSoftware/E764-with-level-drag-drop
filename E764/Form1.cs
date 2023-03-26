using DevExpress.Data.Helpers;
using DevExpress.Utils.DragDrop;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace E764
{

    public partial class Form1 : DevExpress.XtraEditors.XtraForm {
        public Form1() {
            InitializeComponent();
            SetUpGrid();
            HandleBehaviorDragDropEvents();
        }
        public DataTable FillTable() {
            DataTable table = new DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Keyword");
            table.Columns.Add("Level", typeof(decimal));
            table.Rows.Add(new object[] { 1, "record1", 1 });
            table.Rows.Add(new object[] { 2, "record2", 1 });
            table.Rows.Add(new object[] { 3, "record3", 1 });
            table.Rows.Add(new object[] { 4, "record4", 1 });
            table.Rows.Add(new object[] { 5, "record5", 2 });
            table.Rows.Add(new object[] { 6, "record6", 2 });
            table.Rows.Add(new object[] { 7, "record7", 2 });
            table.Rows.Add(new object[] { 8, "record8", 3 });
            table.Rows.Add(new object[] { 9, "record9", 3 });
            table.Rows.Add(new object[] { 10, "record10", 3 });
            table.Rows.Add(new object[] { 11, "record11", 4 });
            table.Rows.Add(new object[] { 12, "record12", 4 });
            table.Rows.Add(new object[] { 13, "record13", 4 });
            table.Rows.Add(new object[] { 14, "record14", 5 });
            table.Rows.Add(new object[] { 15, "record15", 5 });
            table.Rows.Add(new object[] { 16, "record16", 6 });
            table.Rows.Add(new object[] { 17, "record17", 6 });
            return table;
        }
        public void SetUpGrid() {
            gridControl1.DataSource = FillTable();
            GridView view = gridControl1.MainView as GridView;
            view.OptionsBehavior.Editable = false;
            view.Columns["Level"].GroupIndex = 1;
        }
        #region R E C O R D    D R A G    D R O P
        public void HandleBehaviorDragDropEvents() {
            DragDropBehavior gridControlBehavior = behaviorManager1.GetBehavior<DragDropBehavior>(this.gridView1);
            gridControlBehavior.DragDrop += Behavior_DragDrop;
            gridControlBehavior.DragOver += Behavior_DragOver;
        }
        private void Behavior_DragOver(object sender, DragOverEventArgs e) {
            DragOverGridEventArgs args = DragOverGridEventArgs.GetDragOverGridEventArgs(e);
            e.InsertType = args.InsertType;
            e.InsertIndicatorLocation = args.InsertIndicatorLocation;
            e.Action = args.Action;
            Cursor.Current = args.Cursor;
            args.Handled = true;
        }
        private void Behavior_DragDrop(object sender, DevExpress.Utils.DragDrop.DragDropEventArgs e) {
            GridView targetGrid = e.Target as GridView;
            GridView sourceGrid = e.Source as GridView;
            if (e.Action == DragDropActions.None || targetGrid != sourceGrid)
                return;
            DataTable sourceTable = sourceGrid.GridControl.DataSource as DataTable;

            Point hitPoint = targetGrid.GridControl.PointToClient(Cursor.Position);
            GridHitInfo hitInfo = targetGrid.CalcHitInfo(hitPoint);

            int[] sourceHandles = e.GetData<int[]>();

            int targetRowHandle = hitInfo.RowHandle;
            int targetRowIndex = targetGrid.GetDataSourceRowIndex(targetRowHandle);

            List<DataRow> draggedRows = new List<DataRow>();
            foreach (int sourceHandle in sourceHandles) {
                int oldRowIndex = sourceGrid.GetDataSourceRowIndex(sourceHandle);
                DataRow oldRow = sourceTable.Rows[oldRowIndex];
                draggedRows.Add(oldRow);
            }

            int newRowIndex;

            switch (e.InsertType) {
                case InsertType.Before:
                    newRowIndex = targetRowIndex > sourceHandles[sourceHandles.Length - 1] ? targetRowIndex - 1 : targetRowIndex;
                    for (int i = draggedRows.Count - 1; i >= 0; i--) {
                        DataRow oldRow = draggedRows[i];
                        DataRow newRow = sourceTable.NewRow();
                        newRow.ItemArray = oldRow.ItemArray;
                        sourceTable.Rows.Remove(oldRow);
                        sourceTable.Rows.InsertAt(newRow, newRowIndex);
                    }
                    break;
                case InsertType.After:
                    newRowIndex = targetRowIndex < sourceHandles[0] ? targetRowIndex + 1 : targetRowIndex;
                    for (int i = 0; i < draggedRows.Count; i++) {
                        DataRow oldRow = draggedRows[i];
                        DataRow newRow = sourceTable.NewRow();
                        newRow.ItemArray = oldRow.ItemArray;
                        sourceTable.Rows.Remove(oldRow);
                        sourceTable.Rows.InsertAt(newRow, newRowIndex);
                    }
                    break;
                default:
                    newRowIndex = -1;
                    break;
            }
            int insertedIndex = targetGrid.GetRowHandle(newRowIndex);
            targetGrid.FocusedRowHandle = insertedIndex;
            targetGrid.SelectRow(targetGrid.FocusedRowHandle);
        }
        #endregion R E C O R D    D R A G    D R O P
    }
}