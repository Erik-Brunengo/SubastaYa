using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Wallet.Queries;

namespace SubastaYa.Application.UseCases.Wallet.Handlers;

public class GetTransactionsQueryHandler
{
    private readonly IWalletQueries _queries;

    public GetTransactionsQueryHandler(IWalletQueries queries) => _queries = queries;

    public async Task<List<TransactionDto>> Handle(GetTransactionsQuery query) =>
        await _queries.GetTransactionsAsync(query.UsuarioId);
}
