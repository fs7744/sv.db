# SV.Script 使用指南

嵌入式动态脚本引擎，用来把「会变的业务规则」从 C# 代码里挪到可配置的脚本里：定价规则、
校验规则、路由规则、字段计算等。纯托管、无第三方依赖、每次执行零堆分配。

- 库代码：`src/SV.Script`
- 测试：`test/SV.Script.Tests`（683 个用例，按场景分文件）
- 语法速查：见本文[语言参考](#四语言参考)

---

## 目录

1. [五分钟上手](#一五分钟上手)
2. [三种执行方式](#二三种执行方式)
3. [把宿主类型暴露给脚本](#三把宿主类型暴露给脚本)
4. [语言参考](#四语言参考)
5. [数值语义](#五数值语义重要)
6. [常见场景配方](#六常见场景配方)
7. [错误处理](#七错误处理)
8. [缓存与并发](#八缓存与并发)
9. [安全护栏](#九安全护栏)
10. [调试](#十调试)
11. [性能](#十一性能)
12. [限制与常见坑](#十二限制与常见坑)
13. [API 速查](#十三api-速查)

---

## 一、五分钟上手

```csharp
using SV.Script;

var engine = new ScriptEngine()
    .RegisterType(typeof(Math));          // 让脚本能调 Math.Round 之类

var script = engine.Compile("""
    if (x > 1 and x < 6) {
        return y * c;
    } else if (x > 6 and x < 12) {
        return y / c;
    } else {
        return c + 1;
    }
    """);

var r = script.Run(new Dictionary<string, object?>
{
    ["x"] = 3, ["y"] = 2, ["c"] = 5,
});

Console.WriteLine(r.ToDisplayString());   // 10
```

脚本里没声明过的标识符（这里的 `x` / `y` / `c`）自动成为**外部变量**，由宿主在 `Run` 时提供。
没提供的外部变量取值 `null`。

拿到脚本需要哪些变量：

```csharp
Console.WriteLine(string.Join(", ", script.Externals));   // x, y, c
```

结果取值：

```csharp
Value r = script.Run(vars);

r.Kind                  // ValueKind.Int / Dec / Str / Bool / Null / Array / Map / Object
r.AsInt                 // long
r.AsDec                 // decimal（Int 也能按 decimal 读）
r.AsStr                 // string
r.AsBool                // bool
r.IsNull                // bool
r.ToDisplayString()     // 脚本可见的字符串形式
r.ToClrObject()         // 拆成 CLR object（数字/布尔会装箱）
```

---

## 二、三种执行方式

按「同一段脚本要跑多少次」来选。

### 1) 一次性求值 —— `Evaluate`

内部带按源码文本的编译缓存，适合零散调用。

```csharp
var v = engine.Evaluate("return 1 + 2;");
var w = engine.Evaluate("return n * 2;", new Dictionary<string, object?> { ["n"] = 5 });
```

### 2) 编译一次、反复执行 —— `Compile` + `Run(dict)`

最常用。`CompiledScript` 不可变，可长期持有、可跨线程并发执行。

```csharp
var script = engine.Compile(File.ReadAllText("rules/pricing.sx"));

foreach (var order in orders)
{
    var price = script.Run(new Dictionary<string, object?> { ["order"] = order }).AsDec;
}
```

### 3) 高频路径 —— 槽位数组

编译期就把变量名解析成槽位号，执行时不做任何字典查找。行数大、循环调用时用这个。

```csharp
var script = engine.Compile("return order.Total * rate;");

int sOrder = script.SlotOf("order");     // 未被脚本引用则返回 -1
int sRate  = script.SlotOf("rate");
var slots  = script.CreateSlots();       // 长度 = Program.SlotCount

foreach (var order in orders)
{
    slots[sOrder] = Value.Obj(order);
    slots[sRate]  = Value.Dec(0.9m);
    total += script.Run(slots).AsDec;
}
```

`slots` 可以反复复用：局部变量每次执行都会被 `let` 重新初始化，**只有外部变量需要你自己重填**。
注意 `slots` 不是线程安全的，每个线程要有自己的一份。

---

## 三、把宿主类型暴露给脚本

有两条通道，合起来就是脚本能看到的全部世界。

### 通道一：注入实例变量

任何 CLR 对象都可以作为外部变量传进去，脚本就能读它的属性、调它的方法：

```csharp
script.Run(new Dictionary<string, object?> { ["order"] = myOrder });
```

```
order.Total                 // 属性 / 字段读
order.Code = "X";           // 属性 / 字段写
order.Discount(0.1, 5)      // 实例方法（按实参个数与类型选重载）
order.Tags[1]               // 索引器
order.Customer.Name         // 成员链
order.Customer?.Name        // 判空链，前面为 null 时整条链返回 null
```

### 通道二：注册静态类型

```csharp
engine.RegisterType(typeof(Math));            // 脚本里写 Math.Round(x, 2)
engine.RegisterType(typeof(Tax));             // Tax.Rate / Tax.For("CA")
engine.RegisterType(typeof(OrderState));      // OrderState.Paid
engine.RegisterType(typeof(MyHelper), "H");   // 起别名：脚本里写 H.Foo()
```

**没注册的类型名，脚本拿不到。** 未注册的标识符只会变成一个值为 `null` 的外部变量，
所以脚本无法凭空访问 `System.IO.File` 这类东西。注册新类型会清掉引擎的编译缓存。

### 类型映射规则

| 宿主侧 | 脚本侧 |
|---|---|
| `sbyte/byte/short/ushort/int/uint/long` | `int`（内部 long） |
| `float/double/decimal`、大 `ulong` | `decimal` |
| `bool` | `bool` |
| `string`、`char` | `string` |
| `enum` | **枚举名字符串**，如 `"Paid"` |
| `null` | `null` |
| `T[]` / `List<T>` / 任意 `IEnumerable` | 宿主对象（可 `foreach`、可用其成员） |
| 其它引用/值类型 | 宿主对象（透传） |
| `void` 方法 | `null` |

枚举双向都认名字，也认数字：

```
order.State                  // "Paid"
order.State == "Paid"        // true
order.State = "Shipped";     // 按名字写（大小写不敏感）
order.State = 2;             // 按数字写
order.Next(OrderState.Paid)  // 注册枚举类型后可这样传
```

### 重载解析

按 `(方法名, 实参个数)` 过滤候选，再按实参类型给每个候选打分取最优。支持**可选参数**和
**`params` 数组**。不做泛型推断（泛型方法定义会被跳过）。同分时**报「重载有歧义」而不是猜**——
脚本引擎里猜错重载造成的线上问题，比编译失败贵得多。

```
order.Label("x")             // 可选参数补默认值 -> "x-!"
order.Join("-", "a", "b")    // params 打包 -> "a-b"
order.SumAll()               // params 收到空数组 -> 0
```

---

## 四、语言参考

### 语句

```
let x = 1;                              // 声明。块作用域，允许遮蔽外层同名变量
let y;                                  // 不给初值就是 null
x = 2;  x += 1;  x -= 1;  x *= 2;  x /= 2;  x %= 3;

if (c) { } else if (c2) { } else { }    // 大括号可省略：if (c) return 1;
while (c) { }
for (let i = 0; i < n; i += 1) { }      // 三段都可省：for (;;) { break; }
foreach (v in items) { }                // 也可写 foreach (let v in items)
break;  continue;
return expr;   return;                  // 走到末尾没有 return 则返回 null

order.Bump();                           // 调用语句
{ ... }                                 // 块
;                                       // 空语句
// 行注释        /* 块注释 */
```

`else` 归属最近的 `if`。`break` / `continue` 只影响最内层循环，出现在循环外是**编译错误**。

### 表达式（优先级由低到高）

| 运算符 | 说明 |
|---|---|
| `? :` | 三元，右结合 |
| `??` | 空合并，右结合 |
| `or` / `\|\|` | 短路或 |
| `and` / `&&` | 短路与 |
| `==` `!=` | 相等 |
| `<` `<=` `>` `>=` | 比较 |
| `+` `-` | 任一侧是字符串时 `+` 为拼接 |
| `*` `/` `%` | |
| `-x` `!x` `not x` `+x` | 一元 |
| `.` `?.` `[]` `()` | 后缀链（最紧） |

**条件位置必须是 `bool`**：`if (1)` 是运行期错误，不按 truthy 处理。`and` / `or` 的两侧、
`?:` 的条件同样要求 `bool`，避免隐式转换带来的一整类坑。

`and` / `or` 短路，右侧不会被求值：

```
if (order.Customer != null and order.Customer.IsVip) { ... }   // 安全
return false and 1 / 0 == 0;                                    // 不会抛除零
```

### 字面量与集合

```
123        1.5        "str"      'str'      true      false      null
[1, 2, 3]                       // 数组，元素类型可混，允许尾逗号
{ a: 1, "b": 2 }                // 字典，键始终是字符串，允许尾逗号
```

字符串支持 `\n \t \r \\ \' \"` 转义。标识符允许非 ASCII，**中文变量名可用**：`let 数量 = 3;`

数组：`a[0]`、`a[0] = x`、`a[0] += 1`、`a.Count`、`a.Add(v)`、`a.Insert(i, v)`、
`a.RemoveAt(i)`、`a.Clear()`、`a.Contains(v)`、`a.IndexOf(v)`。下标越界会抛错。

字典：`m.k`、`m["k"]`、`m.k = v`、`m.Count`、`m.ContainsKey(k)`、`m.Remove(k)`、
`m.Clear()`、`m.Keys()`、`m.Values()`。**读不存在的键得到 `null`，不抛错。**
成员访问优先当作键读取，键不存在时才退到 `ScriptMap` 自身成员，所以 `{ Count: 9 }.Count` 是 `9`。

`foreach` 可以遍历：数组、字典（产出**键**）、字符串（产出单字符串）、任意宿主 `IEnumerable`。

字符串下标产出单字符字符串：`"hey"[1]` 是 `"e"`。字符串不可修改。

---

## 五、数值语义（重要）

整数走 `long` 快路径，**溢出或出现小数时自动提升为 `decimal`（不是 double）**。
这是为了金额计算安全：

```
return 0.1 + 0.2;                  // 0.3        —— 精确，不是 0.30000000000000004
return 9223372036854775807 + 1;    // 9223372036854775808   —— 溢出提升为 decimal
```

几条要记住的规则：

| 表达式 | 结果 | 说明 |
|---|---|---|
| `5 / 2` | `2.5` | **`/` 始终产生 decimal** |
| `6 / 3` | `2` | 仍是 decimal，只是没有小数部分 |
| `7 % 3` | `1` | `%` 两侧都是整数时保持整数 |
| `1 == 1.0` | `true` | 数字跨表示比较 |
| `1.50` | `1.50` | decimal 的标度会保留 |
| `1 / 0` | 抛错 | 除数为 0 是运行期错误 |

写回宿主的 `int` 属性时，值必须是整数且在范围内，否则抛错：

```
order.Count = 8 / 2;    // OK，decimal 4 无小数部分
order.Count = 1.5;      // 抛错：无法转换
order.Count = 1e11;     // 抛错：超出 int 范围
```

---

## 六、常见场景配方

### 定价规则

```csharp
var script = engine.Compile("""
    let rate = order.Customer.IsVip ? 0.8 : 1.0;
    let sub  = order.Total * rate;
    if (order.Count >= 3) { sub = sub * 0.9; }
    return Math.Round(sub * (1 + Tax.For(order.Ship.Region)), 2);
    """);

decimal price = script.Run(new Dictionary<string, object?> { ["order"] = order }).AsDec;
```

### 校验规则（返回错误列表）

```csharp
var script = engine.Compile("""
    let errors = [];
    if (order.Code == null or order.Code.Length < 3) { errors.Add("code"); }
    if (order.Total <= 0) { errors.Add("total"); }
    foreach (t in order.Tags) {
        if (t.Length > 10) { errors.Add("tag:" + t); }
    }
    return errors;
    """);

var v = script.Run(vars);
if (v.Kind == ValueKind.Array && v.AsArray.Count > 0)
{
    foreach (var e in v.AsArray) Console.WriteLine(e.ToDisplayString());
}
```

### 路由 / 分支判定（返回字符串）

```csharp
var script = engine.Compile("""
    foreach (t in order.Tags) {
        if (t == "urgent") { return "express"; }
    }
    if (order.Weight > 20) { return "freight"; }
    return "standard";
    """);

string channel = script.Run(vars).AsStr;
```

### 聚合计算（没有 lambda，用 foreach 代替 LINQ）

```csharp
var script = engine.Compile("""
    let total = 0;
    let n = 0;
    foreach (line in order.Lines) {
        if (not line.Active) { continue; }
        total += line.Price * line.Qty;
        n += 1;
    }
    return { total: total, count: n, avg: n > 0 ? total / n : 0 };
    """);

var m = script.Run(vars).AsMap;
Console.WriteLine($"{m["total"].AsDec} / {m["count"].AsInt}");
```

### 批量高频计算（槽位快路径）

```csharp
var script = engine.Compile("return row.Price * row.Qty * (1 - discount);");
int sRow = script.SlotOf("row");
int sDis = script.SlotOf("discount");
var slots = script.CreateSlots();

decimal sum = 0m;
foreach (var row in millionsOfRows)
{
    slots[sRow] = Value.Obj(row);
    slots[sDis] = Value.Dec(0.05m);
    sum += script.Run(slots).AsDec;
}
```

### 让配置里的脚本先过一遍校验

```csharp
if (!engine.TryCompile(userSuppliedScript, out var script, out var diags))
{
    foreach (var d in diags) log.Error(d.ToString());   // (行,列): error: 说明
    return;
}
// 顺手检查一下脚本要的变量宿主是否都能提供
var missing = script!.Externals.Except(available).ToList();
```

---

## 七、错误处理

三类异常，都继承 `ScriptException`：

| 异常 | 何时 | 关键成员 |
|---|---|---|
| `ScriptCompileException` | `Compile` 时语法/语义有错 | `Diagnostics`（全部诊断，带行列） |
| `ScriptRuntimeException` | 执行时出错 | `Line` `Col` `SourceLine` `InnerException` |
| `ScriptFuelExhaustedException` | 指令预算耗尽（疑似死循环） | — |

编译错误不要用异常流：

```csharp
if (!engine.TryCompile(src, out var script, out var diags))
{
    foreach (var d in diags)
        Console.WriteLine($"({d.Line},{d.Col}) {d.Message}");
}
```

运行期错误带位置和出错源码行：

```csharp
try
{
    script.Run(vars);
}
catch (ScriptRuntimeException ex)
{
    Console.WriteLine(ex);
    // (3,10): 除数为 0
    //     return a / b;
}
```

**宿主方法抛出的异常会被包装**成 `ScriptRuntimeException`（附上脚本位置），原始异常保留在
`InnerException` 里：

```csharp
catch (ScriptRuntimeException ex) when (ex.InnerException is SqlException sql)
{
    // 拿回宿主侧的真实异常
}
```

---

## 八、缓存与并发

- `CompiledScript` / `ScriptProgram` **不可变，可跨线程并发 `Run`**。内部只有调用点的内联缓存是
  可变的，采用原子引用替换，竞争时最坏只是重复解析一次。
- `engine.GetOrCompile(src)` 按源码文本缓存编译结果（`ConcurrentDictionary`），同一段脚本只编译一次。
- `RegisterType` 会清空该引擎的编译缓存；**已经编译出来的 `CompiledScript` 不受影响**，
  它们看到的是编译那一刻的类型注册表。
- 每次 `Run` 的求值栈来自线程本地缓冲，所以并发执行之间没有争用；宿主方法里再执行脚本
  （重入）会自动退化为临时数组，也是安全的。
- `Value[] slots` **不是**线程安全的，每个线程各自 `CreateSlots()`。

```csharp
// 典型并发用法
Parallel.ForEach(orders, order =>
{
    var slots = script.CreateSlots();
    slots[sOrder] = Value.Obj(order);
    Interlocked.Add(ref total, (long)script.Run(slots).AsDec);
});
```

---

## 九、安全护栏

当前的信任模型是「**脚本作者可信**」（宿主自己或运维配置的规则），不是完整沙箱：
已注册类型的成员是全开的。但仍有几层护栏：

| 护栏 | 默认 | 作用 |
|---|---|---|
| `ScriptOptions.Fuel` | 2000 万条指令 | 防死循环，超出抛 `ScriptFuelExhaustedException` |
| `ScriptOptions.StrictVariables` | `false` | 设为 `true` 时未声明标识符是**编译错误**，能抓住变量名拼写错误 |
| 类型注册制 | — | 没 `RegisterType` 的类型脚本拿不到，无法 `import` 任意 CLR 类型 |

```csharp
var engine = new ScriptEngine(new ScriptOptions
{
    Fuel = 1_000_000,          // 收紧预算
    StrictVariables = true,    // 强制变量先声明
});

// 也可以按脚本单独调
script.Fuel = 100_000;
```

如果将来要跑**不可信**脚本，还需要补：成员白名单、拦截 `Type`/`Assembly`/`MemberInfo` 出现在
任何签名里（否则白名单会被反射穿透）、内存计量、执行超时。

---

## 十、调试

`Disassemble()` 打印带行号的字节码，排查「脚本为什么走了这个分支」很有用：

```csharp
Console.WriteLine(script.Disassemble());
```

```
0000  LoadLocal       0        (line 1)
0001  PushInt         1        (line 1)
0002  Gt                       (line 1)
0003  JumpIfFalse     7        (line 1)
0004  LoadLocal       0        (line 1)
0005  Return                   (line 1)
...
```

还能看编译产物的形状：

```csharp
script.Program.SlotCount    // 槽位总数
script.Program.MaxArgs      // 最大实参个数
script.Program.Code.Length  // 指令数
script.Program.Lines        // ip -> 行号
script.Externals            // 脚本要的外部变量
```

---

## 十一、性能

Release 构建、单线程、每项 100 万次迭代的实测值（数值仅供量级参考，随机器而变）：

| 场景 | 耗时 | 分配 |
|---|---|---|
| `if/else` + 算术（约 20 条指令） | ~254 ns/次 | 0 B |
| `for` 循环 100 轮累加（约 1500 条指令） | ~14.3 μs/次 | 0 B |
| 属性链 + 静态方法调用 | ~492 ns/次 | 0 B |
| 编译 106 字节脚本 | ~11.6 μs/次（约 9 MB/s） | — |

要点：

- **每次执行零堆分配**，不给 GC 添负担。前提是走 `Run(slots)`；`Run(dict)` 会有字典本身的开销。
- 折算下来约 **9~10 ns/指令**。比理论最优的字节码 VM 慢 2~4 倍，原因是 `Value` 结构体为了内联
  承载 `decimal` 而有 40 字节，每条指令都要做几次 40 字节拷贝。**这是选 decimal 的既定代价。**
- **编译成本靠缓存摊销为零**，`GetOrCompile` / 长期持有 `CompiledScript` 即可。

按调用频次判断是否需要继续优化：254 ns/次 ≈ 每秒 390 万次。如果实际是每请求几十次，
脚本执行不会是瓶颈，别再投入。真要压的话按性价比排序：

1. `long`/`decimal` 用 `LayoutKind.Explicit` 做联合 → `Value` 从 40 降到 32 字节，估计 10~15%
2. 超算术指令（`AddLocalLocal`、`LtLocalConst`）→ 热循环可能快 30~50%，opcode 数量翻倍
3. 换 Expression 后端编译成委托 → 个位数 ns，但那是另一套后端的工作量

---

## 十二、限制与常见坑

### 本版本不支持

- **脚本内定义函数、lambda / 闭包** → 因此 `Where` / `Select` / `OrderBy` 等接收回调的宿主方法用不了，
  聚合请用 `foreach`
- `try` / `catch` / `throw`、`switch`、字符串插值
- 泛型方法（含类型推断）、扩展方法、`out` / `ref` 参数、事件
- 完整沙箱白名单（见[安全护栏](#九安全护栏)）

### 容易踩的坑

| 现象 | 原因 / 做法 |
|---|---|
| `if (1)` 报错 | 条件必须是 `bool`，没有 truthy |
| `5 / 2` 得到 `2.5` 而不是 `2` | `/` 始终产生 decimal；要整除请用 `(a - a % b) / b` |
| 循环外读循环变量得到 `null` | `for`/`foreach` 的变量作用域限于循环内，出去就变成外部变量了。开 `StrictVariables` 能把它变成编译错误 |
| 变量名写错却不报错 | 非严格模式下未知标识符自动变外部变量（值 `null`）。**生产建议开 `StrictVariables = true`** |
| `if (a = b)` 编译失败 | 赋值只是语句，不是表达式。这是刻意的 |
| 语句开头的 `{ a: 1 }` 编译失败 | 语句位置的 `{` 一律当作块；字典字面量只能出现在表达式位置 |
| `m.zzz` 得到 `null` 而不报错 | 字典缺键读作 `null`，这是刻意的；宿主对象的未知成员**会**报错 |
| `Math.Round(2.345, 2)` 得到 `2.34` | 选中 `Round(decimal, int)`，走 .NET 的银行家舍入 |
| 报「重载有歧义」 | 打分相同，改一下实参类型消歧，不要指望引擎猜 |
| `order.Echo(1)` 说没这个方法 | 泛型方法定义会被跳过，不参与解析 |
| 宿主异常被包成 `ScriptRuntimeException` | 原始异常在 `InnerException` 里 |
| `foreach` 遍历字典拿到的是键 | 要值就 `m[k]`，或用 `m.Values()` |
| `slots` 在多线程下结果乱 | `Value[] slots` 不是线程安全的，每线程各自 `CreateSlots()` |

---

## 十三、API 速查

### `ScriptEngine`

```csharp
new ScriptEngine(ScriptOptions? options = null)

ScriptOptions Options { get; }

ScriptEngine RegisterType(Type type, string? alias = null)      // 注册静态类型，清空缓存
ScriptEngine RegisterType<T>(string? alias = null)

CompiledScript Compile(string source)                            // 出错抛 ScriptCompileException
bool TryCompile(string source, out CompiledScript?, out IReadOnlyList<Diagnostic>)
CompiledScript GetOrCompile(string source)                       // 按源码文本缓存
Value Evaluate(string source, IReadOnlyDictionary<string, object?>? vars = null)
```

### `ScriptOptions`

```csharp
bool StrictVariables { get; set; }   // 默认 false
long Fuel            { get; set; }   // 默认 20_000_000
```

### `CompiledScript`

```csharp
ScriptProgram Program { get; }
long Fuel { get; set; }
IReadOnlyList<string> Externals { get; }

int SlotOf(string name)              // 未引用返回 -1
Value[] CreateSlots()

Value Run(IReadOnlyDictionary<string, object?>? vars = null)
Value Run(Value[] slots)             // 高频路径
string Disassemble()
```

### `Value`

```csharp
// 构造
Value.Null / Value.True / Value.False / Value.Zero
Value.Bool(bool) / Value.Int(long) / Value.Dec(decimal) / Value.Str(string)
Value.Arr(ScriptArray) / Value.Map(ScriptMap) / Value.Obj(object?) / Value.TypeRef(Type)

// 读取
ValueKind Kind; bool IsNull; bool IsNumber
bool AsBool; long AsInt; decimal AsDec; string AsStr
ScriptArray AsArray; ScriptMap AsMap; Type AsType
object? ToClrObject(); string ToDisplayString(); string TypeName

// 比较
bool Equals(Value)                              // 脚本语义：1 == 1.0
static IEqualityComparer<Value> ExactComparer   // 结构精确：1 != 1.0
```

### `Marshaller`（宿主互操作边界，一般不用直接调）

```csharp
Value FromClr(object?)                  // CLR -> Value
object? ToClr(Value, Type)              // Value -> CLR
IEnumerator<Value> Enumerate(in Value)  // 取脚本可迭代的枚举器
```

---

## 测试文件导航

想知道某个行为的确切语义，直接看对应测试：

| 文件 | 覆盖内容 |
|---|---|
| `LexerTest.cs` | token 切分、关键字、数字、字符串转义、注释、位置信息、词法错误 |
| `ParserTest.cs` | 后缀链组合、括号嵌套、排版无关性、注释穿插、语句边界 |
| `ArithmeticTest.cs` | 优先级结合性、long/decimal 提升、除法取模、字符串拼接、比较相等、逻辑短路 |
| `ControlFlowTest.cs` | if/else 链、while/for/foreach、break/continue、块作用域、return |
| `CollectionTest.cs` | 数组与字典的字面量、下标、成员、引用语义 |
| `InteropTest.cs` | 属性字段读写、重载解析、可选参数、params、静态类型、枚举、索引器、内联缓存、重入 |
| `NullSafetyTest.cs` | `?.` 整链短路、`??`、空引用错误信息 |
| `DiagnosticsTest.cs` | 语法/语义诊断与位置、严格变量模式、运行期错误信息、指令预算 |
| `EngineApiTest.cs` | 编译入口、缓存、外部变量、槽位快路径、并发、编译产物 |
| `ValueTest.cs` | `Value` 语义、精确比较器、`Marshaller` 双向转换、`ScriptArray`/`ScriptMap` |
