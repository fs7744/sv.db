namespace SV.Db.Sloth.Statements
{
    public interface IOrderByField
    {
        public OrderByDirection Direction { get; set; }
    }

    public class OrderByFieldStatement : FieldStatement, IOrderByField
    {
        public OrderByDirection Direction { get; set; }
    }
}