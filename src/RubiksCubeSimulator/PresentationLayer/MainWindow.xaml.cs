using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RubikovaKostka
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, List<Color>> colors;   // 6 stěn => 9 barev
        private Dictionary<string, Grid> sides;           // "Top"->TopSide, ...
        private Stack<Action> undoStack;

        private string selectedSide = null;
        private (int row, int col)? selectedBlock = null;
        private Rectangle highlightedRect = null;

        private int moveCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            sides = new Dictionary<string, Grid>
            {
                {"Top",TopSide},
                {"Left",LeftSide},
                {"Front",FrontSide},
                {"Right",RightSide},
                {"Back",BackSide},
                {"Bottom",BottomSide}
            };

            colors = new Dictionary<string, List<Color>>
            {
                {"Top",    CreateColorList(Colors.Yellow)},
                {"Left",   CreateColorList(Colors.Green)},
                {"Front",  CreateColorList(Colors.Orange)},
                {"Right",  CreateColorList(Colors.Blue)},
                {"Back",   CreateColorList(Colors.Red)},
                {"Bottom", CreateColorList(Colors.White)}
            };

            undoStack = new Stack<Action>();

            InitializeCube();
        }

        private List<Color> CreateColorList(Color c)
        {
            return Enumerable.Repeat(c, 9).ToList();
        }

        // Vykreslí 9 obdélníků pro každou stěnu
        private void InitializeCube()
        {
            foreach (var kvp in sides)
            {
                string sideName = kvp.Key;
                var grid = kvp.Value;
                grid.Children.Clear();

                for (int i = 0; i < 9; i++)
                {
                    var rect = new Rectangle
                    {
                        Fill = new SolidColorBrush(colors[sideName][i]),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    int row = i / 3;
                    int col = i % 3;
                    Grid.SetRow(rect, row);
                    Grid.SetColumn(rect, col);

                    rect.Tag = (sideName, row, col);
                    rect.MouseDown += Rect_MouseDown;

                    grid.Children.Add(rect);
                }
            }
            MovesTextBlock.Text = $"Moves: {moveCount}";
        }

        // Klik => fialový rámeček
        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var r = sender as Rectangle;
            if (r == null) return;

            if (highlightedRect != null)
            {
                highlightedRect.Stroke = Brushes.Black;
                highlightedRect.StrokeThickness = 1;
            }
            if (r.Tag is ValueTuple<string, int, int> info)
            {
                selectedSide = info.Item1;
                selectedBlock = (info.Item2, info.Item3);

                r.Stroke = Brushes.Purple;
                r.StrokeThickness = 3;
                highlightedRect = r;
            }
        }

        // Ovládání šipkami z klávesnice
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (selectedSide == null || !selectedBlock.HasValue) return;
            switch (e.Key)
            {
                case Key.Up: RotateUp_Click(null, null); break;
                case Key.Down: RotateDown_Click(null, null); break;
                case Key.Left: RotateLeft_Click(null, null); break;
                case Key.Right: RotateRight_Click(null, null); break;
            }
        }

        // Tlačítka
        private void RotateUp_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSide == null || !selectedBlock.HasValue) return;
            SaveStateForUndo();

            int row = selectedBlock.Value.row;
            int col = selectedBlock.Value.col;

            // => otáčí sloupec "col" nahoru
            RotateColumnUp(selectedSide, row, col);

            moveCount++;
            InitializeCube();
        }

        private void RotateDown_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSide == null || !selectedBlock.HasValue) return;
            SaveStateForUndo();

            int row = selectedBlock.Value.row;
            int col = selectedBlock.Value.col;

            RotateColumnDown(selectedSide, row, col);

            moveCount++;
            InitializeCube();
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSide == null || !selectedBlock.HasValue) return;
            SaveStateForUndo();

            int row = selectedBlock.Value.row;
            int col = selectedBlock.Value.col;

            RotateRowLeft(selectedSide, row, col);

            moveCount++;
            InitializeCube();
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSide == null || !selectedBlock.HasValue) return;
            SaveStateForUndo();

            int row = selectedBlock.Value.row;
            int col = selectedBlock.Value.col;

            RotateRowRight(selectedSide, row, col);

            moveCount++;
            InitializeCube();
        }

        private void ResetCube_Click(object sender, RoutedEventArgs e)
        {
            colors["Top"] = CreateColorList(Colors.Yellow);
            colors["Left"] = CreateColorList(Colors.Green);
            colors["Front"] = CreateColorList(Colors.Orange);
            colors["Right"] = CreateColorList(Colors.Blue);
            colors["Back"] = CreateColorList(Colors.Red);
            colors["Bottom"] = CreateColorList(Colors.White);

            undoStack.Clear();
            moveCount = 0;
            selectedSide = null;
            selectedBlock = null;
            highlightedRect = null;

            InitializeCube();
        }

        private void UndoMove_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 0)
            {
                var action = undoStack.Pop();
                action.Invoke();
                InitializeCube();
            }
        }

        private void ScrambleCube_Click(object sender, RoutedEventArgs e)
        {
            var sidesAll = new[] { "Top", "Left", "Front", "Right", "Back", "Bottom" };
            var rnd = new Random();
            int moves = 20;
            for (int i = 0; i < moves; i++)
            {
                SaveStateForUndo();
                string s = sidesAll[rnd.Next(sidesAll.Length)];
                int row = rnd.Next(3);
                int col = rnd.Next(3);
                int dir = rnd.Next(4); // 0=Up,1=Down,2=Left,3=Right
                switch (dir)
                {
                    case 0: RotateColumnUp(s, row, col); break;
                    case 1: RotateColumnDown(s, row, col); break;
                    case 2: RotateRowLeft(s, row, col); break;
                    case 3: RotateRowRight(s, row, col); break;
                }
                moveCount++;
            }
            InitializeCube();
        }

        private void SaveStateForUndo()
        {
            var snap = new Dictionary<string, List<Color>>();
            foreach (var kvp in colors)
            {
                snap[kvp.Key] = new List<Color>(kvp.Value);
            }
            undoStack.Push(() => {
                foreach (var k in snap.Keys)
                {
                    colors[k] = new List<Color>(snap[k]);
                }
            });
        }

        // Pomocné: row a column
        private List<Color> GetRow(string side, int row)
        {
            int idx = row * 3;
            return new List<Color>
            {
                colors[side][idx+0],
                colors[side][idx+1],
                colors[side][idx+2]
            };
        }
        private void SetRow(string side, int row, List<Color> rowColors)
        {
            int idx = row * 3;
            colors[side][idx + 0] = rowColors[0];
            colors[side][idx + 1] = rowColors[1];
            colors[side][idx + 2] = rowColors[2];
        }

        private List<Color> GetColumn(string side, int col)
        {
            return new List<Color>
            {
                colors[side][0*3+col],
                colors[side][1*3+col],
                colors[side][2*3+col]
            };
        }
        private void SetColumn(string side, int col, List<Color> colColors)
        {
            colors[side][0 * 3 + col] = colColors[0];
            colors[side][1 * 3 + col] = colColors[1];
            colors[side][2 * 3 + col] = colColors[2];
        }

        private void RotateSideClockwise(string side)
        {
            if (side == "Front")
            {
                var topRow = GetRow("Top", 2);
                var leftCol = GetColumn("Left", 2);
                var bottomRow = GetRow("Bottom", 0);
                var rightCol = GetColumn("Right", 0);

                SetColumn("Left", 2, topRow.AsEnumerable().Reverse().ToList());
                SetRow("Bottom", 0, leftCol);
                SetColumn("Right", 0, bottomRow.AsEnumerable().Reverse().ToList());
                SetRow("Top", 2, rightCol);

                RotateFaceSquaresClockwise("Front");
            }
            else if (side == "Back")
            {
                var topRow = GetRow("Top", 0);
                var rightCol = GetColumn("Right", 2);
                var bottomRow = GetRow("Bottom", 2);
                var leftCol = GetColumn("Left", 0);

                SetColumn("Right", 2, topRow);
                SetRow("Bottom", 2, rightCol.AsEnumerable().Reverse().ToList());
                SetColumn("Left", 0, bottomRow);
                SetRow("Top", 0, leftCol.AsEnumerable().Reverse().ToList());

                RotateFaceSquaresClockwise("Back");
            }
            else if (side == "Left")
            {
                var topCol = GetColumn("Top", 0);
                var frontCol = GetColumn("Front", 0);
                var bottomCol = GetColumn("Bottom", 0);
                var backCol = GetColumn("Back", 2);

                SetColumn("Front", 0, topCol);
                SetColumn("Bottom", 0, frontCol);
                SetColumn("Back", 2, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 0, backCol.AsEnumerable().Reverse().ToList());

                RotateFaceSquaresClockwise("Left");
            }
            else if (side == "Right")
            {
                var topCol = GetColumn("Top", 2);
                var backCol = GetColumn("Back", 0);
                var bottomCol = GetColumn("Bottom", 2);
                var frontCol = GetColumn("Front", 2);

                SetColumn("Back", 0, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 2, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 2, bottomCol);
                SetColumn("Top", 2, frontCol);

                RotateFaceSquaresClockwise("Right");
            }
            else if (side == "Top")
            {
                var backRow = GetRow("Back", 0);
                var rightRow = GetRow("Right", 0);
                var frontRow = GetRow("Front", 0);
                var leftRow = GetRow("Left", 0);

                SetRow("Right", 0, backRow);
                SetRow("Front", 0, rightRow);
                SetRow("Left", 0, frontRow);
                SetRow("Back", 0, leftRow);

                RotateFaceSquaresClockwise("Top");
            }
            else if (side == "Bottom")
            {
                var frontRow = GetRow("Front", 2);
                var rightRow = GetRow("Right", 2);
                var backRow = GetRow("Back", 2);
                var leftRow = GetRow("Left", 2);

                SetRow("Right", 2, frontRow);
                SetRow("Back", 2, rightRow);
                SetRow("Left", 2, backRow);
                SetRow("Front", 2, leftRow);

                RotateFaceSquaresClockwise("Bottom");
            }
        }

        private void RotateSideCounterclockwise(string side)
        {
            for (int i = 0; i < 3; i++)
            {
                RotateSideClockwise(side);
            }
        }

        private void RotateFaceSquaresClockwise(string side)
        {
            var old = colors[side].ToArray();
            colors[side][0] = old[6];
            colors[side][1] = old[3];
            colors[side][2] = old[0];
            colors[side][3] = old[7];
            // [4] střed
            colors[side][5] = old[1];
            colors[side][6] = old[8];
            colors[side][7] = old[5];
            colors[side][8] = old[2];
        }

        private void RotateColumnUp(string side, int row, int col)
        {
            // FRONT, col=0
            if (side == "Front" && col == 0)
            {
                // top col=0 => front col=0 => bottom col=0 => back col=2 (reverse)
                var topCol = GetColumn("Top", 0);
                var frontCol = GetColumn("Front", 0);
                var bottomCol = GetColumn("Bottom", 0);
                var backCol = GetColumn("Back", 2).AsEnumerable().Reverse().ToList();

                // Posun "Up": front <- top, bottom <- front, back <- bottom, top <- back
                SetColumn("Front", 0, topCol);
                SetColumn("Bottom", 0, frontCol);
                SetColumn("Back", 2, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 0, backCol);

                // Kraj => otočení stěny Front (clockwise)
                RotateSideClockwise("Front");
            }
            // FRONT, col=1 (střed)
            else if (side == "Front" && col == 1)
            {
                // top col=1 => front col=1 => bottom col=1 => back col=1 (reverse)
                var topCol = GetColumn("Top", 1);
                var frontCol = GetColumn("Front", 1);
                var bottomCol = GetColumn("Bottom", 1);
                var backCol = GetColumn("Back", 1).AsEnumerable().Reverse().ToList();

                SetColumn("Front", 1, topCol);
                SetColumn("Bottom", 1, frontCol);
                SetColumn("Back", 1, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 1, backCol);

                // Střed => slice move, netočí se celá stěna
            }
            // FRONT, col=2
            else if (side == "Front" && col == 2)
            {
                // top col=2 => front col=2 => bottom col=2 => back col=0 (reverse)
                var topCol = GetColumn("Top", 2);
                var frontCol = GetColumn("Front", 2);
                var bottomCol = GetColumn("Bottom", 2);
                var backCol = GetColumn("Back", 0).AsEnumerable().Reverse().ToList();

                SetColumn("Front", 2, topCol);
                SetColumn("Bottom", 2, frontCol);
                SetColumn("Back", 0, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 2, backCol);

                // Kraj => otočení stěny Front (clockwise)
                RotateSideClockwise("Front");
            }
            else if (side == "Back" && col == 0)
            {
                // "Back" col=0 => top col=2 (reverse?), back col=0 => bottom col=2 (reverse?), front col=2 ...
                // Posun nahoru: back <- top, bottom <- back, front <- bottom, top <- front
                // Je potřeba ohlídat, že "Back" je horizontálně převrácená v netu.

                var topCol = GetColumn("Top", 2).AsEnumerable().Reverse().ToList();   // often reversed
                var backCol = GetColumn("Back", 0);
                var bottomCol = GetColumn("Bottom", 2).AsEnumerable().Reverse().ToList();
                var frontCol = GetColumn("Front", 2);

                // back <- topCol
                SetColumn("Back", 0, topCol.AsEnumerable().Reverse().ToList());
                // "AsEnumerable().Reverse()" kvůli orientaci back?
                // Zde je to už dle logiky netu - ověřte, jestli nepotřebujete dvakrát reverse 
                // bottom <- back
                SetColumn("Bottom", 2, backCol.AsEnumerable().Reverse().ToList());
                // front <- bottom
                SetColumn("Front", 2, bottomCol.AsEnumerable().Reverse().ToList());
                // top <- front
                SetColumn("Top", 2, frontCol.AsEnumerable().Reverse().ToList());

                // kraj => otočení stěny Back (clockwise)
                RotateSideClockwise("Back");
            }
            else if (side == "Back" && col == 1)
            {
                // střed => slice
                var topCol = GetColumn("Top", 1).AsEnumerable().Reverse().ToList();
                var backCol = GetColumn("Back", 1);
                var bottomCol = GetColumn("Bottom", 1).AsEnumerable().Reverse().ToList();
                var frontCol = GetColumn("Front", 1);

                // back <- topCol
                SetColumn("Back", 1, topCol.AsEnumerable().Reverse().ToList());
                // bottom <- back
                SetColumn("Bottom", 1, backCol.AsEnumerable().Reverse().ToList());
                // front <- bottom
                SetColumn("Front", 1, bottomCol.AsEnumerable().Reverse().ToList());
                // top <- front
                SetColumn("Top", 1, frontCol.AsEnumerable().Reverse().ToList());

                // střed => netočí se face "Back"
            }
            else if (side == "Back" && col == 2)
            {
                // BACK col=2 => top col=0 reverse, back col=2 => bottom col=0 reverse, front col=0 ...
                var topCol = GetColumn("Top", 0).AsEnumerable().Reverse().ToList();
                var backCol = GetColumn("Back", 2);
                var bottomCol = GetColumn("Bottom", 0).AsEnumerable().Reverse().ToList();
                var frontCol = GetColumn("Front", 0);

                // back <- top
                SetColumn("Back", 2, topCol.AsEnumerable().Reverse().ToList());
                // bottom <- back
                SetColumn("Bottom", 0, backCol.AsEnumerable().Reverse().ToList());
                // front <- bottom
                SetColumn("Front", 0, bottomCol.AsEnumerable().Reverse().ToList());
                // top <- front
                SetColumn("Top", 0, frontCol.AsEnumerable().Reverse().ToList());

                // kraj => otočit stěnu "Back" (clockwise)
                RotateSideClockwise("Back");
            }
            else if (side == "Left" && col == 0)
            {
                // "Left", col=0 => typicky:
                // top col=0 => left col=0 => bottom col=0 => back col=2(reverse)
                var topCol = GetColumn("Top", 0);
                var leftCol = GetColumn("Left", 0);
                var bottomCol = GetColumn("Bottom", 0);
                var backCol = GetColumn("Back", 2).AsEnumerable().Reverse().ToList();

                // Posun "Up": left <- top, bottom <- left, back <- bottom, top <- back
                SetColumn("Left", 0, topCol);
                SetColumn("Bottom", 0, leftCol);
                SetColumn("Back", 2, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 0, backCol);

                // Kraj => celá stěna "Left" (clockwise)
                RotateSideClockwise("Left");
            }
            else if (side == "Left" && col == 1)
            {
                // Střed => slice: top col=1 => left col=1 => bottom col=1 => back col=1(reverse)
                var topCol = GetColumn("Top", 1);
                var leftCol = GetColumn("Left", 1);
                var bottomCol = GetColumn("Bottom", 1);
                var backCol = GetColumn("Back", 1).AsEnumerable().Reverse().ToList();

                SetColumn("Left", 1, topCol);
                SetColumn("Bottom", 1, leftCol);
                SetColumn("Back", 1, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 1, backCol);
                // střed => netočí se face "Left"
            }
            else if (side == "Left" && col == 2)
            {
                // top col=2 => left col=2 => bottom col=2 => back col=0(reverse)
                var topCol = GetColumn("Top", 2);
                var leftCol = GetColumn("Left", 2);
                var bottomCol = GetColumn("Bottom", 2);
                var backCol = GetColumn("Back", 0).AsEnumerable().Reverse().ToList();

                SetColumn("Left", 2, topCol);
                SetColumn("Bottom", 2, leftCol);
                SetColumn("Back", 0, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 2, backCol);

                // kraj => stěna "Left"
                RotateSideClockwise("Left");
            }
            else if (side == "Right" && col == 0)
            {
                // top col=2 (reverse?) => right col=0 => bottom col=2 (reverse?) => front col=2 ?
                // Záleží na Vašem netu, uvádím variantu s reverzemi v top/bottom
                var topCol = GetColumn("Top", 2).AsEnumerable().Reverse().ToList();
                var rightCol = GetColumn("Right", 0);
                var bottomCol = GetColumn("Bottom", 2).AsEnumerable().Reverse().ToList();
                var frontCol = GetColumn("Front", 2);

                // posun "Up": right <- top, bottom <- right, front <- bottom, top <- front
                SetColumn("Right", 0, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 2, rightCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 2, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 2, frontCol.AsEnumerable().Reverse().ToList());

                // kraj => stěna "Right" (clockwise)
                RotateSideClockwise("Right");
            }
            else if (side == "Right" && col == 1)
            {
                // střed => slice
                var topCol = GetColumn("Top", 1).AsEnumerable().Reverse().ToList();
                var rightCol = GetColumn("Right", 1);
                var bottomCol = GetColumn("Bottom", 1).AsEnumerable().Reverse().ToList();
                var frontCol = GetColumn("Front", 1);

                // right <- top, bottom <- right, front <- bottom, top <- front
                SetColumn("Right", 1, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 1, rightCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 1, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 1, frontCol.AsEnumerable().Reverse().ToList());
            }
            else if (side == "Right" && col == 2)
            {
                var topCol = GetColumn("Top", 0).AsEnumerable().Reverse().ToList();
                var rightCol = GetColumn("Right", 2);
                var bottomCol = GetColumn("Bottom", 0).AsEnumerable().Reverse().ToList();
                var frontCol = GetColumn("Front", 0);

                SetColumn("Right", 2, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 0, rightCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 0, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 0, frontCol.AsEnumerable().Reverse().ToList());

                RotateSideClockwise("Right");
            }
            else if (side == "Top" && col == 0)
            {
                // "Top" col=0 => posun "Up":
                //   left col=0 => top col=0 => right col=0 => bottom col=0 ...
                //   Podle Vašeho layoutu. Zde uvádím příklad, kdy se top stěna dotýká left, front, right, back… 
                //   Ale top sloupec 0 bývá spojen s left row=..., back row=..., etc. 
                //   Záleží, jak to máte definované. Ukážu jedno z možných řešení:

                var leftCol = GetColumn("Left", 0);
                var topCol = GetColumn("Top", 0);
                var rightCol = GetColumn("Right", 0);
                var bottomCol = GetColumn("Bottom", 0);

                // posun: top <- left, right <- top, bottom <- right, left <- bottom
                SetColumn("Top", 0, leftCol);
                SetColumn("Right", 0, topCol);
                SetColumn("Bottom", 0, rightCol);
                SetColumn("Left", 0, bottomCol);

                // kraj => stěna "Top" se otáčí clockwise
                RotateSideClockwise("Top");
            }
            else if (side == "Top" && col == 1)
            {
                // střed => slice
                var leftCol = GetColumn("Left", 1);
                var topCol = GetColumn("Top", 1);
                var rightCol = GetColumn("Right", 1);
                var bottomCol = GetColumn("Bottom", 1);

                // posun: top <- left, right <- top, bottom <- right, left <- bottom
                SetColumn("Top", 1, leftCol);
                SetColumn("Right", 1, topCol);
                SetColumn("Bottom", 1, rightCol);
                SetColumn("Left", 1, bottomCol);

                // střed => neotáčí se "Top"
            }
            else if (side == "Top" && col == 2)
            {
                var leftCol = GetColumn("Left", 2);
                var topCol = GetColumn("Top", 2);
                var rightCol = GetColumn("Right", 2);
                var bottomCol = GetColumn("Bottom", 2);

                SetColumn("Top", 2, leftCol);
                SetColumn("Right", 2, topCol);
                SetColumn("Bottom", 2, rightCol);
                SetColumn("Left", 2, bottomCol);

                RotateSideClockwise("Top");
            }
            else if (side == "Bottom" && col == 0)
            {
                // "Bottom", col=0 => posun "Up":
                //   typically: bottom col=0 -> back col=2 -> top col=0 -> front col=0 ? 
                //   Zde jedna možná definice:
                var bottomCol = GetColumn("Bottom", 0);
                var backCol = GetColumn("Back", 2).AsEnumerable().Reverse().ToList();
                var topCol = GetColumn("Top", 0);
                var frontCol = GetColumn("Front", 0);

                // posun "Up": back <- bottom, top <- back, front <- top, bottom <- front
                SetColumn("Back", 2, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 0, backCol);
                SetColumn("Front", 0, topCol);
                SetColumn("Bottom", 0, frontCol);

                // kraj => stěna "Bottom" => RotateSideClockwise
                RotateSideClockwise("Bottom");
            }
            else if (side == "Bottom" && col == 1)
            {
                // střed => slice
                var bottomCol = GetColumn("Bottom", 1);
                var backCol = GetColumn("Back", 1).AsEnumerable().Reverse().ToList();
                var topCol = GetColumn("Top", 1);
                var frontCol = GetColumn("Front", 1);

                SetColumn("Back", 1, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 1, backCol);
                SetColumn("Front", 1, topCol);
                SetColumn("Bottom", 1, frontCol);
            }
            else if (side == "Bottom" && col == 2)
            {
                var bottomCol = GetColumn("Bottom", 2);
                var backCol = GetColumn("Back", 0).AsEnumerable().Reverse().ToList();
                var topCol = GetColumn("Top", 2);
                var frontCol = GetColumn("Front", 2);

                SetColumn("Back", 0, bottomCol.AsEnumerable().Reverse().ToList());
                SetColumn("Top", 2, backCol);
                SetColumn("Front", 2, topCol);
                SetColumn("Bottom", 2, frontCol);

                RotateSideClockwise("Bottom");
            }

            // (Zde budou else-ify pro Back, Left, Right, Top, Bottom v dalších částech)
        }


        private void RotateColumnDown(string side, int row, int col)
        {
            // FRONT, col=0
            if (side == "Front" && col == 0)
            {
                // Opačný posun: top col=0 -> back col=2 (reverse) -> bottom col=0 -> front col=0 -> top col=0 
                // (Fyzicky je to "přesně opačná" výměna.)
                var topCol = GetColumn("Top", 0);
                var backCol = GetColumn("Back", 2); // reverse bude až při set
                var bottomCol = GetColumn("Bottom", 0);
                var frontCol = GetColumn("Front", 0);

                // Posun "Down": top <- back(rev), front <- top, bottom <- front, back <- bottom(rev)
                SetColumn("Top", 0, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 0, topCol);
                SetColumn("Bottom", 0, frontCol);
                SetColumn("Back", 2, bottomCol.AsEnumerable().Reverse().ToList());

                // Kraj => stěnu otočíme proti směru
                RotateSideCounterclockwise("Front");
            }
            else if (side == "Front" && col == 1)
            {
                // střed => stejná logika, ale bez otáčení stěny
                var topCol = GetColumn("Top", 1);
                var backCol = GetColumn("Back", 1);
                var bottomCol = GetColumn("Bottom", 1);
                var frontCol = GetColumn("Front", 1);

                SetColumn("Top", 1, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 1, topCol);
                SetColumn("Bottom", 1, frontCol);
                SetColumn("Back", 1, bottomCol.AsEnumerable().Reverse().ToList());
            }
            else if (side == "Front" && col == 2)
            {
                var topCol = GetColumn("Top", 2);
                var backCol = GetColumn("Back", 0);
                var bottomCol = GetColumn("Bottom", 2);
                var frontCol = GetColumn("Front", 2);

                SetColumn("Top", 2, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 2, topCol);
                SetColumn("Bottom", 2, frontCol);
                SetColumn("Back", 0, bottomCol.AsEnumerable().Reverse().ToList());

                RotateSideCounterclockwise("Front");
            }
            if (side == "Back" && col == 0)
            {
                // Opačný posun oproti "Up"
                var frontCol = GetColumn("Front", 2).AsEnumerable().Reverse().ToList();
                var bottomCol = GetColumn("Bottom", 2);
                var backCol = GetColumn("Back", 0).AsEnumerable().Reverse().ToList();
                var topCol = GetColumn("Top", 2);

                // front->top, top->back, back->bottom, bottom->front => atd.
                // Tady explicitně nebo radši 3× RotateColumnUp?
                // Pro demonstration:

                SetColumn("Top", 2, frontCol.AsEnumerable().Reverse().ToList());
                SetColumn("Back", 0, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 2, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 2, bottomCol.AsEnumerable().Reverse().ToList());

                RotateSideCounterclockwise("Back");
            }
            else if (side == "Back" && col == 1)
            {
                // střed
                var frontCol = GetColumn("Front", 1).AsEnumerable().Reverse().ToList();
                var bottomCol = GetColumn("Bottom", 1);
                var backCol = GetColumn("Back", 1).AsEnumerable().Reverse().ToList();
                var topCol = GetColumn("Top", 1);

                SetColumn("Top", 1, frontCol.AsEnumerable().Reverse().ToList());
                SetColumn("Back", 1, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 1, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 1, bottomCol.AsEnumerable().Reverse().ToList());
            }
            else if (side == "Back" && col == 2)
            {
                // Opačný posun než "Up" pro (side="Back", col=2).
                // Tj. "Down" je přesně inverze toho, co bylo v RotateColumnUp u (side="Back", col=2).
                // Při "Up" se posouvalo: front <- top, bottom <- front, back <- bottom, top <- back
                // Takže "Down" bude: top <- front, back <- top, bottom <- back, front <- bottom

                var frontCol = GetColumn("Front", 0);
                var topCol = GetColumn("Top", 2).AsEnumerable().Reverse().ToList();
                var backCol = GetColumn("Back", 2);
                var bottomCol = GetColumn("Bottom", 0).AsEnumerable().Reverse().ToList();

                // "Down" => 
                //   top <- front (ale s ohledem na orientaci "back" a "top" obvykle se reverse)
                //   back <- top 
                //   bottom <- back
                //   front <- bottom
                // (Pozn.: Reverse() může být potřeba i v jiných krocích, 
                //  dle přesného layoutu "Back" v netu. Můžete poladit.)

                SetColumn("Top", 2, frontCol.AsEnumerable().Reverse().ToList());
                SetColumn("Back", 2, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 0, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 0, bottomCol.AsEnumerable().Reverse().ToList());

                // Kraj => otočíme stěnu "Back" proti směru
                RotateSideCounterclockwise("Back");
            }
            //else
            //{
            //    // fallback: například 3× RotateColumnUp(...) 
            //    // (případně nic, pokud sem teoreticky nikdy nespadnete)
            //    for (int i = 0; i < 3; i++)
            //    {
            //        RotateColumnUp(side, row, col);
            //    }
            //}
            else if (side == "Left" && col == 0)
            {
                // Opačná logika než "Up"
                // top<-back(reverse), left<-top, bottom<-left, back<-bottom(reverse) atp.
                var backCol = GetColumn("Back", 2);
                var topCol = GetColumn("Top", 0);
                var leftCol = GetColumn("Left", 0);
                var bottomCol = GetColumn("Bottom", 0);

                // posun "Down":
                SetColumn("Top", 0, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Left", 0, topCol);
                SetColumn("Bottom", 0, leftCol);
                SetColumn("Back", 2, bottomCol.AsEnumerable().Reverse().ToList());

                RotateSideCounterclockwise("Left");
            }
            else if (side == "Left" && col == 1)
            {
                // střed => slice
                var backCol = GetColumn("Back", 1);
                var topCol = GetColumn("Top", 1);
                var leftCol = GetColumn("Left", 1);
                var bottomCol = GetColumn("Bottom", 1);

                SetColumn("Top", 1, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Left", 1, topCol);
                SetColumn("Bottom", 1, leftCol);
                SetColumn("Back", 1, bottomCol.AsEnumerable().Reverse().ToList());
            }
            else if (side == "Left" && col == 2)
            {
                var backCol = GetColumn("Back", 0);
                var topCol = GetColumn("Top", 2);
                var leftCol = GetColumn("Left", 2);
                var bottomCol = GetColumn("Bottom", 2);

                SetColumn("Top", 2, backCol.AsEnumerable().Reverse().ToList());
                SetColumn("Left", 2, topCol);
                SetColumn("Bottom", 2, leftCol);
                SetColumn("Back", 0, bottomCol.AsEnumerable().Reverse().ToList());

                RotateSideCounterclockwise("Left");
            }
            else if (side == "Right" && col == 0)
            {
                // Opačný posun: 
                var frontCol = GetColumn("Front", 2).AsEnumerable().Reverse().ToList();
                var bottomCol = GetColumn("Bottom", 2);
                var rightCol = GetColumn("Right", 0);
                var topCol = GetColumn("Top", 2);

                // posun "Down": top <- front, right <- top, bottom <- right, front <- bottom
                SetColumn("Top", 2, frontCol.AsEnumerable().Reverse().ToList());
                SetColumn("Right", 0, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 2, rightCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 2, bottomCol.AsEnumerable().Reverse().ToList());

                // kraj => stěna "Right" CCW
                RotateSideCounterclockwise("Right");
            }
            else if (side == "Right" && col == 1)
            {
                var frontCol = GetColumn("Front", 1).AsEnumerable().Reverse().ToList();
                var bottomCol = GetColumn("Bottom", 1);
                var rightCol = GetColumn("Right", 1);
                var topCol = GetColumn("Top", 1);

                SetColumn("Top", 1, frontCol.AsEnumerable().Reverse().ToList());
                SetColumn("Right", 1, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 1, rightCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 1, bottomCol.AsEnumerable().Reverse().ToList());
            }
            else if (side == "Right" && col == 2)
            {
                var frontCol = GetColumn("Front", 0).AsEnumerable().Reverse().ToList();
                var bottomCol = GetColumn("Bottom", 0);
                var rightCol = GetColumn("Right", 2);
                var topCol = GetColumn("Top", 0);

                SetColumn("Top", 0, frontCol.AsEnumerable().Reverse().ToList());
                SetColumn("Right", 2, topCol.AsEnumerable().Reverse().ToList());
                SetColumn("Bottom", 0, rightCol.AsEnumerable().Reverse().ToList());
                SetColumn("Front", 0, bottomCol.AsEnumerable().Reverse().ToList());

                RotateSideCounterclockwise("Right");
            }
            else if (side == "Top" && col == 0)
            {
                // Opačný posun k "Up".
                var bottomCol = GetColumn("Bottom", 0);
                var rightCol = GetColumn("Right", 0);
                var topCol = GetColumn("Top", 0);
                var leftCol = GetColumn("Left", 0);

                // posun "Down": left <- top => top <- right => right <- bottom => bottom <- left
                SetColumn("Left", 0, topCol);
                SetColumn("Top", 0, rightCol);
                SetColumn("Right", 0, bottomCol);
                SetColumn("Bottom", 0, leftCol);

                // kraj => "Top" protisměr
                RotateSideCounterclockwise("Top");
            }
            else if (side == "Top" && col == 1)
            {
                var bottomCol = GetColumn("Bottom", 1);
                var rightCol = GetColumn("Right", 1);
                var topCol = GetColumn("Top", 1);
                var leftCol = GetColumn("Left", 1);

                SetColumn("Left", 1, topCol);
                SetColumn("Top", 1, rightCol);
                SetColumn("Right", 1, bottomCol);
                SetColumn("Bottom", 1, leftCol);
            }
            else if (side == "Top" && col == 2)
            {
                var bottomCol = GetColumn("Bottom", 2);
                var rightCol = GetColumn("Right", 2);
                var topCol = GetColumn("Top", 2);
                var leftCol = GetColumn("Left", 2);

                SetColumn("Left", 2, topCol);
                SetColumn("Top", 2, rightCol);
                SetColumn("Right", 2, bottomCol);
                SetColumn("Bottom", 2, leftCol);

                RotateSideCounterclockwise("Top");
            }
            else if (side == "Bottom" && col == 0)
            {
                // Opačný posun k "Up"
                //   bottom->front, front->top, top->back, back->bottom
                var frontCol = GetColumn("Front", 0);
                var topCol = GetColumn("Top", 0);
                var backCol = GetColumn("Back", 2);
                var bottomCol = GetColumn("Bottom", 0);

                SetColumn("Front", 0, bottomCol);
                SetColumn("Top", 0, frontCol);
                SetColumn("Back", 2, topCol);
                SetColumn("Bottom", 0, backCol);

                RotateSideCounterclockwise("Bottom");
            }
            else if (side == "Bottom" && col == 1)
            {
                var frontCol = GetColumn("Front", 1);
                var topCol = GetColumn("Top", 1);
                var backCol = GetColumn("Back", 1);
                var bottomCol = GetColumn("Bottom", 1);

                SetColumn("Front", 1, bottomCol);
                SetColumn("Top", 1, frontCol);
                SetColumn("Back", 1, topCol);
                SetColumn("Bottom", 1, backCol);
            }
            else if (side == "Bottom" && col == 2)
            {
                var frontCol = GetColumn("Front", 2);
                var topCol = GetColumn("Top", 2);
                var backCol = GetColumn("Back", 0);
                var bottomCol = GetColumn("Bottom", 2);

                SetColumn("Front", 2, bottomCol);
                SetColumn("Top", 2, frontCol);
                SetColumn("Back", 0, topCol);
                SetColumn("Bottom", 2, backCol);

                RotateSideCounterclockwise("Bottom");
            }

            // (else if pro "Back", "Left", "Right", "Top", "Bottom" atd. v dalších částech)
        }

        private void RotateRowLeft(string side, int row, int col)
        {
            // FRONT, row=0
            if (side == "Front" && row == 0)
            {
                var frontRow = GetRow("Front", 0);
                var leftRow = GetRow("Left", 0);
                var backRow = GetRow("Back", 0);
                var rightRow = GetRow("Right", 0);

                // posun doleva: front->left, left->back, back->right, right->front
                SetRow("Left", 0, frontRow);
                SetRow("Back", 0, leftRow);
                SetRow("Right", 0, backRow);
                SetRow("Front", 0, rightRow);

                // kraj => row=0 => stěnu "Front" otočíme proti směru
                RotateSideCounterclockwise("Front");
            }
            // FRONT, row=1
            else if (side == "Front" && row == 1)
            {
                var frontRow = GetRow("Front", 1);
                var leftRow = GetRow("Left", 1);
                var backRow = GetRow("Back", 1);
                var rightRow = GetRow("Right", 1);

                SetRow("Left", 1, frontRow);
                SetRow("Back", 1, leftRow);
                SetRow("Right", 1, backRow);
                SetRow("Front", 1, rightRow);

                // střed => slice, neotáčí se face
            }
            // FRONT, row=2
            else if (side == "Front" && row == 2)
            {
                var frontRow = GetRow("Front", 2);
                var leftRow = GetRow("Left", 2);
                var backRow = GetRow("Back", 2);
                var rightRow = GetRow("Right", 2);

                SetRow("Left", 2, frontRow);
                SetRow("Back", 2, leftRow);
                SetRow("Right", 2, backRow);
                SetRow("Front", 2, rightRow);

                // kraj => row=2 => stěna "Front" otočit proti směru
                RotateSideCounterclockwise("Front");
            }
            if (side == "Back" && row == 0)
            {
                // row=0 => horní řada "Back" 
                // posun left: back->right, right->front, front->left, left->back
                var backRow = GetRow("Back", 0);
                var rightRow = GetRow("Right", 0);
                var frontRow = GetRow("Front", 0);
                var leftRow = GetRow("Left", 0);

                SetRow("Right", 0, backRow);
                SetRow("Front", 0, rightRow);
                SetRow("Left", 0, frontRow);
                SetRow("Back", 0, leftRow);

                // Kraj => row=0 => stěna "Back" proti směru
                RotateSideCounterclockwise("Back");
            }
            else if (side == "Back" && row == 1)
            {
                // střed => slice: back->right, right->front, front->left, left->back
                var backRow = GetRow("Back", 1);
                var rightRow = GetRow("Right", 1);
                var frontRow = GetRow("Front", 1);
                var leftRow = GetRow("Left", 1);

                SetRow("Right", 1, backRow);
                SetRow("Front", 1, rightRow);
                SetRow("Left", 1, frontRow);
                SetRow("Back", 1, leftRow);

                // střed => netočí se face "Back"
            }
            else if (side == "Back" && row == 2)
            {
                var backRow = GetRow("Back", 2);
                var rightRow = GetRow("Right", 2);
                var frontRow = GetRow("Front", 2);
                var leftRow = GetRow("Left", 2);

                SetRow("Right", 2, backRow);
                SetRow("Front", 2, rightRow);
                SetRow("Left", 2, frontRow);
                SetRow("Back", 2, leftRow);

                // kraj => row=2 => stěna "Back" CCW
                RotateSideCounterclockwise("Back");
            }
            else if (side == "Left" && row == 0)
            {
                // row=0 => horní řada "Left"
                // posun "Left": left->back, back->right, right->front, front->left
                var leftRow = GetRow("Left", 0);
                var backRow = GetRow("Back", 0);
                var rightRow = GetRow("Right", 0);
                var frontRow = GetRow("Front", 0);

                SetRow("Back", 0, leftRow);
                SetRow("Right", 0, backRow);
                SetRow("Front", 0, rightRow);
                SetRow("Left", 0, frontRow);

                // kraj => stěna "Left" proti směru
                RotateSideCounterclockwise("Left");
            }
            else if (side == "Left" && row == 1)
            {
                // střed => slice
                var leftRow = GetRow("Left", 1);
                var backRow = GetRow("Back", 1);
                var rightRow = GetRow("Right", 1);
                var frontRow = GetRow("Front", 1);

                SetRow("Back", 1, leftRow);
                SetRow("Right", 1, backRow);
                SetRow("Front", 1, rightRow);
                SetRow("Left", 1, frontRow);
            }
            else if (side == "Left" && row == 2)
            {
                var leftRow = GetRow("Left", 2);
                var backRow = GetRow("Back", 2);
                var rightRow = GetRow("Right", 2);
                var frontRow = GetRow("Front", 2);

                SetRow("Back", 2, leftRow);
                SetRow("Right", 2, backRow);
                SetRow("Front", 2, rightRow);
                SetRow("Left", 2, frontRow);

                // kraj => stěna "Left" CCW
                RotateSideCounterclockwise("Left");
            }
            else if (side == "Right" && row == 0)
            {
                // row=0 => horní řada "Right"
                // posun "Left": right->front, front->left, left->back, back->right
                var rightRow = GetRow("Right", 0);
                var frontRow = GetRow("Front", 0);
                var leftRow = GetRow("Left", 0);
                var backRow = GetRow("Back", 0);

                SetRow("Front", 0, rightRow);
                SetRow("Left", 0, frontRow);
                SetRow("Back", 0, leftRow);
                SetRow("Right", 0, backRow);

                // kraj => row=0 => stěna "Right" CCW
                RotateSideCounterclockwise("Right");
            }
            else if (side == "Right" && row == 1)
            {
                var rightRow = GetRow("Right", 1);
                var frontRow = GetRow("Front", 1);
                var leftRow = GetRow("Left", 1);
                var backRow = GetRow("Back", 1);

                SetRow("Front", 1, rightRow);
                SetRow("Left", 1, frontRow);
                SetRow("Back", 1, leftRow);
                SetRow("Right", 1, backRow);
                // střed => slice
            }
            else if (side == "Right" && row == 2)
            {
                var rightRow = GetRow("Right", 2);
                var frontRow = GetRow("Front", 2);
                var leftRow = GetRow("Left", 2);
                var backRow = GetRow("Back", 2);

                SetRow("Front", 2, rightRow);
                SetRow("Left", 2, frontRow);
                SetRow("Back", 2, leftRow);
                SetRow("Right", 2, backRow);

                // kraj => row=2 => stěna "Right" CCW
                RotateSideCounterclockwise("Right");
            }
            else if (side == "Top" && row == 0)
            {
                // posun "Left": top->left, left->bottom, bottom->right, right->top
                var topRow = GetRow("Top", 0);
                var leftRow = GetRow("Left", 0);
                var bottomRow = GetRow("Bottom", 0);
                var rightRow = GetRow("Right", 0);

                SetRow("Left", 0, topRow);
                SetRow("Bottom", 0, leftRow);
                SetRow("Right", 0, bottomRow);
                SetRow("Top", 0, rightRow);

                // kraj => stěna "Top" => CCW
                RotateSideCounterclockwise("Top");
            }
            else if (side == "Top" && row == 1)
            {
                // střed => slice
                var topRow = GetRow("Top", 1);
                var leftRow = GetRow("Left", 1);
                var bottomRow = GetRow("Bottom", 1);
                var rightRow = GetRow("Right", 1);

                SetRow("Left", 1, topRow);
                SetRow("Bottom", 1, leftRow);
                SetRow("Right", 1, bottomRow);
                SetRow("Top", 1, rightRow);
            }
            else if (side == "Top" && row == 2)
            {
                var topRow = GetRow("Top", 2);
                var leftRow = GetRow("Left", 2);
                var bottomRow = GetRow("Bottom", 2);
                var rightRow = GetRow("Right", 2);

                SetRow("Left", 2, topRow);
                SetRow("Bottom", 2, leftRow);
                SetRow("Right", 2, bottomRow);
                SetRow("Top", 2, rightRow);

                // kraj => row=2 => stěna "Top" CCW
                RotateSideCounterclockwise("Top");
            }
            else if (side == "Bottom" && row == 0)
            {
                // row=0 => posun "Left"
                //   bottom->right, right->top, top->left, left->bottom  (např.)
                var bottomRow = GetRow("Bottom", 0);
                var rightRow = GetRow("Right", 0);
                var topRow = GetRow("Top", 0);
                var leftRow = GetRow("Left", 0);

                SetRow("Right", 0, bottomRow);
                SetRow("Top", 0, rightRow);
                SetRow("Left", 0, topRow);
                SetRow("Bottom", 0, leftRow);

                // kraj => row=0 => stěna "Bottom" proti směru
                RotateSideCounterclockwise("Bottom");
            }
            else if (side == "Bottom" && row == 1)
            {
                var bottomRow = GetRow("Bottom", 1);
                var rightRow = GetRow("Right", 1);
                var topRow = GetRow("Top", 1);
                var leftRow = GetRow("Left", 1);

                SetRow("Right", 1, bottomRow);
                SetRow("Top", 1, rightRow);
                SetRow("Left", 1, topRow);
                SetRow("Bottom", 1, leftRow);

                // střed => slice
            }
            else if (side == "Bottom" && row == 2)
            {
                var bottomRow = GetRow("Bottom", 2);
                var rightRow = GetRow("Right", 2);
                var topRow = GetRow("Top", 2);
                var leftRow = GetRow("Left", 2);

                SetRow("Right", 2, bottomRow);
                SetRow("Top", 2, rightRow);
                SetRow("Left", 2, topRow);
                SetRow("Bottom", 2, leftRow);

                // kraj => row=2 => stěna "Bottom" CCW
                RotateSideCounterclockwise("Bottom");
            }

            // (Další else if pro Back, Left, Right, Top, Bottom)
        }

        private void RotateRowRight(string side, int row, int col)
        {
            if (side == "Front" && row == 0)
            {
                // posun: front->right, right->back, back->left, left->front
                var frontRow = GetRow("Front", 0);
                var rightRow = GetRow("Right", 0);
                var backRow = GetRow("Back", 0);
                var leftRow = GetRow("Left", 0);

                SetRow("Right", 0, frontRow);
                SetRow("Back", 0, rightRow);
                SetRow("Left", 0, backRow);
                SetRow("Front", 0, leftRow);

                // kraj => row=0 => stěna "Front" otočí se ve směru
                RotateSideClockwise("Front");
            }
            else if (side == "Front" && row == 1)
            {
                var frontRow = GetRow("Front", 1);
                var rightRow = GetRow("Right", 1);
                var backRow = GetRow("Back", 1);
                var leftRow = GetRow("Left", 1);

                SetRow("Right", 1, frontRow);
                SetRow("Back", 1, rightRow);
                SetRow("Left", 1, backRow);
                SetRow("Front", 1, leftRow);
                // střed => slice
            }
            else if (side == "Front" && row == 2)
            {
                var frontRow = GetRow("Front", 2);
                var rightRow = GetRow("Right", 2);
                var backRow = GetRow("Back", 2);
                var leftRow = GetRow("Left", 2);

                SetRow("Right", 2, frontRow);
                SetRow("Back", 2, rightRow);
                SetRow("Left", 2, backRow);
                SetRow("Front", 2, leftRow);

                // kraj => row=2 => stěna "Front" clockwise
                RotateSideClockwise("Front");
            }
            if (side == "Back" && row == 0)
            {
                // back->left, left->front, front->right, right->back
                var backRow = GetRow("Back", 0);
                var leftRow = GetRow("Left", 0);
                var frontRow = GetRow("Front", 0);
                var rightRow = GetRow("Right", 0);

                SetRow("Left", 0, backRow);
                SetRow("Front", 0, leftRow);
                SetRow("Right", 0, frontRow);
                SetRow("Back", 0, rightRow);

                // kraj => row=0 => stěna "Back" (clockwise)
                RotateSideClockwise("Back");
            }
            else if (side == "Back" && row == 1)
            {
                var backRow = GetRow("Back", 1);
                var leftRow = GetRow("Left", 1);
                var frontRow = GetRow("Front", 1);
                var rightRow = GetRow("Right", 1);

                SetRow("Left", 1, backRow);
                SetRow("Front", 1, leftRow);
                SetRow("Right", 1, frontRow);
                SetRow("Back", 1, rightRow);
                // střed => slice
            }
            else if (side == "Back" && row == 2)
            {
                var backRow = GetRow("Back", 2);
                var leftRow = GetRow("Left", 2);
                var frontRow = GetRow("Front", 2);
                var rightRow = GetRow("Right", 2);

                SetRow("Left", 2, backRow);
                SetRow("Front", 2, leftRow);
                SetRow("Right", 2, frontRow);
                SetRow("Back", 2, rightRow);

                RotateSideClockwise("Back");
            }
            else if (side == "Left" && row == 0)
            {
                // posun "Right": left->front, front->right, right->back, back->left
                var leftRow = GetRow("Left", 0);
                var frontRow = GetRow("Front", 0);
                var rightRow = GetRow("Right", 0);
                var backRow = GetRow("Back", 0);

                SetRow("Front", 0, leftRow);
                SetRow("Right", 0, frontRow);
                SetRow("Back", 0, rightRow);
                SetRow("Left", 0, backRow);

                // kraj => stěna "Left" clockwise
                RotateSideClockwise("Left");
            }
            else if (side == "Left" && row == 1)
            {
                var leftRow = GetRow("Left", 1);
                var frontRow = GetRow("Front", 1);
                var rightRow = GetRow("Right", 1);
                var backRow = GetRow("Back", 1);

                SetRow("Front", 1, leftRow);
                SetRow("Right", 1, frontRow);
                SetRow("Back", 1, rightRow);
                SetRow("Left", 1, backRow);
                // střed => slice
            }
            else if (side == "Left" && row == 2)
            {
                var leftRow = GetRow("Left", 2);
                var frontRow = GetRow("Front", 2);
                var rightRow = GetRow("Right", 2);
                var backRow = GetRow("Back", 2);

                SetRow("Front", 2, leftRow);
                SetRow("Right", 2, frontRow);
                SetRow("Back", 2, rightRow);
                SetRow("Left", 2, backRow);

                // kraj => row=2 => stěna "Left" CW
                RotateSideClockwise("Left");
            }
            else if (side == "Right" && row == 0)
            {
                // posun "Right": right->back, back->left, left->front, front->right
                var rightRow = GetRow("Right", 0);
                var backRow = GetRow("Back", 0);
                var leftRow = GetRow("Left", 0);
                var frontRow = GetRow("Front", 0);

                SetRow("Back", 0, rightRow);
                SetRow("Left", 0, backRow);
                SetRow("Front", 0, leftRow);
                SetRow("Right", 0, frontRow);

                // kraj => row=0 => stěna "Right" CW
                RotateSideClockwise("Right");
            }
            else if (side == "Right" && row == 1)
            {
                var rightRow = GetRow("Right", 1);
                var backRow = GetRow("Back", 1);
                var leftRow = GetRow("Left", 1);
                var frontRow = GetRow("Front", 1);

                SetRow("Back", 1, rightRow);
                SetRow("Left", 1, backRow);
                SetRow("Front", 1, leftRow);
                SetRow("Right", 1, frontRow);
                // střed => slice
            }
            else if (side == "Right" && row == 2)
            {
                var rightRow = GetRow("Right", 2);
                var backRow = GetRow("Back", 2);
                var leftRow = GetRow("Left", 2);
                var frontRow = GetRow("Front", 2);

                SetRow("Back", 2, rightRow);
                SetRow("Left", 2, backRow);
                SetRow("Front", 2, leftRow);
                SetRow("Right", 2, frontRow);

                // kraj => row=2 => stěna "Right" CW
                RotateSideClockwise("Right");
            }
            else if (side == "Top" && row == 0)
            {
                // posun "Right": top->right, right->bottom, bottom->left, left->top
                var topRow = GetRow("Top", 0);
                var rightRow = GetRow("Right", 0);
                var bottomRow = GetRow("Bottom", 0);
                var leftRow = GetRow("Left", 0);

                SetRow("Right", 0, topRow);
                SetRow("Bottom", 0, rightRow);
                SetRow("Left", 0, bottomRow);
                SetRow("Top", 0, leftRow);

                // kraj => stěna "Top" => CW
                RotateSideClockwise("Top");
            }
            else if (side == "Top" && row == 1)
            {
                var topRow = GetRow("Top", 1);
                var rightRow = GetRow("Right", 1);
                var bottomRow = GetRow("Bottom", 1);
                var leftRow = GetRow("Left", 1);

                SetRow("Right", 1, topRow);
                SetRow("Bottom", 1, rightRow);
                SetRow("Left", 1, bottomRow);
                SetRow("Top", 1, leftRow);
            }
            else if (side == "Top" && row == 2)
            {
                var topRow = GetRow("Top", 2);
                var rightRow = GetRow("Right", 2);
                var bottomRow = GetRow("Bottom", 2);
                var leftRow = GetRow("Left", 2);

                SetRow("Right", 2, topRow);
                SetRow("Bottom", 2, rightRow);
                SetRow("Left", 2, bottomRow);
                SetRow("Top", 2, leftRow);

                RotateSideClockwise("Top");
            }
            else if (side == "Bottom" && row == 0)
            {
                // posun "Right":
                //   bottom->left, left->top, top->right, right->bottom
                var bottomRow = GetRow("Bottom", 0);
                var leftRow = GetRow("Left", 0);
                var topRow = GetRow("Top", 0);
                var rightRow = GetRow("Right", 0);

                SetRow("Left", 0, bottomRow);
                SetRow("Top", 0, leftRow);
                SetRow("Right", 0, topRow);
                SetRow("Bottom", 0, rightRow);

                // kraj => row=0 => stěna "Bottom" CW
                RotateSideClockwise("Bottom");
            }
            else if (side == "Bottom" && row == 1)
            {
                var bottomRow = GetRow("Bottom", 1);
                var leftRow = GetRow("Left", 1);
                var topRow = GetRow("Top", 1);
                var rightRow = GetRow("Right", 1);

                SetRow("Left", 1, bottomRow);
                SetRow("Top", 1, leftRow);
                SetRow("Right", 1, topRow);
                SetRow("Bottom", 1, rightRow);

                // střed => slice
            }
            else if (side == "Bottom" && row == 2)
            {
                var bottomRow = GetRow("Bottom", 2);
                var leftRow = GetRow("Left", 2);
                var topRow = GetRow("Top", 2);
                var rightRow = GetRow("Right", 2);

                SetRow("Left", 2, bottomRow);
                SetRow("Top", 2, leftRow);
                SetRow("Right", 2, topRow);
                SetRow("Bottom", 2, rightRow);

                RotateSideClockwise("Bottom");
            }

            // (Další else if pro "Back", "Left", "Right", "Top", "Bottom")
        }

    }
}
