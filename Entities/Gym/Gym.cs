using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Gym : BaseWithSeoEntity
    {
        public string Title { get; set; }

        public string Address { get; set; }

        public string LogoUrl { get; set; }

        public string Slug { get; set; }
    }
}
