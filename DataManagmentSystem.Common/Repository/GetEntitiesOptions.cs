namespace DataManagmentSystem.Common.Repository {
    using System.Collections.Generic;
    using DataManagmentSystem.Common.Request;
    using DataManagmentSystem.Common.SelectQuery;

    public class GetEntitiesOptions {
        public RequestFilter Filters { get; set; }
        public SelectColumn Columns { get; set; }
        public IEnumerable<OrderOption> OrderOptions { get; set; }
        public int PageSize { get; set; } = 30;
        public int PageIndex { get; set; } = 0;
        public bool IsColumnReadingRestricted { get; set; } = true;
        public bool AsNoTracking { get; set; } = true;
        public bool CanSkipLocalization { get; set; } = false;
        public bool IgnoreDeletedRecords { get; set; } = true;
    }
}
