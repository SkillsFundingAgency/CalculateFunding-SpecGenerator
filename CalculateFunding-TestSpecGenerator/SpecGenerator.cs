using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Frontend.Clients.CalcsClient.Models;
using CalculateFunding.Frontend.Clients.CommonModels;
using CalculateFunding.Frontend.Clients.DatasetsClient.Models;
using CalculateFunding.Frontend.Clients.SpecsClient.Models;
using CalculateFunding.Frontend.Helpers;
using CalculateFunding.Frontend.Interfaces.ApiClient;
using Serilog;

namespace CalculateFunding_TestSpecGenerator
{
    public class SpecGenerator
    {
        private readonly ISpecsApiClient _specsClient;
        private readonly IDatasetsApiClient _datasetsClient;
        private readonly ICalculationsApiClient _calcsClient;
        private readonly ILogger _logger;

        public SpecGenerator(ISpecsApiClient specsApiClient, IDatasetsApiClient datasetsApiClient, ICalculationsApiClient calcsApiClient, ILogger logger)
        {
            Guard.ArgumentNotNull(specsApiClient, nameof(specsApiClient));
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));
            Guard.ArgumentNotNull(calcsApiClient, nameof(calcsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _specsClient = specsApiClient;
            _datasetsClient = datasetsApiClient;
            _calcsClient = calcsApiClient;
            _logger = logger;
        }

        public async Task Generate(SpecGeneratorConfiguration configuration)
        {
            Specification specification = await GenerateSpecification(configuration);

            await GeneratePolicies(configuration, specification);

            specification = await GetSpecification(specification.Id);

            await GenerateCalculations(configuration, specification);

            await GenerateDataset(specification, configuration);

            // await GenerateCalculationCodeWithReturnFromDatasets(configuration, specification);
        }

        private async Task GenerateCalculationCodeWithReturnFromDatasets(SpecGeneratorConfiguration configuration, Specification specification)
        {

            SearchFilterRequest calcRequest = new SearchFilterRequest()
            {
                Filters = new Dictionary<string, string[]>() { { "specificationId", new[] { "testId" } } },
                PageSize = 500,
            };

            PagedResult<CalculationSearchResultItem> calculations = await _calcsClient.FindCalculations(calcRequest);

            foreach (CalculationSearchResultItem calc in calculations.Items)
            {

            }
        }

        private async Task GenerateDataset(Specification specification, SpecGeneratorConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.DatasetDefinitionId) && !string.IsNullOrWhiteSpace(configuration.DatasetFilePath))
            {
                _logger.Information("Looking up Dataset with ID {DatasetDefinitionId}", configuration.DatasetDefinitionId);

                ApiResponse<DatasetDefinition> datasetDefinitionResponse = await _datasetsClient.GetDatasetDefinitionById(configuration.DatasetDefinitionId);

                if (datasetDefinitionResponse.StatusCode != HttpStatusCode.OK)
                {
                    _logger.Error("Unable to lookup Dataset Definition with ID {DatasetDefinitionId}", configuration.DatasetDefinitionId);
                    return;
                }

                _logger.Information("Found Dataset Definition '{Name}'", datasetDefinitionResponse.Content.Name);

                string filename = Path.GetFileName(configuration.DatasetFilePath);

                CreateNewDatasetModel createNewDatasetModel = new CreateNewDatasetModel()
                {
                    DefinitionId = datasetDefinitionResponse.Content.Id,
                    Description = $"{configuration.SpecificationName} ({datasetDefinitionResponse.Content.Id})",
                    Name = $"{configuration.SpecificationName} ({datasetDefinitionResponse.Content.Name})",
                    Filename = filename,
                };

                _logger.Information("Creating data source with excel spreadsheet located at {DatasetFilePath}", configuration.DatasetFilePath);
                ValidatedApiResponse<NewDatasetVersionResponseModel> datasetVersionResponse = await this._datasetsClient.CreateDataset(createNewDatasetModel);
                if (datasetDefinitionResponse.StatusCode != System.Net.HttpStatusCode.OK && datasetDefinitionResponse.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    _logger.Error("Unable to create data source {Content}", datasetDefinitionResponse.Content);
                    return;
                }

                // Upload file to blob storage - headers should match the TypeScript attributes to set the metadata on the blob
                using (HttpClient httpClient = new HttpClient())
                {
                    using (StreamReader sr = new StreamReader(configuration.DatasetFilePath))
                    {
                        using (StreamContent sc = new StreamContent(sr.BaseStream))
                        {
                            sc.Headers.Add("x-ms-blob-type", "BlockBlob");
                            sc.Headers.Add("x-ms-meta-dataDefinitionId", datasetVersionResponse.Content.DefinitionId);
                            sc.Headers.Add("x-ms-meta-datasetId", datasetVersionResponse.Content.DatasetId);
                            sc.Headers.Add("x-ms-meta-authorName", "Spec Generator");
                            sc.Headers.Add("x-ms-meta-authorId", "specGenerator");
                            sc.Headers.Add("x-ms-meta-filename", filename);
                            sc.Headers.Add("x-ms-meta-name", datasetVersionResponse.Content.Name);
                            sc.Headers.Add("x-ms-meta-description", datasetVersionResponse.Content.Description);

                            HttpResponseMessage fileUploadResponse = await httpClient.PutAsync(datasetVersionResponse.Content.BlobUrl, sc);
                            if (!fileUploadResponse.IsSuccessStatusCode)
                            {
                                _logger.Error("Invalid response received on data source upload {ReasonPhrase}", fileUploadResponse.ReasonPhrase);
                                return;
                            }
                            else
                            {
                                _logger.Information("Uploaded data source file to blob storage {filename}", filename);
                            }
                        }
                    }
                }

                ValidateDatasetModel validateDatasetModel = new ValidateDatasetModel()
                {
                    Comment = "SpecGenerator",
                    DatasetId = datasetVersionResponse.Content.DatasetId,
                    Description = "Spec Generator",
                    Filename = filename,
                    Version = 1,
                };

                _logger.Information("Validating data source");

                ValidatedApiResponse<DatasetValidationStatusModel> apiResponse = await _datasetsClient.ValidateDataset(validateDatasetModel);
                if (apiResponse.ModelState != null && apiResponse.ModelState.Any())
                {
                    _logger.Error("Dataset validation errors");
                    foreach (var error in apiResponse.ModelState)
                    {
                        _logger.Error("Field Key: {Key}", error.Key);
                        foreach (var errorItem in error.Value)
                        {
                            _logger.Error("Message: {errorItem}", errorItem);
                        }
                    }
                    return;
                }
                else if (apiResponse.Content != null)
                {
                    if (!string.IsNullOrWhiteSpace(apiResponse.Content.ErrorMessage))
                    {
                        _logger.Error("Data source validation failed with message: '{Message}'", apiResponse.Content.ErrorMessage);
                        return;
                    }
                }
                else
                {
                    _logger.Error("Data source validation failed with null response");
                    return;
                }


                // Check status
                int validationChecks = 0;
                while (validationChecks < 1000)
                {
                    ApiResponse<DatasetValidationStatusModel> validationStatusResponse = await _datasetsClient.GetDatasetValidateStatus(apiResponse.Content.OperationId);
                    if (validationStatusResponse.StatusCode == HttpStatusCode.OK)
                    {
                        _logger.Information("Validation status: {CurrentOperation}", validationStatusResponse.Content.CurrentOperation);

                        if (validationStatusResponse.Content.CurrentOperation == DatasetValidationStatus.Validated)
                        {
                            break;
                        }
                        else if (validationStatusResponse.Content.CurrentOperation == DatasetValidationStatus.FailedValidation)
                        {
                            _logger.Error("Validation error:", validationStatusResponse.Content.ErrorMessage);
                            return;
                        }
                    }
                    else
                    {
                        _logger.Warning("Validation response returned {StatusCode}, expected OK", validationStatusResponse.StatusCode);
                    }

                    validationChecks++;
                    Thread.Sleep(2000);
                }



                AssignDatasetSchemaModel assignDatasetSchemaModel = new AssignDatasetSchemaModel()
                {
                    DatasetDefinitionId = datasetDefinitionResponse.Content.Id,
                    Description = $"SpecGenerator - Provider Dataset",
                    IsSetAsProviderData = true,
                    Name = "Provider Dataset",
                    SpecificationId = specification.Id,
                    UsedInDataAggregations = false,
                };

                HttpStatusCode assignStatusCode = await _datasetsClient.AssignDatasetSchema(assignDatasetSchemaModel);

                _logger.Information("Creating Data Source '{Name}' for providers on specification", assignDatasetSchemaModel.Name);

                ApiResponse<IEnumerable<DatasetSchemasAssigned>> datasetSchemasAssignedResponse = await _datasetsClient.GetAssignedDatasetSchemasForSpecification(specification.Id);

                DatasetSchemasAssigned assignedSchema = datasetSchemasAssignedResponse.Content.First();


                AssignDatasetVersion assignDatasetVersion = new AssignDatasetVersion()
                {
                    DatasetId = datasetVersionResponse.Content.DatasetId,
                    RelationshipId = assignedSchema.Id,
                    Version = 1,
                };

                _logger.Information("Assigning uploaded data source to specification");

                HttpStatusCode assignStatusVersionToSpecCode = await _datasetsClient.AssignDataSourceVersionToRelationship(assignDatasetVersion);
            }
        }

        private async Task<Specification> GetSpecification(string specificationId)
        {
            ApiResponse<Specification> apiResponse = await _specsClient.GetSpecification(specificationId);
            return apiResponse.Content;
        }

        private async Task GeneratePolicies(SpecGeneratorConfiguration configuration, Specification specification)
        {
            int totalPolices = 1;
            if (configuration.NumberOfPolices.HasValue && configuration.NumberOfPolices.Value > 0)
            {
                totalPolices = configuration.NumberOfPolices.Value;
            }

            _logger.Information("Creating {totalPolices} policies for specification", totalPolices);

            for (int i = 0; i < totalPolices; i++)
            {
                int policyNumber = i + 1;
                CreatePolicyModel policyCreateModel = new CreatePolicyModel()
                {
                    SpecificationId = specification.Id,
                    Name = $"Policy {policyNumber}",
                    Description = "SpecGenerator",
                };

                await _specsClient.CreatePolicy(policyCreateModel);
            }
        }

        private async Task GenerateCalculations(SpecGeneratorConfiguration configuration, Specification specification)
        {
            int totalCalculations = 0;

            if (configuration.NumberOfCalculations > 0)
            {
                totalCalculations = configuration.NumberOfCalculations;
            }

            List<Policy> policies = new List<Policy>(specification.Policies);

            List<AllocationLine> allocationLines = new List<AllocationLine>();

            _logger.Information("Generating {totalCalculations} calculations across {Count} policies", totalCalculations, policies.Count);

            foreach (FundingStream fundingStream in specification.FundingStreams)
            {
                foreach (AllocationLine allocationLine in fundingStream.AllocationLines)
                {
                    allocationLines.Add(allocationLine);
                }
            }

            IEnumerable<string> allocationLineNames = allocationLines.Select(a => a.Name);

            _logger.Information("Creating calculations in the following Allocation Lines {allocationLineNames}", allocationLineNames);

            for (int i = 0; i < totalCalculations; i++)
            {
                Policy policy = policies[i % policies.Count];
                AllocationLine allocationLine = allocationLines[i % allocationLines.Count];

                CalculationCreateModel calculationCreateModel = new CalculationCreateModel()
                {
                    SpecificationId = specification.Id,
                    PolicyId = policy.Id,
                    AllocationLineId = allocationLine.Id,
                    CalculationType = CalculationSpecificationType.Funding,
                    Description = "SpecGenerator",
                    Name = $"{specification.Name} - Calculation {i + 1}",
                    IsPublic = false,
                };

                await _specsClient.CreateCalculation(calculationCreateModel);
            }
        }

        private async Task<Specification> GenerateSpecification(SpecGeneratorConfiguration configuration)
        {
            string periodId = configuration.PeriodId;
            if (string.IsNullOrWhiteSpace(periodId))
            {
                ApiResponse<IEnumerable<Reference>> periodResponse = await _specsClient.GetFundingPeriods();

                Reference firstPeriod = periodResponse.Content.First();
                periodId = firstPeriod.Id;
            }

            IEnumerable<string> fundingStreamIds = configuration.FundingStreamIds;
            if (fundingStreamIds == null || !fundingStreamIds.Any())
            {
                ApiResponse<IEnumerable<FundingStream>> fundingStreamResponse = await _specsClient.GetFundingStreams();
                fundingStreamIds = new string[] { fundingStreamResponse.Content.First().Id };

            }

            CreateSpecificationModel createModel = new CreateSpecificationModel()
            {
                Name = configuration.SpecificationName,
                FundingPeriodId = periodId,
                FundingStreamIds = fundingStreamIds,
                Description = "SpecGenerator " + DateTime.Now.ToLongDateString(),
            };

            _logger.Information("Generating specification called '{Name}' in period '{FundingPeriodId}'", createModel.Name, createModel.FundingPeriodId);

            ValidatedApiResponse<Specification> specificationResult = await _specsClient.CreateSpecification(createModel);
            _logger.Information("Created Specification {Id}", specificationResult.Content.Id);
            return specificationResult.Content;
        }
    }
}
