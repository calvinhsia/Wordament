using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        static Random _random;
        int _mainThread;
        RandLetterGenerator _randLetterGenerator;
        string _WrdHighestPointsFound; //UPPERCASE

        //.................................... A  B  C  D  E  F  G  H  I  J   K  L  M  N  O  P  Q   R  S  T  U  V  W  X  Y  Z
        public static int[] g_LetterValues = { 2, 5, 3, 3, 1, 5, 4, 4, 2, 10, 6, 3, 2, 2, 2, 4, 12, 2, 2, 2, 2, 4, 6, 9, 5, 8 };
        public static int _nCols = 4;
        public static int _nRows = 4;
        public static int _HintDelay = 2;
        public int _nMinWordLen = 12;
        public bool _IsLongWord = true;
        public const int idBtnNew = 10;
        public const int idTxtStatus = 20;
        public const int idtxtWordSoFar = 30;
        public const int idTimer = 40;
        public const int idGrd = 50;
        public const int idBtnHint = 60;
        public const int idLstResults1 = 70;
        public const int idLstResults2 = 80;
        public bool _IsPaused;
        public LtrTile[,] _arrTiles;

        public static Point _ptScreenSize = new Point(); // X = 1440, Y = 2792

        List<string> lstStatusLines = new List<string>();
        public void AddStatusMsg(string str)
        {

            if (Thread.CurrentThread.ManagedThreadId != _mainThread)
            {
                RunOnUiThread(AddStatMsg);
            }
            else
            {
                AddStatMsg();
            }
            void AddStatMsg()
            {
                if (txtStatus != null)
                {
                    while (lstStatusLines.Count > 1)
                    {
                        lstStatusLines.RemoveAt(0);
                    }
                    var txt = $"{DateTime.Now.ToString("hh:mm:ss:fff")} {str}";
                    lstStatusLines.Add(txt);
                    txtStatus.Text = lstStatusLines[0] + "\r\n" + (lstStatusLines.Count > 1 ? lstStatusLines[1] : string.Empty);
                }
            }
        }

        protected override void OnPause()
        {
            //AddStatusMsg($"Pause");
            base.OnPause();
            this._IsPaused = true;
        }
        protected override void OnResume()
        {
            base.OnResume();
            //AddStatusMsg($"Resume");
            this._IsPaused = false;
        }
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _mainThread = Thread.CurrentThread.ManagedThreadId;
            var seed = 0;
            if (System.Diagnostics.Debugger.IsAttached)
            {
                seed = 1;
            }
            else
            {
                seed = System.Environment.TickCount;
            }
            _random = new Random(seed);
            //            var spellDict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small, _random);
            _randLetterGenerator = new RandLetterGenerator();
            WindowManager.DefaultDisplay.GetSize(_ptScreenSize);
            SetContentView(Resource.Layout.activity_main);

            var mainLayout = FindViewById<RelativeLayout>(Resource.Id.container);
            var btnNew = new Button(this)
            {
                Text = $"Results",
                Id = idBtnNew,
                LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent)
            };
            btnNew.LayoutParameters.Width = 500;
            mainLayout.AddView(btnNew);


            var btnHint = new Button(this)
            {
                Id = idBtnHint,
                Text = "Hint",
                LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent)
            };
            ((RelativeLayout.LayoutParams)(btnHint.LayoutParameters)).AddRule(LayoutRules.RightOf, idBtnNew);
            mainLayout.AddView(btnHint);

            txtTimer = new TextView(this)
            {
                Id = idTimer,
                Text = "timer",
                TextSize = 30,
                LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent)
                {
                }
            };
            ((RelativeLayout.LayoutParams)(txtTimer.LayoutParameters)).AddRule(LayoutRules.RightOf, idBtnHint);
            mainLayout.AddView(txtTimer);

            // status, wrdsofar, timer, hint, row,col,  (longword? length)
            txtStatus = new TextView(this)
            {
                Id = idTxtStatus,
                Text = "\r\n",
                TextSize = 10,
                LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent)
                {
                }
            };
            ((RelativeLayout.LayoutParams)(txtStatus.LayoutParameters)).AddRule(LayoutRules.Below, idBtnNew);
            mainLayout.AddView(txtStatus);

            txtWordSoFar = new TextView(this)
            {
                Id = idtxtWordSoFar,
                Text = "wordsofar",
                TextSize = 20,
                LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent)
            };
            ((RelativeLayout.LayoutParams)(txtWordSoFar.LayoutParameters)).AddRule(LayoutRules.Below, idTxtStatus);
            mainLayout.AddView(txtWordSoFar);

            var grd = new GridLayout(this)
            {
                Id = idGrd,
                ColumnCount = _nCols,
                RowCount = _nRows,
                AlignmentMode = GridAlign.Bounds
            };
            grd.SetBackgroundColor(Color.Black);
            var rpg = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.WrapContent);
            rpg.AddRule(LayoutRules.Below, idtxtWordSoFar);
            grd.LayoutParameters = rpg;

            mainLayout.AddView(grd);

            ListView lstResults1 = new ListView(this)
            {
                Id = idLstResults1,
                LayoutParameters = new RelativeLayout.LayoutParams(_ptScreenSize.X / 2, RelativeLayout.LayoutParams.WrapContent)
            };
            mainLayout.AddView(lstResults1);
            ((RelativeLayout.LayoutParams)(lstResults1.LayoutParameters)).AddRule(LayoutRules.Below, idGrd);

            ListView lstResults2 = new ListView(this)
            {
                Id = idLstResults2,
                LayoutParameters = new RelativeLayout.LayoutParams(_ptScreenSize.X / 2, RelativeLayout.LayoutParams.WrapContent)
            };
            mainLayout.AddView(lstResults2);
            ((RelativeLayout.LayoutParams)(lstResults2.LayoutParameters)).AddRule(LayoutRules.Below, idGrd);
            ((RelativeLayout.LayoutParams)(lstResults2.LayoutParameters)).AddRule(LayoutRules.RightOf, idLstResults1);


            WordScoreAdapter scoreAdapter1 = null;
            WordScoreAdapter scoreAdapter2 = null;
            var IsHighlighting = false;
            async Task DoResultItemClick(object o, AdapterView.ItemClickEventArgs e, WordScoreAdapter scoreAdapter)
            {
                if (!IsHighlighting)
                {
                    IsHighlighting = true;
                    btnNew.Enabled = false;
                    for (int iRow = 0; iRow < _nRows; iRow++) // left over selection from user input
                    {
                        for (int iCol = 0; iCol < _nCols; iCol++)
                        {
                            if (_arrTiles[iRow,iCol]._IsSelected)
                            {
                                _arrTiles[iRow, iCol].UnSelectTile();
                            }
                        }
                    }
                    var wrd = scoreAdapter[e.Position];
                    var ltrLst = wrd.LtrList;
                    var firstTile = _arrTiles[ltrLst[0]._row, ltrLst[0]._col];
                    var saveBackground = firstTile.Background;
                    foreach (var ltr in wrd.LtrList)
                    {
                        var tile = _arrTiles[ltr._row, ltr._col];
                        tile.SetBackgroundColor(Color.Red);
                        await Task.Delay(400);
                    }
                    await Task.Delay(1000);
                    foreach (var ltr in wrd.LtrList)
                    {
                        var tile = _arrTiles[ltr._row, ltr._col];
                        tile.SetBackgroundColor(LtrTile.g_colorBackground);
                    }
                    IsHighlighting = false;
                    btnNew.Enabled = true;
                }
                //Android.Widget.Toast.MakeText(this, res1[e.Position].ToString(), Android.Widget.ToastLength.Long).Show();
            }
            lstResults1.ItemClick += async (o, e) =>
            {
                await DoResultItemClick(o, e, scoreAdapter1);
            };
            lstResults2.ItemClick += async (o, e) =>
            {
                await DoResultItemClick(o, e, scoreAdapter2);
            };


            var IsShowingResult = true;
            var fdidFinish = false;
            Task<List<Dictionary<string, LetterList>>> taskGetResultsAsync = null;
            List<Dictionary<string, LetterList>> lstDictResults = null;
            var dtLastHInt = DateTime.Now;
            var nLastHintNum = 0;
            btnHint.Click += async (o, e) =>
              {
                  if (taskGetResultsAsync != null && taskGetResultsAsync.IsCompleted)
                  {
                      if (nLastHintNum < _WrdHighestPointsFound.Length - 1)
                      {
                          nLastHintNum++;
                          AddStatusMsg($"Hint {nLastHintNum} {_WrdHighestPointsFound.Substring(0, nLastHintNum)}");
                          btnHint.Enabled = false;
                          if (nLastHintNum < _WrdHighestPointsFound.Length - 1)
                          {
                              await Task.Delay(TimeSpan.FromSeconds(_HintDelay));
                              btnHint.Enabled = true;
                          }
                      }
                  }
              };

            var timerEnabled = false;
            CancellationTokenSource cts = null;
            void Showresults()
            {
                cts?.Cancel();
                btnHint.Enabled = false;
                fdidFinish = false;
                IsShowingResult = true;
                timerEnabled = false;
                btnNew.Text = "Calc...";
                var dictCommonResults = lstDictResults[1];
                var dictObscureResults = lstDictResults[0];
                foreach (var kvp in dictCommonResults)
                {
                    if (dictObscureResults.ContainsKey(kvp.Key))
                    {
                        dictObscureResults.Remove(kvp.Key);
                    }
                }
                scoreAdapter1 = new WordScoreAdapter(this, dictCommonResults);
                scoreAdapter2 = new WordScoreAdapter(this, dictObscureResults);
                lstResults1.Adapter = scoreAdapter1;
                lstResults2.Adapter = scoreAdapter2;
                btnNew.Text = "New";
            }

            await BtnNewClick(null, null);
            var gridCanRespond = false;
            async Task BtnNewClick(object o, EventArgs e)
            {
                IsShowingResult = !IsShowingResult;
                if (!IsShowingResult)
                {
                    fdidFinish = false;
                    if (taskGetResultsAsync != null)
                    {
                        await taskGetResultsAsync;
                        taskGetResultsAsync = null;
                    }
                    lstDictResults = null;
                    nLastHintNum = 0;
                    lstResults1.Adapter = null;
                    lstResults2.Adapter = null;
                    btnNew.Text = "Results";
                    gridCanRespond = false;
                    grd.RemoveAllViews();
                    txtWordSoFar.Text = string.Empty;
                    await FillGridWithTilesAsync(grd);
                    gridCanRespond = true;
                    var nSecondsElapsed = 0;
                    cts = new CancellationTokenSource();
                    var tskTimer = Task.Run(async () =>
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            RunOnUiThread(() =>
                            {
                                if (timerEnabled && !_IsPaused)
                                {
                                    txtTimer.Text = $"{GetTimeAsString(nSecondsElapsed)}";
                                    nSecondsElapsed++;
                                }
                            });
                            await Task.Delay(1000);
                        }
                    });
                    btnNew.Enabled = false;
                    txtTimer.Text = string.Empty;
                    nSecondsElapsed = 0;
                    timerEnabled = true;
                    btnHint.Enabled = false;
                    taskGetResultsAsync = GetResultsAsync();
                    await taskGetResultsAsync;
                    lstDictResults = taskGetResultsAsync.Result;
                    btnNew.Enabled = true;
                    await Task.Delay(TimeSpan.FromSeconds(_HintDelay));
                    btnHint.Enabled = true;
                }
                else
                {
                    Showresults();
                }
            }
            btnNew.Click += (ob, eb) =>
            {
                var task = BtnNewClick(ob, eb);
            };

            var lstTilesSelected = new List<LtrTile>();
            void UpdateWordSoFar()
            {
                var txt = string.Empty;
                foreach (var tile in lstTilesSelected)
                {
                    txt += tile.ToString();
                }
                txtWordSoFar.Text = txt;
                if (txt == _WrdHighestPointsFound)
                {
                    if (!fdidFinish)
                    {
                        fdidFinish = true;
                        cts.Cancel();
                        var res = $"Got answer in {txtTimer.Text} {_WrdHighestPointsFound} Hints={nLastHintNum}";
                        AddStatusMsg(res);
                        ClearSelection(); // beware recursion
                        //Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                        //alert.SetTitle("Calvin's Wordament");
                        //alert.SetMessage(res);
                        //alert.SetPositiveButton("Ok", (o, e) => { });
                        //var dlog = alert.Create();
                        //dlog.Show();
                        var t = BtnNewClick(null, null);
                    }
                }
            }
            void ClearSelection()
            {
                foreach (var tile in lstTilesSelected)
                {
                    tile.UnSelectTile();
                }
                lstTilesSelected.Clear();
                UpdateWordSoFar();
            }
            LtrTile GetTileFromTouch(View.TouchEventArgs eg)
            {
                // grd.width = 1440, grd.Height = 1532 Til.width = 335, til.height=363
                int[] locg = new int[2];
                grd.GetLocationInWindow(locg);
                var ptRel = new Point()
                {
                    X = (int)eg.Event.RawX - locg[0],
                    Y = (int)eg.Event.RawY - locg[1]
                };
                var col = (int)((ptRel.X) * _nCols / grd.Width);
                var row = (int)((ptRel.Y) * _nRows / grd.Height);
                LtrTile tile = null;
                if (col >= 0 && col < _nCols && row >= 0 && row < _nRows)
                {
                    tile = _arrTiles[row, col];
                    var pointCtr = new Point()
                    {
                        X = col * grd.Width / _nCols + tile.Width / 2,
                        Y = row * grd.Height / _nRows + tile.Height / 2
                    };
                    var distToCtrOfFileSquared = Math.Pow(ptRel.X - pointCtr.X, 2) + Math.Pow(ptRel.Y - pointCtr.Y, 2);
                    //                    AddStatusMsg($"{eg.Event.Action.ToString().Substring(0, 1)} ({eg.Event.RawX},{eg.Event.RawY}) {distToCtrOfFileSquared:n0} {tile?.Text}");
                    if (distToCtrOfFileSquared > tile.Width * tile.Height / 6)
                    {
                        tile = null;
                    }
                }
                return tile;
            }
            grd.Touch += (og, eg) =>
              {
                  try
                  {
                      if (!IsShowingResult)
                      {
                          switch (eg.Event.Action)
                          {
                              case MotionEventActions.Down:
                              case MotionEventActions.Move:
                                  var ltrTile = GetTileFromTouch(eg);
                                  if (ltrTile != null)
                                  {
                                      LtrTile priorSelected = null;
                                      if (lstTilesSelected.Count > 0)
                                      {
                                          priorSelected = lstTilesSelected[lstTilesSelected.Count - 1];
                                      }
                                      if (ltrTile._IsSelected)
                                      {
                                          if (lstTilesSelected.Count > 1)
                                          {
                                              var tilePenultimate = lstTilesSelected[lstTilesSelected.Count - 2];
                                              // AddStatusMsg($"{tilePenultimate} {priorSelected} {ltrTile}");
                                              if (ltrTile.Row == tilePenultimate.Row && ltrTile.Col == tilePenultimate.Col)
                                              {// back to prior one
                                                  priorSelected.UnSelectTile();
                                                  lstTilesSelected.RemoveAt(lstTilesSelected.Count - 1);
                                                  UpdateWordSoFar();
                                              }
                                          }
                                      }
                                      else
                                      {
                                          var okToSelect = false;
                                          if (priorSelected == null)
                                          {
                                              okToSelect = true;
                                          }
                                          else
                                          {
                                              var dist = Math.Pow(priorSelected.Col - ltrTile.Col, 2) + Math.Pow(priorSelected.Row - ltrTile.Row, 2);
                                              if (dist <= 2)
                                              {
                                                  okToSelect = true;
                                              }
                                              else
                                              {
                                                  //                                          AddStatusMsg($"nosel {priorSelected} {ltrTile} {dist}");
                                              }
                                          }
                                          if (okToSelect)
                                          {
                                              ltrTile.SelectTile();
                                              lstTilesSelected.Add(ltrTile);
                                              UpdateWordSoFar();
                                          }
                                      }
                                  }
                                  break;

                              case MotionEventActions.Up:
                                  ClearSelection();
                                  break;
                              case MotionEventActions.Outside:
                                  break;
                          }
                      }

                  }
                  catch (Exception ex)
                  {
                      AddStatusMsg($"GRD {ex.ToString()}");
                  }
              };


            //            SetContentView(mainLayout);

            //            SetContentView(Resource.Layout.activity_main);
            //textMessage = FindViewById<TextView>(Resource.Id.message);
            //BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            //navigation.SetOnNavigationItemSelectedListener(this);
        }

        List<string> _lstLongWords = new List<string>();
        private async Task FillGridWithTilesAsync(GridLayout grd)
        {
            _arrTiles = new LtrTile[_nRows, _nCols];
            char[,] arr = new char[_nRows, _nCols];
            // get array filled on background  thread

            if (_IsLongWord)
            {
                await Task.Run(() =>
                   {
                       var spellDict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small, _random);
                       int[] directions = new int[8];
                       for (int i = 0; i < 8; i++)
                       {
                           directions[i] = i;
                       }
                       spellDict.SeekWord("a");
                       if (_lstLongWords.Count == 0)
                       {
                           while (true)
                           {
                               var wrd = spellDict.GetNextWord();
                               if (string.IsNullOrEmpty(wrd))
                               {
                                   break;
                               }
                               if (wrd.Length >= _nMinWordLen && wrd.Length <= _nRows * _nCols)
                               {
                                   _lstLongWords.Add(wrd.ToUpper());
                               }
                           }
                       }
                       var isGood = false;
                       int nRecurCalls = 0;
                       while (!isGood)
                       {
                           var randnum = _random.Next(_lstLongWords.Count);
                           var randLongWrd = _lstLongWords[randnum];
                           // attempt to place in array
                           // shuffle directions
                           for (int i = 0; i < 8; i++)
                           {
                               var r = _random.Next(8);
                               var tmp = directions[i];
                               directions[i] = directions[r];
                               directions[r] = tmp;
                           }
                           bool recurLam(int r, int c, int ndx)
                           {
                               nRecurCalls++;
                               var ltr = randLongWrd[ndx];
                               arr[r, c] = ltr;
                               if (ndx == randLongWrd.Length - 1)
                               {
                                   isGood = true;
                                   return true;
                               }
                               for (int idir = 0; idir < 8; idir++)
                               {
                                   isGood = true;
                                   var newr = r;
                                   var newc = c;
                                   switch (directions[idir])
                                   {
                                       case 0: // nw
                                           newr--;
                                           newc--;
                                           break;
                                       case 1: // n
                                           newr -= 1;
                                           break;
                                       case 2: // ne
                                           newr--;
                                           newc++;
                                           break;
                                       case 3: // w
                                           newc--;
                                           break;
                                       case 4: // e
                                           newc++;
                                           break;
                                       case 5: // sw
                                           newr++;
                                           newc--;
                                           break;
                                       case 6:// s
                                           newr++;
                                           break;
                                       case 7:// se
                                           newr++;
                                           newc++;
                                           break;
                                   }
                                   if (newr < 0 || newr >= _nRows || newc < 0 || newc >= _nCols)
                                   {
                                       isGood = false;
                                   }
                                   else
                                   {
                                       if (arr[newr, newc] != '\0')
                                       {
                                           isGood = false;
                                       }
                                   }
                                   if (isGood)
                                   {
                                       if (recurLam(newr, newc, ndx + 1))
                                       {
                                           break;
                                       }
                                       isGood = false;
                                   }
                               }
                               if (!isGood)
                               {
                                   arr[r, c] = '\0';
                               }
                               return isGood;
                           }
                           isGood = recurLam(_random.Next(_nRows), _random.Next(_nCols), 0);
                           AddStatusMsg($"NRecurCalls ={nRecurCalls} WrdLen={randLongWrd.Length}");
                       }
                   });
            }
            else
            {
                for (int iRow = 0; iRow < _nRows; iRow++)
                {
                    for (int iCol = 0; iCol < _nCols; iCol++)
                    {
                        //                        var ltr = Convert.ToChar(iRow * _nCols + iCol + 65).ToString();
                        var ltr = _randLetterGenerator.GetRandLet();
                        arr[iRow, iCol] = ltr[0];
                        //                    til.LayoutParameters = new ViewGroup.LayoutParams()
                    }
                }
            }
            // set tiles on foreground
            for (int iRow = 0; iRow < _nRows; iRow++)
            {
                for (int iCol = 0; iCol < _nCols; iCol++)
                {
                    var ltr = arr[iRow, iCol];
                    if (ltr == '\0')
                    {
                        ltr = _randLetterGenerator.GetRandLet()[0];
                    }
                    var til = new LtrTile(this, ltr.ToString(), iRow, iCol);
                    _arrTiles[iRow, iCol] = til;
                    //                    til.LayoutParameters = new ViewGroup.LayoutParams()
                    til.Id = 10 + iRow * _nRows + iCol;
                    grd.AddView(til);
                }
            }
        }

        async Task<List<Dictionary<string, LetterList>>> GetResultsAsync()
        {
            var res = new List<Dictionary<string, LetterList>>();
            await Task.Run(() =>
            {
                foreach (DictionaryLib.DictionaryType dictnum in Enum.GetValues(typeof(DictionaryLib.DictionaryType)))
                {
                    var oneresult = CalcWordList(dictnum);
                    res.Add(oneresult);
                    if (dictnum == DictionaryLib.DictionaryType.Large)
                    {
                        _WrdHighestPointsFound = oneresult.OrderByDescending(kvp => kvp.Value.Points).FirstOrDefault().Value.Word;
                    }
                }
            });
            return res;
        }

        Dictionary<string, LetterList> CalcWordList(DictionaryLib.DictionaryType dictnum)
        {
            var resultWords = new Dictionary<string, LetterList>();
            var spellDict = new DictionaryLib.DictionaryLib(dictnum, _random);
            bool[,] arrVisited = new bool[_nRows, _nCols];
            void VisitCell(int iRow, int iCol, string wordSoFar, int ptsSoFar, LetterList ltrList)
            {
                if (iRow >= 0 && iCol >= 0 && iRow < _nRows && iCol < _nCols)
                {
                    var ltr = _arrTiles[iRow, iCol];
                    if (!arrVisited[iRow, iCol])
                    {
                        wordSoFar += ltr.Letter.ToLower();
                        ptsSoFar += ltr.Points;
                        ltrList.Add(_arrTiles[iRow, iCol]._letter);
                        if (wordSoFar.Length >= 3)
                        {
                            var isPartial = spellDict.SeekWord(wordSoFar, out var compResult);
                            if (!string.IsNullOrEmpty(isPartial) && compResult == 0)
                            {
                                if (!resultWords.ContainsKey(wordSoFar.ToUpper()))
                                {
                                    double pts = ptsSoFar;
                                    if (wordSoFar.Length >= 5)
                                    {
                                        pts *= 1.5;
                                    }
                                    else if (wordSoFar.Length < 8)
                                    {
                                        pts *= 2;
                                    }
                                    else
                                    {
                                        pts *= 2.5;
                                    }
                                    resultWords.Add(wordSoFar.ToUpper(), new LetterList(ltrList, (int)pts));
                                }
                            }
                            else
                            {// not in dict so far: see if partial match
                                if (!isPartial.StartsWith(wordSoFar))
                                {
                                    ltrList.RemoveAt(ltrList.Count - 1);
                                    return;
                                }
                            }
                        }
                        arrVisited[iRow, iCol] = true;
                        VisitCell(iRow - 1, iCol - 1, wordSoFar, ptsSoFar, ltrList);
                        VisitCell(iRow - 1, iCol, wordSoFar, ptsSoFar, ltrList);
                        VisitCell(iRow - 1, iCol + 1, wordSoFar, ptsSoFar, ltrList);
                        VisitCell(iRow, iCol - 1, wordSoFar, ptsSoFar, ltrList);
                        VisitCell(iRow, iCol + 1, wordSoFar, ptsSoFar, ltrList);
                        VisitCell(iRow + 1, iCol - 1, wordSoFar, ptsSoFar, ltrList);
                        VisitCell(iRow + 1, iCol, wordSoFar, ptsSoFar, ltrList);
                        VisitCell(iRow + 1, iCol + 1, wordSoFar, ptsSoFar, ltrList);
                        ltrList.RemoveAt(ltrList.Count - 1);
                        arrVisited[iRow, iCol] = false;
                    }
                }
            }
            for (int iRow = 0; iRow < _nRows; iRow++)
            {
                for (int iCol = 0; iCol < _nCols; iCol++)
                {
                    VisitCell(iRow, iCol, string.Empty, 0, new LetterList());
                }
            }
            return resultWords;
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

        public class WordScore
        {
            public string Word { get; set; }
            public int Points { get; set; }
            public LetterList LtrList { get; set; }
            public override string ToString()
            {
                return $"{Word} |{Points}";
            }
        }
        public class WordScoreAdapter : BaseAdapter<WordScore>
        {
            public List<WordScore> _lst;
            readonly Context _context;
            public WordScoreAdapter(Context context, Dictionary<string, LetterList> result)
            {
                this._context = context;
                var listWords = from kvp in result
                                orderby kvp.Value.Points descending
                                select new
                                {
                                    Word = kvp.Key,
                                    Pts = kvp.Value.Points,
                                    ltrList = kvp.Value
                                };
                _lst = new List<WordScore>();
                foreach (var w in listWords)
                {
                    _lst.Add(new WordScore()
                    {
                        Word = w.Word,
                        Points = w.Pts,
                        LtrList = w.ltrList
                    });
                }
            }
            public override WordScore this[int position] => _lst[position];

            public override int Count => _lst.Count;

            public override long GetItemId(int position)
            {
                return position;
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                View viewRow = null;
                try
                {
                    viewRow = LayoutInflater.From(_context).Inflate(Resource.Layout.ResultsRow, parent, attachToRoot: false);
                    var txtWord = viewRow.FindViewById<TextView>(Resource.Id.textViewWord);
                    txtWord.Text = _lst[position].Word;
                    var txtPoints = viewRow.FindViewById<TextView>(Resource.Id.textViewPoints);
                    txtPoints.Text = _lst[position].Points.ToString();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
                return viewRow;
            }
        }

        public class RandLetterGenerator
        {
            internal static int[] _letDistArr = new int[26];
            internal static string _letDist; // " aaaabbb...qrrrrrssssssstttt
            public RandLetterGenerator()
            {
                var maxScore = g_LetterValues.Max(); // highest score of any letter, e.g. 1=12
                _letDist = string.Empty;
                for (int i = 0; i < g_LetterValues.Length; i++)
                {
                    var nThisLet = maxScore * 30 / g_LetterValues[i]; // lcm = 360
                    _letDistArr[i] = nThisLet;
                    for (int j = 0; j < nThisLet; j++)
                    {
                        _letDist += Convert.ToChar(i + 65);
                    }
                }
            }
            public string GetRandLet()
            {
                var rnd = _random.Next(_letDist.Length);
                return _letDist.Substring(rnd, 1);
            }
        }

        public class LetterList : List<SimpleLetter>
        {
            readonly int _pts;
            public int Points
            {
                get
                {
                    var pts = _pts;
                    return pts;
                }
            }
            public LetterList() : base()
            {

            }
            public LetterList(LetterList lst, int pts)
            {
                this.AddRange(lst);
                this._pts = pts;
            }
            public string Word
            {
                get
                {
                    var str = new StringBuilder();
                    foreach (var ltr in this)
                    {
                        str.Append(ltr);
                    }
                    return str.ToString();
                }
            }
            public override string ToString()
            {
                return base.ToString();
            }
        }

        public class SimpleLetter
        {
            public string _letter;
            public int _row;
            public int _col;
            public int _pts
            {
                get
                {
                    return g_LetterValues[Convert.ToByte(_letter[0] - 65)];
                }
            }
            public SimpleLetter(string letter, int row, int col)
            {
                _letter = letter;
                _row = row;
                _col = col;
            }
            public override string ToString()
            {
                return _letter;
            }
        }
        public class LtrTile : TextView
        {
            public readonly static Color g_colorBackground = Color.DarkCyan;
            public readonly static Color g_colorSelected = Color.Blue;
            public const int margin = 10;
            public bool _IsSelected = false;
            public int Row { get; set; }
            public int Col { get; set; }
            public string Letter { get { return Text; } }
            public int Points { get { return _letter._pts; } }
            public SimpleLetter _letter;
            public LtrTile(Context context, string letter, int row, int col) : base(context)
            {
                _letter = new SimpleLetter(letter, row, col);
                Text = letter;
                Row = row; Col = col;
                this.SetBackgroundColor(g_colorBackground);
                this.SetTextColor(Color.White);
                this.TextSize = 50;
                var l = new GridLayout.LayoutParams();
                l.SetMargins(margin, margin, margin, margin);
                //            l.SetGravity(GravityFlags.FillHorizontal);
                l.Width = MainActivity._ptScreenSize.X / MainActivity._nCols - 2 * margin;

                this.TextAlignment = TextAlignment.Center;
                this.LayoutParameters = l;
            }
            public void SelectTile()
            {
                if (!_IsSelected)
                {
                    SetBackgroundColor(g_colorSelected);
                    _IsSelected = true;
                }
            }
            public void UnSelectTile()
            {
                if (_IsSelected)
                {
                    SetBackgroundColor(g_colorBackground);
                    _IsSelected = false;
                }
            }
            public override string ToString()
            {
                return Text;
            }

        }
    }

}

