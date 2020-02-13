using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;
using System.Threading.Tasks;

namespace WebApplication22
{
    public class AuthorizePageHandlerFilter : IAsyncPageFilter, IOrderedFilter
    {
        private readonly IAuthorizationPolicyProvider policyProvider;
        private readonly IPolicyEvaluator policyEvaluator;

        public AuthorizePageHandlerFilter(
            IAuthorizationPolicyProvider policyProvider,
            IPolicyEvaluator policyEvaluator)
        {
            this.policyProvider = policyProvider;
            this.policyEvaluator = policyEvaluator;
        }

        // Run late in the selection pipeline
        public int Order => 10000;

        public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next) => next();

        public async Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            var attribute = context.HandlerMethod?.MethodInfo?.GetCustomAttribute<AuthorizePageHandlerAttribute>();
            if (attribute is null)
            {
                return;
            }

            var policy = await AuthorizationPolicy.CombineAsync(policyProvider, new[] { new AuthorizeAttribute(attribute.Policy) });
            if (policy is null)
            {
                return;
            }

            var authenticateResult = await policyEvaluator.AuthenticateAsync(policy, context.HttpContext);
            var authorizeResult = await policyEvaluator.AuthorizeAsync(policy, authenticateResult, context.HttpContext, context.ActionDescriptor);
            var httpContext = context.HttpContext;
            if (authorizeResult.Challenged)
            {
                if (policy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await httpContext.ChallengeAsync(scheme);
                    }
                }
                else
                {
                    await httpContext.ChallengeAsync();
                }

                return;
            }
            else if (authorizeResult.Forbidden)
            {
                if (policy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await httpContext.ForbidAsync(scheme);
                    }
                }
                else
                {
                    await httpContext.ForbidAsync();
                }

                return;
            }
        }
    }
}
