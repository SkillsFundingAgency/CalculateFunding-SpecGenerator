﻿namespace CalculateFunding.Frontend.Clients.CommonModels
{
    using System.Collections.Generic;

    public class SearchResults<T>
    {
        public int TotalCount { get; set; }

        public IEnumerable<SearchFacet> Facets { get; set; }

        public IEnumerable<T> Results { get; set; }
    }
}
