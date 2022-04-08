using Microsoft.EntityFrameworkCore;
using SmsHelper.EntityModel;
using System.IO;

namespace SmsHelper.Database
{

    class DbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbSet<SmsRecord> SmsRecords { get; set; }

        public DbContext()
        {
            SQLitePCL.Batteries_V2.Init();

            Database.EnsureCreated();
            Database.Migrate();

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine
                (System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "database.db3");

            optionsBuilder
                .UseSqlite($"Filename={dbPath}");

        }
    }
}