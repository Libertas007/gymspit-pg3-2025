namespace Lecture7
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("== Minesweeper ==");
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

        bool gameGenerated = false;

        ConsoleColor[] colors = new ConsoleColor[]
        {
            ConsoleColor.Blue, ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.White
        };

        public Game()
        {
            for (int i = 0; i < gameSize; i++)
            {
                for (int j = 0; j < gameSize; j++)
                {
                    game[i, j] = 0;
                    visible[i, j] = 0;
                }
            }
            consoleCursorPos = Console.GetCursorPosition();
            GameLoop();
        }

        public void GameLoop()
        {
            while (true)
            {
                PrintGame();
                HandleKeystroke();
            }
        }

        public void PrintGame()
        {
            Console.SetCursorPosition(consoleCursorPos.Item1, consoleCursorPos.Item2);

            for (int i = 0; i < gameSize; i++)
            {
                for (int j = 0; j < gameSize; j++)
                {
                    if (cursorX == i && cursorY == j)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }

                    if (game[i, j] == -1 && visible[i, j] == 1)
                    {
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.White;

                        Console.Write("[ B ]");
                    } else if (visible[i, j] == 1)
                    {
                        Console.ForegroundColor = colors[game[i, j]];
                        Console.Write($"( {game[i, j]} )");
                    }
                    else if (visible[i, j] == 2)
                    {
                        Console.Write("[ F ]");
                    }
                    else
                    {
                        Console.Write("[ - ]");
                    }

                    Console.ResetColor();
                }
                Console.WriteLine();
            }
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

        private void UncoverMine(int x, int y)
        {
            if (visible[x, y] != 0) return;

            if (!gameGenerated)
            {
                InitialiseGame(x, y);
            }

            if (game[x, y] != -1)
            {
                visible[x, y] = 1;
            } else
            {
                EndGame();
            }

            if (game[x, y] == 0)
            {
                foreach (var neighbour in NeigboursOf(x, y))
                {
                    UncoverMine(neighbour.Item1, neighbour.Item2);
                }
            }
        }

        private void FlagMine(int x, int y)
        {
            if (visible[x, y] == 1) return;

            visible[x, y] = 2;
        }

        public void EndGame()
        {
            for (int i = 0; i < gameSize; i++)
            {
                for (int j = 0; j < gameSize; j++)
                {
                    visible[i, j] = 1;
                }
            }
            PrintGame();
            Console.WriteLine("You lost");
            Environment.Exit(0);
        }

        public void HandleKeystroke()
        {
            var key = Console.ReadKey(false);

            switch (key.Key)
            {
                case ConsoleKey.DownArrow:
                    cursorX = Math.Clamp(cursorX + 1, 0, gameSize);
                    break; 
                case ConsoleKey.UpArrow:
                    cursorX = Math.Clamp(cursorX - 1, 0, gameSize);
                    break;
                case ConsoleKey.LeftArrow:
                    cursorY = Math.Clamp(cursorY - 1, 0, gameSize);
                    break;
                case ConsoleKey.RightArrow:
                    cursorY = Math.Clamp(cursorY + 1, 0, gameSize);
                    break;
                case ConsoleKey.Enter:
                    UncoverMine(cursorX, cursorY);
                    break;
                case ConsoleKey.Escape:
                    EndGame();
                    break;
                case ConsoleKey.F:
                    FlagMine(cursorX, cursorY);
                    break;
            }
        }
    }
}