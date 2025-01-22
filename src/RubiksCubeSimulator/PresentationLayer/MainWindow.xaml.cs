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
        private Dictionary<string, List<Color>> colors;
        private Dictionary<string, Grid> sides;
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

            // Výchozí barvy 6 stěn
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

        // Vykreslení 3x3 obdélníků na všech 6 stěn
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

        // Umožní ovládání šipkami z klávesnice
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

        // --- Tlačítka pro rotace (nahoru/dolů/doleva/doprava):

        private void RotateUp_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSide == null || !selectedBlock.HasValue) return;
            SaveStateForUndo();

            // "Nahoru" => otáčí se vybraný sloupec
            DecideAndRotateVertical(selectedSide, selectedBlock.Value.row, selectedBlock.Value.col, moveUp: true);

            // Zvýšíme count a překreslíme
            moveCount++;
            InitializeCube();
        }

        private void RotateDown_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSide == null || !selectedBlock.HasValue) return;
            SaveStateForUndo();

            DecideAndRotateVertical(selectedSide, selectedBlock.Value.row, selectedBlock.Value.col, moveUp: false);

            moveCount++;
            InitializeCube();
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSide == null || !selectedBlock.HasValue) return;
            SaveStateForUndo();

            DecideAndRotateHorizontal(selectedSide, selectedBlock.Value.row, selectedBlock.Value.col, moveLeft: true);

            moveCount++;
            InitializeCube();
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSide == null || !selectedBlock.HasValue) return;
            SaveStateForUndo();

            DecideAndRotateHorizontal(selectedSide, selectedBlock.Value.row, selectedBlock.Value.col, moveLeft: false);

            moveCount++;
            InitializeCube();
        }

        // --- Reset, Undo, Scramble:

        private void ResetCube_Click(object sender, RoutedEventArgs e)
        {
            // Nastavit všechny stěny na původní barvy
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

                // Snížíme moveCount o 1 a hlídáme záporné hodnoty
                moveCount--;
                if (moveCount < 0) moveCount = 0;

                InitializeCube();
            }
        }

        private void ScrambleCube_Click(object sender, RoutedEventArgs e)
        {
            // Provedeme např. 20 náhodných legit tahů, 
            // ALE moveCount nebudeme zvyšovat (aby po scramble byl 0).
            // Avšak pro Undo si ukládáme stavy do undoStack (každý tah).
            // Tím lze teoreticky "Undo" vracet i scramble pohyby.

            var sidesAll = new[] { "Top", "Left", "Front", "Right", "Back", "Bottom" };
            var rnd = new Random();

            int movesToDo = 20; // kolik náhodných tahů
            for (int i = 0; i < movesToDo; i++)
            {
                // Uložíme stav do undoStack
                SaveStateForUndo();

                // Náhodně zvolíme stěnu i polohu
                string s = sidesAll[rnd.Next(sidesAll.Length)];
                int row = rnd.Next(3);
                int col = rnd.Next(3);
                int dir = rnd.Next(4); // 0=Up,1=Down,2=Left,3=Right

                // Provedeme pohyb, ale moveCount NEzvyšujeme
                switch (dir)
                {
                    case 0: DecideAndRotateVertical(s, row, col, moveUp: true); break;
                    case 1: DecideAndRotateVertical(s, row, col, moveUp: false); break;
                    case 2: DecideAndRotateHorizontal(s, row, col, moveLeft: true); break;
                    case 3: DecideAndRotateHorizontal(s, row, col, moveLeft: false); break;
                }
            }

            // Po skončení scramble vynulujeme moveCount 
            moveCount = 0;
            InitializeCube();
        }

        // Ukládá kopii stavu (pro Undo)
        private void SaveStateForUndo()
        {
            var snap = new Dictionary<string, List<Color>>();
            foreach (var kvp in colors)
                snap[kvp.Key] = new List<Color>(kvp.Value);

            undoStack.Push(() =>
            {
                foreach (var k in snap.Keys)
                    colors[k] = new List<Color>(snap[k]);
            });
        }

        // --- Pomocné funkce (GetRow,SetRow,GetColumn,SetColumn)...

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

        // Otočení celé jedné stěny (face):
        private void RotateSideClockwise(string side)
        {
            var old = colors[side].ToArray();
            colors[side][0] = old[6];
            colors[side][1] = old[3];
            colors[side][2] = old[0];
            colors[side][3] = old[7];
            // střed [4] se nemění
            colors[side][5] = old[1];
            colors[side][6] = old[8];
            colors[side][7] = old[5];
            colors[side][8] = old[2];
        }
        private void RotateSideCounterclockwise(string side)
        {
            // 90° proti směru = 3× 90° po směru
            for (int i = 0; i < 3; i++)
                RotateSideClockwise(side);
        }

        // ===============================================
        //   VERTIKÁLNÍ (Up/Down) => sloupec
        // ===============================================

        private void DecideAndRotateVertical(string side, int row, int col, bool moveUp)
        {
            // Např. "Front", col=0 => L/L' atd. 
            if (side == "Front")
            {
                if (col == 0) { if (moveUp) RotateL(); else RotateLprime(); }
                else if (col == 1) { if (moveUp) RotateM(); else RotateMprime(); }
                else if (col == 2) { if (moveUp) RotateR(); else RotateRprime(); }
            }
            else if (side == "Left")
            {
                if (col == 0) { if (moveUp) RotateB(); else RotateBprime(); }
                else if (col == 1) { if (moveUp) RotateM(); else RotateMprime(); }
                else if (col == 2) { if (moveUp) RotateF(); else RotateFprime(); }
            }
            else if (side == "Top")
            {
                if (col == 0) { if (moveUp) RotateL(); else RotateLprime(); }
                else if (col == 1) { if (moveUp) RotateM(); else RotateMprime(); }
                else if (col == 2) { if (moveUp) RotateR(); else RotateRprime(); }
            }
            else if (side == "Right")
            {
                if (col == 0) { if (moveUp) RotateF(); else RotateFprime(); }
                else if (col == 1) { if (moveUp) RotateM(); else RotateMprime(); }
                else if (col == 2) { if (moveUp) RotateB(); else RotateBprime(); }
            }
            else if (side == "Back")
            {
                if (col == 0) { if (moveUp) RotateR(); else RotateRprime(); }
                else if (col == 1) { if (moveUp) RotateM(); else RotateMprime(); }
                else if (col == 2) { if (moveUp) RotateL(); else RotateLprime(); }
            }
            else if (side == "Bottom")
            {
                // col=0 => L' / L, col=1 => M'/M, col=2 => R'/R
                if (col == 0) { if (moveUp) RotateLprime(); else RotateL(); }
                else if (col == 1) { if (moveUp) RotateMprime(); else RotateM(); }
                else if (col == 2) { if (moveUp) RotateRprime(); else RotateR(); }
            }
        }

        // ===============================================
        //   HORIZONTÁLNÍ (Left/Right) => řádek
        // ===============================================

        private void DecideAndRotateHorizontal(string side, int row, int col, bool moveLeft)
        {
            // Front: row=0 => U/U', row=1 => E/E', row=2 => D/D'
            if (side == "Front")
            {
                if (row == 0) { if (moveLeft) RotateUprime(); else RotateU(); }
                else if (row == 1) { if (moveLeft) RotateEprime(); else RotateE(); }
                else if (row == 2) { if (moveLeft) RotateDprime(); else RotateD(); }
            }
            else if (side == "Left")
            {
                if (row == 0) { if (moveLeft) RotateUprime(); else RotateU(); }
                else if (row == 1) { if (moveLeft) RotateEprime(); else RotateE(); }
                else { if (moveLeft) RotateDprime(); else RotateD(); }
            }
            else if (side == "Right")
            {
                if (row == 0) { if (moveLeft) RotateUprime(); else RotateU(); }
                else if (row == 1) { if (moveLeft) RotateEprime(); else RotateE(); }
                else { if (moveLeft) RotateDprime(); else RotateD(); }
            }
            else if (side == "Back")
            {
                // "Back" mívá opačnou interpretaci, 
                //   např. row=0 => (moveLeft)? U : U'
                //   ale je to věc preference
                if (row == 0) { if (moveLeft) RotateU(); else RotateUprime(); }
                else if (row == 1) { if (moveLeft) RotateE(); else RotateEprime(); }
                else { if (moveLeft) RotateD(); else RotateDprime(); }
            }
            else if (side == "Top")
            {
                // row=0 => B'/B, row=1 => S'/S, row=2 => F'/F
                if (row == 0) { if (moveLeft) RotateBprime(); else RotateB(); }
                else if (row == 1) { if (moveLeft) RotateSprime(); else RotateS(); }
                else { if (moveLeft) RotateFprime(); else RotateF(); }
            }
            else if (side == "Bottom")
            {
                // row=0 => F/F', row=1 => S/S', row=2 => B/B'
                if (row == 0) { if (moveLeft) RotateF(); else RotateFprime(); }
                else if (row == 1) { if (moveLeft) RotateS(); else RotateSprime(); }
                else { if (moveLeft) RotateB(); else RotateBprime(); }
            }
        }

        // =============================
        //  KONKRÉTNÍ TAHY FACE / SLICE
        // =============================

        private void RotateU()
        {
            var frontRow = GetRow("Front", 0);
            var rightRow = GetRow("Right", 0);
            var backRow = GetRow("Back", 0);
            var leftRow = GetRow("Left", 0);

            // front->right->back->left->front
            SetRow("Right", 0, frontRow);
            SetRow("Back", 0, rightRow);
            SetRow("Left", 0, backRow);
            SetRow("Front", 0, leftRow);

            RotateSideClockwise("Top");
        }
        private void RotateUprime()
        {
            var f = GetRow("Front", 0);
            var r = GetRow("Right", 0);
            var b = GetRow("Back", 0);
            var l = GetRow("Left", 0);

            SetRow("Front", 0, r);
            SetRow("Right", 0, b);
            SetRow("Back", 0, l);
            SetRow("Left", 0, f);

            RotateSideCounterclockwise("Top");
        }

        private void RotateD()
        {
            var f = GetRow("Front", 2);
            var l = GetRow("Left", 2);
            var b = GetRow("Back", 2);
            var r = GetRow("Right", 2);

            SetRow("Left", 2, f);
            SetRow("Back", 2, l);
            SetRow("Right", 2, b);
            SetRow("Front", 2, r);

            RotateSideClockwise("Bottom");
        }
        private void RotateDprime()
        {
            var f = GetRow("Front", 2);
            var l = GetRow("Left", 2);
            var b = GetRow("Back", 2);
            var r = GetRow("Right", 2);

            SetRow("Front", 2, l);
            SetRow("Left", 2, b);
            SetRow("Back", 2, r);
            SetRow("Right", 2, f);

            RotateSideCounterclockwise("Bottom");
        }

        private void RotateL()
        {
            var topCol = GetColumn("Top", 0);
            var frontCol = GetColumn("Front", 0);
            var bottomCol = GetColumn("Bottom", 0);
            var backCol = GetColumn("Back", 2).AsEnumerable().Reverse().ToList();

            SetColumn("Front", 0, topCol);
            SetColumn("Bottom", 0, frontCol);
            SetColumn("Back", 2, bottomCol.AsEnumerable().Reverse().ToList());
            SetColumn("Top", 0, backCol);

            RotateSideClockwise("Left");
        }
        private void RotateLprime()
        {
            var tC = GetColumn("Top", 0);
            var fC = GetColumn("Front", 0);
            var bC = GetColumn("Bottom", 0);
            var baC = GetColumn("Back", 2).AsEnumerable().Reverse().ToList();

            SetColumn("Top", 0, fC);
            SetColumn("Front", 0, bC);
            SetColumn("Bottom", 0, baC.AsEnumerable().Reverse().ToList());
            SetColumn("Back", 2, tC.AsEnumerable().Reverse().ToList());

            RotateSideCounterclockwise("Left");
        }

        private void RotateR()
        {
            var topCol = GetColumn("Top", 2);
            var frontCol = GetColumn("Front", 2);
            var bottomCol = GetColumn("Bottom", 2);
            var backCol = GetColumn("Back", 0).AsEnumerable().Reverse().ToList();

            SetColumn("Front", 2, topCol);
            SetColumn("Bottom", 2, frontCol);
            SetColumn("Back", 0, bottomCol.AsEnumerable().Reverse().ToList());
            SetColumn("Top", 2, backCol);

            RotateSideClockwise("Right");
        }
        private void RotateRprime()
        {
            var tC = GetColumn("Top", 2);
            var fC = GetColumn("Front", 2);
            var bC = GetColumn("Bottom", 2);
            var baC = GetColumn("Back", 0).AsEnumerable().Reverse().ToList();

            SetColumn("Top", 2, fC);
            SetColumn("Front", 2, bC);
            SetColumn("Bottom", 2, baC.AsEnumerable().Reverse().ToList());
            SetColumn("Back", 0, tC.AsEnumerable().Reverse().ToList());

            RotateSideCounterclockwise("Right");
        }

        private void RotateF()
        {
            var topRow = GetRow("Top", 2);
            var leftCol = GetColumn("Left", 2).AsEnumerable().Reverse().ToList();
            var botRow = GetRow("Bottom", 0).AsEnumerable().Reverse().ToList();
            var rightCol = GetColumn("Right", 0);

            SetColumn("Left", 2, topRow);
            SetRow("Bottom", 0, leftCol.AsEnumerable().Reverse().ToList());
            SetColumn("Right", 0, botRow);
            SetRow("Top", 2, rightCol);

            RotateSideClockwise("Front");
        }
        private void RotateFprime()
        {
            var topRow = GetRow("Top", 2);
            var leftCol = GetColumn("Left", 2).AsEnumerable().Reverse().ToList();
            var botRow = GetRow("Bottom", 0).AsEnumerable().Reverse().ToList();
            var rightCol = GetColumn("Right", 0);

            // inverze F
            SetRow("Top", 2, leftCol.AsEnumerable().Reverse().ToList());
            SetColumn("Left", 2, botRow);
            SetRow("Bottom", 0, rightCol.AsEnumerable().Reverse().ToList());
            SetColumn("Right", 0, topRow);

            RotateSideCounterclockwise("Front");
        }

        private void RotateB()
        {
            var topRow = GetRow("Top", 0).AsEnumerable().Reverse().ToList();
            var rightCol = GetColumn("Right", 2);
            var bottomRow = GetRow("Bottom", 2).AsEnumerable().Reverse().ToList();
            var leftCol = GetColumn("Left", 0).AsEnumerable().Reverse().ToList();

            SetColumn("Right", 2, topRow);
            SetRow("Bottom", 2, rightCol.AsEnumerable().Reverse().ToList());
            SetColumn("Left", 0, bottomRow);
            SetRow("Top", 0, leftCol.AsEnumerable().Reverse().ToList());

            RotateSideClockwise("Back");
        }
        private void RotateBprime()
        {
            var topRow = GetRow("Top", 0).AsEnumerable().Reverse().ToList();
            var rightCol = GetColumn("Right", 2);
            var bottomRow = GetRow("Bottom", 2).AsEnumerable().Reverse().ToList();
            var leftCol = GetColumn("Left", 0).AsEnumerable().Reverse().ToList();

            SetRow("Top", 0, rightCol.AsEnumerable().Reverse().ToList());
            SetColumn("Right", 2, bottomRow.AsEnumerable().Reverse().ToList());
            SetRow("Bottom", 2, leftCol.AsEnumerable().Reverse().ToList());
            SetColumn("Left", 0, topRow.AsEnumerable().Reverse().ToList());

            RotateSideCounterclockwise("Back");
        }

        // ----- Slice tahy (M, E, S) -----

        private void RotateM()
        {
            // střední sloupec (col=1) z pohledu Front
            var topCol = GetColumn("Top", 1);
            var frontCol = GetColumn("Front", 1);
            var bottomCol = GetColumn("Bottom", 1);
            var backCol = GetColumn("Back", 1).AsEnumerable().Reverse().ToList();

            SetColumn("Front", 1, topCol);
            SetColumn("Bottom", 1, frontCol);
            SetColumn("Back", 1, bottomCol.AsEnumerable().Reverse().ToList());
            SetColumn("Top", 1, backCol);
        }
        private void RotateMprime()
        {
            var tC = GetColumn("Top", 1);
            var fC = GetColumn("Front", 1);
            var bC = GetColumn("Bottom", 1);
            var bkC = GetColumn("Back", 1).AsEnumerable().Reverse().ToList();

            SetColumn("Top", 1, fC);
            SetColumn("Front", 1, bC);
            SetColumn("Bottom", 1, bkC.AsEnumerable().Reverse().ToList());
            SetColumn("Back", 1, tC.AsEnumerable().Reverse().ToList());
        }

        private void RotateE()
        {
            // střední řádek (row=1)
            var f = GetRow("Front", 1);
            var r = GetRow("Right", 1);
            var b = GetRow("Back", 1);
            var l = GetRow("Left", 1);

            SetRow("Right", 1, f);
            SetRow("Back", 1, r);
            SetRow("Left", 1, b);
            SetRow("Front", 1, l);
        }
        private void RotateEprime()
        {
            var f = GetRow("Front", 1);
            var r = GetRow("Right", 1);
            var b = GetRow("Back", 1);
            var l = GetRow("Left", 1);

            SetRow("Front", 1, r);
            SetRow("Right", 1, b);
            SetRow("Back", 1, l);
            SetRow("Left", 1, f);
        }

        private void RotateS()
        {
            // střední "hloubka" (mezi F a B)
            var topRow = GetRow("Top", 1);
            var leftCol = GetColumn("Left", 1).AsEnumerable().Reverse().ToList();
            var bottomRow = GetRow("Bottom", 1).AsEnumerable().Reverse().ToList();
            var rightCol = GetColumn("Right", 1);

            SetColumn("Left", 1, topRow);
            SetRow("Bottom", 1, leftCol.AsEnumerable().Reverse().ToList());
            SetColumn("Right", 1, bottomRow);
            SetRow("Top", 1, rightCol);
        }
        private void RotateSprime()
        {
            var topRow = GetRow("Top", 1);
            var leftCol = GetColumn("Left", 1).AsEnumerable().Reverse().ToList();
            var bottomRow = GetRow("Bottom", 1).AsEnumerable().Reverse().ToList();
            var rightCol = GetColumn("Right", 1);

            SetRow("Top", 1, leftCol.AsEnumerable().Reverse().ToList());
            SetColumn("Left", 1, bottomRow);
            SetRow("Bottom", 1, rightCol.AsEnumerable().Reverse().ToList());
            SetColumn("Right", 1, topRow);
        }
    }
}
