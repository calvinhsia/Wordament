using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System;

namespace WordamentAndroid
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        TextView textMessage = null;
        public static int _nCols = 4;
        public static int _nRows = 4;
        public static Point _ptScreenSize = new Point(); // X = 1440, Y = 2792
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            WindowManager.DefaultDisplay.GetSize(_ptScreenSize);
            SetContentView(Resource.Layout.activity_main);

//            var stextMessage = FindViewById<TextView>(Resource.Id.message1);

            var mainLayout = FindViewById<RelativeLayout>(Resource.Id.container);
            //            var mainLayout = new RelativeLayout(this);
            var btnNew = new Button(this)
            {
                Text = $"New {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}",
                Id = 1
            };
            //https://stackoverflow.com/questions/2305395/how-to-lay-out-views-in-relativelayout-programmatically
            var rp = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
            mainLayout.AddView(btnNew, rp);
            // status, wrdsofar, timer, hint, row,col,  (longword? length)
            var grd = new GridLayout(this)
            {
                Id = 2,
                ColumnCount = _nCols,
                RowCount = _nRows,
                AlignmentMode = GridAlign.Bounds
            };
            grd.SetBackgroundColor(Color.Black);
            for (int iRow = 0; iRow < _nRows; iRow++)
            {
                for (int iCol = 0; iCol < _nCols; iCol++)
                {
                    var til = new LtrTile(this, "A", iRow, iCol);
                    //                    til.LayoutParameters = new ViewGroup.LayoutParams()
                    til.Id = 10 + iRow * _nRows + iCol;
                    grd.AddView(til);
                }
            }
            var rpg = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.WrapContent);
            rpg.AddRule(LayoutRules.Below, 1);
            grd.LayoutParameters = rpg;
            mainLayout.AddView(grd);

            var b = new LtrTile(this, "ABCD", 0, 0)
            {
                Id = 3
            };
            var rp2 = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
            rp2.AddRule(LayoutRules.Below, 2);
            mainLayout.AddView(b, rp2);

            //            grd.AddView(btnNew,)
            grd.Touch += (og, eg) =>
              {
                  "".ToString();
                  switch (eg.Event.Action)
                  {
                      case MotionEventActions.Move:
                          "".ToString();
                          var loc = $"{eg.Event.RawX}  {eg.Event.RawY}";
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
        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_home:
                    textMessage.SetText(Resource.String.title_home);
                    return true;
                case Resource.Id.navigation_dashboard:
                    textMessage.SetText(Resource.String.title_dashboard);
                    return true;
                case Resource.Id.navigation_notifications:
                    textMessage.SetText(Resource.String.title_notifications);
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

