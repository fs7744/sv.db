namespace SV.Db.Sloth
{
    public class PageResult<T>
    {
        public int? TotalCount { get; set; }
        public List<T> Rows { get; set; }
    }
}