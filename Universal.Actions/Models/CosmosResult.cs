using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Universal.Actions.Models
{
    public class CosmosResult<T>
    {
        public string ContinuationToken { get; set; }
        public IList<T> Items { get; set; }
    }
}
