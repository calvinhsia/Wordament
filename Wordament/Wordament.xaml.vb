﻿Imports System.Windows.Controls.Primitives
Imports System.ComponentModel
Imports System.Windows.Threading
Imports System.Threading
Imports System.Runtime.CompilerServices
Imports DictionaryLib

Class WordamentWindow : Implements INotifyPropertyChanged

    '........................................... A  B  C  D  E  F  G  H  I  J   K  L  M  N  O  P  Q   R  S  T  U  V  W  X  Y  Z
    Public Shared g_LetterValues() As Integer = {2, 5, 3, 3, 1, 5, 4, 4, 2, 10, 6, 3, 2, 2, 2, 4, 12, 2, 2, 2, 2, 4, 6, 9, 5, 8}
    Public Shared g_Random As Random

    Public Property _nRows As Integer = 4

    Public Property _nCols As Integer = 4


    Private _CountDownTime As Integer
    Private Property CountDownTime As Integer
        Get
            Return _CountDownTime
        End Get
        Set(value As Integer)
            _CountDownTime = value
            Me.OnMyPropertyChanged("CountDownTimeStr")
        End Set
    End Property

    Public Shared Function GetTimeAsString(nSecs As Integer)
        Dim hrs = String.Empty
        Dim mins = String.Empty
        Dim secs
        If (nSecs >= 3600) Then
            hrs = $"{Int(nSecs / 3600):n0}:"
            nSecs -= Int((nSecs / 3600)) * 3600
        End If
        If Not String.IsNullOrEmpty(hrs) OrElse nSecs >= 60 Then
            mins = $"{Int((nSecs / 60)).ToString(If(String.IsNullOrEmpty(hrs), "", "00"))}:"
            nSecs -= Int((nSecs / 60)) * 60
            secs = nSecs.ToString("00")
        Else
            secs = nSecs.ToString()
        End If
        Return $"{hrs}{mins}{secs}"

    End Function
    Public Property CountDownTimeStr As String
        Get
            Return GetTimeAsString(_CountDownTime)
        End Get
        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property

    Dim _strWordSofar As String
    Dim _WrdHighestPointsFound As String ' UPPERCASE
    Public Property StrWordSoFar As String
        Get
            Return _strWordSofar
        End Get
        Set(value As String)
            If value <> _strWordSofar Then
                _strWordSofar = value
                Me.OnMyPropertyChanged()
            End If
        End Set
    End Property

    ''' <summary>
    ''' If we're doing long words
    ''' </summary>
    ''' <returns></returns>
    Public Property _IsLongWrd = True
    Dim _nMinWordLen = 12
    Public Property MinWordLen
        Get
            Return _nMinWordLen
        End Get
        Set(value)
            If (value <= _nCols * _nRows) Then
                _nMinWordLen = value
            Else
                _txtMinWordLen.Text = _nMinWordLen.ToString()
                Me.OnMyPropertyChanged()
            End If
        End Set
    End Property
    Private _HintAvailable As Boolean
    Private _HintDelay As Integer
    Private ReadOnly _lstHints As New List(Of String)
    Public Property HintAvailable
        Get
            Return _HintAvailable
        End Get
        Set(value)
            _HintAvailable = value
            '            AddStatusMsg($"hintavail {value}")
            Me.OnMyPropertyChanged()
        End Set
    End Property

    Private Shared _txtStatus As TextBox
    Private _gridUni As UniformGrid
    Private _gridUniCanRespond As Boolean
    Private _spResults As StackPanel
    Private _seed As Integer
    Private _randLetGenerator As RandLetterGenerator
    Private _txtMinWordLen As TextBox
    Private _arrTiles(,) As LtrTile
    Private ReadOnly _strIGiveUp As String = "_I Give Up"
    Private ReadOnly _strPlayAgain As String = "_Play Again"
    Private ReadOnly _pnl As StackPanel = New StackPanel With {
        .Orientation = Orientation.Horizontal
    }

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        Me.Top = My.Settings.WindowPos.Height
        Me.Left = My.Settings.WindowPos.Width
        Me.Width = My.Settings.WindowSize.Width
        Me.Height = My.Settings.WindowSize.Height
        AddHandler Me.Closing, Sub()
                                   Try
                                       My.Settings.WindowPos = New System.Drawing.Size(Me.Left, Me.Top)
                                       My.Settings.WindowSize = New System.Drawing.Size(Me.Width, Me.Height)
                                       My.Settings.Save()
                                   Catch ex As Exception
                                   End Try
                               End Sub

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Private Sub Window_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Try
            Background = Brushes.SteelBlue
            Title = "Calvin's Wordament"
            _seed = Environment.TickCount
            If Debugger.IsAttached Then
                _seed = 1
                _HintDelay = 100
            Else
                _HintDelay = 100
            End If
            g_Random = New Random(_seed)
            _randLetGenerator = New RandLetterGenerator
            Dim mainGrid = CType(Markup.XamlReader.Load(
                <Grid
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    Margin="10,0,0,0"
                    >
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="500"/>
                        <ColumnDefinition/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Orientation="Vertical">
                        <DockPanel Width="500" HorizontalAlignment="Left">
                            <Button Name="btnNew" Width="80" Height="40">_New</Button>
                            <Label Margin="0,6,10,0">Rows</Label><TextBox Name="tbxRows" Text="{Binding Path=_nRows}" Width="50" Height="20"></TextBox>
                            <Label Margin="0,6,10,0">Cols</Label><TextBox Name="tbxCols" Text="{Binding Path=_nCols}" Width="50" Height="20"></TextBox>
                            <CheckBox Name="chkLongWord" Margin="0,9,10,0" IsChecked="{Binding Path=_IsLongWrd}">LongWord</CheckBox>
                            <TextBox Name="txtMinWordLen" Text="{Binding Path=MinWordLen}" Height="20" Width="18" ToolTip="When doing long words, must be at least this length."></TextBox>
                            <Button Name="btnHint"
                                IsEnabled="{Binding Path=HintAvailable}"
                                HorizontalAlignment="Right"
                                Width="100"
                                ToolTip="new hint available after 30 seconds">Hint</Button>
                        </DockPanel>
                        <DockPanel>
                            <TextBox Name="txtWordSoFar" Width="300" FontSize="24"
                                Margin="0,10,10,0"
                                IsReadOnly="True" Text="{Binding Path=StrWordSoFar}"/>
                            <TextBox Width="90" FontSize="24" HorizontalAlignment="Right" IsReadOnly="True" Text="{Binding Path=CountDownTimeStr}"/>
                        </DockPanel>
                        <UniformGrid Name="grdUniform" Height="500" Width="500" Background="#FF000000" HorizontalAlignment="Left"></UniformGrid>
                        <TextBox Name="tbxStatus" Width="300" IsReadOnly="True" AcceptsReturn="True" VerticalScrollBarVisibility="Auto" HorizontalAlignment="Left"></TextBox>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Name="spResults" Orientation="Horizontal">
                    </StackPanel>
                </Grid>.CreateReader
            ), Grid)
            mainGrid.DataContext = Me
            Me.Content = mainGrid
            Dim btnNew = CType(mainGrid.FindName("btnNew"), Button)
            _txtStatus = CType(mainGrid.FindName("tbxStatus"), TextBox)
            _txtStatus.MaxHeight = Math.Min(200, Me.Height - 150)
            Dim btnHint = CType(mainGrid.FindName("btnHint"), Button)
            _gridUni = CType(mainGrid.FindName("grdUniform"), UniformGrid)
            _spResults = CType(mainGrid.FindName("spResults"), StackPanel)
            _txtMinWordLen = CType(mainGrid.FindName("txtMinWordLen"), TextBox)
            Dim txtWordSoFar = CType(mainGrid.FindName("txtWordSoFar"), TextBox)
            Dim timerEnabled = False
            Dim timer = New DispatcherTimer(
                TimeSpan.FromSeconds(1),
                    DispatcherPriority.Normal,
                    Sub()
                        If timerEnabled Then
                            CountDownTime += 1
                        End If
                    End Sub,
                        _txtStatus.Dispatcher)

            Dim isShowingResult = True ' either is showing a board without results, or board with results
            Dim fdidFinish = False
            Dim taskGetResultsAsync As Task(Of List(Of Dictionary(Of String, LetterList))) = Nothing
            Dim dtLastHint As DateTime = DateTime.Now
            Dim nLastHintNum = 0
            AddHandler _txtMinWordLen.TextChanged, Sub()
                                                       _lstLongWords.Clear()
                                                   End Sub
            AddHandler btnHint.Click,
                Async Sub()
                    If taskGetResultsAsync?.IsCompleted Then
                        If (nLastHintNum < _lstHints.Count) Then
                            AddStatusMsg($"Hint {nLastHintNum} {_lstHints(nLastHintNum)}")
                        End If
                        HintAvailable = False
                        nLastHintNum += 1
                        If (nLastHintNum < _lstHints.Count) Then
                            Await Task.Delay(TimeSpan.FromMilliseconds(_HintDelay))
                            HintAvailable = True
                        End If
                    End If
                End Sub

            Dim IsMouseDown As Boolean = False
            Dim lstTilesSelected As New List(Of LtrTile)
            Dim lamShowResults =
                Async Function()
                    HintAvailable = False
                    fdidFinish = False
                    IsMouseDown = False
                    isShowingResult = True
                    timerEnabled = False
                    btnNew.Content = "calculating..."
                    Dim res = Await taskGetResultsAsync
                    taskGetResultsAsync = Nothing
                    ShowResults(res)
                    btnNew.Content = _strPlayAgain
                End Function
            Dim funcUpdateWordSoFar As Action =
                    Async Sub()
                        Dim str = String.Empty
                        For Each til In lstTilesSelected
                            str += til.ToString()
                        Next
                        StrWordSoFar = $"{str}"
                        If _IsLongWrd AndAlso taskGetResultsAsync IsNot Nothing AndAlso str.Length >= MinWordLen Then
                            If _WrdHighestPointsFound.Length = str.Length Then
                                If _WrdHighestPointsFound = str Then
                                    fdidFinish = True
                                    btnNew.Content = _strPlayAgain
                                    AddStatusMsg($"Got answer in {GetTimeAsString(CountDownTime)} {str}")
                                    Dim saveback = lstTilesSelected(0).Background
                                    For i = 1 To 5
                                        For Each til In lstTilesSelected
                                            til.Background = Brushes.Purple
                                        Next
                                        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, Function() Nothing)
                                        'Await Task.Yield()
                                        Await Task.Delay(100)
                                        For Each til In lstTilesSelected
                                            til.Background = saveback
                                        Next
                                        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, Function() Nothing)
                                        Await Task.Delay(100)
                                        'Await Task.Yield()
                                        'System.Diagnostics.Debug.WriteLine($"Flash {i} {str}")
                                    Next
                                    Await lamShowResults()
                                End If
                            End If
                        End If
                    End Sub
            Dim funcGetTileUnderMouse As Func(Of MouseEventArgs, LtrTile) =
                        Function(ev)
                            Dim ltrTile As LtrTile = Nothing
                            If _gridUniCanRespond Then
                                ' Determine which tile within grd.ActaulWidth, ActualHeight
                                Dim pos = ev.GetPosition(_gridUni)
                                Dim elem = _gridUni.InputHitTest(pos)
                                If elem IsNot Nothing AndAlso elem IsNot _gridUni Then
                                    Do While elem.GetType <> GetType(LtrTile)
                                        elem = CType(elem, FrameworkElement).Parent
                                    Loop
                                    ltrTile = CType(elem, LtrTile)
                                End If
                                If (ltrTile IsNot Nothing) Then
                                    ' Using hittest makes the corners of tiles active, causing diagonals to be difficult
                                    ' with fat fingers, so make a tile "hit" smaller than the tile
                                    ' calculate position of center of tile, and distance from mouse
                                    Dim pixX = pos.X / _gridUni.ActualWidth
                                    Dim pixY = pos.Y / _gridUni.ActualHeight
                                    Dim ctrX = ltrTile._col * _gridUni.ActualWidth / _nCols + ltrTile.ActualWidth / 2
                                    Dim ctrY = ltrTile._row * _gridUni.ActualHeight / _nRows + ltrTile.ActualHeight / 2
                                    Dim distToCtrOfTileSquared = Math.Pow((pos.X - ctrX), 2) + Math.Pow((pos.Y - ctrY), 2)
                                    'AddStatusMsg($"x={pos.X:n2} y={pos.Y:n2}  {distToCtrOfTileSquared:n0}  {ltrTile}")
                                    If (distToCtrOfTileSquared > ltrTile.ActualHeight * ltrTile.ActualWidth / 6) Then
                                        ltrTile = Nothing
                                    End If
                                End If
                            End If
                            Return ltrTile
                        End Function
            Dim funcClearSelection As Action =
                Sub()
                    If Not fdidFinish AndAlso StrWordSoFar IsNot Nothing Then
                        If taskGetResultsAsync IsNot Nothing AndAlso taskGetResultsAsync.IsCompleted Then
                            Dim lstBigDictResult = taskGetResultsAsync.Result(0)
                            If lstBigDictResult.ContainsKey(StrWordSoFar) Then
                                If StrWordSoFar.Length >= 6 AndAlso StrWordSoFar.Length < 9 Then
                                    AddStatusMsg($"Not bad! {StrWordSoFar}")
                                ElseIf StrWordSoFar.Length >= 9 AndAlso StrWordSoFar.Length < MinWordLen Then
                                    AddStatusMsg($"Good Try! {StrWordSoFar}")
                                ElseIf StrWordSoFar.Length >= MinWordLen Then
                                    AddStatusMsg($"Close, but no cigar {StrWordSoFar}")
                                End If
                            End If
                        End If
                    End If
                    For Each itm In lstTilesSelected
                        itm.UnSelectTile()
                    Next
                    lstTilesSelected.Clear()
                    funcUpdateWordSoFar()
                    IsMouseDown = False
                End Sub

            AddHandler _gridUni.MouseDown,
                Sub(o, ev)
                    'AddStatusMsg($"grd.MouseDown")
                    funcClearSelection()
                    Dim ltrTile = funcGetTileUnderMouse(ev)
                    If ltrTile IsNot Nothing Then
                        If ltrTile._isSelected Then ' already selected
                        Else
                            ltrTile.SelectTile()
                            lstTilesSelected.Add(ltrTile)
                            funcUpdateWordSoFar()
                        End If
                        IsMouseDown = True
                    End If
                End Sub
            AddHandler _gridUni.MouseUp,
                Sub()
                    If IsMouseDown Then
                        'AddStatusMsg($"grd.MouseUp")
                        funcClearSelection()
                    End If
                End Sub
            AddHandler _gridUni.MouseMove,
                    Sub(o, ev)
                        If Not System.Windows.Input.Mouse.LeftButton = MouseButtonState.Pressed Then
                            IsMouseDown = False
                        End If
                        '                                                      AddStatusMsg($"mm {IsMouseDown} {fdidFinish}")
                        If IsMouseDown AndAlso Not fdidFinish Then
                            Dim ltrTile = funcGetTileUnderMouse(ev)
                            If ltrTile IsNot Nothing Then
                                Dim priorSelected As LtrTile = Nothing
                                If lstTilesSelected.Count > 0 Then
                                    priorSelected = lstTilesSelected(lstTilesSelected.Count - 1)
                                End If
                                If ltrTile._isSelected Then ' already selected: ' if it is selected, user, might have gone back to prior selection
                                    If (lstTilesSelected.Count > 1) Then
                                        Dim penult = lstTilesSelected(lstTilesSelected.Count - 2)
                                        If (penult._row = ltrTile._row AndAlso penult._col = ltrTile._col) Then 'moved back to prior 1. unselect last one
                                            priorSelected.UnSelectTile()
                                            lstTilesSelected.RemoveAt(lstTilesSelected.Count - 1)
                                            funcUpdateWordSoFar()
                                        End If
                                    End If
                                Else
                                    Dim okToSelect = False
                                    If priorSelected Is Nothing Then
                                        okToSelect = True
                                    Else
                                        ' the distance between the current pos and the last should be 1
                                        Dim dist = Math.Pow(priorSelected._col - ltrTile._col, 2) + Math.Pow(priorSelected._row - ltrTile._row, 2)
                                        If dist <= 2 Then
                                            okToSelect = True
                                        End If
                                    End If
                                    If okToSelect Then
                                        ltrTile.SelectTile()
                                        lstTilesSelected.Add(ltrTile)
                                        funcUpdateWordSoFar()
                                    End If
                                End If
                            End If
                        End If
                    End Sub
            AddHandler _gridUni.MouseLeave, Sub()
                                                '                                                            funcClearSelection()
                                            End Sub

            Dim IsLookingUpWordOnLine = False ' debounce: lots of mousemoves shouldn't trigger lots of lookups
            AddHandler txtWordSoFar.MouseMove, Async Sub()
                                                   If Not String.IsNullOrEmpty(StrWordSoFar) Then
                                                       If Not IsLookingUpWordOnLine AndAlso System.Windows.Input.Mouse.LeftButton = MouseButtonState.Pressed Then
                                                           IsLookingUpWordOnLine = True
                                                           Dim disp = txtWordSoFar.Dispatcher
                                                           Await disp.BeginInvoke(Sub()
                                                                                      System.Diagnostics.Process.Start($"https://www.merriam-webster.com/dictionary/{StrWordSoFar}")
                                                                                  End Sub)

                                                           Await Task.Delay(TimeSpan.FromSeconds(3))
                                                           IsLookingUpWordOnLine = False
                                                       End If
                                                   End If
                                               End Sub
            AddHandler btnNew.Click,
                Async Sub()
                    isShowingResult = Not isShowingResult
                    If Not isShowingResult Then
                        fdidFinish = False
                        If taskGetResultsAsync IsNot Nothing Then
                            Await taskGetResultsAsync
                            taskGetResultsAsync = Nothing
                        End If
                        nLastHintNum = 0
                        _spResults.Children.Clear()
                        btnNew.Content = _strIGiveUp
                        _pnl.Children.Clear()
                        _gridUniCanRespond = False
                        _gridUni.Children.Clear()
                        StrWordSoFar = String.Empty
                        _WrdHighestPointsFound = String.Empty
                        Await FillGridWithTilesAsync(_gridUni)
                        _gridUniCanRespond = True
                        btnNew.IsEnabled = False
                        CountDownTime = 0
                        timerEnabled = True
                        HintAvailable = False
                        taskGetResultsAsync = GetResultsAsync()
                        Dim taskWait2secs = Task.Run(Async Function()
                                                         Await Task.Delay(TimeSpan.FromSeconds(2))
                                                     End Function)
                        Await taskGetResultsAsync
                        Await taskWait2secs ' debounce, prevent hitting the button twice too fast
                        btnNew.IsEnabled = True
                        Await CalculateHintsAsync()
                        Await Task.Delay(TimeSpan.FromMilliseconds(_HintDelay))
                        HintAvailable = True
                    Else
                        If CountDownTime > 2 Then ' debounce
                            Await lamShowResults()
                        Else
                            isShowingResult = False
                        End If
                    End If
                    'Width = 800
                    'Height = 800
                End Sub
            '            btn.AddHandler(Button.ClickEvent, New RoutedEventHandler(AddressOf BtnClick))
            btnNew.RaiseEvent(New RoutedEventArgs With {.RoutedEvent = Button.ClickEvent})
        Catch ex As Exception
            Me.Content = ex.ToString
        End Try
    End Sub

    Private Function CalculateHintsAsync() As Task
        ' add hints for : length or word, 1 for each ltr of word, and 1 each for lettertiles that are not in the word, then randomize
        _lstHints.Clear()
        Dim lstHintTiles = New List(Of String)
        For i = 1 To _WrdHighestPointsFound.Length
            lstHintTiles.Add($"{_WrdHighestPointsFound.Substring(0, i)}")
        Next

        Dim lstOtherHints = New List(Of String) From {
            $"Word length = {_WrdHighestPointsFound.Length}"
        }
        Dim charsNotInWord = String.Empty
        For Each ltr In _arrTiles
            If Not _WrdHighestPointsFound.Contains(ltr._letter._letter) AndAlso
                Not charsNotInWord.Contains(ltr._letter._letter) Then
                lstOtherHints.Add($"NO '{ltr._letter._letter}'")
                charsNotInWord += ltr._letter._letter
            End If
        Next
        ' now shuffle the other hints
        For i = 0 To lstOtherHints.Count - 1
            Dim r = g_Random.Next(lstOtherHints.Count)
            Dim tmp = lstOtherHints(r)
            lstOtherHints(r) = lstOtherHints(i)
            lstOtherHints(i) = tmp
        Next
        ' now interleave the 2 hints randomly: the ltr hints must be sequential
        Dim nTotalHints = lstOtherHints.Count + lstHintTiles.Count
        Dim ndxLstOtherHints = 0
        Dim ndxLstTileHints = 0
        For i = 0 To nTotalHints - 1
            Dim r = g_Random.Next(nTotalHints)
            Dim useOtherhints = False
            Dim useTileHints = False
            If (r < lstHintTiles.Count) Then
                If (ndxLstTileHints < lstHintTiles.Count) Then
                    useTileHints = True
                Else
                    useOtherhints = True
                End If
            Else
                If (ndxLstOtherHints < lstOtherHints.Count) Then
                    useOtherhints = True
                Else
                    useTileHints = True
                End If
            End If
            If (useOtherhints) Then
                _lstHints.Add(lstOtherHints(ndxLstOtherHints))
                ndxLstOtherHints += 1
            End If
            If (useTileHints) Then
                _lstHints.Add(lstHintTiles(ndxLstTileHints))
                ndxLstTileHints += 1
            End If
            'AddStatusMsg($"hh={i} {_lstHints(i)}")
        Next
        Return Task.FromResult(Of Integer)(0)
    End Function

    Protected Sub OnMyPropertyChanged(<CallerMemberName> Optional propname As String = "")
        Dim args = New PropertyChangedEventArgs(propname)
        RaiseEvent PropertyChanged(Me, args)
    End Sub

    Friend Shared Sub AddStatusMsg(msg As String)
        msg = $"{DateTime.Now.ToString("hh:mm:ss")} {Thread.CurrentThread.ManagedThreadId} {msg} {vbCrLf}"
        _txtStatus.Dispatcher.BeginInvoke(Sub()
                                              _txtStatus.AppendText(msg)
                                              _txtStatus.ScrollToEnd()
                                          End Sub)
    End Sub
    Sub ShowResults(results As List(Of Dictionary(Of String, LetterList)))
        Dim dictnum = 0
        _spResults.Children.Clear()
        Dim dictCommonResults = results(1) 'small dict
        Dim dictObsureResults = results(0) 'large dict
        For Each kvp In dictCommonResults
            If dictObsureResults.ContainsKey(kvp.Key) Then
                dictObsureResults.Remove(kvp.Key)
            End If
        Next
        For Each result In {dictCommonResults, dictObsureResults}
            dictnum += 1
            Dim spSingleResult = New StackPanel() With {.Orientation = Orientation.Vertical}
            _spResults.Children.Add(spSingleResult)

            Dim lv As New ListView
            Dim sortedlist = From wrd In result
                             Order By wrd.Value.Points Descending
                             Select Word = wrd.Key,
                             pts = CInt(wrd.Value.Points),
                             lst = wrd.Value

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

            spSingleResult.Children.Add(New TextBlock With {
                                  .Text = String.Format("Dict# {0} Score = {1:n0}" + vbCrLf + "#Words={2}", dictnum, CInt(score), result.Count)
                              })
            spSingleResult.Children.Add(lv)
            Dim fInHandler = False

            AddHandler lv.SelectionChanged,
                Async Sub()
                    If lv.SelectedItems.Count > 0 Then
                        If Not fInHandler Then
                            fInHandler = True
                            ' the 1st time through, there might be some tiles selected from user
                            For iRow = 0 To _nRows - 1
                                For iCol = 0 To _nCols - 1
                                    If _arrTiles(iRow, iCol)._isSelected Then
                                        _arrTiles(iRow, iCol).UnSelectTile()
                                    End If
                                Next
                            Next
                            Dim itm = lv.SelectedItems(0)
                            Dim tdesc = TypeDescriptor.GetProperties(itm)
                            Dim ltrLst = CType(tdesc("lst").GetValue(itm), LetterList)
                            StrWordSoFar = ltrLst.Word
                            Await FlashWordAsync(ltrLst, Brushes.Red)
                            fInHandler = False
                        End If
                    End If
                End Sub
        Next

        Dim arrLetDist(25) As Integer ' calc letter dist
        For Each ltr In _arrTiles
            arrLetDist(Asc(ltr._letter._letter) - 65) += 1
        Next
        Dim letDist = String.Empty
        For i = 0 To 25
            letDist += String.Format("{0}={1}" + vbCrLf, Chr(65 + i), arrLetDist(i))
        Next
        _spResults.Children.Add(
            New TextBlock With {
                    .Text = letDist,
                    .ToolTip = "Current Grid letter distribution"}
            )
        _spResults.Children.Add(
            New ListView With {
                .ItemsSource = WordamentWindow.g_LetterValues,
                .ToolTip = "Points per letter"}
            )
        _spResults.Children.Add(
            New ListView With {
                .ItemsSource = RandLetterGenerator._letDistArr,
                .ToolTip = "#letters in RandLetterGenerator"}
            )
        '_pnl.Children.Add(New ListView With {.ItemsSource = RandLetterGenerator._letDist})
    End Sub

    Private Async Function FlashWordAsync(ltrLst As LetterList, brush As Brush) As Task
        Dim firstTile = _arrTiles(ltrLst(0)._row, ltrLst(0)._col)
        Dim saveback = firstTile.Background
        For Each ltr In ltrLst
            Dim tile = _arrTiles(ltr._row, ltr._col)
            tile.Background = brush
            Await Task.Delay(200)
            'Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, Function() Nothing)
            'System.Threading.Thread.Sleep(200)
        Next
        Await Task.Delay(800)
        'System.Threading.Thread.Sleep(800)
        For Each ltr In ltrLst
            Dim tile = _arrTiles(ltr._row, ltr._col)
            tile.Background = saveback
        Next
    End Function

    ReadOnly _lstLongWords As New List(Of String)
    Private ReadOnly directions(7) As Integer
    Private Async Function FillGridWithTilesAsync(grd As UniformGrid, Optional isTesting As Boolean = False) As Task
        Dim arr(,) As Char = Nothing
        isTesting = False
        If isTesting Then
            arr = Array.CreateInstance(GetType(Char), _nRows, _nCols)
            _arrTiles = Array.CreateInstance(GetType(LtrTile), _nRows, _nCols)
            ' OERT INIE SSCM GOMB 'commissioner, recommission
            arr(0, 0) = "S"
            arr(0, 1) = "N"
            arr(0, 2) = "T"
            arr(0, 3) = "A"
            arr(1, 0) = "O"
            arr(1, 1) = "I"
            arr(1, 2) = "I"
            arr(1, 3) = "P"
            arr(2, 0) = "N"
            arr(2, 1) = "A"
            arr(2, 2) = "C"
            arr(2, 3) = "I"
            arr(3, 0) = "P"
            arr(3, 1) = "A"
            arr(3, 2) = "R"
            arr(3, 3) = "T"
        Else
            _arrTiles = Array.CreateInstance(GetType(LtrTile), _nRows, _nCols)
            ' fill an array on background thread
            For i = 0 To 7
                directions(i) = i
            Next
            If (_IsLongWrd) Then
                Await Task.Run(
                Sub()
                    Dim spellDict = New DictionaryLib.DictionaryLib(DictionaryType.Small, g_Random)
                    ' create a list of random directions (N,S, SE, etc) which can be tried in sequence til success

                    If _lstLongWords.Count = 0 Then
                        spellDict.SeekWord("a")
                        While True
                            Dim wrd = spellDict.GetNextWord
                            If String.IsNullOrEmpty(wrd) Then
                                Exit While
                            End If
                            If wrd.Length >= MinWordLen AndAlso wrd.Length <= _nCols * _nRows Then
                                _lstLongWords.Add(wrd.ToUpper)
                            End If
                        End While
                    End If
                    Dim isGood = False
                    Do While Not isGood
                        Dim randnum = g_Random.Next(_lstLongWords.Count)
                        Dim randLongWord = _lstLongWords(randnum)
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
                        ' now place the word in the grid. Start with a random
                        ' randomize the order of the directions we try
                        Dim nCalls = 0
                        arr = Array.CreateInstance(GetType(Char), _nRows, _nCols)
                        ' Given r,c of empty square with current letter index, put ltr in square
                        ' and find a lefit direction return true if is legit (within bounds and not used) 
                        Dim recurLam As Func(Of Integer, Integer, Integer, Boolean) =
                         Function(r, c, ndxW) As Boolean
                             nCalls += 1
                             Dim ltr = randLongWord(ndxW)
                             Debug.Assert(arr(r, c) = Chr(0))
                             arr(r, c) = ltr
                             If ndxW = randLongWord.Length - 1 Then
                                 isGood = True
                                 Return True
                             End If
                             For j = 0 To 7
                                 Dim rnd = g_Random.Next(8)
                                 Dim tmp = directions(j)
                                 directions(j) = directions(rnd)
                                 directions(rnd) = tmp
                             Next
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
                                     If arr(newr, newc) <> Chr(0) Then
                                         isGood = False
                                     End If
                                 End If
                                 If isGood Then
                                     If recurLam(newr, newc, ndxW + 1) Then
                                         Exit For
                                     Else
                                         isGood = False
                                     End If
                                 Else   ' couldn't place
                                 End If
                             Next
                             If Not isGood Then
                                 arr(r, c) = Nothing
                             End If
                             Return isGood
                         End Function
                        Dim ncurRow = g_Random.Next(_nRows)
                        Dim ncurCol = g_Random.Next(_nCols)
                        isGood = recurLam(ncurRow, ncurCol, 0)
                        'AddStatusMsg($"NRecurCalls= {nCalls} WrdLn={randLongWord.Length}")
                        ' we recurred down and couldn't find a path
                    Loop
                End Sub)
            Else
                arr = Array.CreateInstance(GetType(Char), _nRows, _nCols)
                For iRow = 0 To _nRows - 1
                    For iCol = 0 To _nCols - 1
                        Dim rndLet = _randLetGenerator.GetRandLet
                        'If rndLet = "Q" Then
                        '    rndLet = "QU"
                        '    rndLetPts += RandLetterGenerator._LetterValues(Asc("U") - 65)
                        'End If
                        arr(iRow, iCol) = rndLet
                    Next
                Next
            End If
        End If
        ' now update ui on main thread
        For iRow = 0 To _nRows - 1
            For iCol = 0 To _nCols - 1
                Dim ltr = "A"
                If arr(iRow, iCol) = Chr(0) Then
                    ltr = _randLetGenerator.GetRandLet
                Else
                    ltr = arr(iRow, iCol)
                End If
                _arrTiles(iRow, iCol) = New LtrTile(ltr, iRow, iCol, _nCols)
                grd.Children.Add(_arrTiles(iRow, iCol))
            Next
        Next
    End Function


    Async Function GetResultsAsync() As Task(Of List(Of Dictionary(Of String, LetterList)))
        Dim res = New List(Of Dictionary(Of String, LetterList)) ' large , small'
        Await Task.Run(Sub()
                           For Each dictnum As DictionaryLib.DictionaryType In [Enum].GetValues(GetType(DictionaryLib.DictionaryType))
                               '                               AddStatusMsg($"getres {dictnum}")
                               Dim oneresult = CalcWordList(dictnum)
                               res.Add(oneresult)
                               ' we want the highest small dictionary score 
                               ' Commisioner' is in small dict, 'recommission' is in large dict, same score
                               ' 'misrepresent' is in small dict, 'presentients' is in large dict, same score wrnt rers peim sent
                               ' 'intermittent' 44, 'intromittent' 46 tntg diet meni orte
                               ' manslaughter, slaughterman alna usmr ghej rmtu
                               ' aforethought 64, afterthought 64 oath rfgg tetu wrho
                               ' nonparticipat in large dict snta oiip naci part
                               ' both undescribable and 'indescribable' are same score oesc undr eiai nlbb
                               If (CType(dictnum, DictionaryLib.DictionaryType) = DictionaryType.Small) Then
                                   Dim max = oneresult.
                                        Where(Function(kvp) kvp.Key.Length >= MinWordLen).
                                        OrderByDescending(Function(kvp) kvp.Key.Length).
                                        ThenByDescending(Function(kvp) kvp.Value.Points).
                                        FirstOrDefault
                                   _WrdHighestPointsFound = max.Value.Word
                               End If
                           Next
                           '                          AddStatusMsg($"getres endtask")
                       End Sub)
        Return res
    End Function


    Private Function CalcWordList(dictnum As DictionaryLib.DictionaryType) As Dictionary(Of String, LetterList)
        Dim spellDict = New DictionaryLib.DictionaryLib(dictnum, g_Random)
        Dim resultWords = New Dictionary(Of String, LetterList)
        Dim arrVisited(_nRows - 1, _nCols - 1)
        Dim VisitCell As Action(Of Integer, Integer, String, LetterList)
        Dim nVisits = 0
        Dim nLookups = 0
        VisitCell = Sub(iRow As Integer, iCol As Integer, wordSoFar As String, ltrList As LetterList)
                        nVisits += 1
                        If iRow >= 0 AndAlso iCol >= 0 AndAlso iRow < _nRows AndAlso iCol < _nCols Then
                            Dim ltr = _arrTiles(iRow, iCol)
                            If Not arrVisited(iRow, iCol) Then
                                wordSoFar += ltr._letter._letter.ToLower
                                ltrList.Add(_arrTiles(iRow, iCol)._letter)
                                If wordSoFar.Length >= 3 Then
                                    Dim compResult = 0
                                    nLookups += 1
                                    Dim isPartial = spellDict.SeekWord(wordSoFar, compResult)
                                    If Not String.IsNullOrEmpty(isPartial) AndAlso compResult = 0 Then
                                        If Not resultWords.ContainsKey(wordSoFar.ToUpper()) Then
                                            resultWords.Add(wordSoFar.ToUpper(), New LetterList(ltrList)) ' needs to be a copy
                                        End If
                                    Else
                                        ' not in dict so far: let's see if it's a partial match
                                        If Not isPartial.StartsWith(wordSoFar) Then
                                            ltrList.RemoveAt(ltrList.Count - 1)
                                            Return
                                        End If
                                    End If
                                End If
                                arrVisited(iRow, iCol) = True
                                VisitCell(iRow - 1, iCol - 1, wordSoFar, ltrList)
                                VisitCell(iRow - 1, iCol, wordSoFar, ltrList)
                                VisitCell(iRow - 1, iCol + 1, wordSoFar, ltrList)
                                VisitCell(iRow, iCol - 1, wordSoFar, ltrList)
                                VisitCell(iRow, iCol + 1, wordSoFar, ltrList)
                                VisitCell(iRow + 1, iCol - 1, wordSoFar, ltrList)
                                VisitCell(iRow + 1, iCol, wordSoFar, ltrList)
                                VisitCell(iRow + 1, iCol + 1, wordSoFar, ltrList)
                                ltrList.RemoveAt(ltrList.Count - 1)
                                arrVisited(iRow, iCol) = False
                            End If
                        End If
                    End Sub

        For iRow = 0 To _nRows - 1
            For iCol = 0 To _nCols - 1
                VisitCell(iRow, iCol, String.Empty, New LetterList)
            Next
        Next
        'AddStatusMsg($"{dictnum} nvisits={nVisits} nlookups= {nLookups}")
        Return resultWords
    End Function

    Public Class LetterList
        Inherits List(Of SimpleLetter)
        Public Sub New()
            MyBase.New()
        End Sub
        Public Sub New(ByVal lst As LetterList)
            MyBase.New()
            Me.AddRange(lst)
        End Sub

        Public ReadOnly Property Points As Integer
            Get
                Dim pts = 0
                For Each ltr In Me
                    pts += ltr._pts
                Next
                If (Me.Count >= 5) Then
                    If (Me.Count <= 8) Then
                        pts = CInt(1.5 * pts)
                    Else
                        pts *= 2
                    End If
                End If
                'If _pts = 0 Then
                '    num = Aggregate a In Me Select a._pts Into Sum()
                'End If
                Return pts
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
                Return g_LetterValues(Asc(_letter) - 65)
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
        Shared ReadOnly g_backBrush = Brushes.DarkCyan
        Shared ReadOnly g_backBrushSelected = Brushes.Blue
        Friend _isSelected = False

        Friend ReadOnly _row As Integer
        Friend ReadOnly _col As Integer
        Public _letter As SimpleLetter
        Public Sub New(ByVal letter As String, row As Integer, col As Integer, ByVal nTotalCols As Integer)
            _letter = New SimpleLetter(letter, row, col)
            _row = row
            _col = col
            Background = g_backBrush

            Margin = New Thickness(4, 4, 4, 4) ' space between tiles

            Dim txt As New TextBlock With {
                .Text = _letter._letter,
                .FontSize = If(nTotalCols > 10, 14, 60 - (nTotalCols - 6) * 10),
                .HorizontalAlignment = HorizontalAlignment.Center,
                .Foreground = Brushes.White
            }
            Me.Children.Add(txt)

        End Sub

        Public Sub SelectTile()
            If Not _isSelected Then
                Me.Background = g_backBrushSelected
                _isSelected = True
            End If
        End Sub
        Public Sub UnSelectTile()
            If _isSelected Then
                Me.Background = g_backBrush
                _isSelected = False
            End If
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
        Friend Shared _letDist As String ' aaaabbb...qrrrrrssssssstttt
        Friend Shared _letDistArr(25) As Integer
        Public Sub New()
            Dim maxScore = Aggregate ltr In g_LetterValues Into Max() ' the highest score. e.g. q=12
            _letDist = String.Empty
            For i = 0 To g_LetterValues.Length - 1
                Dim nThisLet = maxScore * 30 / (g_LetterValues(i)) 'lcm = 360
                'uncomment this line for even distribution: 
                '   same # of Q's and E's
                'nThisLet = 1
                _letDistArr(i) = nThisLet
                For j = 1 To nThisLet
                    _letDist += Chr(i + 65) ' upper case
                Next
            Next
        End Sub

        Private ReadOnly _seedArray() As String
        Private _seedIndex As Integer

        Public Function GetRandLet() As String ' letter, score
            Dim rndLet
            If _seedArray Is Nothing Then
                Dim rndNum = g_Random.Next(_letDist.Length)
                rndLet = _letDist.Substring(rndNum, 1)
            Else
                rndLet = _seedArray(_seedIndex)
                _seedIndex += 1
            End If
            Return rndLet
        End Function
    End Class
End Class

