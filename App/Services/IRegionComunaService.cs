using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Services
{
    public interface IRegionComunaService
    {
        Task<List<string>> GetRegionesAsync();
        Task<List<string>> GetComunasAsync(string regionNombre);
    }
}
