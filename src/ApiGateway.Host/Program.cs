using ApiGateway.Auth.Extensions;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseGatewayAuthentication();//colocar antes do yarp!!!


app.MapReverseProxy();

app.Run();
