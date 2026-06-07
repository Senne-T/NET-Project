namespace Restaurant.Data.Repository
{
    public class MailRepository : GenericRepository<Mail>, IMailRepository
    {
        public MailRepository(RestaurantContext context) : base(context)
        {
        }

        public async Task<List<Mail>> GetMailAsync()
        {
            return await _context.Mails.ToListAsync();
        }

        public async Task<Mail> GetMailByIdAsync(int id)
        {
            var mail = await _context.Mails.FindAsync(id);
            return mail ?? throw new KeyNotFoundException($"Mail met id {id} niet gevonden");
        }

        public void Add(Mail mail)
        {
            _context.Mails.Add(mail);
        }

        public void Update(Mail mail)
        {
            _context.Mails.Update(mail);
        }

        public void Remove(Mail mail)
        {
            _context.Mails.Remove(mail);
        }

        public async Task<Mail> GetMailByNameAsync(string naam)
        {
            var mail = await _context.Mails.FirstOrDefaultAsync(m => m.Naam == naam);
            return mail ?? throw new KeyNotFoundException($"Mail met naam {naam} niet gevonden");
        }
    }
}
