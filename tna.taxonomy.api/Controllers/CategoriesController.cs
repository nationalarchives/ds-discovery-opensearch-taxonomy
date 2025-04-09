using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Repository.Common;

namespace tna.taxonomy.api.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<ActionResult<IList<Category>>> GetCategories()
        {
            var categories = _categoryRepository.FindAll();
            return Ok(categories);
        }

        public async Task<ActionResult<Category>> GetCategoryById(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId) || !categoryId.StartsWith("C", StringComparison.InvariantCultureIgnoreCase))
            {
                return BadRequest();
            }

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

        public async Task<ActionResult> AddCategory(string title, string definition)
        {
            throw new NotImplementedException();
        }
    }
}
