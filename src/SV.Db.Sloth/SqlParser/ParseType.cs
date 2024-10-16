namespace SV.Db.Sloth.SqlParser
{
    [Flags]
    public enum ParseType
    {
        Condition = 1,
        SelectField = 2,
        OrderByField = 4,
        GrGroupByFuncField = 8,
    }
}