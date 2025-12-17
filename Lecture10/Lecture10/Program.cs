// tym: Sam Kouklik, Adam Svoboda a Ondrej Stindl
// opet Claude Sonnet 4.5 udelal integraci s terminal UI
// uplne jsem zapomnel, jak je vibe coding super :)

using System.Text;
using System.Text.Json;

namespace Lecture10
{
    class Program
    {
        public static void Main(string[] args)
        {
            // Set console encoding to UTF-8 to support Unicode characters
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            TerminalUI.PrintLogo();
            string path = TerminalUI.GetFilePrompt();

            Twitter twitter = new Twitter();

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string json = sr.ReadToEnd();
                        twitter.LoadData(json);
                        twitter.filePath = path;
                    }
                }
                catch (Exception ex)
                {
                    TerminalUI.ShowMessage($"Could not load file: {ex.Message}", true);
                }
            }

            bool exit = false;

            while (!exit)
            {
                TerminalUI.ClearAndLogo();

                string currentUser = "";

                while (true)
                {
                    currentUser = TerminalUI.LoginPrompt();

                    if (string.IsNullOrWhiteSpace(currentUser))
                    {
                        TerminalUI.ShowMessage("Username cannot be empty!", true);
                        continue;
                    }
                    break;
                }

                if (currentUser == "exit")
                {
                    break;
                }

                // Create user if doesn't exist
                twitter.AddUser(currentUser);

                // Main menu loop
                bool running = true;
                while (running)
                {
                    string[] mainMenuOptions = new[]
                    {
                        "📝 Create a Post",
                        "🏠 View Feed (Recent Posts)",
                        "👤 View My Profile",
                        "👥 Browse Users",
                        "💾 Save to File",
                        "🚪 Exit"
                    };

                    int choice = TerminalUI.ShowMenu($"Welcome, @{currentUser}!", mainMenuOptions);

                    switch (choice)
                    {
                        case 0: // Create Post
                            CreatePost(ref twitter, currentUser);
                            break;
                        case 1: // View Feed
                            ViewFeed(ref twitter);
                            break;
                        case 2: // View Profile
                            ViewProfile(ref twitter, currentUser, currentUser);
                            break;
                        case 3: // Browse Users
                            BrowseUsers(ref twitter, currentUser);
                            break;
                        case 4: // Save
                            SaveToFile(ref twitter);
                            break;
                        case 5: // Exit
                        case -1: // Esc pressed
                            if (TerminalUI.Confirm("Are you sure you want to exit?"))
                            {
                                running = false;
                            }
                            break;
                    }
                }
            }

            TerminalUI.ClearAndLogo();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nThanks for using Twitter Clone! 👋");
            Console.ResetColor();
        }

        static void CreatePost(ref Twitter twitter, string currentUser)
        {
            string content = TerminalUI.GetMultilineTextInput("What's on your mind?");
            
            if (string.IsNullOrWhiteSpace(content))
            {
                TerminalUI.ShowMessage("Post content cannot be empty!", true);
                return;
            }

            twitter.AddPost(content, currentUser);
            TerminalUI.ShowMessage("Post created successfully!");
        }

        static void ViewFeed(ref Twitter twitter)
        {
            var recentPosts = twitter.GetRecentPosts(20).Where(p => p != null).ToArray()!;
            
            if (recentPosts.Length == 0)
            {
                TerminalUI.ShowMessage("No posts in the feed yet!", false);
                return;
            }

            TerminalUI.ShowPostList("Recent Feed", recentPosts);
        }

        static void ViewProfile(ref Twitter twitter, string username, string currentUser)
        {
            bool isCurrentUser = string.IsNullOrEmpty(currentUser) || username == currentUser;
            bool isFollowing = false;
            
            if (!isCurrentUser)
            {
                isFollowing = twitter.GetUserFollows(currentUser).Any(f => f != null && f.Followee == username);
            }

            while (true)
            {
                var userPosts = twitter.GetUserPosts(username).Where(p => p != null).OrderByDescending(p => p.Id).ToArray()!;
                var following = twitter.GetUserFollows(username);
                var followers = twitter.GetUserFollowers(username);

                // Find the user object
                var user = twitter.GetAllUsers().FirstOrDefault(u => u != null && u.Name == username);
                if (user == null)
                {
                    TerminalUI.ShowMessage($"User @{username} not found!", true);
                    return;
                }

                var action = TerminalUI.ShowInteractiveProfile(
                    user, 
                    userPosts, 
                    followers.Length, 
                    following.Length, 
                    isCurrentUser, 
                    isFollowing
                );

                switch (action.Action)
                {
                    case ProfileActionType.Follow:
                        twitter.AddFollow(currentUser, username);
                        isFollowing = true;
                        break;
                    case ProfileActionType.Unfollow:
                        twitter.RemoveFollow(currentUser, username);
                        isFollowing = false;
                        break;
                    case ProfileActionType.ViewFollowers:
                        ViewFollowersList(ref twitter, username, currentUser);
                        break;
                    case ProfileActionType.ViewFollowing:
                        ViewFollowingList(ref twitter, username, currentUser);
                        break;
                    case ProfileActionType.DeletePost:
                        if (isCurrentUser && action.PostIndex >= 0 && action.PostIndex < userPosts.Length)
                        {
                            var postToDelete = userPosts[action.PostIndex];
                            if (TerminalUI.Confirm($"Are you sure you want to delete this post?\n\"{postToDelete.Content}\""))
                            {
                                twitter.RemovePost(postToDelete.Id);
                                TerminalUI.ShowMessage("Post deleted successfully!");
                            }
                        }
                        break;
                    case ProfileActionType.Back:
                        return;
                }
            }
        }

        static void ViewFollowersList(ref Twitter twitter, string username, string currentUser)
        {
            var followers = twitter.GetUserFollowers(username).Where(f => f != null).Select(f => f!.Follower).ToArray();
            
            while (true)
            {
                int index = TerminalUI.ShowFollowList($"@{username}'s Followers", followers, "followers");
                
                if (index == -1)
                    return;
                
                ViewProfile(ref twitter, followers[index], currentUser);
            }
        }

        static void ViewFollowingList(ref Twitter twitter, string username, string currentUser)
        {
            var following = twitter.GetUserFollows(username).Where(f => f != null).Select(f => f!.Followee).ToArray();
            
            while (true)
            {
                int index = TerminalUI.ShowFollowList($"@{username} Following", following, "users");
                
                if (index == -1)
                    return;
                
                ViewProfile(ref twitter, following[index], currentUser);
            }
        }

        static void BrowseUsers(ref Twitter twitter, string currentUser)
        {
            var allUsers = twitter.GetAllUsers().Where(u => u != null).ToArray()!;
            
            if (allUsers.Length == 0)
            {
                TerminalUI.ShowMessage("No users found!", false);
                return;
            }

            var userStats = new Dictionary<int, (int posts, int following, int followers)>();
            foreach (var user in allUsers)
            {
                var posts = twitter.GetUserPosts(user.Name).Length;
                var following = twitter.GetUserFollows(user.Name).Length;
                var followers = twitter.GetUserFollowers(user.Name).Length;
                userStats[user.Id] = (posts, following, followers);
            }

            int index = TerminalUI.ShowUserList("All Users", allUsers, userStats);

            if (index != -1)
            {
                ViewProfile(ref twitter, allUsers[index].Name, currentUser);
            }
        }

        static void SaveToFile(ref Twitter twitter)
        {
            if (string.IsNullOrEmpty(twitter.filePath))
            {
                twitter.filePath = TerminalUI.GetTextInput("Enter filename to save:", "twitter_data.json");
            }

            try
            {
                string json = twitter.Serialize();
                File.WriteAllText(twitter.filePath, json);
                TerminalUI.ShowMessage($"Saved to {twitter.filePath}!");
            }
            catch (Exception ex)
            {
                TerminalUI.ShowMessage($"Failed to save: {ex.Message}", true);
            }
        }
    }

    class Twitter
    {
        public string filePath = "";

        const int MAX_USERS = 100;
        const int MAX_POSTS = MAX_USERS * 100;
        const int MAX_FOLLOWS = MAX_USERS * (MAX_USERS + 1) / 2;

        User?[] users = new User[MAX_USERS];
        Post?[] posts = new Post[MAX_POSTS];
        Follow?[] follows = new Follow[MAX_FOLLOWS];

        private bool AddValue<T>(ref T?[] array, T value, int index = -1) where T : WithId
        {
            index = index == -1 ? Array.FindIndex(array, v => v == null) : index;

            if (index < 0 || index >= array.Length || array[index] != null) 
            {
                return false;
            }

            value.Id = index;

            array[index] = value;
            return true;
        }

        private bool RemoveValue<T>(ref T?[] array, int index) where T: class
        {
            if (index < 0 || index >= array.Length)
            {
                return false;
            }

            array[index] = null;
            return true;
        }

        private bool RemoveWhere<T>(ref T?[] array, Predicate<T?> predicate) where T : class
        {
            int index = IndexOf(ref array, predicate);

            return RemoveValue(ref array, index);
        }

        private bool Exists<T>(ref T?[] array, Func<T?, bool> predicate) where T : WithId
        {
            return array.Any(predicate);
        }

        private int IndexOf<T>(ref T?[] array, Predicate<T?> predicate) where T : class
        {
            return Array.FindIndex(array, predicate);
        }

        public void AddUser(string username)
        {
            if (Exists(ref users, (u) => u != null && u.Name == username))
            {
                PrintErrorMessage("User already exists.");
                return;
            }
            
            User user = new User() { Name = username };

            if (!AddValue(ref users, user))
            {
                PrintErrorMessage("User array full.");
            }
        }

        public void RemoveUser(string username)
        {
            if (!Exists(ref users, (u) => u != null && u.Name == username))
            {
                PrintErrorMessage("User does not exist.");
                return;
            }

            
            if (RemoveWhere(ref users, (u) => u.Name == username))
            {
                PrintErrorMessage("Invalid index.");
            }
        }


        public void AddPost(string content, string author)
        {
            Post post = new Post() { Content = content, Author = author };

            AddValue(ref posts, post);
        }

        public void RemovePost(int postId)
        {
            if (postId < 0 || postId >= posts.Length || posts[postId] == null)
            {
                PrintErrorMessage("Post does not exist.");
                return;
            }

            posts[postId] = null;
        }

        public Post?[] GetUserPosts(string user)
        {
            return Array.FindAll(posts, p => p != null && p.Author == user);
        }

        public User?[] GetAllUsers()
        {
            return Array.FindAll(users, u => u != null);
        }

        public void AddFollow(string follower, string followee)
        {
            Follow follow = new Follow()
            {
                Follower = follower,
                Followee = followee,
            };

            if (follower == followee)
            {
                PrintErrorMessage("Cannot follow yourself.");
                return;
            }

            if (!Exists(ref users, (u) => u != null && (u.Name == follower || u.Name == followee))) {
                PrintErrorMessage("Invalid follower or followee.");
                return;
            }

            if (Exists(ref follows, (f) => f != null && f.Followee == followee && f.Follower == follower))
            {
                PrintErrorMessage("Follow already exists.");
                return;
            }

            AddValue(ref follows, follow);
        }

        public void RemoveFollow(string follower, string followee)
        {
            if (!Exists(ref follows, (f) => f != null && f.Followee == followee && f.Follower == follower))
            {
                PrintErrorMessage("Follow does not exist.");
                return;
            }

            RemoveWhere(ref follows, (f) => f != null && f.Followee == followee && f.Follower == follower);
        }

        public Follow?[] GetUserFollows(string user)
        {
            return [.. follows.Where((f) => f != null && f.Follower == user)];
        }

        public Follow?[] GetUserFollowers(string user)
        {
            return [.. follows.Where((f) => f != null && f.Followee == user)];
        }


        // Bonus
        public Post?[] GetUserTimeline(string user)
        {
            var followedUsers = GetUserFollows(user).Where(f => f != null).Select(f => f!.Followee).ToList();
            followedUsers.Add(user);
            
            return posts.Where(p => p != null && followedUsers.Contains(p.Author))
                       .OrderByDescending(p => p!.Id)
                       .ToArray();
        }

        public Post?[] GetRecentPosts(int number)
        {
            return posts.Where(p => p != null)
                       .OrderByDescending(p => p!.Id)
                       .Take(number)
                       .ToArray();
        }

        // Claude mi pomohl se zprovoznenim ukladani do souboru
        public string Serialize()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(new JSON { Follows = follows, Posts = posts, Users = users }, options);
        }

        public void LoadData(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var doc = JsonSerializer.Deserialize<JSON>(json, options);

            if (doc == null)
            {
                PrintErrorMessage("Could not parse JSON.");
                return;
            }

            posts = doc.Posts ?? new Post?[MAX_POSTS];
            users = doc.Users ?? new User?[MAX_USERS];
            follows = doc.Follows ?? new Follow?[MAX_FOLLOWS];
            
            // Ensure arrays are the correct size
            if (posts.Length != MAX_POSTS)
            {
                Array.Resize(ref posts, MAX_POSTS);
            }
            if (users.Length != MAX_USERS)
            {
                Array.Resize(ref users, MAX_USERS);
            }
            if (follows.Length != MAX_FOLLOWS)
            {
                Array.Resize(ref follows, MAX_FOLLOWS);
            }
        }

        private void PrintErrorMessage(string message)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"ERROR! {message}");
            Console.ResetColor();
        }

        private record class JSON
        {
            public Post?[] Posts { get; set; } = [];
            public User?[] Users { get; set; } = [];
            public Follow?[] Follows { get; set; } = [];
        }
    }

    public abstract record class WithId
    {
        public int Id { get; set; }
    }

    public record class Post : WithId
    {
        public string Content { get; set; } = "";
        public string Author { get; set; } = "";

        public override string ToString()
        {
            return Content;
        }
    }

    public record class User : WithId
    {
        public string Name { get; set; } = "";

        public override string ToString()
        {
            return Name;
        }
    }

    public record class Follow : WithId
    {
        public string Follower { get; set; } = "";
        public string Followee { get; set; } = "";

        public override string ToString()
        {
            return Follower;
        }
    }
}