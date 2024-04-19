namespace APICatalogo.Pagination;

public class ProdutosParameters
{
    const int maxPageSize = 50;
    public int PageNumber { get; set; }
    private int _pageSize;

    public int PageSize
    {
        get
        {
            return _pageSize;
        }
        set
        {
            _pageSize = (value > maxPageSize) ? maxPageSize : value;
        }
    }
}
