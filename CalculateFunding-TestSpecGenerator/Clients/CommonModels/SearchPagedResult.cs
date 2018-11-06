﻿namespace CalculateFunding.Frontend.Clients.CommonModels
{
    using System;
    using CalculateFunding.Frontend.Helpers;

    public class SearchPagedResult<T> : PagedResult<T>
    {
        public SearchPagedResult(SearchFilterRequest filterOptions, int totalCount)
        {
            Guard.ArgumentNotNull(filterOptions, nameof(filterOptions));

            TotalItems = totalCount;
            PageNumber = filterOptions.Page;
            PageSize = filterOptions.PageSize;

            if (totalCount == 0)
            {
                TotalPages = 0;
            }
            else
            {
                TotalPages = (int)Math.Ceiling((decimal)totalCount / filterOptions.PageSize);
            }
        }
    }
}
