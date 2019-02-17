﻿using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WordamentAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        TextView txtStatus;
        TextView txtWordSoFar;
        TextView txtTimer;

        public static int _nCols = 4;
        public static int _nRows = 4;
        const int idBtnNew = 10;
        const int idTxtStatus = 20;
        const int idWordSoFar = 30;
        const int idTimer = 40;
        const int idGrd = 50;
        public static Point _ptScreenSize = new Point(); // X = 1440, Y = 2792

        List<string> lst = new List<string>();
        public void AddStatusMsg(string str)
        {
            if (txtStatus != null)
            {
                if (lst.Count > 2)
                {
                    lst.RemoveAt(0);
                }
                var txt = $"{DateTime.Now.ToString("hh:mm:ss:fff")} {str}";
                lst.Add(txt);
                txtStatus.Text = lst[0] + "\r\n" + (lst.Count > 1 ? lst[1] : string.Empty);
            }
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            WindowManager.DefaultDisplay.GetSize(_ptScreenSize);
            SetContentView(Resource.Layout.activity_main);

            var mainLayout = FindViewById<RelativeLayout>(Resource.Id.container);
            var btnNew = new Button(this)
            {
                Text = $"New {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}",
                Id = idBtnNew
            };
            btnNew.Click += (ob, eb) =>
              {
                  AddStatusMsg($"BtnClick");
              };
            //https://stackoverflow.com/questions/2305395/how-to-lay-out-views-in-relativelayout-programmatically
            var rp = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
            mainLayout.AddView(btnNew, rp);

            txtTimer = new TextView(this)
            {
                Id = idTimer,
                Text = "timer",
                TextSize = 30,
                LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent)
                {

                }
            };
            ((RelativeLayout.LayoutParams)(txtTimer.LayoutParameters)).AddRule(LayoutRules.RightOf, idBtnNew);
            mainLayout.AddView(txtTimer);

            // status, wrdsofar, timer, hint, row,col,  (longword? length)
            txtStatus = new TextView(this)
            {
                Id = idTxtStatus,
                Text = "\r\n",
                LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent)
                {
                }
            };
            ((RelativeLayout.LayoutParams)(txtStatus.LayoutParameters)).AddRule(LayoutRules.Below, idBtnNew);
            mainLayout.AddView(txtStatus);

            txtWordSoFar = new TextView(this)
            {
                Id = idWordSoFar,
                Text = "wordsofar",
                TextSize = 30,
                LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent)
            };
            ((RelativeLayout.LayoutParams)(txtWordSoFar.LayoutParameters)).AddRule(LayoutRules.Below, idTxtStatus);
            mainLayout.AddView(txtWordSoFar);


            var cts = new CancellationTokenSource();
            var tsk = Task.Run(async () =>
            {
                var dtStart = DateTime.Now;
                while (!cts.IsCancellationRequested)
                {
                    RunOnUiThread(() =>
                    {
                        var delt = (int)(DateTime.Now - dtStart).TotalSeconds;
                        txtTimer.Text = $"{GetTimeAsString(delt)}";
                    });
                    await Task.Delay(1000);
                }
            });


            var grd = new GridLayout(this)
            {
                Id = idGrd,
                ColumnCount = _nCols,
                RowCount = _nRows,
                AlignmentMode = GridAlign.Bounds
            };
            grd.SetBackgroundColor(Color.Black);
            for (int iRow = 0; iRow < _nRows; iRow++)
            {
                for (int iCol = 0; iCol < _nCols; iCol++)
                {
                    var ltr = Convert.ToChar(iRow * _nCols + iCol + 65).ToString();
                    var til = new LtrTile(this, ltr, iRow, iCol);
                    //                    til.LayoutParameters = new ViewGroup.LayoutParams()
                    til.Id = 10 + iRow * _nRows + iCol;
                    grd.AddView(til);
                }
            }
            var rpg = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.WrapContent);
            rpg.AddRule(LayoutRules.Below, idWordSoFar);
            grd.LayoutParameters = rpg;
            mainLayout.AddView(grd);

            //var b = new LtrTile(this, "ABCD", 0, 0)
            //{
            //    Id = 3
            //};
            //var rp2 = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
            //rp2.AddRule(LayoutRules.Below, 2);
            //mainLayout.AddView(b, rp2);

            //            grd.AddView(btnNew,)
            grd.Touch += (og, eg) =>
              {
                  var loc = $"{eg.Event.RawX}  {eg.Event.RawY}";
                  AddStatusMsg($"{eg.Event.Action}   {loc}");
                  "".ToString();
                  switch (eg.Event.Action)
                  {
                      case MotionEventActions.Move:
                          break;
                      case MotionEventActions.Down:
                          break;
                      case MotionEventActions.Up:
                          break;
                      case MotionEventActions.Outside:
                          break;
                  }
              };


            //            SetContentView(mainLayout);

            //            SetContentView(Resource.Layout.activity_main);
            //textMessage = FindViewById<TextView>(Resource.Id.message);
            //BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            //navigation.SetOnNavigationItemSelectedListener(this);
        }

        public static string GetTimeAsString(int tmpSecs)
        {
            var hrs = string.Empty;
            var mins = string.Empty;
            var secs = string.Empty;
            if (tmpSecs > 3600)
            {
                hrs = $"{tmpSecs / 3600:n0}:";
                tmpSecs = tmpSecs - (tmpSecs / 3600) * 3600;
            }
            if (!string.IsNullOrEmpty(hrs) || tmpSecs > 60)
            {
                mins = $"{(tmpSecs / 60).ToString(String.IsNullOrEmpty(hrs) ? "" : "00")}:";
                tmpSecs = tmpSecs - (tmpSecs / 60) * 60;
                secs = tmpSecs.ToString("00");
            }
            else
            {
                secs = tmpSecs.ToString();
            }
            return $"{hrs}{mins}{secs}";
            /*
                Public Function GetTimeAsString(tmpSecs As Integer)
                    Dim hrs = String.Empty
                    Dim mins = String.Empty
                    Dim secs = String.Empty
                    If (tmpSecs >= 3600) Then
                        hrs = $"{Int(tmpSecs / 3600):n0}:"
                        tmpSecs = tmpSecs - Int((tmpSecs / 3600)) * 3600
                    End If
                    If Not String.IsNullOrEmpty(hrs) OrElse tmpSecs >= 60 Then
                        mins = $"{Int((tmpSecs / 60)).ToString(If(String.IsNullOrEmpty(hrs), "", "00"))}:"
                        tmpSecs = tmpSecs - Int((tmpSecs / 60)) * 60
                        secs = tmpSecs.ToString("00")
                    Else
                        secs = _CountDownTime.ToString()
                    End If
                    Return $"{hrs}{mins}{secs}"

                End Function

             * */
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_home:
                    //                    textMessage.SetText(Resource.String.title_home);
                    return true;
                case Resource.Id.navigation_dashboard:
                    //                  textMessage.SetText(Resource.String.title_dashboard);
                    return true;
                case Resource.Id.navigation_notifications:
                    //                textMessage.SetText(Resource.String.title_notifications);
                    return true;
            }
            return false;
        }
    }
    class MyGridView : GridView
    {
        public MyGridView(Context context) : base(context)
        {

        }
        public override bool OnTouchEvent(MotionEvent e)
        {
            return base.OnTouchEvent(e);
        }
    }
    class LtrTile : TextView
    {
        readonly static Color g_colorBackground = Color.DarkCyan;
        readonly static Color g_colorSelected = Color.Blue;
        bool _IsSelected = false;
        public int Row { get; set; }
        public int Col { get; set; }
        public LtrTile(Context context, string letter, int row, int col) : base(context)
        {
            Text = letter;
            Row = row; Col = col;
            this.SetBackgroundColor(g_colorBackground);
            this.SetTextColor(Color.White);
            this.TextSize = 60;
            var l = new GridLayout.LayoutParams();
            l.SetMargins(10, 10, 10, 10);
            //            l.SetGravity(GravityFlags.FillHorizontal);
            l.Width = MainActivity._ptScreenSize.X / MainActivity._nCols - 25;

            this.TextAlignment = TextAlignment.Center;
            this.LayoutParameters = l;

            //BackgroundColor = g_colorBackground;
            //FontSize = 40;
            //Margin = new Thickness(2, 2, 2, 2);
            //TextColor = Color.White;
            //CornerRadius = 2;
        }
        public void SelectTile()
        {
            if (!_IsSelected)
            {
                SetBackgroundColor(g_colorSelected);
                _IsSelected = true;
            }
        }
        public override bool OnTouchEvent(MotionEvent e)
        {
            if (!_IsSelected)
            {
                SelectTile();
            }
            else
            {
                UnSelectTile();
            }
            return base.OnTouchEvent(e);
        }

        public void UnSelectTile()
        {
            if (_IsSelected)
            {
                SetBackgroundColor(g_colorBackground);
                _IsSelected = false;
            }
        }

    }
}

