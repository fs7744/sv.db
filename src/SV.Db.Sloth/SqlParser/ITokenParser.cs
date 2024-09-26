namespace SV.Db.Sloth.SqlParser
{
    public interface ITokenParser
    {
        bool TryTokenize(TokenParserContext context, out Token t);
    }
}