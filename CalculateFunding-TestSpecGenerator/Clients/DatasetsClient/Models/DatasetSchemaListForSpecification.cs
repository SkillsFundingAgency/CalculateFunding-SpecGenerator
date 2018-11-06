﻿namespace CalculateFunding.Frontend.Clients.DatasetsClient.Models
{
    using System.Collections.Generic;
    using CalculateFunding.Frontend.Clients.CommonModels;

    public class DatasetSchemaListForSpecification : Reference
    {
        public string Description { get; set; }

        public Reference DatasetDefinition { get; set; }

        public Reference Specification { get; set; }

        public IEnumerable<DatasetSchemasAssigned> AssignedDataSetSchemaAssignedList { get; set; }
    }
}
