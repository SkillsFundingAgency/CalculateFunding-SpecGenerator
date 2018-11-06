using System.Collections.Generic;

namespace CalculateFunding_TestSpecGenerator
{
    public class SpecGeneratorConfiguration
    {
        public int NumberOfCalculations { get; set; }

        public int NumberOfTests { get; set; }

        public string SpecificationName { get; set; }

        public string PeriodId { get; set; }

        public IEnumerable<string> FundingStreamIds { get; set; }

        public int? NumberOfPolices { get; set; }

        public string DatasetDefinitionId { get; set; }

        public string DatasetFilePath { get; set; }
    }
}
