using APICatalogo.Models;
using APICatalogo.Pagination;

namespace APICatalogo.Repositories;

public interface IProdutoRepository : IRepository<Produto>
{
    // Obtem produtos páginados
    PagedList<Produto> GetProdutos(ProdutosParameters produtoParams);

    // Obtem produtos por categoria
    IEnumerable<Produto> GetProdutosPorCategoria(int id);

    // Obtem produtos filtrados por preço
    PagedList<Produto> GetProdutosFiltroPreco(ProdutosFiltroPreco produtosFiltroParams);
}
