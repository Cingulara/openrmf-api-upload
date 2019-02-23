using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using System.Xml.Serialization;
using System.Xml;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NATS.Client;

using openstig_upload_api.Data;
using openstig_upload_api.Models;

namespace openstig_upload_api.Controllers
{
    [Route("/")]
    public class UploadController : Controller
    {
	    private readonly IArtifactRepository _artifactRepo;
        private readonly ILogger<UploadController> _logger;
        private readonly IConnection _msgServer;

        public UploadController(IArtifactRepository artifactRepo, ILogger<UploadController> logger, IOptions<NATSServer> msgServer)
        {
            _logger = logger;
            _artifactRepo = artifactRepo;
            _msgServer = msgServer.Value.connection;
        }

        // POST as new
        [HttpPost]
        public async Task<IActionResult> UploadNewChecklist(IFormFile checklistFile, STIGtype type, string title = "New Uploaded Checklist", string description = "", string system="None")
        {
            try {
                var name = checklistFile.FileName;
                string rawChecklist =  string.Empty;
                using (var reader = new StreamReader(checklistFile.OpenReadStream()))
                {
                    rawChecklist = reader.ReadToEnd();  
                }
                var record = await _artifactRepo.AddArtifact(new Artifact () {
                    title = title,
                    description = description + "\n\nUploaded filename: " + name,
                    system = system,
                    created = DateTime.Now,
                    updatedOn = DateTime.Now,
                    type = type,
                    rawChecklist = rawChecklist
                });

                // publish to the openstig save new realm the new ID we can use
                _msgServer.Publish("openstig.save.new", Encoding.UTF8.GetBytes(record.InternalId.ToString()));
                return Ok();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error uploading checklist file");
                return BadRequest();
            }
        }

        // PUT as update
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChecklist(string id, IFormFile checklistFile, STIGtype type, string title = "New Uploaded Checklist", string description = "", string system="None")
        {
            try {
                var name = checklistFile.FileName;
                string rawChecklist =  string.Empty;
                using (var reader = new StreamReader(checklistFile.OpenReadStream()))
                {
                    rawChecklist = reader.ReadToEnd();  
                }
                await _artifactRepo.UpdateArtifact(id, new Artifact () {
                    updatedOn = DateTime.Now,
                    title = title,
                    system = system,
                    description = description,
                    type = type,
                    rawChecklist = rawChecklist
                });
                // publish to the openstig save new realm the new ID we can use
                _msgServer.Publish("openstig.save.update", Encoding.UTF8.GetBytes(id));

                return Ok();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error Uploading updated Checklist file");
                return BadRequest();
            }
        }
        
    }
}
