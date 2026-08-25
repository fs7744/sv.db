using SV.Script.Runtime;

namespace SV.Script.Emit;

public enum OpCode : byte
{
    Nop,

    /// <summary>A = 常量池索引</summary>
    PushConst,

    /// <summary>A = 直接编码的小整数</summary>
    PushInt,

    PushNull,

    /// <summary>A = 已注册类型索引</summary>
    PushType,

    /// <summary>A = 槽位</summary>
    LoadLocal,

    /// <summary>A = 槽位（弹出栈顶写入）</summary>
    StoreLocal,

    Pop,
    Dup,

    /// <summary>复制栈顶两个值，用于 a[i] += x 这类复合赋值</summary>
    Dup2,

    /// <summary>A = 元素个数，从栈上依次取</summary>
    NewArray,

    /// <summary>A = 键名集合索引, B = 键值对数（值在栈上，键在编译期已知）</summary>
    NewMap,

    Add, Sub, Mul, Div, Mod, Neg, Not,

    /// <summary>校验栈顶是 bool（不改变栈），用于 and/or 的右操作数。A: 0=and, 1=or</summary>
    AssertBool,

    Eq, Ne, Lt, Le, Gt, Ge,

    /// <summary>A = 目标 ip</summary>
    Jump,

    JumpIfFalse,
    JumpIfTrue,

    /// <summary>短路用：条件不满足时跳转且保留栈顶</summary>
    JumpIfFalseKeep,

    JumpIfTrueKeep,

    /// <summary>?? 与 ?. 用：栈顶为 null 时跳转（保留栈顶）</summary>
    JumpIfNull,

    JumpIfNotNull,

    /// <summary>A = site 索引。弹出接收者，压入成员值。</summary>
    GetMember,

    /// <summary>A = site 索引。栈上为 [recv, value]。</summary>
    SetMember,

    /// <summary>栈上为 [recv, index]，压入元素值。</summary>
    GetIndex,

    /// <summary>栈上为 [recv, index, value]。</summary>
    SetIndex,

    /// <summary>A = site 索引（含实参个数）。栈上为 [recv, arg0 .. argN-1]。</summary>
    Call,

    /// <summary>A = 存放枚举器的隐藏槽位。弹出可迭代对象。</summary>
    IterInit,

    /// <summary>A = 枚举器槽位, B = 迭代结束时的跳转目标。未结束则压入当前元素。</summary>
    IterNext,

    Return,
    ReturnNull,
}

public readonly struct Instr
{
    public readonly OpCode Op;
    public readonly int A;
    public readonly int B;

    public Instr(OpCode op, int a = 0, int b = 0)
    {
        Op = op; A = a; B = b;
    }

    public override string ToString() => B != 0 ? $"{Op} {A} {B}" : A != 0 ? $"{Op} {A}" : Op.ToString();
}

/// <summary>
/// 编译产物。除了 <see cref="MemberSite"/> 内部的内联缓存（原子替换、竞争无害）之外全部不可变，
/// 因此同一个 program 可以跨线程并发执行。
/// </summary>
public sealed class ScriptProgram
{
    public required Instr[] Code { get; init; }

    public required Value[] Consts { get; init; }

    public required Type[] Types { get; init; }

    public required MemberSite[] Sites { get; init; }

    public required string[][] MapKeySets { get; init; }

    /// <summary>槽位总数。前 <see cref="ExternalNames"/>.Length 个是宿主注入的变量。</summary>
    public required int SlotCount { get; init; }

    public required int MaxStack { get; init; }

    /// <summary>所有调用点中最大的实参个数，用于一次性分配实参缓冲。</summary>
    public required int MaxArgs { get; init; }

    public required string[] ExternalNames { get; init; }

    /// <summary>与 <see cref="Code"/> 等长的行号映射，用于运行期错误定位。</summary>
    public required int[] Lines { get; init; }

    public required int[] Cols { get; init; }

    public required string Source { get; init; }

    /// <summary>反汇编，调试用。</summary>
    public string Disassemble()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < Code.Length; i++)
        {
            var ins = Code[i];
            sb.Append(i.ToString("D4")).Append("  ").Append(ins.Op.ToString().PadRight(16));
            switch (ins.Op)
            {
                case OpCode.PushConst:
                    sb.Append(ins.A).Append("   ; ").Append(Consts[ins.A].ToDisplayString());
                    break;
                case OpCode.GetMember:
                case OpCode.SetMember:
                case OpCode.Call:
                    sb.Append(ins.A).Append("   ; ").Append(Sites[ins.A]);
                    break;
                case OpCode.PushType:
                    sb.Append(ins.A).Append("   ; ").Append(Types[ins.A].Name);
                    break;
                default:
                {
                    int n = OperandCount(ins.Op);
                    if (n >= 1) sb.Append(ins.A);
                    if (n >= 2) sb.Append(' ').Append(ins.B);
                    break;
                }
            }
            sb.Append("        (line ").Append(Lines[i]).Append(')').AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>每个指令实际使用几个操作数。操作数为 0 时也要打印，否则 LoadLocal 0 会看不出槽位。</summary>
    private static int OperandCount(OpCode op) => op switch
    {
        OpCode.PushConst or OpCode.PushInt or OpCode.PushType
            or OpCode.LoadLocal or OpCode.StoreLocal
            or OpCode.NewArray or OpCode.AssertBool
            or OpCode.Jump or OpCode.JumpIfFalse or OpCode.JumpIfTrue
            or OpCode.JumpIfFalseKeep or OpCode.JumpIfTrueKeep
            or OpCode.JumpIfNull or OpCode.JumpIfNotNull
            or OpCode.GetMember or OpCode.SetMember or OpCode.Call
            or OpCode.IterInit => 1,
        OpCode.NewMap or OpCode.IterNext => 2,
        _ => 0,
    };
}
