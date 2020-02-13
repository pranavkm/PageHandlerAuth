using System;

namespace WebApplication22
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizePageHandlerAttribute : Attribute
    {
        public AuthorizePageHandlerAttribute(string policy = null)
        {
            Policy = policy;
        }

        public string Policy { get; }
    }
}
