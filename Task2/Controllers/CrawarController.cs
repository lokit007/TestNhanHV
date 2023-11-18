using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using Task2.Models;

namespace Task2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrawarController : ControllerBase
    {
        private readonly ILogger<CrawarController> _logger;
        private readonly IConfiguration _config;

        public CrawarController(IConfiguration config, ILogger<CrawarController> logger)
        {
            _config = config;
            _logger = logger;
        }

        [NonAction]
        private string MakeAPIURL(string endpoint)
        {
            var configSetting = _config.GetSection("EndPointPublish").Get<ConfigSetting>();
            return Path.Combine(configSetting.RootUrl, endpoint);
        }

        [HttpGet("{*endpoint}")]
        public async Task<IActionResult?> GetDataFromAPI(string endpoint)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(MakeAPIURL(endpoint));
            return new HttpResponseMessageResult(response);
        }

        [HttpPost("{*endpoint}")]
        public async Task<IActionResult?> PostDataFromAPI(string endpoint, [FromBody] object value)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string json = JsonSerializer.Serialize(value);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(MakeAPIURL(endpoint), content);
            return new HttpResponseMessageResult(response);
        }
    }

    public class ConfigSetting
    {
        public string RootUrl { get; set; }
        public string HelpUrl { get; set; }
    }

    public class HttpResponseMessageResult : IActionResult
    {
        private readonly HttpResponseMessage _responseMessage;

        public HttpResponseMessageResult(HttpResponseMessage responseMessage)
        {
            _responseMessage = responseMessage;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = (int)_responseMessage.StatusCode;

            foreach (var header in _responseMessage.Content.Headers)
            {
                response.Headers.TryAdd(header.Key, header.Value.ToArray());
            }

            using (var stream = await _responseMessage.Content.ReadAsStreamAsync())
            {
                await stream.CopyToAsync(response.Body);
                await response.Body.FlushAsync();
            }
        }
    }
}
