﻿using System.Collections.Generic;

namespace Kodkod.Core.Entities
{
    public class Permission : BaseEntity
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; }
    }
}