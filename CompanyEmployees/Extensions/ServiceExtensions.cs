using Contracts;
using Repository;
using Service;
using Service.Contracts;

using LoggerService;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Versioning;
using CompanyEmployees.Presentation.Controllers;
using Marvin.Cache.Headers;
using AspNetCoreRateLimit;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Entities.ConfigurationModels;
using Microsoft.OpenApi.Models;

namespace CompanyEmployees.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureCors(this IServiceCollection services)
        {
            // CORS - Cross-Origin Request Sharing
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.AllowAnyOrigin()  //WithOrigins("http://obana.ti", "http......")
                           .AllowAnyHeader()  //WithHeader("accept", "content-type",......")
                           .AllowAnyMethod()  //WithMethods("PUT", "GET",.....)
                           .WithExposedHeaders("X-Pagination");
                });
            });
        }
        public static void ConfigureIISIntegration(this IServiceCollection services)
        {
            // default IIS options are fine now
            // (AuthomaticAuthentification = true, AuthentificationDisplayName = null, ForwardClientCertificate = true)
            services.Configure<IISOptions>(options => { });
        }

        public static void ConfigureLoggerService(this IServiceCollection services)
        {
            services.AddSingleton<ILoggerManager, LoggerManager>();
        }

        public static void ConfigureRepositoryManager(this IServiceCollection services)
        {
            services.AddScoped<IRepositoryManager, RepositoryManager>();
        }

        public static void ConfigureServiceManager(this IServiceCollection services)
        {
            services.AddScoped<IServiceManager, ServiceManager>();
        }

        public static void ConfigureSqlContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<RepositoryContext>(opts =>
            {
                opts.UseSqlServer(configuration.GetConnectionString("sqlConnection"));
            });
            // newest version NET 6 RC2 но лишен тонко настраивать db context
            // services.AddSqlServer<RepositoryContext>((configuration.GetConnectionString("sqlConnection")));
        }
        public static IMvcBuilder AddCustomCSVFormatter(this IMvcBuilder builder) =>
            builder.AddMvcOptions(config => 
                    config.OutputFormatters.Add(new CsvOutputFormatter()));

        public static void AddCustomMediaTypes(this IServiceCollection services)
        {
            services.Configure<MvcOptions>(config =>
            {
                var systemTextJsonOutputFormatter = config
                                                        .OutputFormatters
                                                        .OfType<SystemTextJsonOutputFormatter>()?
                                                        .FirstOrDefault();
               
                if(systemTextJsonOutputFormatter != null)
                {
                    systemTextJsonOutputFormatter.SupportedMediaTypes
                        .Add("application/vnd.codemaze.hateoas+json");
                    systemTextJsonOutputFormatter.SupportedMediaTypes
                        .Add("application/vnd.codemaze.apiroot+json");

                }

                var xmlOutputFormatter = config
                                            .OutputFormatters
                                            .OfType<XmlDataContractSerializerOutputFormatter>()?
                                            .FirstOrDefault();

                if(xmlOutputFormatter != null)
                {
                    xmlOutputFormatter.SupportedMediaTypes
                        .Add("application/vnd.codemaze.hateoas+xml");
                    xmlOutputFormatter.SupportedMediaTypes
                        .Add("application/vnd.codemaze.apiroot+xml");
                }
            });
        }

        public static void ConfigureVersioning(this IServiceCollection services)
        {
            // MAIN CONFIGURATION OF VERSIONING
            services.AddApiVersioning(opt =>
            {
                // accepting default version for existing API 1.0
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.DefaultApiVersion = new ApiVersion(1, 0);
               
                // add into response header info about active and deprecated API versions
                // Headers: "api-supported-versions" and "api-deprecated-versions"
                opt.ReportApiVersions = true;

                // for supporting different version schemes by combine diff ways of reading
                // API version (queue: 1. query, 2. request header and 3. media type)
                opt.ApiVersionReader = ApiVersionReader.Combine(
                        new QueryStringApiVersionReader("api-version"),
                        new HeaderApiVersionReader("api-version"),
                        new MediaTypeApiVersionReader("ver")
                    );
                /*
                 setting DefaultApiVerion you must to specify ApiVersionSelector
                 opt.ApiVersionSelector = new CurrentImplementationApiVersionSelector(opt); - select highest version api by default
                 opt.ApiVersionSelector = new DefaultApiSelector(opt); - select opt.DefaultApiVerion (mentioned up)
                */
            });
            
            // ADDITIONAL CONFIGURATION
            services.AddVersionedApiExplorer(opt =>
            {
                // format the version as “‘v’major[.minor][-status]”
                opt.GroupNameFormat = "'v'VVV";
                opt.SubstituteApiVersionInUrl = true;
            });

            // API VERSION CONVENTION
            //services.AddApiVersioning(opt =>
            //{
            //    // we can remove [ApiVersion..] from mentioned down controllers
                
            //    opt.Conventions.Controller<CompaniesController>()
            //        .HasApiVersion(new ApiVersion(1, 0));
            //    opt.Conventions.Controller<CompaniesController>()
            //        .HasDeprecatedApiVersion(new ApiVersion(2, 0));
            //});
        }

        public static void ConfigureResponseCaching(this IServiceCollection services) => 
            services.AddResponseCaching();

        public static void ConfigureHttpCacheHeaders(this IServiceCollection services) =>
            // global settings. may override in controller and action
            services.AddHttpCacheHeaders(
                (expirationOpt) =>
                {
                    expirationOpt.MaxAge = 65;
                    expirationOpt.CacheLocation = CacheLocation.Private;
                },
                (validationOpt) =>
                {
                    validationOpt.MustRevalidate = true;
                });

        public static void ConfigureRateLimitingOptions(this IServiceCollection services)
        {

            var rateLimitRules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Endpoint = "*",
                    //Limit = 3,
                    Limit = 30,
                    Period = "5m"
                }
            };

            services.Configure<IpRateLimitOptions>(opt => { 
                opt.GeneralRules = rateLimitRules; 
            });
            // to store counters and rules
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        }

        public static void ConfigureIdentity(this IServiceCollection services)
        {
            // добавим параметры идентичности для типа E.Models.User через IdentityRole
            var builder = services.AddIdentity<User, IdentityRole>(opt =>
            {
                opt.Password.RequireDigit = true;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequiredLength = 10;
                opt.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<RepositoryContext>()
            .AddDefaultTokenProviders();
        }

        public static void ConfigureJwt(this IServiceCollection services, IConfiguration configuration)
        {
            var secretKey = Environment.GetEnvironmentVariable("SECRET");
            // var jwtSettings = configuration.GetSection("JwtSettings");     
            // configuration binding
     
            var jwtConfiguration = new JwtConfiguration(); 
            configuration.Bind(jwtConfiguration.Section, jwtConfiguration);

            services
                .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                    })
                .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            // сервер - издатель токена актуальный
                            ValidateIssuer = true,
                            // получатель токена одобрен (валидный)
                            ValidateAudience = true,
                            // время действия токена осталось (expired time)
                            ValidateLifetime = true,
                            // ключ подписи токена валиден и одобрен сервером
                            ValidateIssuerSigningKey = true,

                            // we are providing values for the issuer, the audience and secret key for generating jwt signature forJWT
                            /*
                                configuration binding
                             */
                            //ValidIssuer = jwtSettings["validIssuer"],
                            //ValidAudience = jwtSettings["validAudience"],
                            ValidIssuer = jwtConfiguration.ValidIssuer,
                            ValidAudience = jwtConfiguration.ValidAudience,

                            // encrypte secret key 
                            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey))
                        };
                    });
        }

        public static void AddJwtConfiguration(this IServiceCollection services, IConfiguration configuration) =>
            services.Configure<JwtConfiguration>(configuration.GetSection("JwtSettings"));

        public static void ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc("v1", new OpenApiInfo { 
                    Title = "Code Maze Api", 
                    Version = "v1",
                    Description = "CompanyEmployees API by CodeMaze",
                    TermsOfService = new Uri("https://exaple.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "John Doe",
                        Email = "John.Doe@gmail.com",
                        Url = new Uri("https://twitter.com/johndoe")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "CompanyEmployee API LICX",
                        Url = new Uri("https://example.com/license")
                    }
                });
                s.SwaggerDoc("v2", new OpenApiInfo { Title = "Code Maze Api", Version = "v2" });

                // getting methods' description from ///-commets
                var xmlFile = $"{typeof(Presentaion.AssemblyReference).Assembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                s.IncludeXmlComments(xmlPath);

                // activate authorization features in Swagger
                s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Place to add JWT with Bearer",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                s.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Name = "Bearer",
                        },
                        new List<string>()
                    }
                });
            });
        }
    }
}
