namespace APICatalogo.Repositories;

public interface IUnityOfWork
{
    IProdutoRepository ProdutoRepository { get; }
    ICategoriaRepository CategoriaRepository { get; }

    Task CommitAsync();
}
