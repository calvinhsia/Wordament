using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public partial class WordMakeWindow : Window
    {
        int _seed;
        Random _Random;
        DictionaryLib.DictionaryLib _dictSmall;
        DictionaryLib.DictionaryLib _dictLarge;
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
                  this.cboAnagramType.ItemsSource = new[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
                  this.cboAnagramType.SelectedIndex = 2;
                  this.cboAnagramType.SelectionChanged += (os, es) =>
                  {
                      DoCalculateResultsAync();
                  };
                  ChkAllowDuplication_UnChecked(o, e);
              };
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            DoCalculateResultsAync(GetNewRandomWord: true);
        }

        bool IsCalculatingResults = false;
        async void DoCalculateResultsAync(bool GetNewRandomWord = false)
        {
            if (!IsCalculatingResults)
            {
                IsCalculatingResults = true;
                this.btnNext.IsEnabled = false;
                this.lstResults.ItemsSource = null;
                string randword = string.Empty;
                if (GetNewRandomWord || string.IsNullOrEmpty(txtRootWord.Text))
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
                    randword = txtRootWord.Text;
                }
                List<string> resultWords = null;
                var anagType = (DictionaryLib.DictionaryLib.AnagramType)cboAnagramType.Items[cboAnagramType.SelectedIndex];
                var DoSubwords = chkAllowDuplication.IsChecked.Value;
                DictionaryLib.DictionaryLib dictTouse = _dictSmall;
                if (chkUseLargeDict.IsChecked.Value)
                {
                    dictTouse = _dictLarge;
                }
                await Task.Run(() =>
                {
                    if (DoSubwords)
                    {
                        resultWords = dictTouse.FindSubWordsFromLetters(randword, anagType).ToList();
                    }
                    else
                    {
                        resultWords = dictTouse.FindAnagrams(randword, anagType);
                    }
                });
                this.txtResultCount.Text = resultWords.Count().ToString();
                this.lstResults.ItemsSource = resultWords;
                this.btnNext.IsEnabled = true;
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

        private void ChkUseLargeDict_Checked(object sender, RoutedEventArgs e)
        {
            DoCalculateResultsAync();
        }

        private void ChkUseLargeDict_Unchecked(object sender, RoutedEventArgs e)
        {
            DoCalculateResultsAync();
        }
    }
}
