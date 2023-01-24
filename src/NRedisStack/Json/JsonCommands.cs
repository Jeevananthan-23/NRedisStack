﻿using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NRedisStack;

public class JsonCommands : IJsonCommands
{
    IDatabase _db;
    JsonCommandBuilder jsonBuilder = JsonCommandBuilder.Instance;

    public JsonCommands(IDatabase db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public RedisResult[] Resp(RedisKey key, string? path = null)
    {
        RedisResult result = _db.Execute(jsonBuilder.Resp(key, path));

        if (result.IsNull)
        {
            return Array.Empty<RedisResult>();
        }

        return (RedisResult[])result!;
    }

    /// <inheritdoc/>
    public bool Set(RedisKey key, RedisValue path, object obj, When when = When.Always)
    {
        string json = JsonSerializer.Serialize(obj);
        return Set(key, path, json, when);
    }

    /// <inheritdoc/>
    public bool Set(RedisKey key, RedisValue path, RedisValue json, When when = When.Always)
    {
        return _db.Execute(jsonBuilder.Set(key, path, json, when)).OKtoBoolean();
    }

    /// <inheritdoc/>
    public bool SetFromFile(RedisKey key, RedisValue path, string filePath, When when = When.Always)
    {
        if(!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File {filePath} not found.");
        }

        string fileContent  = File.ReadAllText(filePath);
        return Set(key, path, fileContent, when);
    }

    /// <inheritdoc/>
    public int SetFromDirectory(RedisValue path, string filesPath, When when = When.Always)
    {
        int inserted = 0;
        string key;
        var files = Directory.EnumerateFiles(filesPath, "*.json");
        foreach (var filePath in files)
        {
            key = filePath.Substring(0, filePath.IndexOf("."));
            if(SetFromFile(key, path, filePath, when))
            {
                inserted++;
            }
        }

        foreach (var dirPath in Directory.EnumerateDirectories(filesPath))
        {
            inserted += SetFromDirectory(path, dirPath, when);
        }

        return inserted;
    }

    /// <inheritdoc/>
    public long?[] StrAppend(RedisKey key, string value, string? path = null)
    {
        return _db.Execute(jsonBuilder.StrAppend(key, value, path)).ToNullableLongArray();
    }

    /// <inheritdoc/>
    public long?[] StrLen(RedisKey key, string? path = null)
    {
        return _db.Execute(jsonBuilder.StrLen(key, path)).ToNullableLongArray();
    }

    /// <inheritdoc/>
    public bool?[] Toggle(RedisKey key, string? path = null)
    {
        RedisResult result = _db.Execute(jsonBuilder.Toggle(key, path));

        if (result.IsNull)
        {
            return Array.Empty<bool?>();
        }

        if (result.Type == ResultType.Integer)
        {
            return new bool?[] { (long)result == 1 };
        }

        return ((RedisResult[])result!).Select(x => (bool?)((long)x == 1)).ToArray();
    }

    /// <inheritdoc/>
    public JsonType[] Type(RedisKey key, string? path = null)
    {
        RedisResult result = _db.Execute(jsonBuilder.Type(key, path));

        if (result.Type == ResultType.MultiBulk)
        {
            return ((RedisResult[])result!).Select(x => Enum.Parse<JsonType>(x.ToString()!.ToUpper())).ToArray();
        }

        if (result.Type == ResultType.BulkString)
        {
            return new[] { Enum.Parse<JsonType>(result.ToString()!.ToUpper()) };
        }

        return Array.Empty<JsonType>();

    }

    public long DebugMemory(string key, string? path = null)
    {
        return _db.Execute(jsonBuilder.DebugMemory(key, path)).ToLong();
    }

    /// <inheritdoc/>
    public long?[] ArrAppend(RedisKey key, string? path = null, params object[] values)
    {
        return _db.Execute(jsonBuilder.ArrAppend(key, path, values)).ToNullableLongArray();
    }

    /// <inheritdoc/>
    public long?[] ArrIndex(RedisKey key, string path, object value, long? start = null, long? stop = null)
    {
        return _db.Execute(jsonBuilder.ArrIndex(key, path, value, start, stop)).ToNullableLongArray();
    }

    /// <inheritdoc/>
    public long?[] ArrInsert(RedisKey key, string path, long index, params object[] values)
    {
        return _db.Execute(jsonBuilder.ArrInsert(key, path, index, values)).ToNullableLongArray();
    }

    /// <inheritdoc/>
    public long?[] ArrLen(RedisKey key, string? path = null)
    {
        return _db.Execute(jsonBuilder.ArrLen(key, path)).ToNullableLongArray();
    }

    /// <inheritdoc/>
    public RedisResult[] ArrPop(RedisKey key, string? path = null, long? index = null)
    {
        RedisResult result = _db.Execute(jsonBuilder.ArrPop(key, path, index));

        if (result.Type == ResultType.MultiBulk)
        {
            return (RedisResult[])result!;
        }

        if (result.Type == ResultType.BulkString)
        {
            return new[] { result };
        }

        return Array.Empty<RedisResult>();
    }

    /// <inheritdoc/>
    public long?[] ArrTrim(RedisKey key, string path, long start, long stop) =>
        _db.Execute(jsonBuilder.ArrTrim(key, path, start, stop)).ToNullableLongArray();

    /// <inheritdoc/>
    public long Clear(RedisKey key, string? path = null)
    {
        return _db.Execute(jsonBuilder.Clear(key, path)).ToLong();
    }

    /// <inheritdoc/>
    public long Del(RedisKey key, string? path = null)
    {
        return _db.Execute(jsonBuilder.Del(key, path)).ToLong();
    }

    /// <inheritdoc/>
    public long Forget(RedisKey key, string? path = null) => Del(key, path);

    /// <inheritdoc/>
    public RedisResult Get(RedisKey key, RedisValue? indent = null, RedisValue? newLine = null, RedisValue? space = null, RedisValue? path = null)
    {
        return _db.Execute(jsonBuilder.Get(key, indent, newLine, space, path));
    }

    /// <inheritdoc/>
    public RedisResult Get(RedisKey key, string[] paths, RedisValue? indent = null, RedisValue? newLine = null, RedisValue? space = null)
    {
        return _db.Execute(jsonBuilder.Get(key, paths, indent, newLine, space));
    }

    /// <inheritdoc/>
    public T? Get<T>(RedisKey key, string path = "$")
    {
        var res = _db.Execute(jsonBuilder.Get<T>(key, path));
        if (res.Type == ResultType.BulkString)
        {
            var arr = JsonSerializer.Deserialize<JsonArray>(res.ToString()!);
            if (arr?.Count > 0)
            {
                return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(arr[0]));
            }
        }

        return default;
    }

    /// <inheritdoc/>
    public IEnumerable<T?> GetEnumerable<T>(RedisKey key, string path = "$")
    {
        RedisResult res = _db.Execute(jsonBuilder.Get<T>(key, path));
        return JsonSerializer.Deserialize<IEnumerable<T>>(res.ToString());
    }

    /// <inheritdoc/>
    public RedisResult[] MGet(RedisKey[] keys, string path)
    {
        return _db.Execute(jsonBuilder.MGet(keys, path)).ToArray();
    }

    /// <inheritdoc/>
    public double?[] NumIncrby(RedisKey key, string path, double value)
    {
        var res = _db.Execute(jsonBuilder.NumIncrby(key, path, value));
        return JsonSerializer.Deserialize<double?[]>(res.ToString());
    }

    /// <inheritdoc/>
    public IEnumerable<HashSet<string>> ObjKeys(RedisKey key, string? path = null)
    {
        return _db.Execute(jsonBuilder.ObjKeys(key, path)).ToHashSets();
    }

    /// <inheritdoc/>
    public long?[] ObjLen(RedisKey key, string? path = null)
    {
        return _db.Execute(jsonBuilder.ObjLen(key, path)).ToNullableLongArray();
    }
}