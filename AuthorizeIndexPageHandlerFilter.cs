﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace WebApplication22
{
    public class AuthorizeIndexPageHandlerFilter : IAsyncPageFilter, IOrderedFilter
    {
        private readonly IAuthorizationPolicyProvider policyProvider;
        private readonly IPolicyEvaluator policyEvaluator;

        public AuthorizeIndexPageHandlerFilter(
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
            if (context.ActionDescriptor.ModelTypeInfo != typeof(Pages.IndexModel))
            {
                throw new NotSupportedException("This only works for IndexModel.");
            }

            if (context.HandlerMethod.MethodInfo != 
                typeof(Pages.IndexModel).GetMethod(nameof(Pages.IndexModel.OnPostAuthorized)))
            {
                return;
            }

            var authorizeAttribute = new AuthorizeAttribute(policy: "YourAuthPolicyHere");
            await AuthoorizeAsync(context, authorizeAttribute);
        }

        #region AuthZ - do not change
        private async Task AuthoorizeAsync(ActionContext actionContext, AuthorizeAttribute authorizeAttribute)
        {
            var policy = await AuthorizationPolicy.CombineAsync(policyProvider, new[] { authorizeAttribute });
            if (policy is null)
            {
                return;
            }

            var httpContext = actionContext.HttpContext;
            var authenticateResult = await policyEvaluator.AuthenticateAsync(policy, httpContext);
            var authorizeResult = await policyEvaluator.AuthorizeAsync(policy, authenticateResult, httpContext, actionContext.ActionDescriptor);
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
        #endregion
    }
}