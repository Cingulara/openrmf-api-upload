using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using openrmf_upload_api.Classes;
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

            // get the template title from the SCAP to use to grab an empty Checklist
            XmlNodeList title = xmlDoc.GetElementsByTagName("cdf:title");
            if (title != null && title.Count > 0 && title.Item(0).FirstChild != null) {
                // get the title of the STIG so we can ask for the checklist later to fill in
                results.title = title.Item(0).FirstChild.InnerText;
            }

            // get the hostname and other facts off the computer that was SCAP scanned
            XmlNodeList targetFacts = xmlDoc.GetElementsByTagName("cdf:fact");
            if (targetFacts != null && targetFacts.Count > 0 && title.Item(0).FirstChild != null) {
                foreach (XmlNode node in targetFacts) {
                    if (node.Attributes.Count > 1 && node.Attributes[1].InnerText.EndsWith("host_name")) {
                        // grab the Node's InnerText
                        results.hostname = node.InnerText;
                        break; // we found it
                    }
                }
            }

            // get all the rules and their pass/fail results
            XmlNodeList ruleResults = xmlDoc.GetElementsByTagName("cdf:rule-result");
            if (ruleResults != null && ruleResults.Count > 0 && ruleResults.Item(0).FirstChild != null) {
                results.ruleResults = GetResultsListing(ruleResults);
            }
            return results;
        }

        private static List<SCAPRuleResult> GetResultsListing(XmlNodeList nodes) {
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
            string checklistString = NATSClient.GetArtifactByTemplateTitle(results.title);
            // generate the checklist from reading the template in using a Request/Reply to openrmf.template.read
            if (!string.IsNullOrEmpty(checklistString)) {
                // process the raw checklist into the CHECKLIST structure
                CHECKLIST chk = ChecklistLoader.LoadChecklist(checklistString);
                STIG_DATA data;
                SCAPRuleResult result;
                if (chk != null) {
                    // if we read in the hostname, then use it in the Checklist data
                    if (!string.IsNullOrEmpty(results.hostname)) {
                        chk.ASSET.HOST_NAME = results.hostname;
                    }
                    // for each VULN see if there is a rule matching the rule in the 
                    foreach (VULN v in chk.STIGS.iSTIG.VULN) {
                        data = v.STIG_DATA.Where(y => y.VULN_ATTRIBUTE == "Rule_ID").FirstOrDefault();
                        if (data != null) {
                            // find if there is a matching rule
                            result = results.ruleResults.Where(z => z.ruleId.ToLower() == data.ATTRIBUTE_DATA.ToLower()).FirstOrDefault();
                            if (result != null) {
                                // set the status
                                if (result.result.ToLower() == "fail") {
                                    v.STATUS = "Open";
                                } 
                                else if (result.result.ToLower() == "pass") {
                                    v.STATUS = "NotAFinding";
                                }
                            }
                        }
                    }
                }
                System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(chk.GetType());
                using(StringWriter textWriter = new StringWriter())                
                {
                    xmlSerializer.Serialize(textWriter, chk);
                    checklistString = textWriter.ToString();
                }
            }
            // strip out all the extra formatting crap
            System.Xml.Linq.XDocument xDoc = System.Xml.Linq.XDocument.Parse(checklistString, System.Xml.Linq.LoadOptions.None);
            checklistString = xDoc.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
            // return the string
            return checklistString;
        }
    }
}