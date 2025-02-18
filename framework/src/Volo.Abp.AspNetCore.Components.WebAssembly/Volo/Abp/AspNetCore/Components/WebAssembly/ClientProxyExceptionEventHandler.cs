using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Volo.Abp.AspNetCore.Components.Server;
using Volo.Abp.AspNetCore.Components.Web;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Http;

namespace Volo.Abp.AspNetCore.Components.WebAssembly;

public class ClientProxyExceptionEventHandler : ILocalEventHandler<ClientProxyExceptionEventData>, ITransientDependency
{
    protected IServiceProvider ServiceProvider { get; }

    public ClientProxyExceptionEventHandler(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public virtual async Task HandleEventAsync(ClientProxyExceptionEventData eventData)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            switch (eventData.StatusCode)
            {
                case 401:
                {
                    var options = scope.ServiceProvider.GetRequiredService<IOptions<AbpAspNetCoreComponentsWebOptions>>();

                    if (!options.Value.IsBlazorWebApp)
                    {
                        var navigationManager = scope.ServiceProvider.GetRequiredService<NavigationManager>();
                        var accessTokenProvider = scope.ServiceProvider.GetRequiredService<IAccessTokenProvider>();
                        var authenticationOptions = scope.ServiceProvider.GetRequiredService<IOptions<AbpAuthenticationOptions>>();
                        var result = await accessTokenProvider.RequestAccessToken();
                        if (result.Status != AccessTokenResultStatus.Success)
                        {
                            navigationManager.NavigateToLogout(authenticationOptions.Value.LogoutUrl);
                            return;
                        }

                        result.TryGetToken(out var token);
                        if (token != null && DateTimeOffset.Now >= token.Expires.AddMinutes(-5))
                        {
                            navigationManager.NavigateToLogout(authenticationOptions.Value.LogoutUrl);
                        }
                    }
                    else
                    {
                        var jsRuntime = scope.ServiceProvider.GetRequiredService<IJSRuntime>();
                        await jsRuntime.InvokeVoidAsync("eval", "setTimeout(function(){location.assign('/')}, 2000)");
                    }

                    break;
                }
                case 403:
                {
                    var jsRuntime = scope.ServiceProvider.GetRequiredService<IJSRuntime>();
                    await jsRuntime.InvokeVoidAsync("eval", "setTimeout(function(){location.assign('/')}, 2000)");

                    break;
                }
            }
        }
    }
}
