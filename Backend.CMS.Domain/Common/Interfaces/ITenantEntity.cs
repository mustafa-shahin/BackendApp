using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.CMS.Domain.Common.Interfaces
{
    public interface ITenantEntity
    {
        string TenantId { get; set; }
    }
}
