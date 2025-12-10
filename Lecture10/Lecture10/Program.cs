// tym: Sam Kouklik, Adam Svoboda a Ondrej Stindl

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Lecture10
{
    class Program
    {
        public static void Main(string[] args)
        {
            TerminalUI.PrintLogo();
            string path = TerminalUI.GetFilePrompt();

            Twitter twitter = new Twitter();

            if (!string.IsNullOrEmpty(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string json = sr.ReadToEnd();
                    twitter.LoadData(json);
                    twitter.filePath = path;
                }
            }

            TerminalUI.ClearAndLogo();

            TerminalUI.PrintPost(new Post { Author = "libertas", Content = "asdgajlhdbqwuihbudlihasbluidhasl" });
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
            index = index == -1 ? Array.FindIndex(array, v => string.IsNullOrEmpty(v?.ToString())) : index;

            if (array[index] == null) 
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
            int index = Array.FindIndex(array, predicate);

            return RemoveValue(ref array, index);
        }

        private bool Exists<T>(ref T?[] array, Func<T?, bool> predicate) where T : WithId
        {
            return array.Any(predicate);
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

        public Post?[] GetUserPosts(string user)
        {
            return Array.FindAll(posts, p => p != null && p.Author == user);
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
            if (!Exists(ref follows, (f) => f != null && f.Followee == followee && f.Follower == follower));
            {
                PrintErrorMessage("Follow does not exist.");
                return;
            }
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
        public WithId?[] GetUserTimeline(string user)
        {
            // TODO
            return new WithId[] { };
        }

        public Post?[] GetRecentPosts(int number)
        {
            return [.. posts.TakeLast(number)];
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(new JSON { Follows = follows, Posts = posts, Users = users });
        }

        public void LoadData(string json)
        {
            var doc = JsonSerializer.Deserialize(json, JsonTypeInfo.CreateJsonTypeInfo<JSON>(JsonSerializerOptions.Default));

            if (doc == null)
            {
                PrintErrorMessage("Could not parse JSON.");
                return;
            }

            posts = doc.Posts;
            users = doc.Users;
            follows = doc.Follows;
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
            required public Post[] Posts;
            required public User[] Users;
            required public Follow[] Follows;
        }
    }

    public abstract record class WithId
    {
        public int Id;
    }

    public record class Post : WithId
    {
        public string Content = "";
        public string Author = "";

        public override string ToString()
        {
            return Content;
        }
    }

    public record class User : WithId
    {
        public string Name = "";

        public override string ToString()
        {
            return Name;
        }
    }

    public record class Follow : WithId
    {
        public string Follower = "";
        public string Followee = "";

        public override string ToString()
        {
            return Follower;
        }
    }
}