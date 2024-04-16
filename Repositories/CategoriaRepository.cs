using APICatalogo.Context;
using APICatalogo.Models;
using Microsoft.EntityFrameworkCore;

namespace APICatalogo.Repositories;

public class CategoriaRepository : Repository<Categoria>, ICategoriaRepository
{
    // Usa a instancia do context do Repository<T>
    public CategoriaRepository(AppDbContext context) : base(context)
    {
    }
}
