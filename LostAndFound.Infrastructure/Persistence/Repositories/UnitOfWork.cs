using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace LostAndFound.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new Repository<User>(_context);
            Roles = new Repository<Role>(_context);
            UserRoles = new Repository<UserRole>(_context);
            Posts = new Repository<Post>(_context);
            Categories = new Repository<Category>(_context);
            SubCategories = new Repository<SubCategory>(_context);
            ChatSessions = new Repository<ChatSession>(_context);
            ChatMessages = new Repository<ChatMessage>(_context);
            ChatParticipants = new Repository<ChatParticipant>(_context);
            Photos = new Repository<Photo>(_context);
            PostImages = new Repository<PostImage>(_context);
        }

        public IRepository<User> Users { get; }
        public IRepository<Role> Roles { get; }
        public IRepository<UserRole> UserRoles { get; }
        public IRepository<Post> Posts { get; }
        public IRepository<Category> Categories { get; }
        public IRepository<SubCategory> SubCategories { get; }
        public IRepository<ChatSession> ChatSessions { get; }
        public IRepository<ChatMessage> ChatMessages { get; }
        public IRepository<ChatParticipant> ChatParticipants { get; }
        public IRepository<Photo> Photos { get; }
        public IRepository<PostImage> PostImages { get; }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
