using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace WordamentAndroid
{
    public class WordLayout : GridLayout
    {
        public MainActivity.LtrTile[,] _arrTiles;

        List<MainActivity.LtrTile> lstTilesSelected = new List<MainActivity.LtrTile>();

        public WordLayout(Context context) : base(context)
        {
            ((MainActivity)context).AddStatusMsg("wordlayout");
            Id = MainActivity.idGrd;
            ColumnCount = MainActivity._nCols;
            RowCount = MainActivity._nRows;
            SetBackgroundColor(Color.Black);
            _arrTiles = new MainActivity.LtrTile[RowCount, ColumnCount];
            for (int iRow = 0; iRow < RowCount; iRow++)
            {
                for (int iCol = 0; iCol < ColumnCount; iCol++)
                {
                    var ltr = Convert.ToChar(iRow * RowCount + iCol + 65).ToString();
                    var til = new MainActivity.LtrTile(context, ltr, iRow, iCol);
                    _arrTiles[iRow, iCol] = til;
                    //                    til.LayoutParameters = new ViewGroup.LayoutParams()
                    til.Id = 10 + iRow * RowCount + iCol;
                    this.AddView(til);
                }
            }
            var rpg = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.WrapContent);
            rpg.AddRule(LayoutRules.Below, MainActivity.idtxtWordSoFar);
            this.LayoutParameters = rpg;
            this.Touch += WordLayout_Touch;
        }

        private void WordLayout_Touch(object sender, TouchEventArgs e)
        {
        }
    }
}