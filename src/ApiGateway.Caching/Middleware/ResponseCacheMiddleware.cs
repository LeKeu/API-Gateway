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

        MAS ESSA SUBSTITUIÇÃO É TIPO UMA GAMBIARRA??
        pelo uq eu li, é uma técnica legítima chamada de RESPONSE BUFFERING!!
        o aspnet foi projetado pra permitir que essa "troca" ocorra, o campo context.response.body 
        é público até para que middlewares possam susbtrituir
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
            var path = context.Request.Path.Value ?? "/"; //ex: "/api/produtos"
            if(!_options.Routes.TryGetValue(path, out var routeOptions))
            {
                await _next(context);
                return;
            }

            /*
            montando a chave!!
            
            */
            var query = context.Request.QueryString.Value ?? string.Empty;
            var key = $"cache:GET:{path}:{query}"; // exempl: "cache:GET:/api/produtos:?ativo=true&pagina=2"

            /*
             aqui é uma situação de cache hit!!
            */
            var cached = await _cache.GetAsync(key);
            if(cached is not null)
            {
                _logger.LogInformation("Cache hit. Key: {Key}", key);
                Console.WriteLine("CACHE HIT");

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                context.Response.Headers["X-Cache"] = "HIT";

                /*
                aqui eu t escrevendo o ocnteúdo diretamente no stream de resposta HTTP e enviando pro cliente, já que é um cachehit
                o postman vai tá recebendo o mesmo json que ele receberia se fosse pelo wiremock, só que n sabe que veio fdo cache
                */
                await context.Response.WriteAsync(cached);
                return;
            }

            /*
            aqui seria uma situação de cache miss!! 
            */
            _logger.LogInformation("Cache miss. Key: {Key}", key);
            Console.WriteLine("CACHE MISS");

            var originalBody = context.Request.Body;

            /*
            um questionamento sobre isso abaixo é, com um fluxo muito alto, teria como isso sofrer uma sobrecarga ou similar?
            pq, querendo ou não, cada request que passa cria um mempry stream em memória. ent se passassem 1000 requests simultãneos, eu teria 1000 memorystreams, non?

            pelo que eu entendi, para respostas pequenas (como o json de lista de produtos) o impacto é bem pichuto
            mas pra respostas maiores (arquivos, relatórios pesados etc), pode ser um problemãlo

            nesse caso, o await using resolver meio que metade disso, pq ele garante que o memory stream é descartado imediatamente quando o block try termina
            ent ele libera memória sem esperar o garbage collector, pelo menos.
            mas ainda assim a meória tá ocupada durante a execução do código

            isso é um dos motivos que eu limito o cache a rotas específicas e não fico armazenando tudo nele
            */
            await using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream; //o yarp vai escrever aqui!!

            try
            {
                /*
                IMPORTANTE!! eu raciocinei errado sobre isso! sobre esse await _next
                pelo que eu saiba, o await _next chama o próximo middleware na fila, oks? oks
                mas esse auqi é diferente! quando eu cxhamo ele DENTRO DE UM TRY eu to pausando a execução do middleware e deixando
                toda  a cadeia sgeuinte rodar até o fim
                só depois que tudo termina é que a linha sgeuinte dele executa
                ent o SEEK e o READTOASYNC rodam depois dque o yarp já temrinou de escrever
                */
                // !!!!!!!!!!!!!!!!!!!!!!!
                await _next(context);
                // !!!!!!!!!!!!!!!!!!!!!!!

                /*
                como que o seek e o readtoasync funcionam??
                ent tá
                o memorstream tem um CURSOR INTERNO, uma posição que indica onde a próxima leitura ou escrita vai ser
                quando o yarp escreve nele, o cursor avança até o final

                depois qe o yarp escreveu:
                [J][S][O][N][ ][d][o][s][ ][p][r][o][d][u][t][o][s]
                                                                     |
                                                               cursor aqui -final)

                ent., se eu tentasse ler agora o streramreader comecaria no atual que já tá no final e ia ler 0 bytes
                o SEEK(0, seekorigin.begin) move o cursor de volta pro início:
                [J][S][O][N][ ][d][o][s][ ][p][r][o][d][u][t][o][s]
                 |
                cursor aqui agora -início)

                aí o READTOENDASYNC lê do início até o fim e eu vou ter o json compleyo
                */
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
                    _logger.LogInformation("Resposta armazenada no cache. Key: {Key}, TTL: {Ttl}s", key, routeOptions.TtlSeconds);
                }

                /*
                copiando a resposta captirada para o stream real do cliente 
                aqui o seek é chamado dnv pq o yarp escreveou na stream, ent ele tem que ler dnv
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
