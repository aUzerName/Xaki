﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xaki.AspNetCore.LanguageResolvers;
using Xaki.AspNetCore.ModelBinding;
using Xaki.Configuration;
using Xaki.LanguageResolvers;

namespace Xaki.AspNetCore.Configuration
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds Xaki services to the specified <see cref="IMvcBuilder"/>.
        /// </summary>
        public static IMvcBuilder AddXaki(this IMvcBuilder mvc, XakiOptions xakiOptions)
        {
            mvc.Services.AddXaki(xakiOptions);

            return mvc.AddMvcOptions(options =>
            {
                options.ModelBinderProviders.Insert(0, new LocalizableModelBinderProvider());
            });
        }

        /// <summary>
        /// Adds Xaki to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// </summary>
        public static IApplicationBuilder UseXaki(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(options.Value);

            return app;
        }

        private static IServiceCollection AddXaki(this IServiceCollection services, XakiOptions xakiOptions)
        {
            services.AddHttpContextAccessor();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = xakiOptions
                    .SupportedLanguages
                    .Select(i => new CultureInfo(i))
                    .ToList();

                options.DefaultRequestCulture = new RequestCulture("en", "en");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                // TODO support RouteDataRequestCultureProvider (https://joonasw.net/view/aspnet-core-localization-deep-dive)
            });

            services.AddScoped<IObjectLocalizer>(provider => new ObjectLocalizer
            {
                RequiredLanguages = new HashSet<string>(xakiOptions.RequiredLanguages),
                OptionalLanguages = new HashSet<string>(xakiOptions.OptionalLanguages),
                LanguageResolvers = new List<ILanguageResolver>
                {
                    new RequestLanguageResolver(provider.GetService<IHttpContextAccessor>()),
                    new CultureInfoLanguageResolver(),
                    new DefaultLanguageResolver("en")
                }
            });

            if (xakiOptions.EnablePerCallLocalizeExtensions)
            {
                var provider = services.BuildServiceProvider();
                ObjectLocalizerConfig.Set(() => provider.GetService<IObjectLocalizer>());
            }

            return services;
        }
    }
}
