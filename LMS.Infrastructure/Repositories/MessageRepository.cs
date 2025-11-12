using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class MessageRepository : EfRepository<Message>, IMessageRepository
{
    public MessageRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<Message>> GetMessagesBySenderAsync(int senderId)
    {
        return await _dbSet
            .Where(m => m.SenderId == senderId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<List<Message>> GetMessagesByReceiverAsync(int receiverId)
    {
        return await _dbSet
            .Where(m => m.ReceiverId == receiverId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<List<Message>> GetConversationAsync(int userId1, int userId2)
    {
        return await _dbSet
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                       (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }
}

