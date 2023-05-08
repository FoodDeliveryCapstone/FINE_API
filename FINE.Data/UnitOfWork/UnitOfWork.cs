using FINE.Data.Entity;
using FINE.Data.Repository;

namespace FINE.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FineDevDbContext _context;

        public UnitOfWork(FineDevDbContext context)
        {
            _context = context;
        }

        private readonly Dictionary<Type, object> reposotories = new Dictionary<Type, object>();

        public IGenericRepository<T> Repository<T>()
            where T : class
        {
            Type type = typeof(T);
            if (!reposotories.TryGetValue(type, out object value))
            {
                var genericRepos = new GenericRepository<T>(_context);
                reposotories.Add(type, genericRepos);
                return genericRepos;
            }
            return value as GenericRepository<T>;
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int Commit()
        {
            return _context.SaveChanges();
        }

        public Task<int> CommitAsync() => _context.SaveChangesAsync();
    }
}