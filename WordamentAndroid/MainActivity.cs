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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var relLayout = new RelativeLayout(this);
            var btnNew = new Button(this)
            {
                Text = $"New {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}",
                Id = 1
            };
            //https://stackoverflow.com/questions/2305395/how-to-lay-out-views-in-relativelayout-programmatically
            var rp = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
            relLayout.AddView(btnNew, rp);
            //            var lp = new RelativeLayout.LayoutParams(10, 40);
            var b = new LtrTile(this, "ABCD", 0, 0);
            b.Id = 2;
            var rp2 = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
            relLayout.AddView(b, rp2);
            rp2.AddRule(LayoutRules.Below, 1);
            SetContentView(relLayout);

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
                //                this.BackgroundColor = g_colorSelected;
                _IsSelected = true;
            }
        }

        public void UnSelectTile()
        {
            if (_IsSelected)
            {
                //              this.BackgroundColor = g_colorBackground;
                _IsSelected = false;
            }
        }

    }
}

