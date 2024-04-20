using APICatalogo.Context;
using APICatalogo.Models;
using APICatalogo.Pagination;

namespace APICatalogo.Repositories;

public class ProdutoRepository : Repository<Produto>, IProdutoRepository
{
    // Usa a instancia do context do Repository<T>
    public ProdutoRepository(AppDbContext context) : base(context)
    {
    }
        

    public PagedList<Produto> GetProdutos(ProdutosParameters produtosParameters)
    {
        var produtos = GetAll().OrderBy(p => p.ProdutoId).AsQueryable();
        var produtosOrdenados = PagedList<Produto>.ToPagedList(produtos, produtosParameters.PageNumber, produtosParameters.PageSize);

        return produtosOrdenados;

    }

    public IEnumerable<Produto> GetProdutosPorCategoria(int id)
    {
        var p = _context.Set<Produto>().Where(p => p.CategoriaId == id).AsEnumerable();
        return p;
        
    }
}
