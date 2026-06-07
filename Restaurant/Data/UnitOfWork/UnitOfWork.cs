using AutoMapper;
using Microsoft.Extensions.Logging;
using Restaurant.Data.Repository;
using System;
using System.Threading.Tasks;
using Restaurant.Models;

namespace Restaurant.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        // Dependencies
        private readonly RestaurantContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ReservatieRepository> _reservatieLogger;

        // Specialized repositories
        private readonly IProductRepository productRepository;
        private readonly IReservatieRepository reservatiesRepository;
        private readonly IMailRepository mailRepository;
        private readonly IParameterRepository parametersRepository;
        private readonly IBestellingRepository bestellingRepository;
        private readonly ITijdslotRepository tijdslotRepository;
        private readonly ITafelRepository tafelRepository;

        // Generic repositories
        private readonly IGenericRepository<TafelLijst> tafelLijstenRepository;
        private readonly IGenericRepository<Sluitingsdag> sluitingsdagenRepository;
        private readonly IGenericRepository<Land> landRepository;

        public UnitOfWork(
            RestaurantContext context,
            IMapper mapper,
            ILogger<ReservatieRepository> reservatieLogger)
        {
            _context = context;
            _mapper = mapper;
            _reservatieLogger = reservatieLogger;

            // Specialized
            reservatiesRepository = new ReservatieRepository(_context, _mapper, _reservatieLogger);
            mailRepository = new MailRepository(_context);
            parametersRepository = new ParameterRepository(_context);
            productRepository = new ProductRepository(_context);
            tijdslotRepository = new TijdslotRepository(_context);
            tafelRepository = new TafelRepository(_context);
            bestellingRepository = new BestellingRepository(_context, _reservatieLogger);

            // Generic
            tafelLijstenRepository = new GenericRepository<TafelLijst>(_context);
            sluitingsdagenRepository = new GenericRepository<Sluitingsdag>(_context);
            landRepository = new GenericRepository<Land>(_context);
        }

        // Implementatie van IUnitOfWork

        public IProductRepository ProductRepository => productRepository;
        public IReservatieRepository ReservatiesRepository => reservatiesRepository;
        public IMailRepository MailRepository => mailRepository;
        public IParameterRepository ParametersRepository => parametersRepository;
        public IBestellingRepository BestellingRepository => bestellingRepository;

        public ITijdslotRepository TijdslotRepository => tijdslotRepository;
        public ITafelRepository TafelRepository => tafelRepository;

        public IGenericRepository<TafelLijst> TafelLijstenRepository => tafelLijstenRepository;
        public IGenericRepository<Sluitingsdag> SluitingsdagenRepository => sluitingsdagenRepository;
        public IGenericRepository<Land> LandRepository => landRepository;

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
