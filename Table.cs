
using DataBase.Ds;
using Newtonsoft.Json;

public class Table<K, V> where K : IComparable<K>
{
    public string Name { get; private set; }
    public BTree<K, V> DataTable { get; set; }

    // فیلد جدید برای ذخیره داده‌ها هنگام سریالایز شدن
    public List<KeyValuePair<K, V>> SerializedData { get; private set; }

    public Table(string name)
    {
        Name = name;
        DataTable = new BTree<K, V>(3);
        SerializedData = new List<KeyValuePair<K, V>>();
    }

    public void Insert(K key, V value)
    {
        if (DataTable.Search(key) != null)
        {
            throw new Exception("This Data has already been added");
        }
        DataTable.Insert(key, value);
        SerializedData.Add(new KeyValuePair<K, V>(key, value)); // ذخیره برای سریالایز شدن
    }

    public void Update(K key, V value)
    {
        if (DataTable.Search(key) == null)
        {
            throw new Exception("Not found");
        }
        DataTable.Update(key, value);
        var index = SerializedData.FindIndex(kv => kv.Key.Equals(key));
        if (index != -1)
        {
            SerializedData[index] = new KeyValuePair<K, V>(key, value);
        }
    }

    public KeyValuePair<K, V> Get(K key)
    {
        V v = DataTable.Search(key);
        if (v == null)
        {
            throw new Exception("Not found");
        }
        return new KeyValuePair<K, V>(key, v);
    }

    public List<KeyValuePair<K,V>> GetAllTableData()
    {
        return DataTable.TraverseAsKeyValuePairs();
    }

    public void Delete(K key)
    {
        bool deleted = DataTable.Delete(key);
        if (!deleted)
        {
            throw new Exception("Data not found");
        }
        SerializedData.RemoveAll(kv => kv.Key.Equals(key)); // حذف از لیست سریالایز شده
    }
    public void Deserialize(string json)
    {
        var tempTable = JsonConvert.DeserializeObject<Table<K, V>>(json);
        if (tempTable != null)
        {
            Name = tempTable.Name;
            DataTable = tempTable.DataTable;  // حالا درخت هم مقداردهی می‌شود!
        }
    }
    public void LoadData(List<KeyValuePair<K, V>> data)
    {
        foreach (var item in data)
        {
            DataTable.Insert(item.Key, item.Value);
        }
    }
    public List<KeyValuePair<K, V>> ExportData()
    {
        return GetAllTableData();
    }
}