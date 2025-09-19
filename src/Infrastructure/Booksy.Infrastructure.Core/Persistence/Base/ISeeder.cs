using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.Infrastructure.Core.Persistence.Base
{
    public interface ISeeder
    {
        Task SeedAsync();
    }
}
