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
        public WordMakeWindow()
        {
            InitializeComponent();
            this.Loaded += WordMakeWindow_Loaded;
        }

        private void WordMakeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _seed = Environment.TickCount;
                if (Debugger.IsAttached)
                {
                    _seed = 1;
                }
                _Random = new Random(_seed);
                var dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small, _Random);
                var spControls = new StackPanel()
                {
                    Orientation = Orientation.Vertical
                };
                this.Content = spControls;
                var btnNext = new Button()
                {
                    Content = "_Next",
                    Width = 100
                };
                spControls.Children.Add(btnNext);
                var txtWord = new TextBlock()
                {

                };
                spControls.Children.Add(txtWord);
                var txtSubWordCnt = new TextBlock();
                spControls.Children.Add(txtSubWordCnt);

                var lst = new ListView();
                spControls.Children.Add(lst);
                void BtnNext_OnClick()
                {
                    var isGoodWord = false;
                    string randword = string.Empty;
                    while (!isGoodWord)
                    {
                        randword = dict.RandomWord();
                        var randwordLets = randword.OrderBy(l => l).Distinct();
                        if (randwordLets.Count() > 4)
                        {
                            isGoodWord = true;
                        }
                    }
                    txtWord.Text = randword;
                    var subwords = dict.FindAnagrams(randword, DictionaryLib.DictionaryLib.AnagramType.SubWord5);
//                    var subwords = dict.FindSubWordsFromLetters(randword, DictionaryLib.DictionaryLib.AnagramType.SubWord5);
                    txtSubWordCnt.Text = subwords.Count().ToString();
                    lst.ItemsSource = subwords;

                }
                btnNext.Click += (ob, eb) => BtnNext_OnClick();
                BtnNext_OnClick();
            }
            catch (Exception ex)
            {
                this.Content = ex.ToString();
            }
        }
    }
}
