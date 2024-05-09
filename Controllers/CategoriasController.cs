using APICatalogo.DTOs;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Models;
using APICatalogo.Pagination;
using APICatalogo.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Newtonsoft.Json;
using X.PagedList;
namespace APICatalogo.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("[controller]")]
//[EnableCors("OrigensComAcessoPermitido")]
[ApiController]
//[EnableRateLimiting("fixedwindow")]

public class CategoriasController : ControllerBase
{
    private readonly IUnityOfWork _uof;
    private readonly ILogger<CategoriasController> _logger;

    public CategoriasController(IUnityOfWork uof ,ILogger<CategoriasController> logger)
    {
        _logger = logger;
        _uof = uof;
    }

    [HttpGet]
    [DisableRateLimiting]
    //[Authorize]
    public async Task< ActionResult<IEnumerable<CategoriaDTO>>> GetAsync()
    {
        var categorias = await _uof.CategoriaRepository.GetAllAsync();
        if (categorias is null)
        {
            return NotFound("Não foram encontradas categorias cadastradas");
        }
                
        var categoriasDto = categorias.ToCategoriaDTOList();
        return Ok(categoriasDto);
    }

    // Pagination
    [HttpGet("pagination")]
    public async Task<ActionResult<IEnumerable<CategoriaDTO>>> GetCategoriasAsync([FromQuery] CategoriasParameters categoriasParameters)
    {
        var categorias = await _uof.CategoriaRepository.GetCategoriasAsync(categoriasParameters);
        var categoriasDto = categorias.ToCategoriaDTOList().ToList();
        return categoriasDto;
    }

   

    [HttpGet("filter/nome/pagination")]
    public async Task<ActionResult<IEnumerable<CategoriaDTO>>> GetCategoriasFiltradasAsync([FromQuery] CategoriasFiltroNome categoriasFiltro)
    {
        var categoriasFiltradas = await _uof.CategoriaRepository.GetCategoriasFiltroNomeAsync(categoriasFiltro);
        var categoriasDto = categoriasFiltradas.ToCategoriaDTOList().ToList();
        return categoriasDto;

    }

    [DisableCors]
    [HttpGet("{id:int}", Name = "ObterCategoria")]
    public async Task<ActionResult<CategoriaDTO>> GetAsync(int id)
    {
        var categoria = await _uof.CategoriaRepository.GetAllAsync();
        var categoriaPorId = categoria.FirstOrDefault(c => c.CategoriaId == id);

        if (categoriaPorId is null)
        {
            //_logger.LogInformation($"================================== GET api/categoria/id = {id} NOT FOUND ===============");
            return NotFound("Categoria não encontrada...");
                             
        }

        // Fazer um mapeamento para retornar uma categoria DTO
        var categoriaDto = categoriaPorId.ToCategoriaDTO();
        return Ok(categoriaDto);
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaDTO>> PostAsync(CategoriaDTO categoriaDto)
    {
        if (categoriaDto is null)
        {
            return BadRequest("Dados inválidos...");
        }

        // Fazer a conversão de categoriaDto para Categoria
        var categoria = categoriaDto.ToCategoria();

        if(categoria is null)
        {
            return BadRequest("Dados inválidos...");
        }

        var categoriaCriada = _uof.CategoriaRepository.Create(categoria);
        await _uof.CommitAsync();

        var novaCategoriaDto = categoriaCriada.ToCategoriaDTO();

        return new CreatedAtRouteResult("ObterCategoria",
            new { id = novaCategoriaDto.CategoriaId }, novaCategoriaDto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoriaDTO>> PutAsync(int id, CategoriaDTO categoriaDto)
    {
        if (id != categoriaDto.CategoriaId )
        {
            return BadRequest();
        }

        var categoriaAtualizada =  categoriaDto.ToCategoria();
        if(categoriaAtualizada  is null)
        {
            return BadRequest();
        }

        _uof.CategoriaRepository.Update(categoriaAtualizada);
        await _uof.CommitAsync();

        var categoriaAtualizadaDto = categoriaAtualizada.ToCategoriaDTO();
        return Ok(categoriaAtualizadaDto);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CategoriaDTO>> DeleteAsync(int id)
    {
        var categoria = await _uof.CategoriaRepository.GetAsync(c => c.CategoriaId == id);

        if (categoria is null)
        {
            return NotFound("Categoria não encontrada...");
        }

        var categoriaExcluida = _uof.CategoriaRepository.Delete(categoria);
        await _uof.CommitAsync();

        var categoriaExcluidaDto = categoriaExcluida.ToCategoriaDTO();

        return Ok(categoriaExcluidaDto);
    }

    private ActionResult<IEnumerable<CategoriaDTO>> ObterCategorias(IPagedList<Categoria> categorias)
    {
        var metadata = new
        {
            categorias.Count,
            categorias.PageSize,
            categorias.PageCount,
            categorias.TotalItemCount,
            categorias.HasNextPage,
            categorias.HasPreviousPage
        };

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(metadata));

        var categoriasDto = categorias.ToCategoriaDTOList();

        return Ok(categoriasDto);
    }
}
