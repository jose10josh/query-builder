using System.Text;

namespace QueryBuilder
{
    /// <summary>
    /// Class <c>QueryBuilder</c> serves as an abstraction to build a dynamic query,
    /// <see href="https://docs.codingtipi.com/docs/toolkit/query-builder">See More</see>
    /// </summary>
    /// <remarks>
    /// Removes unessesary code when validating multiple parameters to build a query.
    /// </remarks>
    public class QueryBuilder
    {
        private readonly Dictionary<string, string> _validFields;
        private readonly Dictionary<string, string>? _validFilters;
        private readonly Dictionary<string, string>? _validJoins;
        private readonly string _table;
        private readonly string _id;
        private readonly string? _joins;

        #region Constructors
        /// <summary>
        /// This constructor initializes a new <c>QueryBuilder</c> with the needed configurations, 
        /// <see href="https://docs.codingtipi.com/docs/toolkit/query-builder/ctors">See More</see>.
        /// </summary>
        /// <param name="validFields">Represent the valid keys with valid query options.</param>
        /// <param name="table">Represent your main table name.</param>
        /// <param name="id">Represent your main table Id name.</param>
        /// <param name="validFilters">Represent the filters you want to apply to the table, the default value is null.</param>
        /// <param name="validJoins">Represent the joins by fields you want to apply to the table, the default value is null.</param>
        /// <param name="joins">Represent the specification of the Joins you want to apply.</param>
        public QueryBuilder(Dictionary<string, string> validFields, string table, string id,
            Dictionary<string, string>? validFilters = null,
            Dictionary<string, string>? validJoins = null, string? joins = null)
        {
            _validFields = validFields;
            _validFilters = validFilters;
            _validJoins = validJoins;
            _joins = joins;
            //Check if you need join support
            if (!String.IsNullOrEmpty(_joins))
            {
                _table = $"{table} AS a";
                _id = $"a.{id}";
            }
            else
            {
                _table = table;
                _id = id;
            }

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// This method build a query with your provided parameters, 
        /// <see href="https://docs.codingtipi.com/docs/toolkit/query-builder/methods">See More</see>.
        /// </summary>
        /// <remarks>
        /// Build a query and returns it as a string.
        /// </remarks>
        /// <param name="select">Values sent by the user separated by commas or semmi-colons EXAMPLE: Id;name;phone.</param>
        /// <param name="order">Order you want to apply to the main table id, can be desc or asc.</param>
        /// <param name="filters">Filters you want to apply must be provided in this format Field:Value for exmaple to filter by id Id:1.</param>
        /// <param name="page">Int representing your query's current page.</param>
        /// <param name="pagesize">Int representing your query's page size.</param>
        /// <returns>
        /// Returns a <c>string</c> object containg your query.
        /// </returns>
        public string BuildQuery(string? select, string? order = null, string? filters = null, int? page = null, int? pagesize = null)
        {
            return $"SELECT {GetFields(select)} FROM {_table} {GetJoins()} {GetFilters(filters)} {GetOrder(order)} {GetPagination(page, pagesize)};";
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Gets the joins to insert in the query
        /// </summary>
        private string GetJoins()
        {
            if (String.IsNullOrEmpty(_joins))
                return "";

            var joinsQuery = new StringBuilder();
            var cleanJoins = _joins.Replace(',', ';').Split(';');
            for (int i = 0; i < cleanJoins.Length; i++)
            {
                //Gets the values of the joins
                var join = cleanJoins[i].Split(':');
                joinsQuery.Append($"{join[0].ToUpper()} JOIN {join[1]} ON {join[2]} ");
            }

            return joinsQuery.ToString();
        }

        /// <summary>
        /// Gets the ORDER BY part of the query
        /// </summary>
        /// <param name="order">Order you want to apply to the main table id, can be desc or asc.</param>
        private string GetOrder(string? order)
        {
            if (String.IsNullOrEmpty(order))
                return "";
            order = order.ToLower();
            if (order != "asc" && order != "desc")
                return "";

            return $"ORDER BY {_id} {order.ToUpper()}";
        }

        /// <summary>
        /// Gets the LIMIT and OFFSET part of the query
        /// </summary>
        /// <param name="page">Int representing your query's current page.</param>
        /// <param name="pagesize">Int representing your query's page size.</param>
        private static string GetPagination(int? page, int? pagesize)
        {
            if (!page.HasValue || !pagesize.HasValue)
                return "";
            var offset = page == 1 ? "" : $"OFFSET {pagesize * (page - 1)}";
            return $"LIMIT {pagesize} {offset}";
        }
        /// <summary>
        /// Gets the WHERE part of the query
        /// </summary>
        /// <param name="filters">Filters you want to apply must be provided in this format Field:Value for exmaple to filter by id Id:1.</param>
        private string GetFilters(string? filters)
        {
            if (String.IsNullOrEmpty(filters))
                return "";

            if (_validFilters == null)
                return "";

            var cleanFilters = filters.ToLower().Replace(',', ';').Split(';');
            var filterValues = new Dictionary<string, string>();
            for (var i = 0; i < cleanFilters.Length; i++)
            {
                var filterValue = cleanFilters[i].Split(':');
                filterValues.Add(filterValue[0], filterValue[1]);
            }
            var queryFilters = new StringBuilder();
            queryFilters.Append("WHERE ");
            var keys = filterValues.Keys.Intersect(_validFilters.Keys).ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                var isInt = int.TryParse(filterValues[keys[i]], out _);
                string filterValue;
                if (isInt)
                    filterValue = filterValues[keys[i]];
                else
                    filterValue = $"'{filterValues[keys[i]]}'";

                queryFilters.Append($"{_validFilters[keys[i]]} {filterValue} ");
                if (i != keys.Length - 1)
                    queryFilters.Append("AND ");
            }

            return queryFilters.ToString();
        }
        /// <summary>
        /// Gets the selects of the Query
        /// </summary>
        /// <param name="select">Values sent by the user separated by commas or semmi-colons EXAMPLE: Id;name;phone.</param>
        private string GetFields(string? select)
        {
            if (String.IsNullOrEmpty(select))
                return Fallback(_validFields);
            //Clean Fields
            select = select.ToLower().Replace(',', ';');
            var selectFields = select.Split(';');

            var fieldsToSelect = selectFields.Intersect(_validFields.Keys).ToList();

            if (fieldsToSelect == null)
                return Fallback(_validFields);

            if (fieldsToSelect.Count == 0)
                return Fallback(_validFields);

            var querySelect = new StringBuilder();

            for (int i = 0; i < fieldsToSelect.Count; i++)
            {
                if (!String.IsNullOrEmpty(_joins))
                    querySelect.Append("a.");

                querySelect.Append(_validFields[fieldsToSelect[i]]);
                if (i != fieldsToSelect.Count - 1)
                    querySelect.Append(", ");
            }

            if (!String.IsNullOrEmpty(_joins) && _validJoins != null)
            {
                var joinedFields = selectFields.Intersect(_validJoins.Keys).ToList();
                if (joinedFields != null)
                {
                    if (querySelect.Length != 0)
                        querySelect.Append(", ");
                    for (int i = 0; i < joinedFields.Count; i++)
                    {
                        var fieldsToAppend = _validJoins[joinedFields[i]].Split(';');
                        for (int x = 0; x < fieldsToAppend.Length; x++)
                        {
                            querySelect.Append(fieldsToAppend[x]);
                            if (x != fieldsToAppend.Length - 1)
                                querySelect.Append(", ");
                        }
                        if (i != joinedFields.Count - 1)
                            querySelect.Append(", ");
                    }
                }
            }

            return querySelect.ToString();
        }

        /// <summary>
        /// Act as a fallback fields
        /// </summary>
        private string Fallback(Dictionary<string, string> validValues)
        {
            var values = validValues.Values.ToArray();
            var querySelect = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (!String.IsNullOrEmpty(_joins))
                    querySelect.Append($"a.");

                querySelect.Append(values[i]);
                if (i != values.Length - 1)
                    querySelect.Append(", ");
            }

            return querySelect.ToString();
        }

        #endregion
    }

}
