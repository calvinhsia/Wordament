Imports System.Windows.Controls.Primitives
Imports System.ComponentModel
Imports System.Windows.Threading
Imports System.Threading

Class WordamentWindow
    '.......................................... A  B  C  D  E  F  G  H  I  J   K  L  M  N  O  P  Q   R  S  T  U  V  W  X  Y  Z
    Public Shared _LetterValues() As Integer = {2, 5, 3, 3, 1, 5, 4, 4, 2, 10, 6, 3, 2, 2, 2, 4, 12, 2, 2, 2, 2, 4, 6, 9, 5, 8}
    Public Shared _Random As Random

    Public Property _nRows As Integer = 4

    Public Property _nCols As Integer = 4
    Public Property _IsLongWrd = True
    Public Property _nMinWordLen = 12

    Private _stkCtrls As StackPanel
    Private Shared _txtStatus As TextBox
    Private _seed As Integer
    Private _randLetGenerator As RandLetterGenerator
    Private _resultWords As Dictionary(Of String, LetterList)

    Private _arrTiles(,) As LtrTile
    Private _minWordLength As Integer = 3
    Private _pnl As StackPanel = New StackPanel With {
        .Orientation = Orientation.Horizontal,
        .DataContext = Me
    }

    Private Sub Window_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Try
            Height = 800
            Width = 1000
            Title = "Calvin's Wordament"
            _seed = Environment.TickCount
            If Debugger.IsAttached Then
                _seed = 1
            End If
            _Random = New Random(_seed)
            _randLetGenerator = New RandLetterGenerator
            _stkCtrls = CType(Markup.XamlReader.Load(
                <StackPanel
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    >
                    <Button Name="btnNew" Height="60">_New</Button>
                    <StackPanel Orientation="Horizontal" Width="300">
                        <Label>Rows</Label><TextBox Name="tbxRows" Text="{Binding Path=_nRows}" HorizontalAlignment="Right" Width="50"></TextBox>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <Label>Cols</Label><TextBox Name="tbxCols" Text="{Binding Path=_nCols}" HorizontalAlignment="Right" Width="50"></TextBox>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <CheckBox Name="chkLongWord" IsChecked="{Binding Path=_IsLongWrd}">LongWord</CheckBox><TextBox Text="{Binding Path=_nMinWordLen}" ToolTip="When doing long words, must be at least this length"></TextBox>
                    </StackPanel>
                    <TextBox Name="tbxStatus" Width="300" IsReadOnly="True" AcceptsReturn="True" VerticalScrollBarVisibility="Auto" HorizontalAlignment="Left"></TextBox>
                </StackPanel>.CreateReader
            ), StackPanel)

            Dim btn = CType(_stkCtrls.FindName("btnNew"), Button)
            _txtStatus = CType(_stkCtrls.FindName("tbxStatus"), TextBox)
            _txtStatus.MaxHeight = Math.Min(200, Me.Height - 150)
            AddStatusMsg("starting")

            'Dim tmpSpellDict = New Dictionary.CDict
            'Dim ff = Sub()
            '             For i = 1 To 100
            '                 Dim r = tmpSpellDict.RandWord(1)
            '                 For j = 1 To 1000
            '                     Dim iss = tmpSpellDict.IsWord(r)
            '                 Next
            '             Next
            '         End Sub
            'Dim sw = New Stopwatch()
            'sw.Restart()
            'Dim tt = Task.Run(Sub()
            '                      ff.Invoke
            '                  End Sub)
            'tt.Wait()
            'Dim bthread = sw.Elapsed
            'sw.Start()
            'ff.Invoke()
            'Dim mthread = sw.Elapsed
            'AddStatusMsg($"mt={mthread.TotalSeconds} bg = {bthread.TotalSeconds}")

            Dim isShowingResult = False
            Dim taskGetResults As Task(Of List(Of Dictionary(Of String, LetterList))) = Nothing
            AddHandler btn.Click,
                Async Sub()
                    If Not isShowingResult Then
                        btn.Content = "_Show Results"
                        _pnl.Children.Clear()
                        _pnl.Children.Add(_stkCtrls)
                        Dim grd = New UniformGrid With {
                            .Background = Brushes.Black,
                            .Width = 300,
                            .VerticalAlignment = VerticalAlignment.Top,
                            .Height = 300
                            }
                        '_randLetGenerator.PreSeed(2, 6, _nRows * _nCols)
                        _pnl.Children.Add(grd)
                        AddHandler grd.MouseLeave, Sub()
                                                       AddStatusMsg($"grd.mouseleave")
                                                   End Sub
                        If Me._IsLongWrd Then
                            Dim arr = Await Task.Run(Function() FillGridWithLongWord())
                            _arrTiles = Array.CreateInstance(GetType(LtrTile), _nRows, _nCols)
                            For iRow = 0 To _nRows - 1
                                For iCol = 0 To _nCols - 1
                                    Dim ltr = "a"
                                    If arr(iRow, iCol) = 0 Then
                                        ltr = _randLetGenerator.GetRandLet
                                    Else
                                        ltr = Chr(arr(iRow, iCol))
                                    End If
                                    _arrTiles(iRow, iCol) = New LtrTile(ltr, iRow, iCol, _nCols)
                                    grd.Children.Add(_arrTiles(iRow, iCol))
                                Next
                            Next

                        Else
                            FillGridWithRandomletters(grd)
                        End If
                        taskGetResults = GetResultsAsync()
                    Else
                        btn.Content = "calculating..."
                        Dim res = Await taskGetResults
                        ShowResults(res)
                        btn.Content = "_New"
                    End If
                    isShowingResult = Not isShowingResult
                    Me.Content = _pnl
                    'Width = 800
                    'Height = 800
                End Sub
            '            btn.AddHandler(Button.ClickEvent, New RoutedEventHandler(AddressOf BtnClick))
            btn.RaiseEvent(New RoutedEventArgs With {.RoutedEvent = Button.ClickEvent})
        Catch ex As Exception
            Me.Content = ex.ToString
        End Try
    End Sub

    Friend Shared Sub AddStatusMsg(msg As String)
        msg = $"{DateTime.Now.ToString("hh:mm:ss")} {Thread.CurrentThread.ManagedThreadId} {msg} {vbCrLf}"
        Dim x = _txtStatus.Dispatcher
        _txtStatus.Dispatcher.BeginInvoke(Sub()
                                              _txtStatus.AppendText(msg)
                                              _txtStatus.ScrollToEnd()
                                          End Sub)
    End Sub

    Async Function GetResultsAsync() As Task(Of List(Of Dictionary(Of String, LetterList)))
        Dim res = New List(Of Dictionary(Of String, LetterList))
        Await Task.Run(Sub()
                           For dictnum = 1 To 2
                               AddStatusMsg($"getres {dictnum}")
                               res.Add(CalcWordList(dictnum))
                           Next
                           AddStatusMsg($"getres endtask")
                       End Sub)
        Return res
    End Function

    Sub ShowResults(results As List(Of Dictionary(Of String, LetterList)))
        Dim dictnum = 0
        For Each result In results
            dictnum += 1
            Dim spResult = New StackPanel With {.Orientation = Orientation.Vertical}
            Dim lv As New ListView
            Dim sortedlist = From wrd In result
                             Select wrd.Key,
                                 Points = wrd.Value.Points,
                                 ltrList = wrd.Value
                             Order By Points Descending
                             Select Word = Key,
                             pts = CInt(Points),
                             lst = ltrList

            lv.ItemsSource = sortedlist
            Dim gview = New GridView
            lv.View = gview
            lv.MaxHeight = Me.Height - 100
            gview.Columns.Add(New GridViewColumn With {
                              .Header = New GridViewColumnHeader With {.Content = "Word"},
                              .DisplayMemberBinding = New Binding("Word"),
                              .Width = 130
                            }
                          )
            gview.Columns.Add(New GridViewColumn With {
                              .Header = New GridViewColumnHeader With {.Content = "Points"},
                              .DisplayMemberBinding = New Binding("pts"),
                              .Width = 60
                            }
                          )
            Dim score = Aggregate wrd In result Select pts = wrd.Value.Points Into Sum()

            spResult.Children.Add(New TextBlock With {
                                  .Text = String.Format("Dict# {0} Score = {1:n0}" + vbCrLf + "#Words={2}", dictnum, CInt(score), result.Count)
                              })
            spResult.Children.Add(lv)
            Dim fInHandler = False
            AddHandler lv.SelectionChanged,
                Sub()
                    If lv.SelectedItems.Count > 0 Then
                        If Not fInHandler Then
                            fInHandler = True
                            Dim itm = lv.SelectedItems(0)
                            Dim tdesc = TypeDescriptor.GetProperties(itm)
                            Dim ltrLst = CType(tdesc("lst").GetValue(itm), LetterList)

                            Dim firstTile = _arrTiles(ltrLst(0)._row, ltrLst(0)._col)

                            Dim saveback = firstTile.Background
                            For Each ltr In ltrLst
                                Dim tile = _arrTiles(ltr._row, ltr._col)
                                tile.Background = Brushes.Red
                                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, Function() Nothing)
                                System.Threading.Thread.Sleep(200)
                            Next
                            System.Threading.Thread.Sleep(800)
                            For Each ltr In ltrLst
                                Dim tile = _arrTiles(ltr._row, ltr._col)
                                tile.Background = saveback
                            Next
                            fInHandler = False
                        End If
                    End If
                End Sub

            _pnl.Children.Add(spResult)
        Next

        Dim arrLetDist(25) As Integer ' calc letter dist
        For Each ltr In _arrTiles
            arrLetDist(Asc(ltr._letter._letter) - 65) += 1
        Next
        Dim letDist = String.Empty
        For i = 0 To 25
            letDist += String.Format("{0}={1}" + vbCrLf, Chr(65 + i), arrLetDist(i))
        Next
        _pnl.Children.Add(
            New TextBlock With {
                    .Text = letDist,
                    .ToolTip = "Current Grid letter distribution"}
            )
        _pnl.Children.Add(
            New ListView With {
                .ItemsSource = WordamentWindow._LetterValues,
                .ToolTip = "Points per letter"}
            )
        _pnl.Children.Add(
            New ListView With {
                .ItemsSource = RandLetterGenerator._letDistArr,
                .ToolTip = "#letters in RandLetterGenerator"}
            )
        '_pnl.Children.Add(New ListView With {.ItemsSource = RandLetterGenerator._letDist})
    End Sub

    Dim _lstLongWords As New List(Of String)

    Private Function FillGridWithLongWord() As Integer(,)
        Dim spellDict = New Dictionary.Dictionary(Dictionary.DictionaryType.Small, _Random)
        ' create a list of random directions (N,S, SE, etc) which can be tried in sequence til success
        Dim directions(7) As Integer ' 8 directions
        For i = 0 To 7
            directions(i) = i
        Next
        If _lstLongWords.Count = 0 Then
            spellDict.SeekWord("a")
            While True
                Dim wrd = spellDict.GetNextWord
                If String.IsNullOrEmpty(wrd) Then
                    Exit While
                End If
                If wrd.Length >= _nMinWordLen Then
                    _lstLongWords.Add(wrd)
                End If
            End While
        End If

        Dim isGood = False
        Dim arr(,) As Integer = Nothing ' asc
        Do While Not isGood
            Dim randLongWord = String.Empty
            Dim randnum = _Random.Next(_lstLongWords.Count)
            randLongWord = _lstLongWords(randnum)
            'Dim nTries = 0
            'Do
            '    nTries += 1
            '    randLongWord = spellDict.RandomWord()
            '    If randLongWord.Length > 16 Then
            '        AddStatusMsg($"Got word too long {randLongWord}")
            '        randLongWord = String.Empty
            '    End If
            'Loop While randLongWord.Length < _nMinWordLen
            'AddStatusMsg($"Got long word searching dict {nTries} tries")
            '                randLongWord = "ABCDEFGHIJ"
            randLongWord = randLongWord.ToUpper
            ' now place the word in the grid. Start with a random
            ' randomize the order of the directions we try
            For j = 0 To 7
                Dim r = _Random.Next(8)
                Dim tmp = directions(j)
                directions(j) = directions(r)
                directions(r) = tmp
            Next
            Dim nCalls = 0
            arr = Array.CreateInstance(GetType(Integer), _nRows, _nCols)
            ' Given r,c of empty square with current letter index, put ltr in square
            ' and find a lefit direction return true if is legit (within bounds and not used) 
            Dim recurLam As Func(Of Integer, Integer, Integer, Boolean) =
                        Function(r, c, ndxW) As Boolean
                            nCalls += 1
                            Dim ltr = randLongWord(ndxW)
                            Debug.Assert(arr(r, c) = 0)
                            arr(r, c) = Asc(ltr)
                            If ndxW = randLongWord.Length - 1 Then
                                isGood = True
                                Return True
                            End If
                            For idir = 0 To 7
                                isGood = True
                                Dim newr = r
                                Dim newc = c
                                Select Case directions(idir)
                                    Case 0 ' nw
                                        newr -= 1
                                        newc -= 1
                                    Case 1 ' n
                                        newr -= 1
                                    Case 2 ' ne
                                        newr -= 1
                                        newc += 1
                                    Case 3 ' w
                                        newc -= 1
                                    Case 4 'e
                                        newc += 1
                                    Case 5 'sw
                                        newr += 1
                                        newc -= 1
                                    Case 6 ' s
                                        newr += 1
                                    Case 7 'se
                                        newr += 1
                                        newc += 1
                                End Select
                                If newr < 0 Or newr >= _nRows Or newc < 0 Or newc >= _nCols Then
                                    isGood = False
                                Else
                                    If arr(newr, newc) > 0 Then
                                        isGood = False
                                    End If
                                End If
                                If isGood Then
                                    If recurLam(newr, newc, ndxW + 1) Then
                                        Exit For
                                    Else
                                        isGood = False
                                    End If
                                    If ndxW = randLongWord.Length - 1 Then ' have we placed all letters
                                        Exit For ' exit loop with isgood=true
                                    Else 'else we need to place more, so recur
                                    End If
                                Else   ' couldn't place
                                End If
                            Next
                            If Not isGood Then
                                arr(r, c) = Nothing
                            End If
                            Return isGood
                        End Function
            Dim ncurRow = _Random.Next(_nRows)
            Dim ncurCol = _Random.Next(_nCols)
            isGood = recurLam(ncurRow, ncurCol, 0)
            AddStatusMsg($"NRecurCalls= {nCalls} WrdLn={randLongWord.Length}")
            ' we recurred down and couldn't find a path
        Loop
        Return arr
    End Function

    Private Sub FillGridWithRandomletters(uGrid As UniformGrid)
        _arrTiles = Array.CreateInstance(GetType(LtrTile), _nRows, _nCols)
        For iRow = 0 To _nRows - 1
            For iCol = 0 To _nCols - 1
                Dim rndLet = _randLetGenerator.GetRandLet
                'If rndLet = "Q" Then
                '    rndLet = "QU"
                '    rndLetPts += RandLetterGenerator._LetterValues(Asc("U") - 65)
                'End If
                Dim tile = New LtrTile(rndLet, iRow, iCol, _nRows)
                _arrTiles(iRow, iCol) = tile
                uGrid.Children.Add(tile)
            Next
        Next
    End Sub

    Private _visitedarr(,) As Boolean
    Private _spellDict As Dictionary.Dictionary

    Private Function CalcWordList(dictnum As Integer) As Dictionary(Of String, LetterList)
        _spellDict = New Dictionary.Dictionary(CType(dictnum, Dictionary.DictionaryType), _Random)
        _resultWords = New Dictionary(Of String, LetterList)
        ReDim _visitedarr(_nRows - 1, _nCols - 1)
        For iRow = 0 To _nRows - 1
            For iCol = 0 To _nCols - 1
                VisitCell(iRow, iCol, String.Empty, 0, New LetterList)
            Next
        Next
        Return _resultWords
    End Function

    Private Sub VisitCell(ByVal iRow As Integer,
                          ByVal iCol As Integer,
                          ByVal wordSoFar As String,
                          ByVal ptsSoFar As Integer,
                          ByVal ltrList As LetterList)
        If iRow >= 0 AndAlso iCol >= 0 AndAlso iRow < _nRows AndAlso iCol < _nCols Then
            Dim ltr = _arrTiles(iRow, iCol)
            If Not _visitedarr(iRow, iCol) Then
                wordSoFar += ltr._letter._letter.ToLower
                ptsSoFar += ltr._pts
                ltrList.Add(_arrTiles(iRow, iCol)._letter)
                If wordSoFar.Length >= _minWordLength Then
                    Dim compResult = 0
                    Dim isPartial = _spellDict.SeekWord(wordSoFar, compResult)
                    If compResult = 0 Then
                        If Not _resultWords.ContainsKey(wordSoFar.ToUpper()) Then
                            Dim pts As Double = ptsSoFar
                            If wordSoFar.Length >= 5 Then
                                If wordSoFar.Length = 5 Then
                                    pts *= 1.5
                                ElseIf wordSoFar.Length < 8 Then
                                    pts *= 2
                                Else
                                    pts *= 2.5
                                End If
                            End If
                            _resultWords.Add(wordSoFar.ToUpper(), New LetterList(ltrList, pts)) ' needs to be a copy
                        End If
                    Else
                        ' not in dict so far: let's see if it's a partial match
                        If Not isPartial.StartsWith(wordSoFar) Then
                            ltrList.RemoveAt(ltrList.Count - 1)
                            Return
                        End If
                    End If
                End If
                _visitedarr(iRow, iCol) = True
                VisitCell(iRow - 1, iCol - 1, wordSoFar, ptsSoFar, ltrList)
                VisitCell(iRow - 1, iCol, wordSoFar, ptsSoFar, ltrList)
                VisitCell(iRow - 1, iCol + 1, wordSoFar, ptsSoFar, ltrList)
                VisitCell(iRow, iCol - 1, wordSoFar, ptsSoFar, ltrList)
                VisitCell(iRow, iCol + 1, wordSoFar, ptsSoFar, ltrList)
                VisitCell(iRow + 1, iCol - 1, wordSoFar, ptsSoFar, ltrList)
                VisitCell(iRow + 1, iCol, wordSoFar, ptsSoFar, ltrList)
                VisitCell(iRow + 1, iCol + 1, wordSoFar, ptsSoFar, ltrList)
                ltrList.RemoveAt(ltrList.Count - 1)
                _visitedarr(iRow, iCol) = False
            End If
        End If
    End Sub

    Public Class LetterList
        Inherits List(Of SimpleLetter)
        Public Sub New()
            MyBase.New()
        End Sub
        Private ReadOnly _pts As Integer
        Public Sub New(ByVal lst As LetterList, ByVal pts As Integer)
            MyBase.New()
            _pts = pts
            Me.AddRange(lst)
        End Sub

        Public ReadOnly Property Points As Integer
            Get
                Dim num = _pts
                If _pts = 0 Then
                    num = Aggregate a In Me Select a._pts Into Sum()
                End If
                Return num
            End Get
        End Property
        Public ReadOnly Property Word As String
            Get
                Dim str = New Text.StringBuilder
                For Each ltr In Me
                    str.Append(ltr)
                Next
                Return str.ToString
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return String.Format("{0} {1}", Word, Points)
        End Function
    End Class

    Public Class SimpleLetter
        Public Property _letter As String
        Public _row As Integer
        Public _col As Integer
        Public ReadOnly Property _pts As Integer ' points for this tile
            Get
                Return _LetterValues(Asc(_letter) - 65)
            End Get
        End Property
        Public Sub New(letter As String, row As Integer, col As Integer)
            _letter = letter
            _row = row
            _col = col
        End Sub
        Public Overrides Function ToString() As String
            Return _letter
        End Function
    End Class

    Public Class LtrTile
        Inherits DockPanel
        Shared g_lstItemsSelected As New List(Of LtrTile)
        Shared g_MouseIsDown As Boolean = False
        Dim _isSelected = False

        ReadOnly _row As Integer
        ReadOnly _col As Integer
        Public _letter As SimpleLetter
        Public Sub New(ByVal letter As String, row As Integer, col As Integer, ByVal nTotalCols As Integer)
            _letter = New SimpleLetter(letter, row, col)
            _row = row
            _col = col
            Background = Brushes.DarkSlateBlue

            Margin = New Thickness(2, 2, 2, 2) ' space between tiles

            Dim txt As New TextBlock With {
                .Text = _letter._letter,
                .FontSize = If(nTotalCols > 10, 10, 40 - (nTotalCols - 6) * 5),
                .HorizontalAlignment = HorizontalAlignment.Center,
                .Foreground = Brushes.White
            }
            Me.Children.Add(txt)
            AddHandler txt.MouseDown, Sub()
                                          Me.Background = Brushes.Red
                                          WordamentWindow.AddStatusMsg($"md {Me}")
                                          If Not _isSelected Then
                                              _isSelected = True
                                              g_lstItemsSelected.Add(Me)
                                              g_MouseIsDown = True
                                          End If
                                      End Sub
            AddHandler txt.MouseUp, Sub()
                                        Me.Background = Brushes.DarkSlateBlue
                                        g_MouseIsDown = False
                                        WordamentWindow.AddStatusMsg($"mu {Me}")
                                        For Each t In g_lstItemsSelected
                                            t._isSelected = False
                                            t.Background = Brushes.DarkSlateBlue
                                        Next
                                        g_lstItemsSelected.Clear()
                                    End Sub
            AddHandler txt.MouseMove, Sub()
                                          WordamentWindow.AddStatusMsg($"mm {Me}  MouseIsDown{g_MouseIsDown}")
                                          If g_MouseIsDown Then
                                              Dim lastSelected As LtrTile = Nothing
                                              If g_lstItemsSelected.Count > 0 Then
                                                  lastSelected = g_lstItemsSelected(g_lstItemsSelected.Count - 1)
                                              End If
                                              If Not _isSelected Then
                                                  Dim okToSelect = False
                                                  If lastSelected Is Nothing Then
                                                      okToSelect = True
                                                  Else
                                                      If (((Math.Abs(lastSelected._col - Me._col) <= 1) Or
                                                       (Math.Abs(lastSelected._row - Me._row) <= 1))) Then
                                                          okToSelect = True
                                                      End If
                                                  End If
                                                  If okToSelect Then
                                                      _isSelected = True
                                                      g_lstItemsSelected.Add(Me)
                                                      Me.Background = Brushes.Red
                                                  End If
                                              Else ' if it is selected, user, might have gone back to prior selection
                                                  If (g_lstItemsSelected.Count > 2) Then
                                                      Dim penult = g_lstItemsSelected(g_lstItemsSelected.Count - 2)
                                                      If (penult._row = Me._row AndAlso penult._col = Me._col) Then 'moved back to prior 1

                                                      End If
                                                  End If
                                              End If
                                          End If
                                      End Sub

        End Sub
        Public ReadOnly Property _pts As Integer
            Get
                Return _letter._pts
            End Get
        End Property
        Public Overrides Function ToString() As String
            Return _letter._letter
        End Function
    End Class

    Public Class RandLetterGenerator
        Friend Shared _letDist As String
        Friend Shared _letDistArr(25) As String
        Public Sub New()
            Dim maxScore = Aggregate ltr In _LetterValues Into Max() ' the highest score. e.g. 12
            Dim letdist = String.Empty
            For i = 0 To _LetterValues.Length - 1
                Dim nThisLet = maxScore * 30 / (_LetterValues(i)) 'lcm = 360
                'uncomment this line for even distribution: 
                '   same # of Q's and E's
                'nThisLet = 1
                _letDistArr(i) = nThisLet
                For j = 1 To nThisLet
                    letdist += Chr(i + 65)
                Next
            Next
            _letDist = letdist
        End Sub

        Private ReadOnly _seedArray() As String
        Private _seedIndex As Integer
        'Public Sub PreSeed(ByVal nWords As Integer, ByVal nWordLen As Integer, ByVal nTiles As Integer)
        '    Dim strSeeded = String.Empty
        '    _seedIndex = 0
        '    For i = 1 To nWords
        '        Dim wrd = String.Empty
        '        Do While wrd.Length <> nWordLen
        '            wrd = MainWindow._spellDict.RandWord(Environment.TickCount)
        '        Loop
        '        strSeeded += wrd.ToUpper
        '    Next
        '    For i = strSeeded.Length To nTiles
        '        strSeeded += _letDist.Substring(_Random.Next(_letDist.Length), 1)
        '    Next
        '    ReDim _seedArray(nTiles)
        '    For i = 0 To nTiles - 1
        '        _seedArray(i) = strSeeded(i)
        '    Next
        '    For i = 0 To nTiles - 1
        '        Dim rnd = _Random.Next(nTiles)
        '        Dim tmp = _seedArray(i)
        '        _seedArray(i) = _seedArray(rnd)
        '        _seedArray(rnd) = tmp
        '    Next
        '    'For i = 0 To strSeeded.Length - 1
        '    '    Dim rnd = _Random.Next(strSeeded.Length)
        '    '    Dim tmp = strSeeded(rnd)
        '    '    strSeeded = strSeeded.Substring(0, i - 1) + tmp + strSeeded(i + 1)

        '    'Next
        'End Sub

        Public Function GetRandLet() As String ' letter, score
            Dim rndLet = String.Empty
            If _seedArray Is Nothing Then
                Dim rndNum = _Random.Next(_letDist.Length)
                rndLet = _letDist.Substring(rndNum, 1)
            Else
                rndLet = _seedArray(_seedIndex)
                _seedIndex += 1
            End If
            Return rndLet
        End Function
    End Class
End Class

