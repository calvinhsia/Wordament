Imports System.Windows.Controls.Primitives
Imports System.ComponentModel
Imports System.Windows.Threading

Class MainWindow
    Public Shared _spellDict As Dictionary.CDict
    Public ReadOnly Property _nRows As Integer
        Get
            Return CInt(_txtRows.Text)
        End Get
    End Property
    Public ReadOnly Property _nCols As Integer
        Get
            Return CInt(_txtCols.Text)
        End Get
    End Property
    Private _stkCtrls As StackPanel
    Private _txtRows As TextBox
    Private _txtCols As TextBox
    Private _randLetGenerator As RandLetterGenerator
    Private _resultWords As Dictionary(Of String, LetterList)

    Private _arrTiles(,) As WrdTile
    Private _minWordLength As Integer = 3
    Private _pnl As StackPanel = New StackPanel With {.Orientation = Orientation.Horizontal}
    Private Sub Window_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Try
            Height = 800
            Width = 1000
            Title = "Calvin's WordAMent"
            Dim seed = Environment.TickCount
            If Debugger.IsAttached Then
                seed = 1
            End If
            _randLetGenerator = New RandLetterGenerator(seed)
            _spellDict = New Dictionary.CDict
            _stkCtrls = CType(Markup.XamlReader.Load(
                <StackPanel
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    >
                    <StackPanel Orientation="Horizontal">
                        <Label>Rows</Label><TextBox Name="tbxRows" HorizontalAlignment="Right" Width="30">4</TextBox>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <Label>Cols</Label><TextBox Name="tbxCols" HorizontalAlignment="Right" Width="30">4</TextBox>
                    </StackPanel>
                    <Button Name="btnNew">_New</Button>
                </StackPanel>.CreateReader
            ), StackPanel)

            Dim btn = _stkCtrls.FindName("btnNew")
            _txtRows = CType(_stkCtrls.FindName("tbxRows"), TextBox)
            _txtCols = CType(_stkCtrls.FindName("tbxCols"), TextBox)
            btn.AddHandler(Button.ClickEvent, New RoutedEventHandler(AddressOf AddContent))
            AddContent()
        Catch ex As Exception
            Me.Content = ex.ToString
        End Try
    End Sub

    Private Sub AddContent()
        _pnl.Children.Clear()
        _pnl.Children.Add(_stkCtrls)
        Dim grd = MakeGrid()
        _pnl.Children.Add(grd)
        For dictnum = 1 To 2
            Dim spResult = New StackPanel With {.Orientation = Orientation.Vertical}
            _spellDict.DictNum = dictnum
            Dim lv As New ListView
            CalcWordList()
            Dim sortedlist = From wrd In _resultWords
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
                              .Width = 100
                            }
                          )
            gview.Columns.Add(New GridViewColumn With {
                              .Header = New GridViewColumnHeader With {.Content = "Points"},
                              .DisplayMemberBinding = New Binding("pts"),
                              .Width = 60
                            }
                          )
            Dim score = Aggregate wrd In _resultWords Select pts = wrd.Value.Points Into Sum()

            spResult.Children.Add(New TextBlock With {
                                  .Text = String.Format("Dict# {0} Score = {1:n0}" + vbCrLf + "#Words={2}", dictnum, CInt(score), _resultWords.Count)
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
                            Dim saveback = ltrLst(0).Background
                            For Each ltr In ltrLst
                                ltr.Background = Brushes.Red
                                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, Function() Nothing)
                                System.Threading.Thread.Sleep(100)
                            Next
                            System.Threading.Thread.Sleep(500)
                            For Each ltr In ltrLst
                                ltr.Background = saveback
                            Next
                            fInHandler = False
                        End If
                    End If
                End Sub

            _pnl.Children.Add(spResult)
        Next

        Dim arrLetDist(25) As Integer ' calc letter dist
        For Each ltr In _arrTiles
            arrLetDist(Asc(ltr._letter) - 65) += 1
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
                .ItemsSource = RandLetterGenerator._LetterValues,
                .ToolTip = "Points per letter"}
            )
        _pnl.Children.Add(
            New ListView With {
                .ItemsSource = RandLetterGenerator._letDistArr,
                .ToolTip = "#letters in RandLetterGenerator"}
            )
        '_pnl.Children.Add(New ListView With {.ItemsSource = RandLetterGenerator._letDist})
        Me.Content = _pnl
        'Width = 800
        'Height = 800
    End Sub

    Private Function MakeGrid() As UIElement
        Dim uGrid = New UniformGrid With {
            .Background = Brushes.Black,
            .Width = 300,
            .VerticalAlignment = VerticalAlignment.Top,
            .Height = 300
            }
        _arrTiles = Array.CreateInstance(GetType(WrdTile), _nRows, _nCols)
        '_randLetGenerator.PreSeed(2, 6, _nRows * _nCols)

        For iRow = 0 To _nRows - 1
            For iCol = 0 To _nCols - 1
                Dim rndLetResult = _randLetGenerator.GetRandLet
                Dim rndLet = rndLetResult.Item1
                Dim rndLetPts = rndLetResult.Item2
                'If rndLet = "Q" Then
                '    rndLet = "QU"
                '    rndLetPts += RandLetterGenerator._LetterValues(Asc("U") - 65)
                'End If
                Dim tile = New WrdTile(rndLet, rndLetPts, _nRows)
                _arrTiles(iRow, iCol) = tile
                uGrid.Children.Add(tile)
            Next
        Next
        Return uGrid
    End Function

    Private _visitedarr(,) As Boolean
    Private Sub CalcWordList()
        _resultWords = New Dictionary(Of String, LetterList)
        ReDim _visitedarr(_nRows - 1, _nCols - 1)
        For iRow = 0 To _nRows - 1
            For iCol = 0 To _nCols - 1
                VisitCell(iRow, iCol, String.Empty, 0, New LetterList)
            Next
        Next
    End Sub

    Private Sub VisitCell(ByVal iRow As Integer,
                          ByVal iCol As Integer,
                          ByVal wordSoFar As String,
                          ByVal ptsSoFar As Integer,
                          ByVal ltrList As LetterList)
        If iRow >= 0 AndAlso iCol >= 0 AndAlso iRow < _nRows AndAlso iCol < _nCols Then
            Dim tile = _arrTiles(iRow, iCol)
            If Not _visitedarr(iRow, iCol) Then
                wordSoFar += tile._letter
                ptsSoFar += tile._pts
                ltrList.Add(_arrTiles(iRow, iCol))
                If wordSoFar.Length >= _minWordLength Then
                    If _spellDict.IsWord(wordSoFar) Then
                        If Not _resultWords.ContainsKey(wordSoFar) Then
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
                            _resultWords.Add(wordSoFar, New LetterList(ltrList, pts)) ' needs to be a copy
                        End If
                    Else
                        ' not in dict so far: let's see if it's a partial match
                        Dim isPartial = _spellDict.FindMatch(wordSoFar + "*")
                        If String.IsNullOrEmpty(isPartial) Then
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
        Inherits List(Of WrdTile)
        Public Sub New()
            MyBase.New()
        End Sub
        Private _pts As Integer
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

    Public Class WrdTile
        Inherits DockPanel
        Public Property _letter As String
        Public Property _pts As Integer ' points for this tile
        Public Sub New(ByVal letter As String, ByVal pts As Integer, ByVal nCols As Integer)
            _letter = letter ' could be digraph, like "qu"
            _pts = pts
            Background = Brushes.DarkSlateBlue

            Margin = New Thickness(2, 2, 2, 2)

            Dim txt As New TextBlock With {
                .Text = _letter,
                .FontSize = If(nCols > 10, 10, 40 - (nCols - 6) * 5),
                .HorizontalAlignment = HorizontalAlignment.Center,
                .Foreground = Brushes.White
            }
            Me.Children.Add(txt)
        End Sub

        Public Overrides Function ToString() As String
            Return _letter
        End Function
    End Class

    Public Class RandLetterGenerator
        Private _Random As Random
        '.......................................... A  B  C  D  E  F  G  H  I  J   K  L  M  N  O  P  Q   R  S  T  U  V  W  X  Y  Z
        Public Shared _LetterValues() As Integer = {2, 5, 3, 3, 1, 5, 4, 4, 2, 10, 6, 3, 2, 2, 2, 4, 12, 2, 2, 2, 2, 4, 6, 9, 5, 8}
        Friend Shared _letDist As String
        Friend Shared _letDistArr(25) As String
        Public Sub New(ByVal seed As Integer)
            _Random = New Random(seed)
            Dim maxScore = Aggregate ltr In _LetterValues Into Max()
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

        Private _seedArray() As String
        Private _seedIndex As Integer
        Public Sub PreSeed(ByVal nWords As Integer, ByVal nWordLen As Integer, ByVal nTiles As Integer)
            Dim strSeeded = String.Empty
            _seedIndex = 0
            For i = 1 To nWords
                Dim wrd = String.Empty
                Do While wrd.Length <> nWordLen
                    wrd = MainWindow._spellDict.RandWord(Environment.TickCount)
                Loop
                strSeeded += wrd.ToUpper
            Next
            For i = strSeeded.Length To nTiles
                strSeeded += _letDist.Substring(_Random.Next(_letDist.Length), 1)
            Next
            ReDim _seedArray(nTiles)
            For i = 0 To nTiles - 1
                _seedArray(i) = strSeeded(i)
            Next
            For i = 0 To nTiles - 1
                Dim rnd = _Random.Next(nTiles)
                Dim tmp = _seedArray(i)
                _seedArray(i) = _seedArray(rnd)
                _seedArray(rnd) = tmp
            Next
            'For i = 0 To strSeeded.Length - 1
            '    Dim rnd = _Random.Next(strSeeded.Length)
            '    Dim tmp = strSeeded(rnd)
            '    strSeeded = strSeeded.Substring(0, i - 1) + tmp + strSeeded(i + 1)

            'Next
        End Sub

        Public Function GetRandLet() As Tuple(Of String, Integer) ' letter, score
            Dim rndLet = String.Empty
            Dim scorelet = 0
            If _seedArray Is Nothing Then
                Dim rndNum = _Random.Next(_letDist.Length)
                rndLet = _letDist.Substring(rndNum, 1)
            Else
                rndLet = _seedArray(_seedIndex)
                _seedIndex += 1
            End If
            scorelet = _LetterValues(Asc(rndLet) - 65)
            Return New Tuple(Of String, Integer)(rndLet, scorelet)
        End Function
    End Class
End Class
