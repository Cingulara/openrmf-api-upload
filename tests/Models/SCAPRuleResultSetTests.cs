using Xunit;
using openrmf_upload_api.Models;
using System;

namespace tests.Models
{
    public class SCAPRuleResultTests
    {
        [Fact]
        public void Test_NewSCAPRuleResultSetIsValid()
        {
            SCAPRuleResultSet srrs = new SCAPRuleResultSet();
            Assert.True(srrs != null);
            Assert.True(srrs.ruleResults != null);
            Assert.True(srrs.ruleResults.Count == 0);
        }
    
        [Fact]
        public void Test_SCAPRuleResultSetWithDataIsValid()
        {
            SCAPRuleResultSet srrs = new SCAPRuleResultSet();
            srrs.title = "DEGTHATTESTSERVERS";
            srrs.hostname = "DEGTHAT1";
            srrs.ipaddress = "x.x.123.232";

            SCAPRuleResult sr = new SCAPRuleResult();
            sr.ruleId = "1234";
            sr.result = "pass";

            srrs.ruleResults.Add(sr);
            
            // test things out
            Assert.True(srrs != null);
            Assert.True (!string.IsNullOrEmpty(srrs.title));
            Assert.True (!string.IsNullOrEmpty(srrs.hostname));
            Assert.True (!string.IsNullOrEmpty(srrs.ipaddress));
            Assert.True(srrs.ruleResults.Count == 1);
        }
    }
}
