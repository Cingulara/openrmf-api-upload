using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Xml;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Microsoft.AspNetCore.Authorization;

using openrmf_upload_api.Data;
using openrmf_upload_api.Models;

namespace openrmf_upload_api.Controllers
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
        [Authorize(Roles = "Administrator,Editor,Assessor")]
        public async Task<IActionResult> UploadNewChecklist(List<IFormFile> checklistFiles, string system="None")
        {
            try {
                if (checklistFiles.Count > 0) {
                  foreach(IFormFile file in checklistFiles) {
                    string rawChecklist =  string.Empty;
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        rawChecklist = reader.ReadToEnd();  
                    }
                    rawChecklist = SanitizeData(rawChecklist);
                    // create the new record
                    Artifact newArtifact = MakeArtifactRecord(system, rawChecklist);
                    // grab the user/system ID from the token if there which is *should* always be
                    var claim = this.User.Claims.Where(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier).FirstOrDefault();
                    if (claim != null) { // get the value
                      newArtifact.createdBy = Guid.Parse(claim.Value);
                    }
                    // save it to the database
                    var record = await _artifactRepo.AddArtifact(newArtifact);

                    // publish to the openrmf save new realm the new ID we can use
                    _msgServer.Publish("openrmf.checklist.save.new", Encoding.UTF8.GetBytes(record.InternalId.ToString()));
                    _msgServer.Flush();
                  }
                  return Ok();
                }
                else
                    return BadRequest();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error uploading checklist file");
                return BadRequest();
            }
        }

        // PUT as update
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,Editor,Assessor")]
        public async Task<IActionResult> UpdateChecklist(string id, IFormFile checklistFile, string system="None")
        {
          try {
              var name = checklistFile.FileName;
              string rawChecklist =  string.Empty;
              using (var reader = new StreamReader(checklistFile.OpenReadStream()))
              {
                  rawChecklist = reader.ReadToEnd();  
              }
              rawChecklist = SanitizeData(rawChecklist);
              // update and fill in the same info
              Artifact newArtifact = MakeArtifactRecord(system, rawChecklist);
              Artifact oldArtifact = await _artifactRepo.GetArtifact(id);
              if (oldArtifact != null && oldArtifact.createdBy != Guid.Empty){
                // this is an update of an older one, keep the createdBy intact
                newArtifact.createdBy = oldArtifact.createdBy;
              }
              oldArtifact = null;

              // grab the user/system ID from the token if there which is *should* always be
              var claim = this.User.Claims.Where(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier).FirstOrDefault();
              if (claim != null) { // get the value
                newArtifact.updatedBy = Guid.Parse(claim.Value);
              }

              await _artifactRepo.UpdateArtifact(id, newArtifact);
              // publish to the openrmf save new realm the new ID we can use
              _msgServer.Publish("openrmf.checklist.save.update", Encoding.UTF8.GetBytes(id));
              _msgServer.Flush();
              return Ok();
          }
          catch (Exception ex) {
              _logger.LogError(ex, "Error Uploading updated Checklist file");
              return BadRequest();
          }
      }
      
      // this parses the text and system, generates the pieces, and returns the artifact to save
      private Artifact MakeArtifactRecord(string system, string rawChecklist) {
        Artifact newArtifact = new Artifact();
        newArtifact.system = system;
        newArtifact.created = DateTime.Now;
        newArtifact.updatedOn = DateTime.Now;
        newArtifact.rawChecklist = rawChecklist;

        // parse the checklist and get the data needed
        rawChecklist = rawChecklist.Replace("\n","").Replace("\t","");
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(rawChecklist);

        newArtifact.hostName = "Unknown-Host";
        XmlNodeList assetList = xmlDoc.GetElementsByTagName("ASSET");
        // get the host name from here
        foreach (XmlElement child in assetList.Item(0).ChildNodes)
        {
          switch (child.Name) {
            case "HOST_NAME":
              if (!string.IsNullOrEmpty(child.InnerText)) 
                newArtifact.hostName = child.InnerText;
              break;
          }
        }
        // get the title and release which is a list of children of child nodes buried deeper :face-palm-emoji:
        XmlNodeList stiginfoList = xmlDoc.GetElementsByTagName("STIG_INFO");
        foreach (XmlElement child in stiginfoList.Item(0).ChildNodes) {
          if (child.FirstChild.InnerText == "releaseinfo")
            newArtifact.stigRelease = child.LastChild.InnerText;
          else if (child.FirstChild.InnerText == "title")
            newArtifact.stigType = child.LastChild.InnerText;
        }

        // shorten the names a bit
        if (newArtifact != null && !string.IsNullOrEmpty(newArtifact.stigType)){
          newArtifact.stigType = newArtifact.stigType.Replace("Security Technical Implementation Guide", "STIG");
          newArtifact.stigType = newArtifact.stigType.Replace("Windows", "WIN");
          newArtifact.stigType = newArtifact.stigType.Replace("Application Security and Development", "ASD");
          newArtifact.stigType = newArtifact.stigType.Replace("Microsoft Internet Explorer", "MSIE");
          newArtifact.stigType = newArtifact.stigType.Replace("Red Hat Enterprise Linux", "REL");
          newArtifact.stigType = newArtifact.stigType.Replace("MS SQL Server", "MSSQL");
          newArtifact.stigType = newArtifact.stigType.Replace("Server", "SVR");
          newArtifact.stigType = newArtifact.stigType.Replace("Workstation", "WRK");
        }
        if (newArtifact != null && !string.IsNullOrEmpty(newArtifact.stigRelease)) {
          newArtifact.stigRelease = newArtifact.stigRelease.Replace("Release: ", "R"); // i.e. R11, R2 for the release number
          newArtifact.stigRelease = newArtifact.stigRelease.Replace("Benchmark Date:","dated");
        }
        return newArtifact;
      }

      private string SanitizeData (string rawdata) {
        return rawdata.Replace("\t","").Replace(">\n<","><");
      }
    }
}
