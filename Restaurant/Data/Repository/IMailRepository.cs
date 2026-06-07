namespace Restaurant.Data.Repository
{
    public interface IMailRepository: IGenericRepository<Mail>
    {
        Task<List<Mail>> GetMailAsync();
        Task<Mail> GetMailByIdAsync(int id);
        Task<Mail> GetMailByNameAsync(string naam);

        void Add(Mail mail);
        void Update(Mail mail);
        void Remove(Mail mail);
    }
}
