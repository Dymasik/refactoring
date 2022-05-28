namespace DataManagmentSystem.Common.SelectQuery
{
    public class OrderOption
    {
        public string ColumnName { get; set; }
        public OrderDirection Direction { get; set; }
    }

    public enum OrderDirection {
        ASC,
        DESC
    }
}