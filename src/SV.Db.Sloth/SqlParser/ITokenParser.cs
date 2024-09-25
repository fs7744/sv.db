namespace SV.Db.Sloth.SqlParser
{
    public interface ITokenParser
    {
        bool TryTokenize(ParserContext context, out Token t);
    }
}