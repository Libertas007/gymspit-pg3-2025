using System.Net;

namespace TreeSort
{
    class Program
    {
        public static void Main(string[] args)
        {
            TreeSort sort = new TreeSort([11, 25, 3, 84, 65]);

            Console.WriteLine(sort.Get());
        }
    }

    class TreeSort
    {
        private int[] numbers;
        private Node[] nodes;

        public TreeSort(int[] numbers)
        {
            this.numbers = numbers;
            nodes = new Node[numbers.Length];

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new Node(-1);
            }


            for (int index = 0; index < numbers.Length; index++)
            {
                ProcessNumber(index);
            }

            for (int index = 0; index < nodes.Length; index++)
            {
                Console.WriteLine(nodes[index].ToString());
            }
        }

        public int[] Get()
        {
            return TraverseTree();
        }

        private void ProcessNumber(int index)
        {
            int num = numbers[index];

            InsertNumber(0, -1, num);
        }

        private void InsertNumber(int index, int parent, int num)
        {
            Node node = nodes[index];

            Console.WriteLine($"{index}, {parent}, {num}");
            Console.WriteLine(node.ToString());

            if (!node.Occupied)
            {
                if (parent != -1)
                {
                    Node parentNode = nodes[parent];
                    if (parentNode.Value < num)
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

            if (num < node.Value)
            {
                InsertNumber(node.Lower, index, num);
            } else
            {
                InsertNumber(node.Higher, index, num);
            }
        }

        private int[] TraverseTree()
        {
            List<int> list = new List<int>();

            VisitNode(list, 0);

            return list.ToArray();
        }

        private void VisitNode(List<int> list, int index)
        {
            Node node = nodes[index];

            if (node.Occupied)
            {
                if (node.Lower > -1)
                {
                    VisitNode(list, node.Lower);
                }

                list.Add(node.Value);

                if (node.Higher > -1)
                {
                    VisitNode(list, node.Higher);
                }
            }
        }

        struct Node
        {
            public int Value;
            public int Lower = -1;
            public int Higher = -1;
            public int Parent = -1;
            public bool Occupied = false;

            public Node(int value) { Value = value; }

            override public String ToString()
            {
                return $"{Value}, P: {Parent}, L: {Lower}, H: {Higher}";
            }
        }
    }
}