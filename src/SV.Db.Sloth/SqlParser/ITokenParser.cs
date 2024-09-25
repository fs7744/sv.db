using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth.SqlParser
{
    public abstract class Token
    {
    }

    public interface ITokenParser
    {
        bool TryTokenize(ParserContext context, out Token t);
    }
}