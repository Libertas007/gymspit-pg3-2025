// Terminal UI z velke casti udelal Claude Sonnet 4.5

namespace Lecture10
{
    public class TerminalUI
    {
        public static void PrintLogo()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("   __           _ __  __           \r\n  / /__      __(_) /_/ /____  _____\r\n / __/ | /| / / / __/ __/ _ \\/ ___/\r\n/ /_ | |/ |/ / / /_/ /_/  __/ /    \r\n\\__/ |__/|__/_/\\__/\\__/\\___/_/     \r\n                                   ");
            Console.WriteLine();
            Console.ResetColor();
        }

        public static string GetFilePrompt()
        {
            Console.WriteLine("Enter the file name of the instance, or write 'temp' for a temporary instance.");
            Console.Write("> ");
            string path = (Console.ReadLine() ?? "").Trim();

            return path == "temp" ? "" : path;
        }

        public static void ClearAndLogo()
        {
            Console.Clear();
            PrintLogo();
        }

        public static void PrintPost(Post post, bool isSelected = false, bool showId = false)
        {
            int width = Math.Min(Console.WindowWidth - 4, 80);
            
            if (isSelected)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  ╔{new string('═', width)}╗");
            
            Console.Write("  ║ ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("@");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(post.Author);
            
            if (showId)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($" (ID: {post.Id})");
                int spacesNeeded = width - 3 - post.Author.Length - $" (ID: {post.Id})".Length;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{new string(' ', Math.Max(0, spacesNeeded))} ║");
            }
            else
            {
                int spacesNeeded = width - 2 - post.Author.Length;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{new string(' ', Math.Max(0, spacesNeeded))} ║");
            }
            
            Console.WriteLine($"  ╟{new string('─', width)}╢");
            
            // Word wrap the content
            var lines = WrapText(post.Content, width - 2);
            Console.ForegroundColor = ConsoleColor.White;
            foreach (var line in lines)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("  ║ ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(line);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{new string(' ', width - 1 - line.Length)}║");
            }
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  ╚{new string('═', width)}╝");
            
            Console.ResetColor();
        }

        public static void PrintUser(User user, bool isSelected = false, int followersCount = 0, int followingCount = 0, int postsCount = 0)
        {
            int width = Math.Min(Console.WindowWidth - 4, 60);
            
            if (isSelected)
            {
                Console.BackgroundColor = ConsoleColor.DarkGreen;
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  ┌{new string('─', width)}┐");
            
            Console.Write("  │ ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("👤 @");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{user.Name}");
            
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($" (ID: {user.Id})");
            int spacesNeeded = width - 7 - user.Name.Length - $" (ID: {user.Id})".Length;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{new string(' ', Math.Max(0, spacesNeeded))}  │");
            
            Console.WriteLine($"  ├{new string('─', width)}┤");
            
            Console.Write("  │ ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"📝 Posts: {postsCount}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("  ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"Following: {followingCount}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("  ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"Followers: {followersCount}");
            
            int statsLength = $"📝 Posts: {postsCount}  Following: {followingCount}  Followers: {followersCount}".Length;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{new string(' ', Math.Max(0, width - 1 - statsLength))}│");
            
            Console.WriteLine($"  └{new string('─', width)}┘");
            
            Console.ResetColor();
        }

        public static int ShowMenu(string title, string[] options, int currentSelection = 0)
        {
            Console.CursorVisible = false;
            int selection = currentSelection;

            while (true)
            {
                ClearAndLogo();
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n╔══ {title} ══╗\n");
                Console.ResetColor();

                for (int i = 0; i < options.Length; i++)
                {
                    if (i == selection)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkCyan;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"  ▶ {options[i]}  ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"    {options[i]}");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Use ↑/↓ arrows to navigate, Enter to select, Esc to go back");
                Console.ResetColor();

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selection = (selection - 1 + options.Length) % options.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        selection = (selection + 1) % options.Length;
                        break;
                    case ConsoleKey.Enter:
                        Console.CursorVisible = true;
                        return selection;
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        return -1;
                }
            }
        }

        public static int ShowPostList(string title, Post[] posts, int currentSelection = 0)
        {
            Console.CursorVisible = false;
            int selection = currentSelection;
            int scrollOffset = 0;
            int maxVisible = 5;

            while (true)
            {
                ClearAndLogo();
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n╔══ {title} ══╗");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Showing {posts.Length} post(s)\n");
                Console.ResetColor();

                if (posts.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  No posts to display.");
                    Console.ResetColor();
                }
                else
                {
                    // Adjust scroll offset
                    if (selection < scrollOffset)
                        scrollOffset = selection;
                    if (selection >= scrollOffset + maxVisible)
                        scrollOffset = selection - maxVisible + 1;

                    int endIndex = Math.Min(scrollOffset + maxVisible, posts.Length);
                    
                    for (int i = scrollOffset; i < endIndex; i++)
                    {
                        PrintPost(posts[i], i == selection, true);
                        Console.WriteLine();
                    }

                    if (scrollOffset > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("  ↑ More posts above...");
                    }
                    if (endIndex < posts.Length)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("  ↓ More posts below...");
                    }
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Use ↑/↓ arrows to navigate, Enter to select, Esc to go back");
                Console.ResetColor();

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (posts.Length > 0)
                            selection = (selection - 1 + posts.Length) % posts.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        if (posts.Length > 0)
                            selection = (selection + 1) % posts.Length;
                        break;
                    case ConsoleKey.Enter:
                        Console.CursorVisible = true;
                        return posts.Length > 0 ? selection : -1;
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        return -1;
                }
            }
        }

        public static int ShowUserList(string title, User[] users, Dictionary<int, (int posts, int following, int followers)>? userStats = null)
        {
            Console.CursorVisible = false;
            int selection = 0;
            int scrollOffset = 0;
            int maxVisible = 5;

            while (true)
            {
                ClearAndLogo();
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n╔══ {title} ══╗");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Showing {users.Length} user(s)\n");
                Console.ResetColor();

                if (users.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  No users to display.");
                    Console.ResetColor();
                }
                else
                {
                    // Adjust scroll offset
                    if (selection < scrollOffset)
                        scrollOffset = selection;
                    if (selection >= scrollOffset + maxVisible)
                        scrollOffset = selection - maxVisible + 1;

                    int endIndex = Math.Min(scrollOffset + maxVisible, users.Length);
                    
                    for (int i = scrollOffset; i < endIndex; i++)
                    {
                        var stats = userStats?.GetValueOrDefault(users[i].Id, (0, 0, 0)) ?? (0, 0, 0);
                        PrintUser(users[i], i == selection, stats.Item3, stats.Item2, stats.Item1);
                        Console.WriteLine();
                    }

                    if (scrollOffset > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("  ↑ More users above...");
                    }
                    if (endIndex < users.Length)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("  ↓ More users below...");
                    }
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Use ↑/↓ arrows to navigate, Enter to select, Esc to go back");
                Console.ResetColor();

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (users.Length > 0)
                            selection = (selection - 1 + users.Length) % users.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        if (users.Length > 0)
                            selection = (selection + 1) % users.Length;
                        break;
                    case ConsoleKey.Enter:
                        Console.CursorVisible = true;
                        return users.Length > 0 ? selection : -1;
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        return -1;
                }
            }
        }

        public static string GetTextInput(string prompt, string defaultValue = "")
        {
            ClearAndLogo();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(prompt);
            Console.ResetColor();
            Console.Write("> ");
            Console.CursorVisible = true;
            
            string? input = Console.ReadLine();
            Console.CursorVisible = false;
            
            return string.IsNullOrWhiteSpace(input) ? defaultValue : input.Trim();
        }

        public static string GetMultilineTextInput(string prompt)
        {
            ClearAndLogo();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(prompt);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("(Press Enter twice to finish)");
            Console.ResetColor();
            Console.WriteLine();
            Console.CursorVisible = true;

            List<string> lines = new List<string>();
            string? line;
            
            while (true)
            {
                line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line) && lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
                {
                    lines.RemoveAt(lines.Count - 1); // Remove the last empty line
                    break;
                }
                lines.Add(line ?? "");
            }

            Console.CursorVisible = false;
            return string.Join(" ", lines).Trim();
        }

        public static string LoginPrompt()
        {
            Console.CursorVisible = true;
            Console.WriteLine("\nEnter your username:");
            Console.Write("> ");
            string username = (Console.ReadLine() ?? "").Trim();
            Console.CursorVisible = false;
            return username;
        }

        public static void ShowMessage(string message, bool isError = false)
        {
            Console.WriteLine();
            if (isError)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  ✖ ERROR: {message}  ");
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.DarkGreen;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  ✓ SUCCESS: {message}  ");
            }
            Console.ResetColor();
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(true);
        }

        public static bool Confirm(string message)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ {message}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Continue? (y/n): ");
            Console.ResetColor();
            
            var key = Console.ReadKey(true);
            Console.WriteLine(key.KeyChar);
            
            return key.Key == ConsoleKey.Y;
        }

        public static ProfileAction ShowInteractiveProfile(User user, Post[] userPosts, int followersCount, int followingCount, bool isCurrentUser, bool isFollowing)
        {
            Console.CursorVisible = false;
            int selection = 0;
            int postScrollOffset = 0;
            int maxVisiblePosts = 3;

            // Options: 0 = Follow/Unfollow button (or View Followers), 1 = View Following, 2+ = individual posts, last = Back
            int followButtonIndex = isCurrentUser ? -1 : 0;
            int viewFollowersIndex = isCurrentUser ? 0 : 1;
            int viewFollowingIndex = isCurrentUser ? 1 : 2;
            int firstPostIndex = isCurrentUser ? 2 : 3;
            int backButtonIndex = firstPostIndex + userPosts.Length;
            int totalOptions = backButtonIndex + 1;

            while (true)
            {
                ClearAndLogo();
                Console.WriteLine();
                
                // Print user info
                PrintUser(user, false, followersCount, followingCount, userPosts.Length);
                Console.WriteLine();

                // Follow/Unfollow button (only if not current user)
                if (!isCurrentUser)
                {
                    if (selection == 0)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkMagenta;
                        Console.ForegroundColor = ConsoleColor.White;
                        if (isFollowing)
                            Console.WriteLine($"  ▶ ➖ Unfollow @{user.Name}  ");
                        else
                            Console.WriteLine($"  ▶ ➕ Follow @{user.Name}  ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        if (isFollowing)
                            Console.WriteLine($"    ➖ Unfollow @{user.Name}");
                        else
                            Console.WriteLine($"    ➕ Follow @{user.Name}");
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }

                // View Followers button
                if (selection == viewFollowersIndex)
                {
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"  ▶ 👥 View Followers ({followersCount})  ");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"    👥 View Followers ({followersCount})");
                    Console.ResetColor();
                }

                // View Following button
                if (selection == viewFollowingIndex)
                {
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"  ▶ 🔗 View Following ({followingCount})  ");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"    🔗 View Following ({followingCount})");
                    Console.ResetColor();
                }

                Console.WriteLine();

                // Posts section
                if (userPosts.Length > 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Posts ({userPosts.Length}):");
                    Console.ResetColor();
                    Console.WriteLine();

                    // Calculate which posts to show based on selected post
                    if (selection >= firstPostIndex && selection < firstPostIndex + userPosts.Length)
                    {
                        int selectedPostIndex = selection - firstPostIndex;
                        if (selectedPostIndex < postScrollOffset)
                            postScrollOffset = selectedPostIndex;
                        if (selectedPostIndex >= postScrollOffset + maxVisiblePosts)
                            postScrollOffset = selectedPostIndex - maxVisiblePosts + 1;
                    }

                    int endIndex = Math.Min(postScrollOffset + maxVisiblePosts, userPosts.Length);
                    
                    for (int i = postScrollOffset; i < endIndex; i++)
                    {
                        bool isSelected = (selection == i + firstPostIndex);
                        PrintPost(userPosts[i], isSelected, true);
                        Console.WriteLine();
                    }

                    if (postScrollOffset > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("  ↑ More posts above...");
                        Console.ResetColor();
                    }
                    if (endIndex < userPosts.Length)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("  ↓ More posts below...");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  No posts yet.");
                    Console.ResetColor();
                }

                Console.WriteLine();

                // Back button
                if (selection == backButtonIndex)
                {
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("  ▶ ← Back  ");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("    ← Back");
                    Console.ResetColor();
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                if (isCurrentUser && selection >= firstPostIndex && selection < backButtonIndex)
                {
                    Console.WriteLine("Use ↑/↓ arrows to navigate, Enter to select, Delete to remove post, Esc to go back");
                }
                else
                {
                    Console.WriteLine("Use ↑/↓ arrows to navigate, Enter to select, Esc to go back");
                }
                Console.ResetColor();

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selection = (selection - 1 + totalOptions) % totalOptions;
                        break;
                    case ConsoleKey.DownArrow:
                        selection = (selection + 1) % totalOptions;
                        break;
                    case ConsoleKey.Delete:
                        if (isCurrentUser && selection >= firstPostIndex && selection < backButtonIndex)
                        {
                            Console.CursorVisible = true;
                            return new ProfileAction { Action = ProfileActionType.DeletePost, PostIndex = selection - firstPostIndex };
                        }
                        break;
                    case ConsoleKey.Enter:
                        Console.CursorVisible = true;
                        if (selection == followButtonIndex && !isCurrentUser)
                        {
                            return new ProfileAction { Action = isFollowing ? ProfileActionType.Unfollow : ProfileActionType.Follow };
                        }
                        else if (selection == viewFollowersIndex)
                        {
                            return new ProfileAction { Action = ProfileActionType.ViewFollowers };
                        }
                        else if (selection == viewFollowingIndex)
                        {
                            return new ProfileAction { Action = ProfileActionType.ViewFollowing };
                        }
                        else if (selection == backButtonIndex)
                        {
                            return new ProfileAction { Action = ProfileActionType.Back };
                        }
                        break;
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        return new ProfileAction { Action = ProfileActionType.Back };
                }
            }
        }

        public static int ShowFollowList(string title, string[] usernames, string listType)
        {
            Console.CursorVisible = false;
            int selection = 0;
            int scrollOffset = 0;
            int maxVisible = 8;

            while (true)
            {
                ClearAndLogo();
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n╔══ {title} ══╗");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Showing {usernames.Length} {listType}\n");
                Console.ResetColor();

                if (usernames.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  No {listType} yet.");
                    Console.ResetColor();
                }
                else
                {
                    // Adjust scroll offset
                    if (selection < scrollOffset)
                        scrollOffset = selection;
                    if (selection >= scrollOffset + maxVisible)
                        scrollOffset = selection - maxVisible + 1;

                    int endIndex = Math.Min(scrollOffset + maxVisible, usernames.Length);
                    
                    for (int i = scrollOffset; i < endIndex; i++)
                    {
                        if (i == selection)
                        {
                            Console.BackgroundColor = ConsoleColor.DarkCyan;
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"  ▶ @{usernames[i]}  ");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine($"    @{usernames[i]}");
                            Console.ResetColor();
                        }
                    }

                    if (scrollOffset > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("  ↑ More above...");
                    }
                    if (endIndex < usernames.Length)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("  ↓ More below...");
                    }
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Use ↑/↓ arrows to navigate, Enter to view profile, Esc to go back");
                Console.ResetColor();

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (usernames.Length > 0)
                            selection = (selection - 1 + usernames.Length) % usernames.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        if (usernames.Length > 0)
                            selection = (selection + 1) % usernames.Length;
                        break;
                    case ConsoleKey.Enter:
                        Console.CursorVisible = true;
                        return usernames.Length > 0 ? selection : -1;
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        return -1;
                }
            }
        }

        private static List<string> WrapText(string text, int maxWidth)
        {
            List<string> lines = new List<string>();
            
            if (string.IsNullOrEmpty(text))
            {
                lines.Add("");
                return lines;
            }

            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (string word in words)
            {
                if (currentLine.Length + word.Length + 1 <= maxWidth)
                {
                    if (currentLine.Length > 0)
                        currentLine += " ";
                    currentLine += word;
                }
                else
                {
                    if (currentLine.Length > 0)
                        lines.Add(currentLine);
                    currentLine = word;
                }
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine);

            return lines;
        }
    }

    public enum ProfileActionType
    {
        Back,
        Follow,
        Unfollow,
        ViewFollowers,
        ViewFollowing,
        DeletePost
    }

    public struct ProfileAction
    {
        public ProfileActionType Action;
        public int PostIndex;
    }
}
