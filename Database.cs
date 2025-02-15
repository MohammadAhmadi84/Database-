using DataBase.Ds;
using Newtonsoft.Json;

namespace DataBase;

public class Database
{
    private static Database DataBase;
    private Dictionary<string, object>? Tables;
    private Dictionary<string, (string KeyType, string ValueType)> Schema = new Dictionary<string, (string, string)>();

    public static Database GetDatabase()
    {
        if (DataBase == null)
        {
            DataBase = new Database();
        }

        return DataBase;
    }

    private Database()
    {
        Tables = new Dictionary<string, object>();
    }

    public void Create<K, V>(string name) where K : IComparable<K>
    {
        ReadFile(); 

        if (Tables == null)
        {
            Tables = new Dictionary<string, object>();
        }

        if (Tables.ContainsKey(name.ToLower()))
        {
            throw new Exception("⚠ جدول از قبل وجود دارد!");
        }

        Tables.Add(name.ToLower(), new Table<K, V>(name.ToLower()));
        Schema[name.ToLower()] = (typeof(K).AssemblyQualifiedName, typeof(V).AssemblyQualifiedName);

        SaveFile();
    }

    public void Insert<K, V>(String name, K key, V value) where K : IComparable<K>
    {
        if (!Tables.ContainsKey(name.ToLower()))
        {
            throw new Exception("Not found");
        }

        if (Tables[name.ToLower()] is Table<K, V> table)
        {
            table.Insert(key, value);
            SaveFile();
        }
    }

    public void Update<K, V>(String name, K key, V value) where K : IComparable<K>
    {
        if (!Tables.ContainsKey(name))
        {
            throw new Exception("Not found");
        }

        if (Tables[name.ToLower()] is Table<K, V> table)
        {
            table.Update(key, value);
            SaveFile();
        }
    }

    public KeyValuePair<K, V> Get<K, V>(String name, K key) where K : IComparable<K>
    {
        KeyValuePair<K, V> keyValuePair = new KeyValuePair<K, V>();
        if (!Tables.ContainsKey(name.ToLower()))
        {
            throw new Exception("Table not found");
        }

        if (Tables[name.ToLower()] is Table<K, V> table)
        {
            return table.Get(key);
        }

        throw new Exception("K and V not match");
    }

    public void Delete<K, V>(String name, K k) where K : IComparable<K>
    {
        if (!Tables.ContainsKey(name))
        {
            throw new Exception("Table not found");
        }

        if (Tables[name] is Table<K, V> table)
        {
            table.Delete(k);
        }

        throw new Exception("Key Value not match");
    }

    public List<KeyValuePair<K, V>> GetTableData<K, V>(String name) where K : IComparable<K>
    {
        if (!Tables.ContainsKey(name))
        {
            throw new Exception("Table not found");
        }

        if (Tables[name] is Table<K, V> table)
        {
            return table.GetAllTableData();
        }

        throw new Exception("Key Value not match");
    }

    public void ReadFile()
{
    try
    {
        string filePath = "D:\\dot net\\Dotnet-B1\\DataBase\\DataBase.json";
        
        if (!File.Exists(filePath))
        {
            Tables = new Dictionary<string, object>();
            Schema = new Dictionary<string, (string, string)>();
            return;
        }

        string json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            Tables = new Dictionary<string, object>();
            Schema = new Dictionary<string, (string, string)>();
            return;
        }

        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? 
                       new Dictionary<string, object>();
        
        Schema = jsonData.TryGetValue("schema", out var schemaObj) && schemaObj != null
            ? JsonConvert.DeserializeObject<Dictionary<string, (string, string)>>(schemaObj.ToString())
            : new Dictionary<string, (string, string)>();
        
        Tables = new Dictionary<string, object>();

        if (jsonData.TryGetValue("tables", out var tablesObj) && tablesObj != null)
        {
            var tablesData = JsonConvert.DeserializeObject<Dictionary<string, string>>(tablesObj.ToString()) ?? 
                             new Dictionary<string, string>();

            foreach (var table in tablesData)
            {
                if (!Schema.ContainsKey(table.Key)) continue;

                Type? keyType = GetTypeFromAssembly(Schema[table.Key].Item1);
                Type? valueType = GetTypeFromAssembly(Schema[table.Key].Item2);

                if (keyType == null || valueType == null)
                    throw new Exception($"Type not found: keyType={Schema[table.Key].Item1}, valueType={Schema[table.Key].Item2}");

                Type tableType = typeof(Table<,>).MakeGenericType(keyType, valueType);
                object tableInstance = Activator.CreateInstance(tableType, table.Key);

                var jsonTable = JsonConvert.DeserializeObject<Dictionary<string, object>>(table.Value);
                if (jsonTable != null && jsonTable.TryGetValue("Data", out var dataObj) && dataObj != null)
                {
                    Type dataType = typeof(List<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType));
                    var dataList = JsonConvert.DeserializeObject(dataObj.ToString(), dataType);

                    if (dataList != null)
                    {
                        var loadDataMethod = tableType.GetMethod("LoadData");
                        loadDataMethod?.Invoke(tableInstance, new object[] { dataList });
                    }
                }

                Tables[table.Key] = tableInstance;
            }
        }
    }
    catch (JsonException jsonEx)
    {
        throw new Exception("JSON parsing error: " + jsonEx.Message, jsonEx);
    }
    catch (IOException ioEx)
    {
        throw new Exception("File I/O error: " + ioEx.Message, ioEx);
    }
    catch (Exception ex)
    {
        throw new Exception("Error while loading database: " + ex.Message, ex);
    }
}


    public void SaveFile()
    {
        if (Tables == null || Tables.Count == 0)
        {
            return;
        }

        var tablesAsJson = new Dictionary<string, string>();

        foreach (var table in Tables)
        {
            var tableObj = table.Value;
            if (tableObj == null) continue;
            var exportDataMethod = tableObj.GetType().GetMethod("ExportData");
            if (exportDataMethod == null) continue;
            var dataList = exportDataMethod.Invoke(tableObj, null);
            if (dataList == null) continue;
            var jsonTable = new
            {
                Name = table.Key,
                Data = dataList
            };

            tablesAsJson[table.Key] = JsonConvert.SerializeObject(jsonTable, Formatting.Indented);
        }

        var jsonData = new
        {
            schema = Schema,
            tables = tablesAsJson
        };

        string json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
        File.WriteAllText("D:\\dot net\\Dotnet-B1\\DataBase\\DataBase.json", json);
    }



    private Type? GetTypeFromAssembly(string typeName)
    {
        return Type.GetType(typeName, throwOnError: false) ??
               AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(a => a.GetTypes())
                   .FirstOrDefault(t => t.AssemblyQualifiedName == typeName);
    }
}