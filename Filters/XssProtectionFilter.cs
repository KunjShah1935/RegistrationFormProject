using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Reflection;

namespace RegistrationFormProject.Filters
{
    public class XssProtectionFilter : IActionFilter
    {
        private static readonly string[] DangerousPatterns = new[]
        {
            "<script", "javascript:", "onerror", "onload", "<iframe", "<img", "onmouseover", "onfocus", "onblur", "onclick", "href=\"javascript:",
            "<svg", "<object", "<embed", "<link", "<applet", "<meta", "onchange", "onsubmit", "onmouseenter", "onmouseleave"
        };

        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                if (argument is string strValue)
                {
                    if (IsDangerous(strValue))
                    {
                        RejectRequest(context);
                        return;
                    }
                }
                else
                {
                    if (HasDangerousProperties(argument))
                    {
                        RejectRequest(context);
                        return;
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        private bool IsDangerous(string? value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            var lower = value.ToLowerInvariant();
            foreach (var pattern in DangerousPatterns)
            {
                if (lower.Contains(pattern)) return true;
            }
            return false;
        }

        private bool HasDangerousProperties(object obj)
        {
            var type = obj.GetType();
            if (type.Namespace != null && (type.Namespace.StartsWith("System") || type.Namespace.StartsWith("Microsoft")))
            {
                return false;
            }

            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(string))
                {
                    try
                    {
                        var val = prop.GetValue(obj) as string;
                        if (IsDangerous(val)) return true;
                    }
                    catch
                    {
                        // Ignore property read errors
                    }
                }
            }
            return false;
        }

        private void RejectRequest(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            string referer = context.HttpContext.Request.Headers["Referer"].ToString();

            if (controller != null)
            {
                controller.TempData["Error"] = "Potential security threat detected: invalid characters or script tags in inputs.";
                if (!string.IsNullOrEmpty(referer))
                {
                    context.Result = new RedirectResult(referer);
                }
                else
                {
                    context.Result = new RedirectToActionResult("Index", "Landing", null);
                }
            }
            else
            {
                context.Result = new BadRequestObjectResult("Security threat: XSS payload detected.");
            }
        }
    }
}
