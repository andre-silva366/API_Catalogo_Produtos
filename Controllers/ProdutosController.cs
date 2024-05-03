using APICatalogo.DTOs;
using APICatalogo.Models;
using APICatalogo.Pagination;
using APICatalogo.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using X.PagedList;

namespace APICatalogo.Controllers;

[Route("[controller]")]// rota = /produtos
[ApiController]

public class ProdutosController : ControllerBase
{
    private readonly IUnityOfWork _uof;
    private readonly IMapper _mapper;

    public ProdutosController(IUnityOfWork uof, IMapper mapper)
    {
        _uof = uof;
        _mapper = mapper;
    }

    [HttpGet("CategoriaId/{id}")]
    public async Task<ActionResult<IEnumerable<ProdutoDTO>>> GetProdutosCategoriaAsync(int id) 
    {
        var produtos = await _uof.ProdutoRepository.GetProdutosPorCategoriaAsync(id);

        if(produtos is null)
        {
            return NotFound("Nenhum produto encontrado .");
        }

        // var futuro = _mapper.Map<futuro>(atual);
        var produtosDto = _mapper.Map<IEnumerable<ProdutoDTO>>(produtos);

        return Ok(produtosDto);
    }

    // produtos
    
    [HttpGet]
    [Authorize(Policy = "UserOnly")]
    public async Task<ActionResult<IEnumerable<ProdutoDTO>>> GetAsync()
    {
        var produtos = await _uof.ProdutoRepository.GetAllAsync();
        if (produtos == null)
        {
            return NotFound("Produtos não encontrados");
        }

        var produtosDto = _mapper.Map<IEnumerable<ProdutoDTO>>(produtos);

        return Ok(produtosDto);
    }

    // Teste - número de produtos
    [HttpGet("QuantidadeProdutos")]
    public async Task<ActionResult <int>> GetQuantidadeProduto()
    {
        var produtos = await _uof.ProdutoRepository.GetAllAsync();
        return produtos.Count();
    }


    // Pagination
    [HttpGet("pagination")]
    public async Task<ActionResult<IEnumerable<ProdutoDTO>>> GetAsync([FromQuery] ProdutosParameters produtosParameters)
    {
        var produtos = await _uof.ProdutoRepository.GetProdutosAsync(produtosParameters);
        return Ok(ObterProdutos(produtos)); ;
    }

    [HttpGet("filter/preco/pagination")]
    public async Task<ActionResult<IEnumerable<ProdutoDTO>>> GetProdutosFilterPrecoAsync([FromQuery] ProdutosFiltroPreco produtosFilterParameters)
    {
        var produtos = await _uof.ProdutoRepository.GetProdutosFiltroPrecoAsync(produtosFilterParameters);
        return Ok(ObterProdutos(produtos));
    }

    private ActionResult<IEnumerable<ProdutoDTO>> ObterProdutos(IPagedList<Produto> produtos)
    {
        var metadata =  new
        {
            produtos.Count,
            produtos.PageSize,
            produtos.PageCount,
            produtos.TotalItemCount,
            produtos.HasNextPage,
            produtos.HasPreviousPage
        };

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(metadata));
        var produtosDto = _mapper.Map<IEnumerable<ProdutoDTO>>(produtos);

        return Ok(produtosDto);
    }

    // api/produtos/id
    [HttpGet("{id:int:min(1)}", Name = "ObterProduto")]
    public async Task<ActionResult<ProdutoDTO>>  GetByIdAsync(int id)
    {

        var produto = await _uof.ProdutoRepository.GetAsync(p => p.ProdutoId == id);

        if (produto == null)
        {
            return NotFound($"Produto com id: {id} não encontrado");
        }

        var produtoDto = _mapper.Map<ProdutoDTO>(produto);

        return Ok(produtoDto);
    }

    // produtos/id
    [HttpPost]
    public async Task<ActionResult<ProdutoDTO>> PostAsync(ProdutoDTO produtoDto)
    {
        if (produtoDto == null)
        {
            return BadRequest();
        }
        
        // Antes de salvar necessario mapear o produto dto para produto
        var produto = _mapper.Map<Produto>(produtoDto);

        var novoProduto = _uof.ProdutoRepository.Create(produto);
        _uof.CommitAsync();

        var novoProdutoDto = _mapper.Map<ProdutoDTO>(novoProduto);
        return  new CreatedAtRouteResult("ObterProduto", new { id = novoProdutoDto.ProdutoId, novoProdutoDto });
    }

    [HttpPatch("{id}/UpdatePartial")]
    public async Task<ActionResult<ProdutoDTOUpdateResponse>> PatchAsync(int id, JsonPatchDocument<ProdutoDTOUpdateRequest> patchProdutoDTO)
    {
        if (patchProdutoDTO == null || id <= 0) 
        { 
            return BadRequest(); 
        }

        var produto = await _uof.ProdutoRepository.GetAsync(p => p.ProdutoId == id);

        if (produto == null)
            return NotFound();

        var produtoUpdateRequest = _mapper.Map<ProdutoDTOUpdateRequest>(produto);

        // Aplicando as alterações no produto
        patchProdutoDTO.ApplyTo(produtoUpdateRequest);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _mapper.Map(produtoUpdateRequest, produto);
        _uof.ProdutoRepository.Update(produto);
        await _uof.CommitAsync();

        return Ok(_mapper.Map<ProdutoDTOUpdateResponse>(produto));

    }

    // produtos/id
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProdutoDTO>> PutAsync(int id, ProdutoDTO produtoDto)
    {
        if (id != produtoDto.ProdutoId)
        {
            return BadRequest();
        }

        // Converter o produtoDTO para produto antes de atualizar
        var produto = _mapper.Map<Produto>(produtoDto);

        var produtoAtualizado = _uof.ProdutoRepository.Update(produto);
        await _uof.CommitAsync();

        // Converter de produto para produtoDTO para retornar
        var produtoAtualizadoDto = _mapper.Map<ProdutoDTO>(produtoAtualizado);

        return Ok(produtoAtualizadoDto);           

    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ProdutoDTO>> DeleteAsync(int id)
    {
        var produtoDeletado = await _uof.ProdutoRepository.GetAsync(p => p.ProdutoId == id);

        if (produtoDeletado is null)
        {
            return StatusCode(500, $"Falha ao deletar o produto de id = {id}");
            
        }
        else
        {
            _uof.ProdutoRepository.Delete(produtoDeletado);
            await _uof.CommitAsync();

            var produtoDeletadoDto = _mapper.Map<ProdutoDTO>(produtoDeletado);

            return Ok(produtoDeletadoDto);
        }
        
    }
}
