using System.Linq.Expressions;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManager.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await DbSet.FindAsync(id);

    public async Task<T?> GetByLongIdAsync(long id) => await DbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await DbSet.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await DbSet.Where(predicate).ToListAsync();

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate) =>
        await DbSet.FirstOrDefaultAsync(predicate);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) =>
        await DbSet.AnyAsync(predicate);

    public async Task<T> AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities) =>
        await DbSet.AddRangeAsync(entities);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) =>
        predicate == null ? await DbSet.CountAsync() : await DbSet.CountAsync(predicate);
}
