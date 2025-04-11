using Lucene.Net.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NationalArchives.Taxonomy.Common;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;
using SharpCompress;


namespace tna.taxonomy.api.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryRepository categoryRepository, ILogger<CategoriesController> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        [Route("GetCategories")]
        [HttpGet]
        public async Task<ActionResult<IList<Category>>> GetCategories()
        {
            try
            {
                IList<Category> categories =  await _categoryRepository.FindAll();
                categories.Sort((c1, c2) => c1.Title.CompareTo(c2.Title));
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving list of categories.");
                return StatusCode(500);
            }
        }

        [Route("Search")]
        [HttpGet]
        public async Task<ActionResult<IList<Category>>> Search(string searchText)
        {
            try
            {
                IList<Category> categories = await _categoryRepository.FindCategories(searchText);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving list of categories.");
                return StatusCode(500);
            }
        }

        [Route("GetCategoryById")]
        [HttpGet]
        public async Task<ActionResult<Category>> GetCategoryById(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId) || !categoryId.StartsWith("C", StringComparison.InvariantCultureIgnoreCase))
            {
                return BadRequest();
            }

            try
            {
                var category = _categoryRepository.FindByCiaid(categoryId);

                if (category != null)
                {
                    return Ok(category);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving category with id {categoryId}");
                return StatusCode(500);
            }
        }

        [Route("AddNewCategory")]
        [HttpPost]
        public async Task<ActionResult<Category>> AddCategory(string title, string query, double score, bool catLock)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(query))
            {
                return BadRequest();
            }

            try
            {
                var category = _categoryRepository.AddNewCategory(title, query, score);

                if (category != null)
                {
                    return Ok(category);
                }
                else
                {
                    return StatusCode(500);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating category with title {title}, query definition {query}");
                return StatusCode(500);
            }
        }

        [Route("SaveCategory")]
        [HttpPost]
        public async Task<ActionResult> SaveCategory(Category category)
        {
            if (category == null || string.IsNullOrEmpty(category.Title) || string.IsNullOrEmpty(category.Query))
            {
                return BadRequest();
            }

            try
            {
                //var category = new Category() { Title = title, Query = query, Score = score, Lock = catLock };
                _categoryRepository.Save(category);
                return NoContent();
            }
            catch (CategoryNotFoundException)
            {
                return NotFound(category.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating category {category.Id}");
                return StatusCode(500);
            }
        }
    } 
}