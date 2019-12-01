using openrmf_upload_api.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace openrmf_upload_api.Data {
    public interface ISystemGroupRepository
    {
        Task<IEnumerable<SystemGroup>> GetAllSystemGroups();
        Task<SystemGroup> GetSystemGroup(string id);

        // add new system document
        Task<SystemGroup> AddSystemGroup(SystemGroup item);

        // remove a system document
        Task<bool> RemoveSystemGroup(string id);

        // update just a single system document
        Task<bool> UpdateSystemGroup(string id, SystemGroup body);
    }
}