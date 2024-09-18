namespace SV.Db.Sloth.Statements
{
    public class OrderByFieldStatement : Statement
    {
        public string Name { get; set; }

        public OrderByDirection Direction { get; set; }
    }
}