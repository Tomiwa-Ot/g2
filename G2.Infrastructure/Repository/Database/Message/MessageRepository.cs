using G2.Infrastructure.Repository.Database.Base;

namespace G2.Infrastructure.Repository.Database.Message
{
    public class MessageRepository : DbRepository<Model.Message>, IMessageRepository                     
    {
        public MessageRepository(DbFactory dbFactory) : base(dbFactory) { }
    }
}