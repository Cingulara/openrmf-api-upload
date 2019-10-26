using System;
using System.Text;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace openrmf_upload_api.Models
{
    // there is a cdf:TestResult area under the cdf:Benchmark tag
    // read in each cdf:rule_result under the TestResult area
    // there is the *idref* field that matches to the rule Id field from the VULN in each checklist (i.e. SV-78007r1_rule)
    // the *cdf:result* will have pass or fail for that rule
    // save all that rule result data into a list to use for that VULN based on the rule idref field
    // use .Replace(xxx,"") to just get the SV-xxx rule information
    [Serializable]
    public class SCAPRuleResult
    {
        public SCAPRuleResult () {
        }

        public string ruleId { get; set; }
        public string result { get; set; }
    }
}