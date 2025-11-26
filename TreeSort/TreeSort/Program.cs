namespace TreeSort
{
    class Program
    {
        public static void Main(string[] args)
        {
            TreeSort<int> sort = new TreeSort<int>([-222, 845, 9874, -1111111111, 0]);

            Console.WriteLine(string.Join(", ", sort.Get()));
        }
    }

    class TreeSort<T> where T: IComparable<T>, new()
    {
        private T[] numbers;
        private Node[] nodes;
        private List<T> list = new List<T>();

        public TreeSort(T[] numbers)
        {
            this.numbers = numbers;
            nodes = new Node[numbers.Length];

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new Node(new T());
            }


            for (int index = 0; index < numbers.Length; index++)
            {
                ProcessNumber(index);
            }
        }

        public T[] Get()
        {
            return TraverseTree();
        }

        private void ProcessNumber(int index)
        {
            T num = numbers[index];

            InsertNumber(0, -1, num, index);
        }

        private void InsertNumber(int index, int parent, T num, int numIndex)
        {
            if (index == -1)
            {
                index = numIndex;
            }

            Node node = nodes[index];

            if (!node.Occupied)
            {
                if (parent != -1)
                {
                    Node parentNode = nodes[parent];
                    if (num.CompareTo(parentNode.Value) == -1)
                    {
                        parentNode.Lower = index;
                    } else
                    {
                        parentNode.Higher = index;
                    }

                    nodes[parent] = parentNode;

                    node.Parent = parent;
                }

                node.Occupied = true;
                node.Value = num; 
                nodes[index] = node;
                return;
            }

            if (num.CompareTo(node.Value) == -1)
            {
                InsertNumber(node.Lower, index, num, numIndex);
            } else
            {
                InsertNumber(node.Higher, index, num, numIndex);
            }
        }

        private T[] TraverseTree()
        {
            list.Clear();
            VisitNode(0);

            return list.ToArray();
        }

        private void VisitNode(int index)
        {
            Node node = nodes[index];

            if (node.Occupied)
            {
                if (node.Lower != -1)
                {
                    VisitNode(node.Lower);
                }

                list.Add(node.Value);

                if (node.Higher != -1)
                {
                    VisitNode(node.Higher);
                }
            }
        }

        struct Node
        {
            public T Value;
            public int Lower = -1;
            public int Higher = -1;
            public int Parent = -1;
            public bool Occupied = false;

            public Node(T value) { Value = value; }

            override public String ToString()
            {
                return $"{Value}, P: {Parent}, L: {Lower}, H: {Higher}";
            }
        }
    }
}