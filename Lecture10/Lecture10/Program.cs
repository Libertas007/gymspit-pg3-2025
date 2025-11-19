// tym: Sam Kouklik a Adam Svoboda

namespace Lecture10
{
    class Program
    {
        public static void Main(string[] args)
        {
            Twitter twitter = new Twitter();
        }
    }

    class Twitter
    {
        const int MAX_USERS = 100;
        const int MAX_POSTS = MAX_USERS * 100;
        const int MAX_FOLLOWS = MAX_USERS * (MAX_USERS + 1) / 2;

        User[] users = new User[MAX_USERS];
        Post[] posts = new Post[MAX_POSTS];
        Follows[] follows = new Follows[MAX_FOLLOWS];

        private bool AddValue<T>(T value, ref T[] array, int index = -1) where T : WithId
        {
            index = index == -1 ? Array.FindIndex(array, v => string.IsNullOrEmpty(v.ToString())) : index;

            if (array[index] == null) 
            {
                Console.WriteLine("Index occupied.");
                return false;
            }

            value.Id = index;

            array[index] = value;
            return true;
        }

        private bool RemoveValue<T>(int index, ref T[] array) where T: new()
        {
            if (index < 0 || index >= array.Length)
            {
                Console.WriteLine("Invalid index.");
                return false;
            }

            array[index] = default(T);
            return true;
        }

        public void AddUser(string username)
        {
            int index = Array.IndexOf(users, username);
            if (index >= 0)
            {
                Console.WriteLine("User already exists.");
                return;
            }
            
            User user = new User() { Name = username };

            AddValue(user, ref users);
        }

        public void RemoveUser(string username)
        {
            int index = Array.IndexOf(users, username);
            if (index < 0)
            {
                Console.WriteLine("User does not exist.");
                return;
            }

            if (index >= 0) RemoveValue(index, ref users);
        }


        public void AddPost(string content, string author)
        {
            Post post = new Post() { Content = content, Author = author };

            AddValue(post, ref posts);
        }

        public Post[] GetUserPosts(string user)
        {
            return Array.FindAll(posts, p => p.Author == user);
        }


        public void AddFollow(string follower, string followee)
        {
            // TODO
        }

        public void RemoveFollow(string follower, string followee)
        {
            // TODO
        }

        public string[] GetUserFollows(string user)
        {
            // TODO
            return new string[] { };
        }

        string[] GetUserFollowers(string user)
        {
            // TODO
            return new string[] { };
        }


        // Bonus
        public string[] GetUserTimeline(string user)
        {
            // TODO
            return new string[] { };
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

        public record class Follows : WithId
        {
            public string Follower = "";
            public string Followee = "";

            public override string ToString()
            {
                return Follower;
            }
        }
    }
}