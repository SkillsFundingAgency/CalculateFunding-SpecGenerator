﻿using System;
using System.Collections.Generic;
using CalculateFunding.Frontend.Clients.CommonModels;

namespace CalculateFunding.Frontend.Clients.SpecsClient.Models
{
    public class SpecificationSearchResultItem :  Reference
    {
        public string FundingPeriodName { get; set; }

        public string FundingPeriodId { get; set; }

        public IEnumerable<string> FundingStreamNames { get; set; }

        public IEnumerable<string> FundingStreamIds { get; set; }

        public DateTimeOffset LastUpdatedDate { get; set; }

        public string Status { get; set; }

        public string Description { get; set; }
    }
}
