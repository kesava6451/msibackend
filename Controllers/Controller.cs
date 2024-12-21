using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace YourNamespace.Controllers
{
    [Route("api/")]
    [ApiController]
    public class PipelineController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PipelineController> _logger;

        public PipelineController(IConfiguration configuration, ILogger<PipelineController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Azure DevOps Configuration
        private readonly string _azureOrganization = "snovasysteam";
        private readonly string _azureProject = "MSI-Automation";
        private readonly string _azurePipelineId = "106";
        private readonly string _azurePersonalAccessToken = "bifh6bhzjcdxrebmxtv3n5ofkrs6mbx4nedfochg4gmj43xj46rq"; // Store securely

        // Cache for storing build ID
        private static string buildIdCache = null;

        // Endpoint to trigger Azure DevOps pipeline
        [HttpPost("trigger-pipeline")]
        public async Task<IActionResult> TriggerPipeline([FromBody] TriggerPipelineRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data.");
            }

            // Prepare pipeline variables
            var pipelineVariables = new
            {
                companyName = new { value = request.CompanyName },
                tracker = new { value = request.Tracker },
                version = new { value = request.Version },
                encryptedRegistryKey = new { value = request.EncryptedRegistryKey },
                defaultSiteDomain = new { value = request.DefaultSiteDomain },
                timeChampApiUrl = new { value = request.TimeChampApiUrl }
            };

            var content = new StringContent(JsonConvert.SerializeObject(new { variables = pipelineVariables }), Encoding.UTF8, "application/json");

            try
            {
                // Call Azure DevOps pipeline API
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_azurePersonalAccessToken}")));
                    var response = await client.PostAsync(
                        $"https://dev.azure.com/{_azureOrganization}/{_azureProject}/_apis/pipelines/{_azurePipelineId}/runs?api-version=7.1-preview.1",
                        content
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();
                        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseData);
                        buildIdCache = jsonResponse.id.ToString(); // Cache build ID

                        return Ok(new { message = "Pipeline triggered successfully!", data = jsonResponse, buildId = buildIdCache });
                    }

                    return StatusCode((int)response.StatusCode, new { error = "Failed to trigger the pipeline", details = await response.Content.ReadAsStringAsync() });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while triggering the pipeline.", details = ex.Message });
            }
        }

        // Endpoint to get versions based on the selected tracker
        [HttpGet("get-versions/{tracker}")]
        public IActionResult GetVersions(string tracker)
        {
            var buildBasePath = @"C:\TimeChampMSI\TimeChampbuilds";
            var trackerPath = Path.Combine(buildBasePath, tracker);

            // Check if the directory exists
            if (!Directory.Exists(trackerPath))
            {
                return NotFound(new { error = $"Tracker path not found: {trackerPath}" });
            }

            try
            {
                // Get all directories (versions) under the tracker
                var versions = Directory.GetDirectories(trackerPath)
                                         .Select(Path.GetFileName)
                                         .ToList();

                if (versions.Count == 0)
                {
                    return NotFound(new { error = "No versions found for this tracker" });
                }

                return Ok(new { message = $"Versions for {tracker} fetched successfully", versions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error reading the directory", details = ex.Message });
            }
        }

        // Updated endpoint to check if any file is available in the directory
        [HttpGet("file-check/{companyName}/{tracker}/{version}")]
        public IActionResult CheckFilesInVersionPath(string companyName, string tracker, string version)
        {
            try
            {
                // Construct the base path to the company directory
                var buildBasePath = @"C:\TimeChampMSI\TimechampMSIbuilds";
                var companyNamePath = Path.Combine(buildBasePath, companyName);
                var trackerPath = Path.Combine(companyNamePath, tracker);
                var versionPath = Path.Combine(trackerPath, version);

                // Check if the version path exists
                if (!Directory.Exists(versionPath))
                {
                    return NotFound(new { error = "Version directory not found." });
                }

                // Get the list of files in the version directory
                var files = Directory.GetFiles(versionPath)
                                     .Select(Path.GetFileName)  // Extract only file names
                                     .ToList();

                // If no files are found in the version directory
                if (files.Count == 0)
                {
                    return NotFound(new { error = "No files found in this version path." });
                }

                // Return the list of file names found in the version path
                return Ok(new { files });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching the files.", details = ex.Message });
            }
        }

    }

    // Request body model for triggering the pipeline
    public class TriggerPipelineRequest
    {
        public required string CompanyName { get; set; }
        public required string Tracker { get; set; }
        public required string Version { get; set; }
        public required string EncryptedRegistryKey { get; set; }
        public required string DefaultSiteDomain { get; set; }
        public required string TimeChampApiUrl { get; set; }
    }
}
