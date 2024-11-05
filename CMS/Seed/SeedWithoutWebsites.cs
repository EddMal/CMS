using CMS.Data;
using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMS.Seed
{
    public static class SeedWithoutWebsites
    {
        public static async Task InitAsync(ApplicationDbContext context, ICreateUserService registerService, RoleManager<IdentityRole> roleManager)
        {
            //adds templates we have made in folder if we haven't any values in the template table
            if (!context.Templates.Any())
            {
                var templates = CreateTemplates();
                await context.Templates.AddRangeAsync(templates);
            }

            // Check and seed roles if they don't exist

            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Check and seed test user if they don't exist
            //var testEmail = @"test@test.com";
            //if (!context.Users.Any(u => EF.Functions.Like(u.Email, testEmail)))
            //{
            //    var result = await registerService.CreateUser(testEmail, "pSrkXHN6z8s%KHW@");
            //}
        }

        private static ICollection<Template> CreateTemplates()
        {
            var list = new List<Template>
            {
                new Template
                {
                    TemplateType = "Bild Kort",
                    TemplatePath = "BlazorComponents.HtmlTemplates.TemplatesForComponents.ImageCardTemplate",
                    InputFormPath = "BlazorComponents.HtmlTemplates.InputFormsForTemplates.ImageCardInputForm"
                },
                 new Template
                {
                    TemplateType = "Bild",
                    TemplatePath = "BlazorComponents.HtmlTemplates.TemplatesForComponents.ImageTemplate",
                    InputFormPath = "BlazorComponents.HtmlTemplates.InputFormsForTemplates.ImageInputForm"
                },
                  new Template
                {
                    TemplateType = "Text(MarkDown)",
                    TemplatePath = "BlazorComponents.HtmlTemplates.TemplatesForComponents.TextMarkDownTemplate",
                    InputFormPath = "BlazorComponents.HtmlTemplates.InputFormsForTemplates.TextMarkdownInputForm"
                },
                    new Template
                {
                    TemplateType = "Text(Quill)",
                    TemplatePath = "BlazorComponents.HtmlTemplates.TemplatesForComponents.TextQuillTemplate",
                    InputFormPath = "BlazorComponents.HtmlTemplates.InputFormsForTemplates.TextQuillInputForm"
                },
               
                  new Template
                {
                    TemplateType = "Video",
                    TemplatePath = "BlazorComponents.HtmlTemplates.TemplatesForComponents.VideoTemplate",
                    InputFormPath = "BlazorComponents.HtmlTemplates.InputFormsForTemplates.VideoInputForm"
                },
                    new Template
                {
                    TemplateType = "SidFot",
                    TemplatePath = "BlazorComponents.HtmlTemplates.TemplatesForComponents.FooterTemplate",
                    InputFormPath = "BlazorComponents.HtmlTemplates.InputFormsForTemplates.DoubleInputForm"
                },
                    new Template
                {
                    TemplateType = "SidHuvud",
                    TemplatePath = "BlazorComponents.HtmlTemplates.TemplatesForComponents.HeaderTemplate",
                    InputFormPath = "BlazorComponents.HtmlTemplates.InputFormsForTemplates.DoubleInputForm"
                },
                    new Template
                {
                    TemplateType = "Länk",
                    TemplatePath = "BlazorComponents.HtmlTemplates.TemplatesForComponents.LinkTemplate",
                    InputFormPath = "BlazorComponents.HtmlTemplates.InputFormsForTemplates.DoubleInputForm"
                },
                    new Template
                {
                    TemplateType = "Navigation Bar",
                    TemplatePath = "BlazorComponents.HtmlTemplates.TemplatesForComponents.NavBarTemplate",
                    InputFormPath = "BlazorComponents.HtmlTemplates.InputFormsForTemplates.NavBarInputForm"
                },

            };
            return list;
        }
    }
}
