namespace DataManagmentSystem.Common.SelectQuery {
    using System.Collections.Generic;
    using System.Linq;

    public class SelectColumn {
        public IEnumerable<string> ColumnNames { get; set; }
        public IEnumerable<SelectRelatedColumn> RelatedColumns { get; set; }

        internal void Merge(IEnumerable<string> columnChain) {
            var firstColumn = columnChain.First();
            if (columnChain.Count() == 1) {
                Merge(firstColumn);
            } else {
                var relatedColumn = RelatedColumns.SingleOrDefault(c => c.ColumnName == firstColumn);
                if (relatedColumn == null) {
                    relatedColumn = new SelectRelatedColumn {
                        ColumnName = firstColumn,
                        ColumnNames = Enumerable.Empty<string>(),
                        RelatedColumns = Enumerable.Empty<SelectRelatedColumn>()
                    };
                    RelatedColumns = RelatedColumns.Concat(new[] { relatedColumn });
                }
                relatedColumn.Merge(columnChain.Skip(1));
            }
        }

        internal void Merge(string columnName) {
            if (!ColumnNames.Any(c => c == columnName)) {
                ColumnNames = ColumnNames.Concat(new[] { columnName });
            }
        }
    }

    public class SelectRelatedColumn : SelectColumn {
        public string ColumnName { get; set; }
    }
}
