using ApiGateway.Caching.Options;
using ApiGateway.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiGateway.Caching.Middleware
{
    public class ResponseCacheMiddleware
    {
        /*
        esse middleware é mais difícil e diferente!!
        nos outros, eu só precisava interceptar e validar
        aqui, no cache é diferente. auqi eu preciso interceptar a RESPOSTA NA VOLTA DO YARP!! pra guardar no redis!!
        na teoria, blz, é fazível
        na prática... quando eu chamo o await _next(context), o yarp escreve a resposta diretamente no context.responde.body
        que é uma stream que vai direto para o cliente. ou seja, quando eu tento ler depois, ele já foi enviado e tá vazio

        possível solução: substituir temporariamente o stream de resposta por um mempry stream próprio
        o yarp vai escrever no memory stream em vez de mandar para o cliente
        eu leio o ocnteúdo, guardo no redis e copio para a stream real do cliente

        Sem interceptação:
          YARP _> context.Response.body (stream do cliente) -> cliente recebe -> não leio nada
        Com interceptação:
          YARP -> memoryStream (meu) -> leio -> guarda no Redis → copia pro cliente
        */
        private readonly RequestDelegate _next;
        private readonly ICacheProvider _cache;
        private readonly CacheOptions _options;
        private readonly ILogger<ResponseCacheMiddleware> _logger;

        public ResponseCacheMiddleware(RequestDelegate next, ICacheProvider cache, IOptions<CacheOptions> options, ILogger<ResponseCacheMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            /*
            aqui eu to cacheando somente o get
            se for outro tipo de request, sai
            */
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                await _next(context);
                return;
            }

            /*
            aqui eu to verificando se a rota tá ocnfigurada pra cache 
            se não tiver rota configurada pra cache, pula essa parte
            */
            var path = context.Request.Path.Value ?? "/";
            if(!_options.Routes.TryGetValue(path, out var routeOptions))
            {
                await _next(context);
                return;
            }

            /*
            montando a chave!!
            
            */
            var query = context.Request.QueryString.Value ?? string.Empty;
            var key = $"cache:GET?{path}:{query}";

            /*
             aqui é uma situação de cache hit!!
            */
            var cached = await _cache.GetAsync(key);
            if(cached is not null)
            {
                _logger.LogInformation($"Cache hit. Key: {key}");

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                context.Response.Headers["X-Cache"] = "HIT";

                await context.Response.WriteAsync(cached);
                return;
            }

            /*
            aqui seria uma situação de cache miss!! 
            */
            _logger.LogInformation($"Cache miss. Key: {key}");

            var originalBody = context.Request.Body;

            await using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream; //o yarp vai escrever aqui!!

            try
            {
                await _next(context);

                memoryStream.Seek(0, SeekOrigin.Begin);//to lendo o que o yarp escreveu
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

                /*
                aqui eu só vou estar armazenando o cache se a resposta foi bem sucedida 
                */
                if(context.Response.StatusCode == StatusCodes.Status200OK && !string.IsNullOrEmpty(responseBody))
                {
                    var ttl = TimeSpan.FromSeconds(routeOptions.TtlSeconds);
                    await _cache.SetAsync(key, responseBody, ttl);

                    context.Response.Headers["X-Cache"] = "MISS";
                    _logger.LogInformation($"Resposta armazenada no cache. Key: {key}, TTL: {ttl}s");
                }

                /*
                copiando a resposta captirada para o stream real do cliente 
                */
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBody);


            }finally
            {
                //restaurando a stream original!! mesmo se der erroo
                context.Response.Body = originalBody;   
            }
        }
    }
}
