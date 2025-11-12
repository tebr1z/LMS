using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IMessageRepository : IRepository<Message>
{
    Task<List<Message>> GetMessagesBySenderAsync(int senderId);
    Task<List<Message>> GetMessagesByReceiverAsync(int receiverId);
    Task<List<Message>> GetConversationAsync(int userId1, int userId2);
}

