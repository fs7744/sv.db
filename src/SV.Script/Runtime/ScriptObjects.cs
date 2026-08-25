using System.Collections;
using System.Text;

namespace SV.Script.Runtime;

/// <summary>
/// 脚本数组字面量 <c>[1, 2, 3]</c> 的运行时类型。
/// 公开成员会被成员解析器直接反射到，所以脚本里可以写 <c>a.Count</c> / <c>a.Add(x)</c>。
/// </summary>
public sealed class ScriptArray : IEnumerable<Value>
{
    private readonly List<Value> _items;

    public ScriptArray() => _items = new List<Value>();

    public ScriptArray(int capacity) => _items = new List<Value>(capacity);

    public ScriptArray(IEnumerable<Value> items) => _items = new List<Value>(items);

    public int Count => _items.Count;

    public Value this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_items.Count)
                throw new ScriptRuntimeException($"数组下标越界: {index}，长度 {_items.Count}");
            return _items[index];
        }
        set
        {
            if ((uint)index >= (uint)_items.Count)
                throw new ScriptRuntimeException($"数组下标越界: {index}，长度 {_items.Count}");
            _items[index] = value;
        }
    }

    public void Add(Value item) => _items.Add(item);

    public void Insert(int index, Value item) => _items.Insert(index, item);

    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_items.Count)
            throw new ScriptRuntimeException($"数组下标越界: {index}，长度 {_items.Count}");
        _items.RemoveAt(index);
    }

    public void Clear() => _items.Clear();

    public int IndexOf(Value item) => _items.IndexOf(item);

    public bool Contains(Value item) => _items.Contains(item);

    public List<Value>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<Value> IEnumerable<Value>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public string ToDisplayString()
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < _items.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var v = _items[i];
            if (v.Kind == ValueKind.Str) sb.Append('"').Append(v.AsStr).Append('"');
            else sb.Append(v.ToDisplayString());
        }
        return sb.Append(']').ToString();
    }

    public override string ToString() => ToDisplayString();
}

/// <summary>脚本字典字面量 <c>{ a: 1, "b": 2 }</c> 的运行时类型。键始终是字符串。</summary>
public sealed class ScriptMap : IEnumerable<KeyValuePair<string, Value>>
{
    private readonly Dictionary<string, Value> _items;

    public ScriptMap() => _items = new Dictionary<string, Value>(StringComparer.Ordinal);

    public ScriptMap(int capacity) => _items = new Dictionary<string, Value>(capacity, StringComparer.Ordinal);

    public int Count => _items.Count;

    /// <summary>读取不存在的键返回 null（与成员访问语义一致），而不是抛异常。</summary>
    public Value this[string key]
    {
        get => _items.TryGetValue(key, out var v) ? v : Value.Null;
        set => _items[key] = value;
    }

    public bool ContainsKey(string key) => _items.ContainsKey(key);

    public bool Remove(string key) => _items.Remove(key);

    public void Clear() => _items.Clear();

    public ScriptArray Keys()
    {
        var a = new ScriptArray(_items.Count);
        foreach (var k in _items.Keys) a.Add(Value.Str(k));
        return a;
    }

    public ScriptArray Values()
    {
        var a = new ScriptArray(_items.Count);
        foreach (var v in _items.Values) a.Add(v);
        return a;
    }

    public Dictionary<string, Value>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<KeyValuePair<string, Value>> IEnumerable<KeyValuePair<string, Value>>.GetEnumerator()
        => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public string ToDisplayString()
    {
        var sb = new StringBuilder("{");
        bool first = true;
        foreach (var kv in _items)
        {
            if (!first) sb.Append(", ");
            first = false;
            sb.Append(kv.Key).Append(": ");
            if (kv.Value.Kind == ValueKind.Str) sb.Append('"').Append(kv.Value.AsStr).Append('"');
            else sb.Append(kv.Value.ToDisplayString());
        }
        return sb.Append('}').ToString();
    }

    public override string ToString() => ToDisplayString();
}
