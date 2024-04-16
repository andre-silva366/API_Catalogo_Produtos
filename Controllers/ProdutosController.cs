using APICatalogo.DTOs;
using APICatalogo.Models;
using APICatalogo.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet("produtos/{id}")]
    public ActionResult<IEnumerable<ProdutoDTO>> GetProdutosCategoria(int id) 
    {
        var produtos = _uof.ProdutoRepository.GetProdutosPorCategoria(id).ToList();

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
    public ActionResult<IEnumerable<ProdutoDTO>> Get()
    {
        var produtos = _uof.ProdutoRepository.GetAll();
        if (produtos == null)
        {
            return NotFound("Produtos não encontrados");
        }

        var produtosDto = _mapper.Map<IEnumerable<ProdutoDTO>>(produtos);

        return Ok(produtosDto);
    }

   // api/produtos/id
    [HttpGet("{id:int:min(1)}", Name = "ObterProduto")]
    public ActionResult<ProdutoDTO>  GetById(int id)
    {
        
        var produto = _uof.ProdutoRepository.Get(p => p.ProdutoId == id);

        if (produto == null)
        {
            return NotFound($"Produto com id: {id} não encontrado");
        }

        var produtoDto = _mapper.Map<ProdutoDTO>(produto);

        return Ok(produtoDto);
    }

    // produtos/id
    [HttpPost]
    public ActionResult<ProdutoDTO> Post(ProdutoDTO produtoDto)
    {
        if (produtoDto == null)
        {
            return BadRequest();
        }
        
        // Antes de salvar necessario mapear o produto dto para produto
        var produto = _mapper.Map<Produto>(produtoDto);

        var novoProduto = _uof.ProdutoRepository.Create(produto);
        _uof.Commit();

        var novoProdutoDto = _mapper.Map<ProdutoDTO>(novoProduto);
        return new CreatedAtRouteResult("ObterProduto", new { id = novoProdutoDto.ProdutoId, novoProdutoDto });
    }

    // produtos/id
    [HttpPut("{id:int}")]
    public ActionResult<ProdutoDTO> Put(int id, ProdutoDTO produtoDto)
    {
        if (id != produtoDto.ProdutoId)
        {
            return BadRequest();
        }

        // Converter o produtoDTO para produto antes de atualizar
        var produto = _mapper.Map<Produto>(produtoDto);

        var produtoAtualizado = _uof.ProdutoRepository.Update(produto);
        _uof.Commit();

        // Converter de produto para produtoDTO para retornar
        var produtoAtualizadoDto = _mapper.Map<ProdutoDTO>(produtoAtualizado);

        return Ok(produtoAtualizadoDto);           

    }

    [HttpDelete("{id:int}")]
    public ActionResult<ProdutoDTO> Delete(int id)
    {
        var produtoDeletado = _uof.ProdutoRepository.Get(p => p.ProdutoId == id);

        if (produtoDeletado is null)
        {
            return StatusCode(500, $"Falha ao deletar o produto de id = {id}");
            
        }
        else
        {
            _uof.ProdutoRepository.Delete(produtoDeletado);
            _uof.Commit();

            var produtoDeletadoDto = _mapper.Map<ProdutoDTO>(produtoDeletado);

            return Ok(produtoDeletadoDto);
        }
        
    }
}
