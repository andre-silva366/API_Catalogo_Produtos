using APICatalogo.Models;
using APICatalogo.Pagination;
using X.PagedList;

namespace APICatalogo.Repositories;

public interface IProdutoRepository : IRepository<Produto>
{
    // Obtem produtos páginados
    Task<IPagedList<Produto>> GetProdutosAsync(ProdutosParameters produtoParams);

    // Obtem produtos por categoria
    Task<IEnumerable<Produto>> GetProdutosPorCategoriaAsync(int id);

    // Obtem produtos filtrados por preço
    Task<IPagedList<Produto>> GetProdutosFiltroPrecoAsync(ProdutosFiltroPreco produtosFiltroParams);
}
