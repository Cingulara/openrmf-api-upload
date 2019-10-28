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

    }
}