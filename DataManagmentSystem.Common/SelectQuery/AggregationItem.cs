namespace DataManagmentSystem.Common.SelectQuery {

    public class AggregationItem {
        public string ColumnName { get; set; }
        public AggregationType Type { get; set; }
    }

    public enum AggregationType {
        Count,
        Sum,
        Max,
        Min,
        Average
    }
}
