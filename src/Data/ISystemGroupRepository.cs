// Copyright (c) Cingulara LLC 2019 and Tutela LLC 2019. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007 license. See LICENSE file in the project root for full license information.
using openrmf_upload_api.Models;
using System.Collections.Generic;
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