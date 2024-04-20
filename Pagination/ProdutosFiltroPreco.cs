namespace APICatalogo.Pagination;

public class ProdutosFiltroPreco : QueryStringParameters
{
    public decimal Preco { get; set; }
    public string? PrecoCriterio { get; set; } // "Maior", "Menor" ou "Igual"

}
