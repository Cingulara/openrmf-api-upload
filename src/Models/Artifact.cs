// Copyright (c) Cingulara LLC 2019 and Tutela LLC 2019. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007 license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace openrmf_upload_api.Models
{
    [Serializable]
    public class Artifact
    {
        public Artifact () {
        }

        public DateTime created { get; set; }
        public CHECKLIST CHECKLIST { get; set; }
        public string rawChecklist { get; set; }

        // if this is part of a system, list that system group ID here
        public string systemGroupId { get; set; }
        public string systemTitle { get; set; }
        public string hostName { get; set;}
        public string stigType { get; set; }
        public string version {get; set;}
        public string stigRelease { get; set; }
        public string title { get {
            return hostName.Trim() + "-" + stigType.Trim() + "-V" + version + "-" + stigRelease.Trim();
        }}
        
        [BsonId]
        // standard BSonId generated by MongoDb
        public ObjectId InternalId { get; set; }
        public string InternalIdString { get { return InternalId.ToString();}}

        [BsonDateTimeOptions]
        // attribute to gain control on datetime serialization
        public DateTime? updatedOn { get; set; }

        public Guid createdBy { get; set; }
        public Guid? updatedBy { get; set; }

        // v1.7
        public List<string> tags {get; set;}
    }
}