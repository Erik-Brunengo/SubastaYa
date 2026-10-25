using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.Mappings;
using SubastaYa.Application.UseCases.Auctions.Queries;

namespace SubastaYa.Application.UseCases.Auctions.Handlers
{
    public class GetAuctionByIdQueryHandler
    {
        private readonly IAuctionRepository _repository;

        public GetAuctionByIdQueryHandler(IAuctionRepository repository)
        {
            _repository = repository;
        }

        public async Task<AuctionDto?> Handle(GetAuctionByIdQuery query)
        {
            var subasta = await _repository.ObtenerPorIdAsync(query.Id);
            return subasta?.ToDto();
        }
    }
}
