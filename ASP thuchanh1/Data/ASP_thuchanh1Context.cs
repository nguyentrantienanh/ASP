using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ASP_thuchanh1.Models;

namespace ASP_thuchanh1.Data
{
    public class ASP_thuchanh1Context : DbContext
    {
        public ASP_thuchanh1Context (DbContextOptions<ASP_thuchanh1Context> options)
            : base(options)
        {
        }

        public DbSet<ASP_thuchanh1.Models.Book> Book { get; set; } = default!;
    }
}
