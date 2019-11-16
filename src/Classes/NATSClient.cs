using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using NATS.Client;
using openrmf_upload_api.Models;
using Newtonsoft.Json;

namespace openrmf_upload_api.Classes
{
    public static class NATSClient
    {        
        /// <summary>
        /// Get a single checklist back by passing the ID.
        /// </summary>
        /// <param name="title">The title of the Template for the checklist.</param>
        /// <returns></returns>
        public static string GetArtifactByTemplateTitle(string title)
        {
            string rawChecklist = "";
            // Create a new connection factory to create a connection.
            ConnectionFactory cf = new ConnectionFactory();

            // Creates a live connection to the default NATS Server running locally
            IConnection c = cf.CreateConnection(Environment.GetEnvironmentVariable("NATSSERVERURL"));

            Msg reply = c.Request("openrmf.template.read", Encoding.UTF8.GetBytes(title), 3000); // publish to get this Artifact checklist back via ID
            c.Flush();
            // save the reply and get back the checklist score
            if (reply != null) {
                rawChecklist = Compression.DecompressString(Encoding.UTF8.GetString(reply.Data));
            }
            c.Close();
            return rawChecklist;
        }

        /// <summary>
        /// Return a checklist raw string based on the ID requested. Uses a request/reply 
        /// method to get a checklist.
        /// </summary>
        /// <param name="id">The id of the checklist record to return</param>
        /// <returns>A checklist raw XML string, if found</returns>
        private static string GetChecklist(string id){
            try {
                // Create a new connection factory to create a connection.
                ConnectionFactory cf = new ConnectionFactory();

                // Creates a live connection to the default NATS Server running locally
                IConnection conn = cf.CreateConnection(Environment.GetEnvironmentVariable("NATSSERVERURL"));
                Artifact art = new Artifact();
                Msg reply = conn.Request("openrmf.checklist.read", Encoding.UTF8.GetBytes(id), 3000); // publish to get this Artifact checklist back via ID
                // save the reply and get back the checklist to score
                if (reply != null) {
                    art = JsonConvert.DeserializeObject<Artifact>(Compression.DecompressString(Encoding.UTF8.GetString(reply.Data)));
                    return art.rawChecklist;
                }
                return art.rawChecklist;
            }
            catch (Exception ex) {
                Console.WriteLine(string.Format("openrmf-msg-score Error in GetChecklist with Artifact id {0}. Message: {1}",
                    id, ex.Message));
                throw ex;
            }
        }

    }
}