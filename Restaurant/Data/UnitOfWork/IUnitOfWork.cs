using System;
using System.Threading.Tasks;
using Restaurant.Models;

namespace Restaurant.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        // Specialized repositories
        IReservatieRepository ReservatiesRepository { get; }
        IMailRepository MailRepository { get; }
        IParameterRepository ParametersRepository { get; }
        IProductRepository ProductRepository { get; }
        IBestellingRepository BestellingRepository { get; }
        ITijdslotRepository TijdslotRepository { get; }
        ITafelRepository TafelRepository { get; }

        // Generic repositories
        IGenericRepository<TafelLijst> TafelLijstenRepository { get; }
        IGenericRepository<Sluitingsdag> SluitingsdagenRepository { get; }
        IGenericRepository<Land> LandRepository { get; }

        Task<int> SaveChangesAsync();
    }
}
