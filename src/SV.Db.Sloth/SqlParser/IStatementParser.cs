namespace SV.Db.Sloth.SqlParser
{
    public interface IStatementParser
    {
        bool TryParse(StatementParserContext context);
    }
}