using Xunit;
using openrmf_upload_api.Models;
using System;

namespace tests.Models
{
    public class SettingsTests
    {
        [Fact]
        public void Test_NewSettingsIsValid()
        {
            Settings s = new Settings();
            Assert.True(s != null);
        }
    
        [Fact]
        public void Test_SettingsWithDataIsValid()
        {
            Settings s = new Settings();
            s.ConnectionString = "myConnection";
            s.Database = "user=x; database=x; password=x;";

            // test things out
            Assert.True(s != null);
            Assert.True (!string.IsNullOrEmpty(s.ConnectionString));
            Assert.True (!string.IsNullOrEmpty(s.Database));
        }
    }
}
