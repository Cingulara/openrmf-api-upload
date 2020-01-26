// Copyright (c) Cingulara LLC 2019 and Tutela LLC 2019. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007 license. See LICENSE file in the project root for full license information.
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
using Newtonsoft.Json;

using openrmf_upload_api.Data;
using openrmf_upload_api.Models;
using openrmf_upload_api.Classes;

namespace openrmf_upload_api.Controllers
{
    [Route("/")]
    public class UploadController : Controller
    {
	    private readonly IArtifactRepository _artifactRepo;
	    private readonly ISystemGroupRepository _systemRepo;
      private readonly ILogger<UploadController> _logger;
      private readonly IConnection _msgServer;

        public UploadController(IArtifactRepository artifactRepo, ILogger<UploadController> logger, IOptions<NATSServer> msgServer, ISystemGroupRepository systemRepo )
        {
            _logger = logger;
            _artifactRepo = artifactRepo;
            _systemRepo = systemRepo;
            _msgServer = msgServer.Value.connection;
        }

        /// <summary>
        /// POST Called from the OpenRMF UI (or external access) to create one or more checklist/artifact records within a system.
        /// </summary>
        /// <param name="checklistFiles">The CKL files to add into the system</param>
        /// <param name="systemGroupId">The system Id if adding to a current system</param>
        /// <param name="system">A new System title if creating a new system from checklists</param>
        /// <returns>
        /// HTTP Status showing they were created or that there is an error.
        /// </returns>
        /// <response code="200">Returns the newly updated item</response>
        /// <response code="400">If the item did not update correctly</response>
        /// <response code="404">If the ID passed in is not valid</response>
        [HttpPost]
        [Authorize(Roles = "Administrator,Editor,Assessor")]
        public async Task<IActionResult> UploadNewChecklist(List<IFormFile> checklistFiles, string systemGroupId, string system)
        {
          try {
            _logger.LogInformation("Calling UploadNewChecklist() with {0} checklists", checklistFiles.Count.ToString());
            if (checklistFiles.Count > 0) {

              // grab the user/system ID from the token if there which is *should* always be
              var claim = this.User.Claims.Where(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier).FirstOrDefault();
              // make sure the SYSTEM GROUP is valid here and then add the files...
              SystemGroup sg;
              SystemGroup recordSystem = null;

              if (string.IsNullOrEmpty(systemGroupId)) {
                sg = new SystemGroup();
                sg.title = system;
                sg.created = DateTime.Now;
                if (claim != null && claim.Value != null) {
                  sg.createdBy = Guid.Parse(claim.Value);
                }
                recordSystem = _systemRepo.AddSystemGroup(sg).GetAwaiter().GetResult();
              } else {
                sg = await _systemRepo.GetSystemGroup(systemGroupId);
                if (sg == null) {
                  sg = new SystemGroup();
                  sg.title = "None";
                  sg.created = DateTime.Now;
                  if (claim != null && claim.Value != null) {
                    sg.createdBy = Guid.Parse(claim.Value);
                  }
                recordSystem = _systemRepo.AddSystemGroup(sg).GetAwaiter().GetResult();
                }
                else {
                  sg.updatedOn = DateTime.Now;
                  if (claim != null && claim.Value != null) {
                    sg.updatedBy = Guid.Parse(claim.Value);
                  }
                  var updated = _systemRepo.UpdateSystemGroup(systemGroupId, sg).GetAwaiter().GetResult();
                }
              }

              // result we send back
              UploadResult uploadResult = new UploadResult();

              // now go through the Checklists and set them up
              foreach(IFormFile file in checklistFiles) {
                try {
                    string rawChecklist =  string.Empty;

                    if (file.FileName.ToLower().EndsWith(".xml")) {
                      // if an XML XCCDF SCAP scan file
                      _logger.LogInformation("UploadNewChecklist() parsing the SCAP Scan file for {0}.", file.FileName.ToLower());
                      using (var reader = new StreamReader(file.OpenReadStream()))
                      {
                        // read in the file
                        string xmlfile = reader.ReadToEnd();
                        // pull out the rule IDs and their results of pass or fail and the title/type of SCAP scan done
                        SCAPRuleResultSet results = SCAPScanResultLoader.LoadSCAPScan(xmlfile);
                        // get the rawChecklist data so we can move on
                        // generate a new checklist from a template based on the type and revision
                        rawChecklist = SCAPScanResultLoader.GenerateChecklistData(results);
                      }
                    }
                    else if (file.FileName.ToLower().EndsWith(".ckl")) {
                      // if a CKL file
                      _logger.LogInformation("UploadNewChecklist() parsing the Checklist CKL file for {0}.", file.FileName.ToLower());
                      using (var reader = new StreamReader(file.OpenReadStream()))
                      {
                          rawChecklist = reader.ReadToEnd();  
                      }
                    }
                    else {
                      // log this is a bad file
                      return BadRequest();
                    }

                    // clean up any odd data that can mess us up moving around, via JS, and such
                    _logger.LogInformation("UploadNewChecklist() sanitizing the checklist for {0}.", file.FileName.ToLower());
                    rawChecklist = SanitizeData(rawChecklist);

                    // create the new record for saving into the DB
                    Artifact newArtifact = MakeArtifactRecord(rawChecklist);

                    if (claim != null) { // get the value
                      _logger.LogInformation("UploadNewChecklist() setting the created by ID of the checklist {0}.", file.FileName.ToLower());
                      newArtifact.createdBy = Guid.Parse(claim.Value);
                      if (sg.createdBy == Guid.Empty)
                        sg.createdBy = Guid.Parse(claim.Value);
                      else 
                        sg.updatedBy = Guid.Parse(claim.Value);
                    }

                    // add the system record ID to the Artifact to know how to query it
                    _logger.LogInformation("UploadNewChecklist() setting the title of the checklist {0}.", file.FileName.ToLower());
                    if (recordSystem != null) {
                      newArtifact.systemGroupId = recordSystem.InternalId.ToString();
                      // store the title for ease of use
                      newArtifact.systemTitle = recordSystem.title;
                    }
                    else {
                      newArtifact.systemGroupId = sg.InternalId.ToString();
                      // store the title for ease of use
                      newArtifact.systemTitle = sg.title;
                    }
                    // save the artifact record and checklist to the database
                    _logger.LogInformation("UploadNewChecklist() saving the checklist {0} to the database", file.FileName.ToLower());
                    var record = await _artifactRepo.AddArtifact(newArtifact);
                    _logger.LogInformation("UploadNewChecklist() saved the checklist {0} to the database.", file.FileName.ToLower());

                    // add to the number of successful uploads
                    uploadResult.successful++;

                    // publish to the openrmf save new realm the new ID we can use
                    _logger.LogInformation("UploadNewChecklist() publish a message on a new checklist {0} for the scoring of it.", file.FileName.ToLower());
                    _msgServer.Publish("openrmf.checklist.save.new", Encoding.UTF8.GetBytes(record.InternalId.ToString()));
                    // publish to update the system checklist count
                    _logger.LogInformation("UploadNewChecklist() publish a message on a new checklist {0} for updating the count of checklists in the system.", file.FileName.ToLower());
                    _msgServer.Publish("openrmf.system.count.add", Encoding.UTF8.GetBytes(record.systemGroupId));
                    _msgServer.Flush();

                    // publish an audit event
                    _logger.LogInformation("UploadNewChecklist() publish an audit message on a new checklist {0}.", file.FileName.ToLower());
                    Audit newAudit = GenerateAuditMessage(claim, "add checklist");
                    newAudit.message = string.Format("UploadNewChecklist() uploaded a new checklist {0} in system group ({1}) {2}.", file.FileName.ToLower(), sg.InternalId.ToString(), sg.title);
                    newAudit.url = "POST /";
                    _msgServer.Publish("openrmf.audit.upload", Encoding.UTF8.GetBytes(Compression.CompressString(JsonConvert.SerializeObject(newAudit))));
                    _msgServer.Flush();
                }
                catch (Exception ex) {
                  // add to the list of failed uploads
                  uploadResult.failed++;
                  uploadResult.failedUploads.Add(file.FileName);
                  // log it
                  _logger.LogError(ex, "UploadNewChecklist() error on checklist file not parsing right: {0}.", file.FileName.ToLower());
                  // see if there are any left
                }
              }
              _logger.LogInformation("Called UploadNewChecklist() with {0} checklists successfully", checklistFiles.Count.ToString());
              return Ok(uploadResult);
            }
            else {              
              _logger.LogWarning("Called UploadNewChecklist() with NO checklists!");
              return BadRequest();
            }
          }
          catch (Exception ex) {
              _logger.LogError(ex, "Error uploading checklist file");
              return BadRequest();
          }
        }

        /// <summary>
        /// PUT Called from the OpenRMF UI (or external access) to update a current checklist via a PUT if you 
        /// have the correct roles in your JWT.
        /// </summary>
        /// <param name="id">The ID of the checklist/artifact record to update</param>
        /// <param name="checklistFile">The actual CKL file uploaded</param>
        /// <param name="systemGroupId">The System ID</param>
        /// <returns>
        /// HTTP Status showing it was updated or that there is an error.
        /// </returns>
        /// <response code="200">Returns the newly updated item</response>
        /// <response code="400">If the item did not update correctly</response>
        /// <response code="404">If the ID passed in is not valid</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,Editor,Assessor")]
        public async Task<IActionResult> UpdateChecklist(string id, IFormFile checklistFile, string systemGroupId)
        {
          try {
              _logger.LogInformation("Calling UpdateChecklist({0})", id);
              //var name = checklistFile.FileName;
              string rawChecklist =  string.Empty;
              if (checklistFile.FileName.ToLower().EndsWith(".xml")) {
                // if an XML XCCDF SCAP scan checklistFile
                using (var reader = new StreamReader(checklistFile.OpenReadStream()))
                {
                  // read in the checklistFile
                  string xmlfile = reader.ReadToEnd();
                  // pull out the rule IDs and their results of pass or fail and the title/type of SCAP scan done
                  SCAPRuleResultSet results = SCAPScanResultLoader.LoadSCAPScan(xmlfile);
                  // get the raw checklist from the msg checklist NATS reader                  
                  // update the rawChecklist data so we can move on
                  var record = await _artifactRepo.GetArtifact(id);
                  rawChecklist = SCAPScanResultLoader.UpdateChecklistData(results, record.rawChecklist, false);
                }
              }
              else if (checklistFile.FileName.ToLower().EndsWith(".ckl")) {
                // if a CKL file
                using (var reader = new StreamReader(checklistFile.OpenReadStream()))
                {
                    rawChecklist = reader.ReadToEnd();  
                }
              }
              else {
                // log this is a bad checklistFile
                return BadRequest();
              }

              _logger.LogInformation("UpdateChecklist({0}) sanitizing the checklist XML", id);
              rawChecklist = SanitizeData(rawChecklist);
              // update and fill in the same info
              Artifact newArtifact = MakeArtifactRecord(rawChecklist);
              Artifact oldArtifact = await _artifactRepo.GetArtifact(id);
              if (oldArtifact != null && oldArtifact.createdBy != Guid.Empty){
                _logger.LogInformation("UpdateChecklist({0}) copying the old data into the new one to replace it", id);
                // this is an update of an older one, keep the createdBy intact
                newArtifact.createdBy = oldArtifact.createdBy;
                // keep it a part of the same system group
                if (!string.IsNullOrEmpty(oldArtifact.systemGroupId)) {
                  newArtifact.systemGroupId = oldArtifact.systemGroupId;
                  newArtifact.systemTitle = oldArtifact.systemTitle;
                }
              }
              oldArtifact = null;

              // grab the user/system ID from the token if there which is *should* always be
              var claim = this.User.Claims.Where(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier).FirstOrDefault();
              if (claim != null) { // get the value
                _logger.LogInformation("UpdateChecklist({0}) getting the updated by ID", id);
                newArtifact.updatedBy = Guid.Parse(claim.Value);
              }
              
              _logger.LogInformation("UpdateChecklist({0}) saving the new artifact record", id);
              await _artifactRepo.UpdateArtifact(id, newArtifact);
              // publish to the openrmf save new realm the new ID we can use
              _logger.LogInformation("UpdateChecklist({0}) publishing the updated checklist for scoring", id);
              _msgServer.Publish("openrmf.checklist.save.update", Encoding.UTF8.GetBytes(id));
              _msgServer.Flush();
              _logger.LogInformation("Called UpdateChecklist({0}) successfully", id);
              
              // publish an audit event
              _logger.LogInformation("UpdateChecklist() publish an audit message on an updated checklist {0}.", checklistFile.FileName);
              Audit newAudit = GenerateAuditMessage(claim, "update checklist");
              newAudit.message = string.Format("UpdateChecklist() updated checklist {0} with file {1}.", id, checklistFile.FileName);
              newAudit.url = "PUT /";
              _msgServer.Publish("openrmf.audit.upload", Encoding.UTF8.GetBytes(Compression.CompressString(JsonConvert.SerializeObject(newAudit))));
              _msgServer.Flush();
              return Ok();
          }
          catch (Exception ex) {
              _logger.LogError(ex, "Error Uploading updated Checklist file");
              return BadRequest();
          }
      }
      
      // this parses the text and system, generates the pieces, and returns the artifact to save
      private Artifact MakeArtifactRecord(string rawChecklist) {
        Artifact newArtifact = new Artifact();
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
          else if (child.FirstChild.InnerText == "version")
              newArtifact.version = child.LastChild.InnerText;
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

      private Audit GenerateAuditMessage(System.Security.Claims.Claim claim, string action) {
        Audit audit = new Audit();
        audit.program = "Upload API";
        audit.created = DateTime.Now;
        audit.action = action;
        if (claim != null) {
          audit.userid = claim.Value;
          var fullname = claim.Subject.Claims.Where(x => x.Type == "name").FirstOrDefault();
          if (fullname != null) 
            audit.fullname = fullname.Value;
          var username = claim.Subject.Claims.Where(x => x.Type == "preferred_username").FirstOrDefault();
          if (username != null) 
            audit.username = username.Value;
          var useremail = claim.Subject.Claims.Where(x => x.Type.Contains("emailaddress")).FirstOrDefault();
          if (useremail != null) 
            audit.email = useremail.Value;
        }
        return audit;
      }
    }
}
