using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WordMake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WordMakeWindow : Window, INotifyPropertyChanged
    {
        int _seed;
        Random _Random;
        DictionaryLib.DictionaryLib _dictSmall;
        DictionaryLib.DictionaryLib _dictLarge;
        bool _IsUIEnabled = false;
        public bool IsUIEnabled
        {
            get { return _IsUIEnabled; }
            set
            {
                if (value != _IsUIEnabled) { _IsUIEnabled = value; OnPropertyChanged(); }
            }
        }

        public WordMakeWindow()
        {
            InitializeComponent();
            var settings = Properties.Settings.Default;
            this.Top = settings.WindowPos.Height;
            this.Left = settings.WindowPos.Width;
            this.Width = settings.WindowSize.Width;
            this.Height = settings.WindowSize.Height;
            this.Closing += (o, e) =>
              {
                  settings.WindowPos = new System.Drawing.Size((int)this.Left, (int)this.Top);
                  settings.WindowSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
                  settings.Save();
              };
            this.Loaded += (o, e) =>
              {
                  _seed = Environment.TickCount;
                  if (Debugger.IsAttached)
                  {
                      _seed = 1;
                  }
                  _Random = new Random(_seed);
                  _dictSmall = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small, _Random);
                  _dictLarge = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Large, _Random);
                  this.cboAnagramType.ItemsSource = new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
                  this.cboAnagramType.SelectedIndex = 4;
                  this.cboAnagramType.SelectionChanged += (os, es) =>
                  {
                      DoCalculateResultsAync();
                  };
                  ChkAllowDuplication_UnChecked(o, e);
              };
            this.lstResultsSmall.SelectionChanged += (o, e) =>
              {
                  ShowWordInOnlineDict(lstResultsSmall);
              };
            this.lstResultsLarge.SelectionChanged += (o, e) =>
              {
                  ShowWordInOnlineDict(lstResultsLarge);
              };
            void ShowWordInOnlineDict(ListView listView)
            {
                var y = listView.SelectedIndex;
                if (y >= 0)
                {
                    var word = listView.Items[y];
                    System.Diagnostics.Process.Start($"https://www.merriam-webster.com/dictionary/{word}");
                }

            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            DoCalculateResultsAync(GetNewRandomWord: true);
        }

        bool IsCalculatingResults = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        async void DoCalculateResultsAync(bool GetNewRandomWord = false)
        {
            if (!IsCalculatingResults)
            {
                IsCalculatingResults = true;
                IsUIEnabled = false;
                this.lstResultsSmall.ItemsSource = null;
                this.lstResultsLarge.ItemsSource = null;
                this.txtResultCountSmall.Text = string.Empty;
                this.txtResultCountLarge.Text = string.Empty;
                string randword = string.Empty;
                if (GetNewRandomWord || string.IsNullOrEmpty(txtRootWord.Text.Trim()))
                {
                    var isGoodWord = false;
                    await Task.Run(() =>
                    {
                        while (!isGoodWord)
                        {
                            randword = _dictSmall.RandomWord();
                            var randwordLets = randword.OrderBy(l => l).Distinct();
                            if (randwordLets.Count() > 3)
                            {
                                isGoodWord = true;
                            }
                        }
                    });
                    this.txtRootWord.Text = randword;
                }
                else
                {
                    var strRootWord = txtRootWord.Text.Trim();
                    if (strRootWord.Any(s => char.IsWhiteSpace(s)))
                    {
                        txtRootWord.Text = "invalidtext";
                    }
                    randword = txtRootWord.Text.Trim();
                }
                List<string> resultWordsSmall = null;
                List<string> resultWordsLarge = null;
                var anagType = (DictionaryLib.DictionaryLib.AnagramType)cboAnagramType.Items[cboAnagramType.SelectedIndex];
                var DoSubwords = chkAllowDuplication.IsChecked.Value;
                var doLargeDictToo = false;
                if (chkLargeDictToo.IsChecked.Value)
                {
                    doLargeDictToo = true;
                }
                resultWordsSmall = await GetResultsFromDictAsync(_dictSmall);
                this.txtResultCountSmall.Text = resultWordsSmall.Count().ToString();
                this.lstResultsSmall.ItemsSource = resultWordsSmall;

                if (doLargeDictToo)
                {
                    resultWordsLarge = await GetResultsFromDictAsync(_dictLarge);
                    var smDict = resultWordsSmall.ToDictionary(p => p);
                    var lstWrdsOnlyInLarge = new List<string>();
                    foreach (var wrd in resultWordsLarge)
                    {
                        if (!smDict.ContainsKey(wrd))
                        {
                            lstWrdsOnlyInLarge.Add(wrd);
                        }
                    }

                    this.lstResultsLarge.ItemsSource = lstWrdsOnlyInLarge;
                    this.txtResultCountLarge.Text = lstWrdsOnlyInLarge.Count().ToString();
                }
                async Task<List<string>> GetResultsFromDictAsync(DictionaryLib.DictionaryLib dict)
                {
                    List<string> results = null;
                    await Task.Run(() =>
                    {
                        if (DoSubwords)
                        {
                            results = dict.FindSubWordsFromLetters(randword, anagType).ToList();
                        }
                        else
                        {
                            results = dict.FindAnagrams(randword, anagType);
                        }
                    });
                    return results;
                }
                IsUIEnabled = true;
                IsCalculatingResults = false;
            }
        }

        private void ChkAllowDuplication_Checked(object sender, RoutedEventArgs e)
        {
            DoCalculateResultsAync();
        }

        private void ChkAllowDuplication_UnChecked(object sender, RoutedEventArgs e)
        {
            DoCalculateResultsAync();
        }

        private void ChkLargeDictToo_Checked(object sender, RoutedEventArgs e)
        {
            DoCalculateResultsAync();
        }

        private void ChkLargeDictToo_Unchecked(object sender, RoutedEventArgs e)
        {
            DoCalculateResultsAync();
        }

        private void TxtRootWord_LostFocus(object sender, RoutedEventArgs e)
        {
            DoCalculateResultsAync();
        }
    }
}
