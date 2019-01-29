
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using openstig_upload_api.Models;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using openstig_upload_api.Data;

namespace openstig_upload_api.Controllers
{
    [Route("/")]
    public class UploadController : Controller
    {
	    private readonly IArtifactRepository _artifactRepo;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IArtifactRepository artifactRepo, ILogger<UploadController> logger)
        {
            _logger = logger;
            _artifactRepo = artifactRepo;
        }

        // POST as new
        [HttpPost]
        public async Task<IActionResult> UploadNewChecklist(IFormFile checklistFile, STIGtype checklistType)
        {
            try {
                var name = checklistFile.FileName;
                string rawChecklist =  string.Empty;
                using (var reader = new StreamReader(checklistFile.OpenReadStream()))
                {
                    rawChecklist = reader.ReadToEnd();  
                }
                await _artifactRepo.AddArtifact(new Artifact () {
                    title = "New Uploaded Checklist file " + name,
                    created = DateTime.Now,
                    type = checklistType,
                    rawChecklist = rawChecklist
                });

                return Ok();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error uploading checklist file");
                return BadRequest();
            }
        }

        // PUT as update
        [HttpPut]
        public async Task<IActionResult> UpdateChecklist(string id, IFormFile checklistFile, STIGtype checklistType, string title = "New Uploaded Checklist", string description = "")
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
                    description = description,
                    type = checklistType,
                    rawChecklist = rawChecklist
                });

                return Ok();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error Uploading updated Checklist file");
                return BadRequest();
            }
        }
        
    }
}
