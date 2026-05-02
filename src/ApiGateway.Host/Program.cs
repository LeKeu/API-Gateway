using ApiGateway.Auth.Extensions;
using ApiGateway.Caching.Extensions;
using ApiGateway.RateLimiting.Extensions;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddGatewayRateLimiting(builder.Configuration);
builder.Services.AddGatewayCaching(builder.Configuration);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseGatewayRateLimiting();// primeiro!! rate limit antes de autenticar
app.UseGatewayAuthentication();//segundo! autentyica quem passou
app.UseGatewayCaching();// parte do cache!!
app.MapReverseProxy();//roteia quem foi autenticado!

app.Run();
