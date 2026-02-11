using System.Text;

namespace Lecture6
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("== Minesweeper ==");
            Console.OutputEncoding = Encoding.UTF8;
            Game game = new Game();
        }
    }

    class Game
    {
        const int mineCount = 20;
        const int gameSize = 10;

        int[,] game = new int[gameSize, gameSize];
        int[,] visible = new int[gameSize, gameSize];
        int cursorX = 0;
        int cursorY = 0;
        (int, int) consoleCursorPos;

        int markedMines = 0;

        bool gameGenerated = false;

        ConsoleColor[] colors = new ConsoleColor[]
        {
            ConsoleColor.DarkGray, ConsoleColor.Blue, ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.DarkYellow, ConsoleColor.Cyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkBlue, ConsoleColor.DarkGreen
        };

        public Game()
        {
            Console.WriteLine(@"
Welcome to Minesweeper!

- use arrows to move the cursor (shown with parenthesis)
- press Enter to dig the land at cursor position
- press F to mark a mine

Watch out and don't dig the mine!
");

            Console.WriteLine($"Mines marked {markedMines} / {mineCount}");
            consoleCursorPos = Console.GetCursorPosition();

            Console.CursorVisible = false;

            for (int i = 0; i < gameSize; i++)
            {
                for (int j = 0; j < gameSize; j++)
                {
                    game[i, j] = 0;
                    SetVisible(i, j, 0);
                }
            }
            GameLoop();
        }

        public void GameLoop()
        {
            while (true)
            {
                HandleKeystroke();
            }
        }

        public void SetVisible(int x, int y, int val)
        {
            visible[x, y] = val;

            if (val != 1)
            {
                Console.SetCursorPosition(consoleCursorPos.Item1, consoleCursorPos.Item2 - 1);

                Console.WriteLine($"Mines marked {markedMines} / {mineCount}  ");
            }

            bool isCursorHere = cursorX == x && cursorY == y;

            Console.SetCursorPosition((consoleCursorPos.Item1 + y) * 5, consoleCursorPos.Item2 + x);

            if (game[x, y] == -1 && visible[x, y] == 1)
            {
                //Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.Red;

                Console.Write("  *  ");
            }
            else if (visible[x, y] == 1)
            {
                Console.BackgroundColor = colors[game[x, y]];
                Console.ForegroundColor = ConsoleColor.White;

                if (game[x, y] == 0)
                {
                    Console.Write($"{(isCursorHere ? '(' : ' ')}   {(isCursorHere ? ')' : ' ')}");
                } else
                {
                    Console.Write($"{(isCursorHere ? '(' : ' ')} {game[x, y]} {(isCursorHere ? ')' : ' ')}");
                }
            }
            else if (visible[x, y] == 2)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Magenta;
                Console.Write($"{(isCursorHere ? '(' : ' ')} ⚐ {(isCursorHere ? ')' : ' ')}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{(isCursorHere ? '(' : ' ')} - {(isCursorHere ? ')' : ' ')}");
            }

            Console.ResetColor();
        }

        public void InitialiseGame(int preventX, int preventY)
        {
            gameGenerated = true;
            for (int i = 0; i < mineCount; i++)
            {
                int x = Random.Shared.Next(gameSize);
                int y = Random.Shared.Next(gameSize);

                if ((x <= preventX + 1 && x >= preventX - 1 && y <= preventY + 1 && y >= preventY -1) || game[x, y] == -1)
                {
                    i--;
                    continue;
                }

                game[x, y] = -1;
            }

            for (int i = 0; i < gameSize; i++)
            {
                for (int j = 0; j < gameSize; j++)
                {
                    if (game[i, j] != -1)
                    {
                        var neighbours = NeigboursOf(i, j);

                        int numOfMines = neighbours.Where((x) => game[x.Item1, x.Item2] == -1).Count();

                        game[i, j] = numOfMines;
                    }
                }
            }
        }

        public List<(int, int)> NeigboursOf(int x, int y)
        {
            List<(int, int)> neigbours = new List<(int, int)>();

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (IsBetween(i+x, 0, gameSize - 1) && IsBetween(j+y, 0, gameSize - 1))
                    {
                        neigbours.Add((i+x, j+y));
                    }
                }
            }

            return neigbours;
        }

        public static bool IsBetween(int value, int min, int max)
        {
            return value <= max && value >= min;
        }

        private void UncoverMine(int x, int y, bool bypass = false)
        {
            if (visible[x, y] != 0 && !bypass) return;

            if (!gameGenerated)
            {
                InitialiseGame(x, y);
            }

            if (game[x, y] != -1)
            {
                SetVisible(x, y, 1);
            } else
            {
                EndGame();
            }

            if (game[x, y] == 0 || (game[x, y] == NeigboursOf(x, y).Count(g => visible[g.Item1, g.Item2] == 2) && bypass))
            {
                foreach (var neighbour in NeigboursOf(x, y))
                {
                    UncoverMine(neighbour.Item1, neighbour.Item2, false);
                }
            }
        }

        private void FlagMine(int x, int y)
        {
            if (visible[x, y] == 1) return;

            if (visible[x, y] == 2)
            {
                SetVisible(x, y, 0);
            } else
            {
                SetVisible(x, y, 2);
            }

            if (CheckWinCondition())
            {
                WinGame();
            }
        }

        private void UpdateCursor(int oldX, int oldY)
        {
            SetVisible(cursorX, cursorY, visible[cursorX, cursorY]);

            SetVisible(oldX, oldY, visible[oldX, oldY]);
        }

        public void EndGame()
        {
            for (int i = 0; i < gameSize; i++)
            {
                for (int j = 0; j < gameSize; j++)
                {
                    SetVisible(i, j, 1);
                }
            }
            Console.WriteLine("\n\nOh no! You have lost!");

            Console.WriteLine("Press Esc to exit");

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                continue;
            }
            Environment.Exit(0);
        }

        public void WinGame()
        {
            for (int i = 0; i < gameSize; i++)
            {
                for (int j = 0; j < gameSize; j++)
                {
                    if (visible[i, j] != 2)
                    {
                        SetVisible(i, j, 1);
                    }
                }
            }

            Console.WriteLine("\n\nCongratulations, you have won the game!");
            Console.WriteLine("Press Esc to exit");

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                continue;
            }
            Environment.Exit(0);
        }

        private bool CheckWinCondition()
        {
            if (!gameGenerated)
            {
                return false;
            }

            int minesChecked = 0;
            int minesFlagged = 0;
 
            for (int i = 0; i < gameSize; i++)
            {
                for (int j = 0; j < gameSize; j++)
                {
                    if (visible[i, j] == 2) {
                        minesFlagged++;

                        if (game[i, j] != -1)
                        {
                            continue;
                        }

                        minesChecked++;
                    }
                }
            }

            markedMines = minesFlagged;

            return minesChecked == mineCount;
        }

        public void HandleKeystroke()
        {
            var key = Console.ReadKey(true);

            int oldX = cursorX;
            int oldY = cursorY;

            switch (key.Key)
            {
                case ConsoleKey.DownArrow:
                    cursorX = Math.Clamp(cursorX + 1, 0, gameSize - 1);
                    break; 
                case ConsoleKey.UpArrow:
                    cursorX = Math.Clamp(cursorX - 1, 0, gameSize - 1);
                    break;
                case ConsoleKey.LeftArrow:
                    cursorY = Math.Clamp(cursorY - 1, 0, gameSize - 1);
                    break;
                case ConsoleKey.RightArrow:
                    cursorY = Math.Clamp(cursorY + 1, 0, gameSize - 1);
                    break;
                case ConsoleKey.Enter:
                    UncoverMine(cursorX, cursorY, bypass: true);
                    break;
                case ConsoleKey.Escape:
                    EndGame();
                    break;
                case ConsoleKey.F:
                    FlagMine(cursorX, cursorY);
                    break;
            }

            UpdateCursor(oldX, oldY);
        }
    }
}