
using Microsoft.EntityFrameworkCore;

namespace taskium.server 
{
    class TaskDb : DbContext
    {
        public static IServiceProvider? SERVICE;

        public TaskDb(DbContextOptions<TaskDb> options) : base(options) 
        {
            this.Database.EnsureCreated();
        }

        public DbSet<Task> Tasks => Set<Task>();

        public static TaskDb GetDBFromNewScope()
        {
            if (SERVICE != null) {
                return SERVICE.GetService<TaskDb>()!;
            } else throw new Exception("SERVICE not set");
        }
    }
}
