using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ComicCollector.Services;

namespace ComicCollector.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IExternalAPIService _externalAPIService;
        public dynamic TopCards { get; set; }
        public string Type { get; set; } = "comic";  // Default "comic"
        public string TypeTitle => char.ToUpper(Type[0]) + Type.Substring(1);

        public IndexModel(IExternalAPIService externalAPIService)
        {
            _externalAPIService = externalAPIService;
        }

        public async Task OnGetAsync(string type)
        {
            if (!string.IsNullOrEmpty(type))
                Type = type;
            TopCards = await _externalAPIService.GetTopCards(Type);
        }
    }
}
