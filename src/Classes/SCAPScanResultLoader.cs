using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using openrmf_upload_api.Models;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace openrmf_upload_api.Models
{
    // there is a cdf:TestResult area under the cdf:Benchmark tag
    // read in each cdf:rule_result under the TestResult area
    // there is the *idref* field that matches to the rule Id field from the VULN in each checklist (i.e. SV-78007r1_rule)
    // the *cdf:result* will have pass or fail for that rule
    // save all that rule result data into a list to use for that VULN based on the rule idref field
    // use .Replace(xxx,"") to just get the SV-xxx rule information

    public static class SCAPScanResultLoader
    {

        public static SCAPRuleResultSet LoadSCAPScan(string xmlfile) {
            SCAPRuleResultSet results = new SCAPRuleResultSet();
            // get the title of the SCAP scan we are using, which correlates to the Checklist
            xmlfile = xmlfile.Replace("\n","").Replace("\t","");
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlfile);

            XmlNodeList title = xmlDoc.GetElementsByTagName("cdf:title");
            if (title != null && title.Count > 0 && title.Item(0).FirstChild != null) {
                // get the title of the STIG so we can ask for the checklist later to fill in
                results.title = title.Item(0).FirstChild.InnerText;
            }
            // get all the rules and their pass/fail results
            XmlNodeList ruleResults = xmlDoc.GetElementsByTagName("cdf:rule-result");
            if (ruleResults != null && ruleResults.Count > 0 && ruleResults.Item(0).FirstChild != null) {
                results.ruleResults = getResultsListing(ruleResults);
            }
            return results;
        }

        private static List<SCAPRuleResult> getResultsListing(XmlNodeList nodes) {
            List<SCAPRuleResult> ruleResults = new List<SCAPRuleResult>();
            SCAPRuleResult result;
            
            foreach (XmlNode node in nodes) {
                result = new SCAPRuleResult();
                foreach (XmlAttribute attr in node.Attributes) {
                    if (attr.Name == "idref") {
                        result.ruleId = attr.InnerText.Replace("xccdf_mil.disa.stig_rule_","");
                    }
                }
                if (node.ChildNodes.Count > 0) {
                    foreach (XmlElement child in node.ChildNodes) {
                        // switch on the fields left over to fill them in the SCAPRuleResult class 
                        if (child.Name == "cdf:result") {
                                // pass or fail
                                result.result = child.InnerText;
                                break;
                        }
                    }
                }
                ruleResults.Add(result);
            }
            return ruleResults;
        }

        public static string GenerateChecklistData(SCAPRuleResultSet results) {
            string checklistString = "";
            // generate the checklist

            return checklistString;
        }

    }
}