using openstig_upload_api.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace openstig_upload_api.Data {
    public interface IArtifactRepository
    {
        Task<IEnumerable<Artifact>> GetAllArtifacts();
        Task<Artifact> GetArtifact(string id);

        // query after multiple parameters
        //Task<IEnumerable<Artifact>> GetArtifact(string bodyText, DateTime updatedFrom, long headerSizeLimit);

        // add new note document
        Task<Artifact> AddArtifact(Artifact item);

        // remove a single document
        Task<bool> RemoveArtifact(string id);

        // update just a single document
        Task<bool> UpdateArtifact(string id, Artifact body);
    }
}