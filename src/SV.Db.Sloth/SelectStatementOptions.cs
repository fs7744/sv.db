using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth
{
    public record class SelectStatementOptions
    {
        public static readonly SelectStatementOptions Default = new SelectStatementOptions();

        public bool AllowNotFoundFields { get; init; } = false;
        public bool AllowNonStrictCondition { get; init; } = false;
        public Action<Statement> Visiter { get; init; } = null;
    }
}