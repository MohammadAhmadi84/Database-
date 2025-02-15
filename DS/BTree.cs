namespace DataBase.Ds
{
    public class BTree<K, V> where K : IComparable<K>
    {
        public class BTreeNode
        {
            public List<K> Keys { get; set; }
            public List<V> Values { get; set; }
            public List<BTreeNode> Children { get; set; }
            public bool IsLeaf { get; set; }

            public BTreeNode(bool isLeaf)
            {
                Keys = new List<K>();
                Values = new List<V>();
                Children = new List<BTreeNode>();
                IsLeaf = isLeaf;
            }
        }

        private BTreeNode root;
        private int degree;

        public BTree(int degree)
        {
            this.degree = degree;
            root = new BTreeNode(true);
        }

        public void Insert(K key, V value)
        {
            if (root.Keys.Count == 2 * degree - 1)
            {
                BTreeNode newRoot = new BTreeNode(false);
                newRoot.Children.Add(root);
                SplitChild(newRoot, 0, root);
                root = newRoot;
            }

            InsertNonFull(root, key, value);
        }

        private void InsertNonFull(BTreeNode node, K key, V value)
        {
            int i = node.Keys.Count - 1;
            if (node.IsLeaf)
            {
                while (i >= 0 && key.CompareTo(node.Keys[i]) < 0)
                {
                    i--;
                }

                node.Keys.Insert(i + 1, key);
                node.Values.Insert(i + 1, value);
            }
            else
            {
                while (i >= 0 && key.CompareTo(node.Keys[i]) < 0)
                {
                    i--;
                }

                i++;
                if (node.Children[i].Keys.Count == 2 * degree - 1)
                {
                    SplitChild(node, i, node.Children[i]);
                    if (key.CompareTo(node.Keys[i]) > 0)
                    {
                        i++;
                    }
                }

                InsertNonFull(node.Children[i], key, value);
            }
        }

        private void SplitChild(BTreeNode parent, int i, BTreeNode child)
        {
            BTreeNode newNode = new BTreeNode(child.IsLeaf);

            for (int j = 0; j < degree - 1; j++)
            {
                newNode.Keys.Add(child.Keys[degree]);
                newNode.Values.Add(child.Values[degree]);
                child.Keys.RemoveAt(degree);
                child.Values.RemoveAt(degree);
            }

            if (!child.IsLeaf)
            {
                for (int j = 0; j < degree; j++)
                {
                    newNode.Children.Add(child.Children[degree]);
                    child.Children.RemoveAt(degree);
                }
            }

            parent.Children.Insert(i + 1, newNode);
            parent.Keys.Insert(i, child.Keys[degree - 1]);
            parent.Values.Insert(i, child.Values[degree - 1]);
            child.Keys.RemoveAt(degree - 1);
            child.Values.RemoveAt(degree - 1);
        }

        public V Search(K key)
        {
            V result = Search(root, key);
            if (EqualityComparer<V>.Default.Equals(result, default(V)))
            {
                return default;
            }
            return result;
        }

        private V Search(BTreeNode node, K key)
        {
            int i = 0;
            while (i < node.Keys.Count && key.CompareTo(node.Keys[i]) > 0)
            {
                i++;
            }

            if (i < node.Keys.Count && key.CompareTo(node.Keys[i]) == 0)
            {
                return node.Values[i];
            }

            if (node.IsLeaf)
            {
                return default;
            }

            return Search(node.Children[i], key);
        }

        public bool Delete(K key)
        {
            return Delete(root, key);
        }

        private bool Delete(BTreeNode node, K key)
        {
            int i = 0;
            while (i < node.Keys.Count && key.CompareTo(node.Keys[i]) > 0)
            {
                i++;
            }

            if (i < node.Keys.Count && key.CompareTo(node.Keys[i]) == 0)
            {
                if (node.IsLeaf)
                {
                    node.Keys.RemoveAt(i);
                    node.Values.RemoveAt(i);
                    return true;
                }
                else
                {
                    if (node.Children[i].Keys.Count >= degree)
                    {
                        KeyValuePair<K, V> pred = GetPredecessor(node.Children[i]);
                        node.Keys[i] = pred.Key;
                        node.Values[i] = pred.Value;
                        return Delete(node.Children[i], pred.Key);
                    }
                    else if (node.Children[i + 1].Keys.Count >= degree)
                    {
                        KeyValuePair<K, V> succ = GetSuccessor(node.Children[i + 1]);
                        node.Keys[i] = succ.Key;
                        node.Values[i] = succ.Value;
                        return Delete(node.Children[i + 1], succ.Key);
                    }
                    else
                    {
                        MergeChildren(node, i);
                        return Delete(node.Children[i], key);
                    }
                }
            }

            if (node.IsLeaf)
            {
                return false;
            }

            return Delete(node.Children[i], key);
        }

        private KeyValuePair<K, V> GetPredecessor(BTreeNode node)
        {
            while (!node.IsLeaf)
            {
                node = node.Children[node.Children.Count - 1];
            }
            return new KeyValuePair<K, V>(node.Keys[node.Keys.Count - 1], node.Values[node.Values.Count - 1]);
        }

        private KeyValuePair<K, V> GetSuccessor(BTreeNode node)
        {
            while (!node.IsLeaf)
            {
                node = node.Children[0];
            }
            return new KeyValuePair<K, V>(node.Keys[0], node.Values[0]);
        }

        private void MergeChildren(BTreeNode parent, int i)
        {
            BTreeNode child = parent.Children[i];
            BTreeNode sibling = parent.Children[i + 1];

            child.Keys.Add(parent.Keys[i]);
            child.Values.Add(parent.Values[i]);

            child.Keys.AddRange(sibling.Keys);
            child.Values.AddRange(sibling.Values);

            if (!child.IsLeaf)
            {
                child.Children.AddRange(sibling.Children);
            }

            parent.Keys.RemoveAt(i);
            parent.Values.RemoveAt(i);
            parent.Children.RemoveAt(i + 1);
        }

        public List<KeyValuePair<K, V>> TraverseAsKeyValuePairs()
        {
            List<KeyValuePair<K, V>> result = new List<KeyValuePair<K, V>>();
            Traverse(root, result);
            return result;
        }

        private void Traverse(BTreeNode node, List<KeyValuePair<K, V>> list)
        {
            if (node == null) return;

            for (int i = 0; i < node.Keys.Count; i++)
            {
                if (node.Children.Count > i)
                {
                    Traverse(node.Children[i], list);
                }
                list.Add(new KeyValuePair<K, V>(node.Keys[i], node.Values[i]));
            }

            if (node.Children.Count > node.Keys.Count)
            {
                Traverse(node.Children[node.Keys.Count], list);
            }
        }
        public void Update(K key, V newValue)
        {
            if (!Update(root, key, newValue))
            {
                throw new Exception("Key not found");
            }
        }

        private bool Update(BTreeNode node, K key, V newValue)
        {
            int i = 0;
            while (i < node.Keys.Count && key.CompareTo(node.Keys[i]) > 0)
            {
                i++;
            }

            if (i < node.Keys.Count && key.CompareTo(node.Keys[i]) == 0)
            {
                node.Values[i] = newValue; 
                return true;
            }

            if (node.IsLeaf)
            {
                return false; 
            }

            return Update(node.Children[i], key, newValue);
        }
        public List<KeyValuePair<K, V>> ExportData()
        {
            List<KeyValuePair<K, V>> dataList = new List<KeyValuePair<K, V>>();
            TraverseAndCollect(root, dataList);
            return dataList;
        }

        private void TraverseAndCollect(BTreeNode node, List<KeyValuePair<K, V>> dataList)
        {
            if (node == null)
                return;

            for (int i = 0; i < node.Keys.Count; i++)
            {
                if (!node.IsLeaf)
                {
                    TraverseAndCollect(node.Children[i], dataList);
                }
                dataList.Add(new KeyValuePair<K, V>(node.Keys[i], node.Values[i]));
            }

            if (!node.IsLeaf)
            {
                TraverseAndCollect(node.Children[node.Keys.Count], dataList);
            }
        }
    }
    
}
