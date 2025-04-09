using Microsoft.AspNetCore.Mvc;
using NationalArchives.Taxonomy.Common;
using NationalArchives.Taxonomy.Common.Domain;
using NationalArchives.Taxonomy.Common.Service;
using NationalArchives.Taxonomy.Common.BusinessObjects;

namespace tna.taxonomy.api.Controllers
{
    [Produces("application/json")]
    [ApiController]
    public class TaxonomyController : Controller
    {
        private readonly IInformationAssetViewService _iaViewService;
        private readonly ICategoriserService<CategorisationResult> _categoriserService;
        private ILogger<TaxonomyController> _logger;

        public TaxonomyController(IInformationAssetViewService iaViewService, ICategoriserService<CategorisationResult> categoriserService, ILogger<TaxonomyController> logger)
        {

            _iaViewService = iaViewService;
            _categoriserService = categoriserService;
            _logger = logger;
        }

        [Route("search")]
        [HttpPost]
        public async Task<ActionResult<PaginatedList<InformationAssetViewWithScore>>> SearchIAView(SearchIAViewRequest searchRequest)
        {
            if (searchRequest == null || String.IsNullOrWhiteSpace(searchRequest.CategoryQuery))
            {
                _logger.LogError("No search request provided");
                return Ok(searchRequest?.CategoryQuery);
            }

            try
            {
                PaginatedList<InformationAssetViewWithScore> listOfIAViews = await _iaViewService.PerformSearch(
                        searchRequest.CategoryQuery, searchRequest.Score, searchRequest.Limit, searchRequest.Offset);

                return Ok(listOfIAViews);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving information assets for category.");
                return Ok(e);
            }

        }

        [Route("Test")]
        [HttpGet]
        public Task<string> Test()
        {
            var tcs = new TaskCompletionSource<string>();
            tcs.SetResult("Test Method for Taxonomy API Controller");
            return tcs.Task;
        }

        [Route("TestCategoriseSingle")]
        [HttpPost]
        public async Task<ActionResult<IList<CategorisationResult>>> TestCategoriseSingle(TestCategoriseSingleRequest testCategoriseSingleRequest)
        {
            if (String.IsNullOrWhiteSpace(testCategoriseSingleRequest?.Description?.ToString()) || String.IsNullOrWhiteSpace(testCategoriseSingleRequest?.DocReference?.ToString()))
            {
                _logger.LogError("No document info supplied for categorisation request!");
                return BadRequest();
            }

            try
            {
                IList<CategorisationResult> results = await _categoriserService.TestCategoriseSingle(testCategoriseSingleRequest.DocReference);
                return Ok(results);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error on Test Categorisation of {testCategoriseSingleRequest.DocReference}");
                return BadRequest();
            }
        }



        [Route("TestBrowseAll")]
        [HttpPost]
        public ActionResult<InformationAssetScrollList> TestBrowseAll(InformationAssetScrollRequest scrollRequest)
        {
            try
            {
                var elasticBrowseparams = new OpenSearchAssetBrowseParams() { ScrollTimeout = scrollRequest.Timeout, PageSize = scrollRequest.PageSize };
                var results = _iaViewService.BrowseAllDocReferences(elasticBrowseparams, scrollId: scrollRequest.ScrollId);
                return Ok(results);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error on browsing database documen ts using scroll request.");
                return BadRequest();
            }
        }


        [Route("Test")]
        [HttpPost]
        public string Test(TaxonomyTest taxonomyTest)
        {
            _logger.LogInformation("Calling Taxonomy Controler Test Method");
            var tcs = new TaskCompletionSource<string>();
            tcs.SetResult(String.Concat(taxonomyTest.FirstName, " ", taxonomyTest.LastName));
            return tcs.Task.Result;
        }
    }
}
