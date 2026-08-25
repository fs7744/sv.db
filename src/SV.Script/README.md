# SV.Script

嵌入式动态脚本引擎。把「会变的业务规则」从 C# 代码挪进可配置的脚本：定价、校验、路由、字段计算。

纯托管、无第三方依赖、**每次执行零堆分配**。

```csharp
var engine = new ScriptEngine().RegisterType(typeof(Math));

var script = engine.Compile("""
    let rate = order.Customer.IsVip ? 0.8 : 1.0;
    let sub  = order.Total * rate;
    if (order.Count >= 3) { sub = sub * 0.9; }
    return Math.Round(sub, 2);
    """);

var price = script.Run(new Dictionary<string, object?> { ["order"] = order }).AsDec;
```

## 能力范围

支持 `let` / 赋值 / `if`-`else if`-`else` / `while` / `for` / `foreach` / `break` / `continue` /
`return`、数组与字典字面量、`?:` / `??` / `?.`、短路 `and` / `or`，以及**宿主对象的属性、字段、
方法、索引器、静态类型成员、枚举**。

数值是 **long + decimal 混合**：整数走 long 快路径，溢出或出现小数时提升为 decimal（金额安全，
`0.1 + 0.2` 精确等于 `0.3`）。

**不支持**：脚本内定义函数、lambda / 闭包（因此 LINQ 用不了，聚合请用 `foreach`）、
`try/catch`、`switch`、字符串插值、泛型方法、扩展方法。

## 实现

| 层 | 做法 |
|---|---|
| 词法 | 单遍无回溯，字符分类查表，token 是值类型 |
| 语法 | 表驱动 Pratt（优先级爬升），产出扁平数组 AST |
| 编译 | 单遍作用域解析 + 槽位分配 → 单帧字节码 |
| 执行 | 字节码解释器，变量是槽位号，零反射、零装箱、零分配 |
| 互操作 | 调用点内联缓存 + 表达式树生成的 thunk；无 JIT 环境自动回退反射 |

## 文档

**完整使用指南：[`docs/SV.Script-使用指南.md`](../../docs/SV.Script-使用指南.md)** —— 上手、三种执行方式、
宿主类型暴露、语言参考、场景配方、错误处理、并发、安全护栏、性能数据、常见坑、API 速查。

## 测试

`test/SV.Script.Tests`，683 个用例按场景分文件（词法 / 语法 / 算术 / 控制流 / 集合 / 互操作 /
空安全 / 诊断 / 引擎 API / Value 与转换）。想确认某个行为的确切语义，直接看对应测试文件。

```bash
dotnet test test/SV.Script.Tests/SV.Script.Tests.csproj
```
