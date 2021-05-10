﻿using System.Threading.Tasks;
using Bit.Core.Models;
using System.Collections.Generic;
using Bit.Core.Models.Data;
using System;

namespace Bit.Core.Services
{
    public interface ICollectionService
    {
        Task SaveAsync(Collection collection, IEnumerable<Models.CollectionMember> users = null);
        Task DeleteAsync(Collection collection);
        Task DeleteUserAsync(Collection collection, Guid organizationUserId);
    }
}
