using NLog;
// for our custom IServiceCollection extensions
using CompanyEmployees.Extensions;
// for ForwardedHeaders:enum
using Microsoft.AspNetCore.HttpOverrides;
// Domain Layer Contracts
using Contracts;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using CompanyEmployees.Presentaion.ActionFilters;
using Shared.DTO;
using Service.DataShaping;
using CompanyEmployees.Presentation.ActionFilters;
using CompanyEmployees.Utility;
using AspNetCoreRateLimit;
// THE MOST IMPLEMENT EXAMPLES RELATED WITH EMPLOYEE-SPECIFIC SOLUTION
var builder = WebApplication.CreateBuilder(args);

LogManager.LoadConfiguration(string.Concat(Directory.GetCurrentDirectory(), "/nlog.config"));
//===============================================
builder.Services.ConfigureCors();
builder.Services.ConfigureIISIntegration();
builder.Services.ConfigureLoggerService();
builder.Services.ConfigureRepositoryManager();
builder.Services.ConfigureServiceManager();
builder.Services.ConfigureSqlContext(builder.Configuration);
builder.Services.ConfigureVersioning();
builder.Services.ConfigureResponseCaching();
builder.Services.ConfigureHttpCacheHeaders();

builder.Services.AddAuthentication();
builder.Services.ConfigureIdentity();
builder.Services.ConfigureJwt(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);

builder.Services.ConfigureSwagger();

// rate limiting: use memory cache to store its counters and rules
builder.Services.AddMemoryCache();
// rate limiting: configure rules
builder.Services.ConfigureRateLimitingOptions();
// rate limiting: access to request response http context
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

//===============================================
// Это в первую очередь API, что им будет пользоваться похер http, gRPC и пр.
// Контроллеры здесь просто по умолчанию
// Здесь мы вывели в В Презентационый слой на наши кастомные контроллеры.
// Теперь API знает, куда направлять запросыи фреймворк из настроит! 
builder.Services
    .AddControllers(config =>
    {
        config.RespectBrowserAcceptHeader = true; // content negotiation
        config.ReturnHttpNotAcceptable = true; // content negotiation
        config.InputFormatters.Insert(0, GetJsonPatchInputFormatter()); // for handling PATCH requests
        config.CacheProfiles.Add("120SecondsDuration", new CacheProfile { Duration = 120 }); // caching
    })
    .AddXmlDataContractSerializerFormatters()
    .AddCustomCSVFormatter()
    .AddApplicationPart(typeof(CompanyEmployees.Presentaion.AssemblyReference).Assembly);

builder.Services.AddCustomMediaTypes();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddScoped<ValidationFilterAttribute>();
builder.Services.AddScoped<ValidateMediaTypeAttribute>();
builder.Services.AddScoped<IDataShaper<EmployeeDTO>, DataShaper<EmployeeDTO>>();
builder.Services.AddScoped<IEmployeeLinks, EmployeeLinks>();

// builder.Build() implements
// IHost(start stop host)
// IApplicationBuilder (make request pipeline) <- += all MW
// IEndpointRouteBuilder (use endpoints to out app) <- += all endpoints (from diff ways, etc. action controllers)
var app = builder.Build();
// this moment app builded and all services registered (add in IoC)

var logger = app.Services.GetRequiredService<ILoggerManager>();
app.ConfigureExceptionHandler(logger);

if (app.Environment.IsDevelopment())
    // inform client using only https!
    app.UseHsts(); 
// MW - forced redirect HTTP requests to HTTPS requests 
app.UseHttpsRedirection();
// if don't set our path to static files, wwwroot folder by default
app.UseStaticFiles(); 
// forward proxy headers to current request(fine for development)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All
});

app.UseIpRateLimiting();
app.UseCors("CorsPolicy");
app.UseResponseCaching();
app.UseHttpCacheHeaders();

app.UseAuthentication();
app.UseAuthorization();

// add endpoints to actions methods of controllers by default without specifing any routes.
// app.UseRouting is not needed 
// usual use attribute routing!!
app.MapControllers();
// start app and block calling thread until the host shutdown

app.UseSwagger();
app.UseSwaggerUI(s =>
{
    s.SwaggerEndpoint("/swagger/v1/swagger.json", "Code Maze API v1");
    s.SwaggerEndpoint("/swagger/v2/swagger.json", "Code Maze API v2");
});

app.Run();

// for patchDoc during PATCH-request handling in controller
NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter() =>
    new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .AddNewtonsoftJson()
                .Services.BuildServiceProvider()
                                .GetRequiredService<IOptions<MvcOptions>>()
                                .Value
                                .InputFormatters
                                    .OfType<NewtonsoftJsonPatchInputFormatter>().First();
#region DI IoC (INFO)
/*
      Dependency injection is a technique we use to achieve the decoupling of
    objects and their dependencies. It means that rather than instantiating an
    object explicitly in a class every time we need it, we can instantiate it
    once and then send it to the class.
     This is often done through a constructor. The specific approach we
    utilize is also known as the Constructor Injection.
     
        In a system that is designed around DI, you may find many classes
    requesting their dependencies via their constructors. In this case, it is
    helpful to have a class that manages and provides dependencies to
    classes through the constructor.
     
        (IoC) These classes are referred to as containers or more specifically, Inversion
    of Control containers. An IoC container is essentially a factory that is
    responsible for providing instances of the types that are requested from
    it.
 */
#endregion
#region Onion Architecture (INFO)
/*
    Независимая разработка каждого слоя своей командой, тестирование через Moq
    Инкапсуляция бизнес-логики (DL SL) без знаний о реализации
    Про КАК слои ВЗАИМОДЕСТВУЮТ: domain layer (DL), service layer (SL), presentatin layer (PL), infrastruture layer (IL) 
    Концептуально PL и Il на ОДНОМ УРОВНЕ: PL + IL --> SL --> DL. Направление зависимостей к ядру (DL)
    
    ВСЕ СЛОИ (ЕСЛИ ИМ НУЖНЫ УСЛУГИ ИЗ ДРУГОГО СЛОЯ) МОГУТ ВЗАИМОДЕЙСТВОВАТЬ ЧЕРЕЗ ИНТЕРФЕЙСЫ, ОПРЕДЕЛЕННЫЕ ТОЛЬКО НА СЛОЕ НИЖЕ. 
    напр, CEP -> SL.Contracts, SL.Contracts --> E + Sh
        Благодаря IoC мы зависим от абстракций только во время компиляции (дает нам строгие контракты для логики),
        а реализация предоставляется во время выполнения.
    НИЗ (интерфейс контракт) ВЕРХ (реализация)
    DL (ядро) - бизнес логика, транзакционная целостность. ПОЛНАЯ ИЗОЛЯЦИЯ ОТ ВНЕШНИХ СЛОЕВ
  

    SL - про убрать бизнес-логику из контроллеров и PL
    
    Repository pattern (DAL) - слой абстракции между data access (IL) и бизнес-логикой (DL) (class lib, хранение состояния модели) 
 */
#endregion
#region BEGIN HERE) CONFIGURATION 1, 2 - about Program.cs
/*
    Begining
    web.config not use anymore
    - create main project CE (companyEmployees) (NET 6, HTTPS, use Controllers)
    - lauchSettings.json (launchBrowser: false everywhere, we test in Postman)
    - create C (Contracts) (class lib) and LS (LoggerService - impl logging) (class lib)
    - add proj deps: C <--- LS <---- СE (CE know C throu LS)
    create main log interface
    - add C.ILoggerManager.cs (info, error, debug, warn log messages)
    - add LS nuget>NLog
    - add LS.LoggerManager : ILoggerMaanger
        (Ilogger and LogManager from NLog namespace, LS.LoggerManager is wrap for it..)
    config (NLog need information: where to keep log file in fs, name log file, min level of logging...)
    - add New Item -> TextFile -> CE.nlog.config
    - modify CE.Program.cs with prev configuring log service LogManager.LoadConfiguration(str....
    - register log service in SE (CE.ServiceExtentions.ConfigureLoggerService) like Singleton (1 logS for all requests)
    - modify CE.Program with builder.Services.CofigureLoggerService
   
    NOTE: : If you want to have more control over the log output, we suggest
            renaming the current file to nlog.development.config and creating another
            configuration file called nlog.production.config. Then you can do something like
            this in the code: env.ConfigureNLog($"nlog.{env.EnvironmentName}.config");
            to get the different configuration files for different environments. From our
            experience production path is what matters, so this might be a bit redundant.
    
    after start app we get 2 folder (CE.internallogs and in CE//bin/debug/logs) all by nlog.config

    we you need logging inject logger service into our class by DI -> Constructor Injection
 */
#endregion
#region CREATE MODEL AND REPOSITORY PATTERN AND SERVICE LAYER 3
/*
    Т.к. API сразу создаем модель. Начинаем проект после подготоки здесь!
    CREATE MODELS    
    - create Entities (class lib) (E)
    - add E.Models (содержит все классы сущностей (мапы на БД))
    - add E.Models.[Company.cs, Employee.cs] (pay attention navigate props)
    - create Repository (class lib)(R) + M.EFCore(nuget)
    - add R ---> E  

    CREATE DB CONTEXT WITH START INIT
    - add R.RespositoryContext.cs:DbContext
    - add ConnnectionString into appSettings.json
    - add CE ---> R
    - add CE.ContextFactory.RepositoryContextFactory.cs : IDesignTimeRepositoryContextFactory<out TContext>
            (для получения экземпляра RepositoryContext при запуске миграций из консоли) (for disign time)
    - add CE + M.EFCore.Tools for migrations cli-commands
    - from CE : PE > Add-Migrations DatabaseCreation, 
                PM > Update-Database (Консоль диспетчера пакетов)
    - populate some initial data
        add R.Configuration.[CompanyConfiguration|EmployeeConfiguration].cs
        add them in R.RepositoryContext.OnModelCreating()
        seed this data : PM > Add-Migration InitialData PM > Update-Database

    CREATE REPOSITORY (DAL)
    - create shared repo interface C.IRepositoryBase<T> (for separet logic is common for all and specific itself)
    - add R ----> C
    - add abstract R.RepositoryBase : IRespositoryBase<T> where T : class
        generic T - no concrete model class. ITS BEHAVIOR PROPER FOR ANY REPO IMPLS
        trackChages - improve query speed
    - add C.[IEmployeeRepository | ICompanyRepository].cs
    - add R.[CompanyRepository | EmployeeRepository] : RepositoryBase<[Company|Employee]>, [ICompanyRepository | IEmployeeRespository].cs
    we need to combine repo logic from several or more classes. 

    CREATE REPOSITORY MANAGER
    - add C.IRepositoryManager
    - add R.RepositoryManagerr : IRepositoryManager
        SAVE CHANGES here! for fix changes from several classes
        LAZY LOADING here! for eject only needed classes for query
    - register our repo manager SE.ConfigureRepositoryManager
    - call it service in CE.Program.cs

    SERVICE LAYER (SL)
        SL split into two projects (S.Contracts and S)
    - create Service (S) and Service.Contracts (SC) (class libs) (INCAPSULATE MAIN BUSINESS LOGIC)
    - add SC.[IEmployeeService | ICompanyService | IServiceManager].cs
    - add S.[EmployeeService | CompanyService | ServiceManager].cs
        ctor(IRepositoryManager | ILoggerManager)
        Lazy loading
    - register SE.ConfigureServiceManager
    - register SE.ConfigureSqlContext (for runtime, its needed for ConfigureServiceManager. Don't need to specify MigrationAssembly)   
    - call in CE.Program.cs
 */
#endregion
#region HANDLING GET REQUEST AND PRESENTATION LAYER 4    
/*
     PL - its entry point for consumers
     Because ASP.NET Core uses Dependency Injection everywhere, we need to have a reference to all of the projects in the solution from the main project. 
     This allows us to configure our services inside the Program class. We can implement this layer in many ways, for example creating a REST API, gRPC, etc.

      S and SC is parts of this puzzle
    - create CompanyEmployees.Presentation (class lib) (CEP)
    - add CEP nuget package for ControllerBase.cs (NET 6 - Microsoft.AspNetCore.Mvc.Core)
    - add AssemblyReference.cs (its ref for CE)
    - add CEP ---> SC
    - add CE ---> CEP by CE.Program.cs (....AddApplicationPart...)
            without it app won't knew where to route requests. App will find all Controllers in CEP
    - add CEP.Controllers.CompanyController:ControllerBase
        Attribute routing
    
    [Route] RESOURCE NAMING
        The resource name in the URI should always be a noun and not an action.
    That means if we want to create a route to get all companies, we should
    create this route: api/companies and not this one:
            /api/getCompanies.

        The noun used in URI represents the resource and helps the consumer to
    understand what type of resource we are working with. So, we shouldn’t
    choose the noun products or orders when we work with the companies
    resource; the noun should always be companies. Therefore, by following
    this convention if our resource is employees (and we are going to work
    with this type of resource), the noun should be employees.
        Another important part we need to pay attention to is the hierarchy
    between our resources. In our example, we have a Company as a
    principal entity and an Employee as a dependent entity. When we create
    a route for a dependent entity, we should follow a slightly different
    convention:
             /api/principalResource/{principalId}/dependentResource.
        Because our employees can’t exist without a company, the route for the
    employee's resource should be
             /api/companies/{companyId}/employees.
    With all of this in mind, we can start with the Get requests.

    GET - REQUEST (Getting All Companies)
        p.s. Getting all the entities from the database is a bad idea)  
    - add C.ICompanyRepository.GetAllCompanies(bool trackChanges)
    - add R.CompanyRepository.GetAllCompanies(bool trackChanges) -> RepositoryBase.FindAll...
    - add SC.ICompanyService.GetAllCompanies(bool trackChanges)
    - add S.CompanyService.GetAllACompanies(bool trackChanges)
    - add CEP.CompanyController.GetAllCompanies().. look code

    - create Shared (Sh) (class lib) (DTO - data transfer objects, return only needed immutable data (no validation need), can manipulate needed props)
    - add Sh.DTO.CompanyDTO (record (C# 9 ref type, record struct (value type)) - eay way create immutable data)
    - remove SC ---> E
    - add SC ----> Sh
    - modify SC.ICompanyService.GetAllCompanies to return IEnumerable<CompanyDTO>
    - same for S.CompanyService.GetAllACompanies
    
    AUTOMAPPER (remove code for manual mapping from SL)

    
    - add S --> Automapper (PM> Install-Package AutoMapper.Extensions.Microsoft.DependencyInjection)
    - register Automapper in CE.Program.cs AddAutoMapper
    - add MappingProfile class (Auomapper work from ctor ForMember ForCtorMember look)
    - apply mapper in S:
        - add IMapper _mapper in S.[CompanyService | EmployeeService]
        - modify S.CompanyService.GetAllCompanies with _mapper.Map
        
*/
#endregion
#region Centralized Exception Handling (asp net core build-in) 5   
/*
    1. add E.ErrorModel.ErrorDetails.cs 
    2. add CE.Extensions.ExceptionMiddleWareExtension.cs in 
    3. modify Program.cs with code below
    4. remove app.UseDeveloperExceptionPage if exists (usual below in if(app.Env..IsDev(.
    5. remove try-catch blocks from 
        S.CompanyService.GetAllCompanies() and
        CEP.CompaniesController.GetCompanies() and other same
      test postman
    6. add abstract base E.Exeptions.NotFoundException
    7. add E.Exceptions.CompanyNotFoundException:NotFoundException
    8. update S.CompanyService.GetCompanyById() (add throw not found exception)
    9. update CE.Extextions.ExceptionMiddlewareExtension (add contextFeature.Error switch.. and contextFeature.Error.Message below.)
 */
#endregion
#region Flow GET-requests 6   
/*
 * 1.  remove all presentation code folder from main project
 * 2.  create separate presention project CEP.Controllers
 * 3.  add AssemblyReference.cs for routing knows where to redirect requests 
 *     (look at Program.cs:22 AddApplicationPart up)
 * 4.  add CEP.Controllers.EmployeeController -> ctor(SC.IServiceManager...)
 * 5.  add C.IEmployeeRepository.GetEmployees T from E.Entities
 * 6.  impl R.EmployeeRepository.GetEmployees()
 * 7.  for service add Sh.DTO.EmployeeDTO
 * 8.  add CE.MappingProfice.cs mapping rule for sending dto for client
 * 9.  add SC.IEmployeeService.GetEmployees()
 * 10. impl S.EmployeeService.GetEmployees()
 * 11. at last modify CEP.Controllers.EmployeeController add GetEmployeesForCompany(...
 * 12. during development add all nessary E.Exceptions classes
 */
#endregion
#region Flow POST-requests (9)  
/*
 * 1. modify decoration attribute CEP.Controllers.CompanyController.GetCompany with
 *    [HttpGet("{id:guid}, Name="CompanyById")] - set action Name. Come in handy for creating resource (Company)
 * 2. add separate DTO for input (without ID)! Sh.DTO.CompanyForCreationDTO.cs(record)
 *    always make input and output separate DTO. We don't want validate output!
 * 3. modify C.ICompannyRepository.CreateCompany...
 * 4. impl R.CompanyRepository.Create...
 * 5. modify SC.ICompanyService.Create...
 * 6. impl S.CompanyService.Create...
 * 7. add CE.MappingProfile rule from CompanyForCreationDTO
 * 8. CEP.CompanyController.CreateCompany return CreateAtRoute - for populate reposnse header Location
 *    Location header contents uri where we can to retrieve created entity.
 *    
 *    check input dto for invalid state -> this check for null
 *    repository.Save for state Added and return new Guid.
 */
#endregion
#region Content Negotiation 7 
/* here is content negotiation configues e.g. XML BUILD-IN
 * By default server formatting answers in JSON format. Where server does it, we don't know but we can manage this.
 
  1. tell server to respect request header Accept (англ. принимать)
    (в каком формате сервер должен вернуть ответ клиету)
     build.Services.AddControllers(confid => ...RespectBrowserAccept
  2. add Accept required nessary answer formatter
        .AddXmlDataContractSerializerFormatters() (xml build-in for Accept text/xml)
        
        I. way
  3. mark all nessesary answer objects (e.g. CompanyDTO) [Serializable] attribute, if need
  4. send request to server with Accept = text/xml, например
       
        II. way (we can get strange tag-names (XML formatter compile record like a class)
  5. modify record Sh.DTO.CompanyDTO like Guid Id { get; init }
  6. remove [Serializable] attribute from Sh.DTO.CompanyDTO!
  7. modify CE.MappingProfile.cs ForMember(.... // comment ForCtorParam(.. -> previos version
  
     change Accept text/xml or application/json - get right aswer format !)
     RESTRICT MEDIA TYPES
     we can restrict client to ask not supported by server Accept formats
     ReturnHttpNotAcceptable = true -> (status code 406 Not Acceptable)
 */
#endregion
#region Custom Answer Formatter due to Access Header 8 
/* 
 * 1. add CE.CvsCustomFormatter.cs : TextOutputFormatter
 * 2. add CE.Extensions.ServiceExtensions.AddCustomCSVFormatter(this IMvcBuilder builder.....
 * 3. add builder.Services.AddCustomerCSVFormatter() after AddXmlDataContractSerializerFormatter()
 */
#endregion
#region Suppress ApiController attribute behavior (ModelStateInvalid)  9.2.1  
/*
* [ApiController] attribute activate following API behavior
* - Attribute routing requirements
* - Authomatice HTTP 400 response
* - ModelState validation in parametr
* - Binding source parameter inference(англ. вывод)
* - Multipart/form-data request inference
* - Problem details for error status code
* 
* to suppress model state validation we can hit breakpoint and use owr custom validation
* without this we can't catch model validate error!
* 
*/
#endregion
#region Creating a ChildResource 9.3 
/*
 * 1. create Sh.DTO.EmployeeForCreationDto
 * without CompanyId - id we'll get from contoller route!!([Route("api/companies/{companyId}/employees")]
 * 2. modify C.IEmployeeRepository.CreateEmpl....
 * 3. impl R.EmployeeREpository.CreateEmpl....
 * 4. add CE.MappingRule - we get DTO in action and we have to map DTO to Entity
 *    to pass into repo and service!!!
 * 5. modify SC.IEmployeeService.CreateEmpl ...
 * 6. impl S.EmployeeService.CreateEmpl....
 * 7. add new action to employee controller CEP.CreateEmployeeForCompany
 * 
 * SERVICE GET INPUT DTO AND RETURN OUTPUT DTO!
 * main diff with company creation is in returned statement
 */
#endregion
#region Creating Children Resources Together with Parent 9.4
/*
 * if Entity has correct navigationProperties we have to do nothing but easy modify proper DTO object
 * modify Sh.DTO.CompnayForCreationDto with parametr (IEnumerable<EmployeeForCreationDto> Employees)
 * (same name (Employees) with navigation property in E.Company)
 * it's enough just to set MappingProfile with correct rule
 * 
 * look CEP.CompanyController.CreateCompany(...
 */
#endregion
#region Create Collections 9.5 
/*
 * 1. modify C.ICompanyRepository with GetByIds...
 * 2. impl R.CompanyRepository.GetByIds...
 * 3. modify SC.ICompanyService.GetByIds...
 * 4. impl S.CompanyService.GetByIds...
 * 5. add abstract E.Expcetions.BadRequestException.cs
 * 6. add E.Exceptions.IdParametrBadRequestException | CollectionByIdsBadRequestExxecption).cs
 *       for catch bad pass param and not equal ids.count with company count
 * 7. modify CE.Extensions.ExceptionMiddlewareExtenion with BadRequestException handling
 * 8. add CEP.Controllers.CompanyController.GetCompanyCollection(ids:ienumerable...
 *       for CreateArRoute return statement after future company collection creatings
 * 9. add SC.ICompanyService.CreateCompanyCollection... return tuple(companyCollection, ids) !!! 
 *      its nessesary for CreateAtRoute, because we can't to specify in Location url IEnumerable<T> type, that's why its comma-separeted string
 * 10. impl S.CompanyService.CreateCompanyCollection()...
 * 11. create CEP.Controllers.CompanyController.C
 * 12. postman > we can catch data in response Location header: https://localhost:5001/api/companies/collections/(b12378d1-3001-4e3c-94e4-08db809ff0d1,48b7a079-11b9-4197-94e5-08db809ff0d1)
 *      but it would'be 415 Unsupported Media Type! -> because our API can's bind string ids to IEnumerable<Guid> in GetCompaniesCollection
 */
#endregion
#region ModelBinding in API
/*
 * 1. add CEP.ModelBinders.ArrayModelBinder.cs
 * 2. mark binded params with [ModelBinder(BinderType=ArrayModelBinder))]IEnumerable<Guid> ids)....
 */
#endregion
#region Flow DELETE-request 10
/*
   1. C.IEmployeeRepository.DeleteEmployee...
   2. R.EmployeeRepository.DeleteEmployee...
   3. SC.IEmployeeService.DeleteEmployee...
   4. S.EmployeeService.DeleteEmployee...
   5. add service Exception if need
   6. [HttpDelete("{id:guid}")]
       CEP.Controllers.EmployeeController.DeleteEmpl... -> return NoContent(); 
 */
#endregion
#region Flow DELETE parent with CHILDREN 10.1
/*
 * deleting Company with Employees
    we have COMPANY (1) -> (*) EMPLOYEE
    1. setting up cascade deleting through modelBinder in CE.Migrations.RepositoryContextModelSnapShot.cs
        in this file you'li see that cascade deleting already setted by default
          122 row in file:     modelBuilder.Entity("Entities.Models.Employee", b =>
                                    {
                                        b.HasOne("Entities.Models.Company", "Company")
                                            .WithMany("Employees")
                                            .HasForeignKey("CompanyId")
                                            .OnDelete(DeleteBehavior.Cascade)
                                            .IsRequired();
    2. repeat usual Flow DELETE-request - all will work.
 */
#endregion
#region Flow FULL UPDATE (PUT)-request 11
/*
    UPDATE flow quiet differ from GET CREATE DELETE flows!
    1. add Sh.DTO.EmployeeForUpdate.cs 
        We must have different DTO - object for creating and for updating. Even they'll be equal
        once we get validation we'll understand
    2. new DTO -> new rule in CE.MappingProfile.cs
    3. modify SC.IEmployeeService.UpdateEmployeeForCompany(....
  ! 4. impl S.EmployeeService.UpdateEmpl.... (..id, bool compTrackChanges, bool empTrackChanges)
        we need to track modifications in employee, but not in company! 
  ! 5. add CEP.Controllers.EmployeeController.UpdateEmployeeForCompany...
        we use [HttpPut] - PUT it's about FULL UPDATING. IF YOU SEND to APIe.g. only ONE modified
        property without others, others property will set in default type values!
        While PUT always send all properties of updating entity!!!

    NOTE: we have C.RepositoryBase.Update method but we don't use it.
    UPDATING we made up is CONNECTED UPDATE (same db context for fetch and update). 
        Using C.RepositoryBase.Update it's DISCONNECTED UPDATE without tracking! 
        We get e.g. Company with Id and other properties from client! without fetching by id)
        But you have to inform EF Core to track with entity and set its state to modified.
        And (with disconnected update) we always update all entity properties!!! (VERY CRUTIAL WAY UPDATING)
*/
#endregion
#region Flow FULL UPDATE (PUT)-request Insert Resources Updating One
/*
    Updating parent resource, we can create child resource as well!
    1. add Sh.DTO.CompanyForUpdateDTO.cs
    2. add rule in MappingProfile.cs
    3. modify SC.ICompanyService.UpdateCompany(...
    4. impl S.CompanyService.UpdateCompany(...
    5. modify CEP.Controllers.EmployeeController.UpdateCompany(...

    The main point - ef core will track chacnges with Company modified State
    and Employees Added State
 */
#endregion
#region PARTIAL UPDATE (PATCH)-request 12
/*
 * PATCH it's about partially UPDATING, not full PUT
 * We must have:
 * 1. Different request body 
 *      for parametr mark in controller [FromBody]JsonPatchDocument<Company> company, not [FromBody]Company company
 * 2. Different request media type
 *      application/json-patch+json instead of application/json.
 *      (last one would'd be accepted for PATCH, but it's recommend REST standart use fist one)
 * 3. PATCH request body consist of patch operations array:
 *      [
 *          {
 *              "op":"replace",     -> operation type (replace - substitude old value with new value)
 *              "path":"/name",     -> object property we want to modify
 *              "value":"/new name" -> modification value
 *           },
 *           {
 *              "op":"remove",      -> remove (delete current value and set default value)
 *              "path":"/name"
 *            }
 *       ]
 *       
 *   Table. 6 different operation for a PATCH request:
 *   OPERATION          REQUEST BODY            EXPLANATION
 * ------------------------------------------------------------------------------------
 *     Add             {                       Assigns a new value to a required property
 *                          "op":"add",
 *                          "path":"/name",
 *                          "value":"new value"
 *                     }
 * -------------------------------------------------------------------------------------
 *     Remove          {                      Sets a default value to a required property
 *                           "op":"remove",
 *                           "path":"/name"
 *                     }
 * --------------------------------------------------------------------------------------
 *     Replace         {                      Replace a value of a required property to new value
 *                           "op":"replace",
 *                           "path":"/name",
 *                           "value":"new value"
 *                     }
 * ---------------------------------------------------------------------------------------
 *     Copy            {                      Copies the value from a property in the "form" part to
 *                          "op":"copy",       the property in the "path" part
 *                          "from":"/name",
 *                          "path":"/title"
 *                     }
 *  ---------------------------------------------------------------------------------------
 *     Move            {                      Moves the value from a property in the "form" part
 *                          "op":"move",        to a property in the "path" part
 *                          "from":"/name",
 *                          "path":"/title"
 *                     }
 *  ----------------------------------------------------------------------------------------
 *     Test            {                       Tests if a property has a specified value
 *                          "op":"test",
 *                          "path":"/name",
 *                          "value":"new value"
 *                      }
 */
#endregion
#region Applying PATCH to the Employee Entity 12.1
/*
    1. add CEP -> Microsoft.AspNetCode.JsonPatch (for JsonPatchDocument)
    2. add CE  -> Microsoft.AspNetCode.Mvc.NewtonsoftJson 
            (for request body conversion to a PatchDocument once we send our request)
    3. add workaround (обходной путь) в Program.cs чтобы добавить дополнительныу возможности к
            NewtonsoftJson(AddNewtonsoftJson) к System.Text.Json json-formatters, а не полностью заменять их
            локальная функция см строку 365 Program.cs
            Эта функция даст поддержку JSON Patch из Newtonsoft.Json не вмешиваясь в работу JSON formatter'ов из коробки
            Можем использовать и те и те.
    4 modify builder.Services.AddController.... with comfig.InputFormaters.Insert(0....
            placing our patch formatter in index 0 formatters list 
    5 add MappingProfile new rule for EmployeeForUpdateDTO but in Reverse way!
    6 add SC.IEmployeeService.(GetEmployeeForPatch | SaveChangesForPatch)
        SC.IEmployeeService.GetEmployeeForPatch return tuple (EmployeeUPdateDTO, Entity) because
            in controller patchDoc can apply chages in DTO, then after patchDoc.ApplyTo(.. done
            we map result to employeeEntity and Save in Db with SaveChagesForPatch
*/
#endregion
#region VALIDATION 13
/*
    - Applies only dto objects during POST PUT PATCH, not GET!
      Validation occurs after model binding and these both before request reaches action method!
    
    - We not use ModelState.IsValid expression in controllers beacuse [ApiController] setted up.
      All error return 400 BadRequest - must suppress this behavior and catch this manually.
         400 BadRequest - if not suppress [ApiController] attribute with its behavior
         422 Unprocessable Entity - validation failed
    
   -  Repeat Validation (e.g. after assignment object property for already validated object)
        ModelState.ClearValidationState(nameof(Book));
        if(!TryValidateModel(book, nameof(Book)))
            return UnprocessableEntity(ModelState)
   
    - Complete build-in attribute list
        https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=net-7.0

    - If build-in attribute not enough create custom attribute
      local example E.Models.Book | E.Validations.ScienceBookAttribute.cs

    - To not achieve 500 Internal Server Error during repository.SaveChanges() in DB.
      We must to wrap validation attributes input DTO objects (Sh.DTO.EmployeeForCreateDTO
      ,but not only Domain model entity!
    
    - add CEP.EmployeeController.CreateEmployeeForCompany ... 
      ModelState.IsValid (because DTO validation before requiest go into action)

    - If we don't send value type (e.g. Integer) we don't get [Required] error.
      Value types in this case get degault value. Need substitude [Required] with [Range(min. max. er mes)]

    - Add abstract Sh.DTO.EmployeeForManipulationDTO.cs

    - For PATCH patchDoc.applyToEmployeeToPatch, ModelState) 
            need to remove mvc.newtonsoftjson from CE and install in CEP ?
 */
#endregion
#region Safety Method and Idempotention (INFO) 
/*  to decide which method to use for which use case
 * HTTP method   is it safe  is it idempotent
 * -------------------------------------------
 * GET              YES         YES
 * OPTIONS          YES         YES
 * HEAD             YES         YES
 * POST             NO          NO
 * DELETE           NO          YES
 * PUT              NO          YES
 * PATCH            NO          NO
 * 
 * not safe - changing resources, changing resource presentation
 * idempontent - many requests - always only one the same result!
 * immutable
 */
#endregion
#region ASYNCHRONOUS CODING 14
/*
    Thread pool, available threads. Async - await marks
    Return 3 types:
        Task<T> - for method returned T-type result
        Task - for method not returned result (like void) await without return 
        void - for event handlers
    From C# 7.0 we can to return not only Task. Any type in which includes GetAwaiter method

    Task have properties (Status, IsComplete, IsFauiled, IsCancel). Task tracks async operation 
    with this properties. It's a TAP (Task-Based Asynchronous Pattern)

    General advice: to use async code wherever it's possible. But somethimes async method works slower
    than sync due to extra code for handling. Switch back to sync one.

    modify C.ICompanyRepository exclude Create and Delete leave sync. These methods not making changes in 
    db, only change entity state Added and Deleted

    await:
        - help to extract result
        - validate success of the operation
        - provide co-ntinuation of execution the rest of the code async method

     ASP Core doesn't have SynchronizationContexts -> thread pool direct to request
     In one async method many await calls.

     Don't use e.g. ..ToListAsync().Result - it can invoke deadlock of application
 */
#endregion
#region Action Filters (INFO)
/*
    works in project which have libs:
    MS.AspNet(Core).Mvc(WebApi).Abstractions...(Filters)
    or ASP CORE projects!
    
    Отличный способ подключиться к конвееру вызова методов контроллера и 
    убрать в фильтр лишнюю логику и улучшить читабельность кода controller'jd
    Great way to HOOK INTO action invocation pipeline.
    
      build-in filters:
    Authorization filter - run first (user is auth? for cur request)
    Resource filter      - run after Filter.auth (usualy for cashing improving performance)
    Action filter        - run before and after action excecusion
    Exception filter     - handle except before response body populated
    Result filter        - run before and after execution of the action methods result
        
    e.g. Custom Action.filter -
        we need to create Action filter class inherits from:
            IActionFilter or IAsyncActionFilter, or ActionFilterAttribute class which implements as weeL
            IActionFilter, IAsyncAction Filter
    public abstract class ActionFilterAttribute : 
            Attribute, IActionAttribute, IFilterMetadata, 
            IResultFilter, IASyncResultFilter, IOrderedFilter
    
    LOOK examples in CEP.ActionFilters.Examples.cs

    Define the filter scope level: Global, Action, or Controller
        Globally use
            add through 
                builder.Services.AddControllers(config => {
                    config.Filters.Add(new GlobalFilterExample());
                }
        Action or Controller using only like ServiceType
         a. register in IoC
                builder.Services.AddScoped<ActionFilterExample>()
                builder.Services.AddScoped<ControllerFilterExample>()
         
         b. apply there it's needed (over controller or over action)  
         namespace AspNetCore.Controllers 
            {
                [ServiceFilter(typeof(ControllerFilterExample))]
                [Route("api/[controller]")]
                [ApiController]
                public class TestController : ControllerBase {
                    [HttpGet]
                    [ServiceType(typeof(ActionFilterExample))]
                    public IEnumerable<string> Get()
                    {
                          return new string[] { "example", "data" }
                    }
            }
            
          order invoication (we can change that order using Order
          [ServiceType(typeof(..Filter), Order = 2]
          [ServiceType(typeof(..Filter), Order = 1]
            
            onActionExecutING (Global filter)
            |
            | onActionExecutING (Controller filter)
            | |
            | | onActionExecutING (Action filter)
            | | |
            | | | Action method execution ---->
            | | |
            | | onActionExecutED (Action filter)
            | |
            | onActionExecutED (Controller filter)
            |
            onActionExecutED (Global filter)

 */
#endregion
#region Action Filters (AF) 15
/*
    1. create CE.ActionFilter.ValidateFilterAttribute.cs
    2. register this filter in CE.Program.cs (rows: 12, 90)
    3. remove try-catch from (POST) CEP.CompanyController.CreateCompany(...
                              (PUT) CEP.CompanyController.UpdateCompany(...
    4. apply to these actions [ServiceType...

    5. make refactoring the Service layer class with private static method
 */
#endregion
#region Pagging 16
/*
    about url?pageNumber=2&pageSize=2
    (ALL BENEFITS WITH METADATA PAGINATION IN:
           -  create and return to caller pagination metadata in X-Pagination header
           -  this information is very usefull when we creating any frontend pagination in our benefit (в наших интересах)
           -  E.g. we can create using this metadata links to next or previous pagination page! but it's in HATEOUS scope) 

    1. add Sh.RequestFeature.RequestParamets.cs (entity for [FromQuery] params)
        to hold common query properties of all our entites in project
        (maxPageSize - to restrict API 50 rows per page,
         PageNumber - 1 by default, PageSize - 10 by default. if not set in url query
    2. create Sh.RequestFetures.EmployeeParameters.cs : RequestParamets
        for employee
    3. modify repository logic
           add  C.IEmployeeRepository.GetEmployeesAsync() with EmployeeParameters
           impl R.EmployeeRepositoty.GetEmployeeAsync() with Skip Take pattern!
    4. modyfing repo layer we must to modify service layer
           add SC.IEmployeeService.GetEmployeesAsync() with EmployeeParameters
    5. if we accept param in repo, modify everywhere repo invocations (in controllers, services)
    
    6. remove from methods Skip/Take logic
        add Sh.RequestFeatures.MetaData.cs - pagging metadata (e.g. page count, has next, has prev, and other)
        add Sh.RequestFeatures.PagedList.cs : List - skip/take logic
    
    6.1. modify repo - service - controller inherence
        modify  C.IEmployeeRepository.GetEmployeeAsync() with new return type (PagedList..)
                R.EmployeeRepository.GetEmployeesAsync() as well
               SC.IEmployeeService.GetEmployeesAsync() as well
                S.EmployeeService.GetEmployeesAsync() as well + employeeFromDb -> employeeWithMetaData
                CEP.EmployeeController.GetEmployeesForCompany()
                        add our paggind metadata in X-Pagination header..
  !!!  THIS SOLUTION GOOD FOR QUIET SMALL DATA IN DB, BUT WHEN THERE IS A LOT OF MILLIONS
       LOOKS SECOND WAY in R.EmployeeRepository.GetEmployeeAsync!

     7. enable client application read new X-Pagination header 
            SE.Extensions.ConfigureCors() with .WithExposedHeaders("X-Pagination")
 */
#endregion
#region Filtering 17
/*
    Filtering is used by users when they know how DATA STRUCTURE in api implemented
    (for this purpose we following are used drop lists, check boxes, radio buttons and other)

    1. Define correct specification for requested resource (Employees in Company)
          modify Sh.RequestFeatures.EmployeeParameters with adding specification properties
    2. Add specification violation check exception to note client about wrong request params
          add E.Exceptions.MaxAgeRangeBadRequestException.cs
    3. Add this exception check in Service Layer
          modify S.EmployeeService.GetEmployeesAsync
    4. implement FULL specification in repository. If something ones not setted will be used default.
        modify R.EmployeeRepository.GetEmployeesAsync
 */
#endregion
#region Searching 18 
/*
    Searching differs from Filtering we DON'T KNOW DATA STRUCTURE and our need in search RELEVANT DATA
    like search line in BROWSER. We don't know what concrete we want, but make relevant query string

    1. add serching term in query parameters object
        modify Sn.RequestFeatures.EmployeeParameters with SearchTerm
    2. add some IQuerable extension for repo refactoring
        R.Extensions.RepositoryEmployeeExtensions.FilterEmployees
                                                 .Search(string searchTerm)
    3. modify R.EmployeesRepository.GetEmployeesAsync with FilterEmpl and Search extension method

 */
#endregion
#region Sorting 19
/*
    1. modify Sh.RequestFeatures.RequestParameters with OrderBy prop
    2. modify ctor Sh.RequestFeatures.EmployeeParameters with OrderBy init for Employee queries
    
    3. DINAMICALLY CREATING SORTING TERMS 
        3.1 add to R System.Linq..Dinamic.Core library NuGet
        3.2 add new IQueryable extension Sort R.Extensions.RepositoryEmployeeExtensions.Sort
        (THIS IS LITTLE TRICK TO FORM QUERY WHEN YOU DON'T KNOW IN ADVANCE HOW YOU SHOULD SORT)
    
    4. modify R.EmployeeRepository.GetEmployeesAsync with .Sort(employeeParameters.OrderBy)

    method Sort with high propability will be reused 
    therefore extract some common logic in separate place 

    1. add R.Extensions.Utility.OrderQueryBuilder.cs
    2. modify R.EmployeeRepository.GetEmployeessync with OrderQueryBuilder.CreateOrderQuery<Employee>(...
 */
#endregion
#region Data Shaping 20
/*
    This feature about reduce stress on the API by requesting
        some fields of record in DB, not all.
    USING IN SERVICE LAYER
    BUT IT NEEDS REFLECTION WHICH TAKES ITS TOLL AND SLOWA OUR APPLICATION DOWN.
   
    1. add new features for out request query string
        modify Sh.RequestFeatures.RequestParameters.cs with Fields:string
    2. create DataShapers one for single entity another for collection of entities
        add C.IDataShaper<T>:interface
        using System.Dynamic.ExpandObject to shape our data the way we want
    3. impl C.IDataShaper in S.DataShaping.DataShping.cs
    4. register DataShaper in SE
        add builder.Services.AddScoped<IDataShaper<EmployeeDto>, DataShaper<EMployeeDto>>() (row 94)
        for providing need shaping type
    DATA SHAPING IS BEING USED IN SERVICE LAYER
    5. modify S.ServiceManager.cs for using dataShaper
       modify S.EmployeeService.cs for using dataShaper
       modify S.EmployeeService.GetEmployeesAsync for using dataShaper
    
    6. modify all service interface caught under the influence
             SC.IEmployeeService.GetEmployeesAsync with return ExpandoObject

    with Accept text/xml result our build-in XmlDataContractSerializerFormatter can return
    ugly tag names result
        1. create class named e.g. ExpObjEntity and specify it only nessary properties for XML serialization
                better to create separate service method for retrun XML serialized data set. Not touch ExpandoObject, that more useful for generic cases
        2. substitude all ExpandoObject entries to ExpObjEntity 
 */
#endregion
#region HATEOUS 21
/*
    HATEOUS (Hypermedia as the Engine of Application State) - very important REST constraint
    - HYPERMEDIA its about REST API refers for links to media types (video, audio, images, etc) 
    - RETURN CONSUMER ALL LINKS TO SEARCHABLE CONTENT WHICH YOU CAN TO ACHIVE ALL POSSIBLE WAYS 
      AS RESULT ALL REQUESTS POINTS TO SERACHED CONTENT (in response Location header after PUT GET POST PATCH requests etc)
      
        HATEOUS response (e.g. WE GET LIST OF EMPLOYEES AND FOR EACH EMPLOYEE ALL ACTIONS WE CAN PERFORM ON THEM..)
        IN OTHER WORDS SELF-DISCOVERED API and EVOLVABLE
        "value": [
            {
                "name": "Sam Raiden",
                "age" : 28, 
                "links" : [
                    {
                    (representes target URI) 
                        "href": "https://localhost:5001/api/companies/{guid}/employees/{guid}?fields=name,age",
                    (link relation type, which means 
                    it describes how current context is related 
                    to the taget resource)   
                         "rel" : "self",
     !!!!!!!!       (HTTP method to know how to DISTINGUISH same target URIs)
                         "method" : "GET"
                    },
                    {
                        "href": "https://localhost:5001/api/companies/{guid}/employees/{guid}",
                        "rel" : "delete_employee",
                        "method" : "DELETE"
                    },
                    {
                        "href": "https://localhost:5001/api/companies/{guid}/employees/{guid}",
                        "rel" : "update_employee",
                        "method" : "PUT"
                    },
                    {
                        "href": "https://localhost:5001/api/companies/{guid}/employees/{guid}",
                        "rel" : "partially_update_employee",
                        "method" : "PATCH"
                    }
                ]
            },
            ......
        ]

        links - simple RFC5988, .."a typed connection between two resources
                that are identified by Internationalised Resource Identified (IRIs)
        
    - without this REST not become REATful. And the most of REST architeture benefits is unavailable.
    - relies heavily on paging, filtering, searching, sorting and espessial data shaping
   
    Generating links:
    
        1. create E.LinkModels.Link.cs with empty ctor (need for XML serilization)
        2. For keeping all our links
               create E.LinkModels.LinkResourceBase.cs for keep all our links
        3. For responce need to describe root of controller
               create E.LinkModels.LinkCollectionWrapper.cs
        4. For correct custom XML formatter (without ugly tag names)
                add E.Models.[Entity].cs method WriteLinksToXml 
  !!!!          implement Entity: DinamicObject, IXmlSerializable, IDictionary<string, object?>...
        5. HATEOUS strongly relles on having the ids available 
                to construct the links for the response. DataShaping allows fields
                only specify in query string. To solve that
                create E.Models.ShapedEntity.cs
        (ExpandoObject --> Entity --> ShapedEntity)
        
        6. Update C.IDataShaper and its impl S.DataShaiping.DataShapers.cs with SharedEntity
        
        7. We need object answer that response has links. 
            true ? LinkedEntities : ShapedEntities;
            add E.LinkModels.LinkResponse.cs

      WE SHOULD BE ABLE TO CHANGE OUR FORMATTER DUE TO ACCEPT HEADER BECAUSE HATEOUS IS JSON or XML as well.
      AND WE NEED TO EXPLAIN API WHAT TO SEND (simple json (xml) or HATEOUS-enriched json (xml)

       Add (Implement) Custom Media Types 
        we need e.g. (application/vnd.codemaze.hateoas+json) to compare (application/json)
        media type consist of consist of:
            vnd - vendor prefix; its always there
            codemaze - vendor identifier (codemaze - for example)
            hateoas  - media type name
            json - suffix; we can use it to describe if we want json or an XML response, for example
         
         1. Register custom media type in middleware (otherwise 406 Not Acceptable)
                register two new custom media types for the JSON and XML output formmatters
         2. register in pipeline 
                builder.Services.AddCustomMediaTypes(); (row 90)

        WE WANT OUR ACCEPT HEADER TO BE PRESENT (присутствовал) IN OUR REQUESTS 
        SO WE CAN DETECT WHEN USER REQUESTED THE HATEOUS-enriched (ответ был дополнен HATEOUS данными) response

        1. add action filter for check accept header and its media type value
             CEP.ActionFilter.ValidateMediaType.cs
        2. register action filter in Propgram
             builder.Services.AddScoped<ValidateMediaTypeAttribute>();
        3. decorate CEP.EmployeeController.GetEmployees with this attribute

        IMPLEMENT HATEOUS
        1. C.IEmployeeLinks.cs
        2. CE.Utility.EmployeeLinks.cs : IEmployeeLinks
        3. use LinkGenerator to generate links for our responses and IDataShaper to shape our data.
        4. register like service CE.Utility.EmployeeLinks.cs in Program.cs
                builder.Services.AddScoped<IEmployeeLinks, EmployeeLinks>();
        5. for transfer required params from controller to service to avoid installation of anadditional NuGet package in S and SC
                add E.LinkModels.LinkParameters.record 
        6. we've got HttpContext error from IEmployeeLinks
            add in C Microsoft.AspCoreNet.Mvc.Abstractions for needed HttpContext
        WE DON'T INSTALL Abstractions NUGET package since C refereces in E. IN VS KEEP ASKING
        FOR PACKAGE INSTALLATION REMOVE E refereces from C AND ADD NEEDED NAMESPACE AGAIN
        
        ALL FUTHER MODIFACTIONS RELATED WITH METHOD GetEmployeesAsync and its using in service and controller
        POINT IS: result LinkResponse + metaData GetEmployeesAsync accept LinkParameters consist of
            data shape fields
        if request is HATEOUS ? links : shaped data

        7.  modify CEP.EmployeeController.GetEmployeeForCompany
        8.  modify SC.IEmployeeService.GetEmployeesAsync
        9.  modify S.EmployeeService with IEmployeeLinks instead IDataShper -> IEmplLinks data shaper inside
        10. modify.S.ServiceManager
        11. modify CEP.EmployeeController.GetEmployeesForCompany
 */
#endregion
#region OPTIONS and HEAD
/*
    OPTIONS - сообщает о различных вариантах коммуникации с сервером
    какие HTTP методы пользователь может использовать. 
        [HttpOptions]
   1. add CE.CompanyController.GetCompaniesoptions() with
       e.g. Response.Header.Add("Allow", "GET, PUT, OPTIONS")

    HEAD - идентичен GET возвращает тоже самое но без response body
    Используется чтобы сообщить о последних изменениях, действительности и 
        проверить доступность ресурса в данный момент (ТЕСТ ДОСТУПНОСТИ РЕСУРСА и ПОЛУЧЕНИЕ МЕТАДАННЫХ)
        результаты кешируются
    (e.g. validity, accessability, recent modifications)
    1. [HttpHead] CEP.EmployeeController.GetEmployeesForCompany
        выполниться весь код но вернется только информация в Response Headers (X-Pagination, Content-Type, ..)
        тело ответа будет пустое.

У этих запросов разное назначение:
      HEAD -  служит для проверки существования ресурса, 
              он полностью аналогичен GET, но без возврата тела ответа.
              Такой запрос может быть выполнен перед загрузкой большого ресурса, 
              например, для экономии пропускной способности.
    OPTIONS - служит для получения параметров для ресурса или для сервера 
              в целом и при этом сам ресурс ни как не затрагивается (то есть это 
              более дешевая операция по сравнению с HEAD)
 */
#endregion
#region ROOT DOCUMENT 23
/*
    Starting point consumer to learn how to interact with rest of our API.
    Точка входа определяет иерархию вызовов методов API при пустой базе данных.
    Поэтому стартовых точек входа у нас 2 GetCompanies и CreateCompany из CEP.CompanyController. Так как Employees
    дочерняя сущность и не существует вне компании, поэтому изначально доступа к ней нет!
    
    1. add CEP.RootController.cs -> ctor accepts LinkGenerator
    2. RootController has single action method GetRoot return root level links 
    3. add names to root api points [HttpGet(Name="...")] to
        CEP.CompanyController.[GetCompanies | CreateCompanies]
    3.1 in CEP.RootController.GetRoot made starting list links and return Ok or NoContent
    
    4 register another media types for root api in SE.ServiceExtensions.AddCutomMediaTypes()
            add application/vnd.codemaze.apiroot+xml(json) media type Accept
 */
#endregion
#region VERSIONING 24
/*
 * ALLOWS US TO IMPLEMENT SOME BREAKING CHANGES IN WORKING API.
    Improving our project we might to make BREAKING CHANGES, e.g.
        Renaming fields, properties, resource URI..
        Changes is the payload structure
        Modifying response codes or HTTP
        Redesigning our API endpoints
        or etc.
        Chaching the type of DTO property
        Removing a DTO property
        Renaming a DTO property
        Adding a required field ont he request

    In order not to break existing API and not to force consumers to change existing code
    we must to mandating(предписывать) using versioning (to switch between versions)!!! 

    1. add Microsoft.AspNetCore.Mvc.Versioning.(ApiExplorer) to CEP
    2. add CE.Extensions.ServiceExtensions(SE).ConfigureVersioning()
    2.1 add to service collection builder.Services.ConfigureVersioning()
    3 mark original CopmanyController with [ApiVersion("1.0"] check postman
 */
#endregion
#region CASHING 25
/*
    КЕШИРОВАНИЕ нужно для исключения необходимости отправки запросов к API во многих случаях, 
        а также отправки полных ответов в других случаях.
    МЕХАНИЗМЫ:
        - истечение срока действия (expiration mechanism) -> снизить траффик (кол-во) запросов до сервера API
        - валидация кеша (validation mechanism) -> чтобы не отправлять полный ответ от API (работать при слабом соединении)
    КЕШ отдельный компонент. Первым перехватывает запрос к API, и первым получает ответ от API до клиента на хранение    
   
    ТИПЫ кеша:
        1. Client Cache (on browser -> private cache (related to a single client). Every clients has one.)
        2. Gateway Cache (on server -> shared cache (resources it chaches are shared over different clients)
        3. Proxy Cache (live in network (nor client and server) -> shared cache.

    КАК ОНИ РАБОТАЮТ
    Если 5 клиентов делают первый запрос к API.
       - Клиентский приватный (private) кеш (Client Cache)
             Все эти 5 запросов обработаются в API, а не в кеше. Все оставльные запросы от этих клиентов
             получат ответы от кеша (если EXPIRED TIME кеша не вышло)
       - Разделяемый (shared) кеш (Gateway Cache, Proxy Cache)
             Ответ 1го запроса от API сохранится в кеше. Все остальные 4 клиента получат API ответ из кеша
    
    ЧТОБЫ КЕШИРОВАТЬ РЕСУРСЫ НАДО ПОНЯТЬ КЕШИРУЕМЫ ЛИ ОНИ
       - делается через response header (если получен Cache-Control: max-age=180 (хранить ответ 180 сек) - ресурс помечен как кешиемю)
       - пометить action method [ResponseCache..] (отправить кеш-headers с ответом)
      (пока что кеширование не работает, просто в ответ добавляются cache-хедеры)
    
    АКТИВАЦИЯ КЕШИРОВАНИЯ В ASP CORE (Adding Cache-Store)
        - add SE.ConfigureResponseCaching(this IServiceCol....
        - register response caching service in IoC 
                builder.Services.ConfigureResponseCaching()
        - add cachinng in app middleware pipeline right after UseCors() (recomended microsoft)
                app.UseResponseCaching()
    
    ПРОВЕРКА (при запросе request header Cache-Control: no-cache надо убрать
        Если в response headers помимо Cache-Control:.. появился header Age (сколько сек объект ответа хранится в private cache),
        то кеширование работает. Brekapoint в action method. Пока max-age не прошло до сервера запрос не дойдет. 
        Клиент получит ответ из кеша. Потом поновой

    ПРОФИЛЬ КЕШИРОВАНИЯ
        Чтобы не настраивать кеширование для каждого action method'a можно зарегистрировать профиль
        - add builder.Services
                .AddControllers(config => 
                        ...
                    config.CacheProfiles.Add("120SecondsDuration", new ....
       - пометить CEP.Controllers.CompanyController [RepsonseCache(CachieProfileName="120SecondsDuration")]
            все action method'ы будут применять это профиль кеширования

   МОДЕЛЬ ИСТЕЧЕНИЯ СРОКА ДЕЙСТВИЯ
     NO-CACHED (first request)
        CLIENT ----------(/api/companies)-----> CACHE (if no-cached, to forward to) -----(/api/companies)----> API
        CLIENT <------------- 200 OK ---------- CACHE (response stored, EXP PERIOD) <-------- 200 OK --------- API
                    Cache-Control: max-age:500                                      Cache-Control: max-age:500
                
     CACHED 
        CLIENT ----------(/api/companies)------> CACHE (if cached) ------------- X ----------------> API
        CLIENT <------------ 200 OK ------------ CACHE (counts remaining time)                       API
                    Cache-Control: max-age:500  
                            Age: 129

   МОДЕЛЬ ВАЛИДАЦИИ КЕША (проверка актуальности данных в кеше)
       Мы имеет shared cache на 30 мин (GetCompanies). Чз 5 мин произошло обновление.
       Оставшиеся 25 мин в кеш содержится уже устаревшая информация
       Чтобы избежать этого используют ВАЛИДАТОРЫ
            HTTP стандарт рекомендует исп-ть ETag и Last-Modified response headers
        CLIENT ----------(/api/companies)---- CACHE (if no-cached, to forward to)----(/api/companies)---> API
        CLIENT <-------------- 200 OK ------- CACHE (response stored) <---------------- 200 OK ---------- API
                        ETag: "123403945"                                         ETag: "123403945"
                    Last-Modified: Mon, 15 Oct 2019                        Last-Modified: Mon, 15 Oct 2019
                            11:20:33 GMT                                             11:20:33 GMT
        
        
        CLIENT ----------(/api/companies)------> CACHE (if cached) --------- GET (api/companies)------------ API
                                                                        If-None-Match: "123403945" (sets from ETag)
                                                                    If-Modified-Since: Mon, 15 Oct 2019 (sets from Lat-Modified)
        CLIENT -------------- 200 OK --------------- CACHE <---------------- 304 Not modified -------------- API
                         ETag: "123403945"                    
                  Last-Modified: Mon, 15 Oct 2019          
                           11:20:33 GMT      

    НАСТРОЙКА МОДЕЛИ ВАЛИДАЦИИ
        - add Marvin.Cache.Headers lib to CEP (supports Cache-Control, Expires, ETag, Last-Modified)
        - add SE.ConfigureHttoCacheHeaders(this IServiceCollection.....
        - modify Program.cs with builrder.Services.ConfigureHttpCacheHeaders()
        - add app.UseHttpCacheHeaders() into pipeline
        - comment [ResponseCache.. under controller (installed lib provide Cache-Control already)
        - test postman (we get ETag, Expires (60 sec by dfeault), Last-Modified... headers). Next request cache add Age
        
        make PoT -> check ETag -> request: If-None-Modified 
        304 Not Modified
    
    АЛЬТЕРНАТИВНЫЕ СИСТЕМЫ КЕШИРОВАНИЯ
         Varnish https://varnish-cache.org/
         Apache Traffic Server https://traffecserver.apache.org/
         Squid http://www.squid-cache.org/
*/
#endregion
#region RATE LIMITING AND QUEUE TROTTLING 26
/*
    Rate limiting
    To provide information about rate limiting, we use the response headers.
    Separate between Allowed requests, which all start with the X-Rate-Limit 
    and Disallowed requests

    X-Rate-Limit-Limit - rate limit requests
    X-Rate-Limit-Remainning - number of remaining rqeuests
    X-Rate-Limit-Reset - data/time information about resetting the request limit

    for disallowed rqeuests - use 429 status code (to many rqeuests). This header may
    include the Retry-After (how many seconds to resetting rate limit rule) response header and should explain details in the response body.

    - add AspNetCoreRateLimit to CE
    - rate limit uses memory cache to store counter and rules.
      modify Program.cs with
            builder.Services.AddMemoryCache();
    - add SE.ConfigureRateLimitingOptions(this IServiceCollection...
    - modify Program with 
            builder.Services.ConfigureRateLimitingOPtions();
            builder.Services.AddHttpContextAccessor();
            app.UseIpRateLimiting();
                    before
            app.UseCors();
 */
#endregion

#region J  W  T 27 
/*
    
    build-in ASP CORE Identity and Jwt functionalities
    
    resource endpoint validates access token and return protected resource

    ASP CORE Identity
        membership system for web application includes:
            - membership
            - login
            - user data
        contains rich services set of:
            - creating users
            - hashing passwords
            - creating database model
            - authentification overall (в целом)
     
     Implementation
        - add MS.AspNetCore.Identity.EntityFrameworkCore to E
        - add E.User.cs : IdentityUser (provided by asp core identity, contains diff useful props...)
        - integrating our context with Identity
                    inherit R.RepositoryContext.cs with : IdentityDbContext<User>
        - for properly migration work
                add R.RepositoryContext -> base.OnModelCreating(modelBuilder) in OnModelCreating.... (needed for properly work of migration)
        - add SE.ConfigureIdentity.....
        - modify Program.cs with 
            builder.Services.AddAuthentification()
            builder.Services.ConfigureIdentity()
            app.UseAuthentification()
            app.UseAuthorization()
        - create and apply build-in identity tables migrations
            PM> Add-Migration CreatingIdentityTables
            PM> Update-Database
                will create tables AspNetRoles, AspNetUserRoles, AspNetUsers (quiet enough for our project)
                in AspNetUsers table we will see additional properties from E.Models.User (FirstName, LastName)
        - add several roles into AspNetRoles with migrations
            add R.Configuration.RoleConfiguration.cs : IEntityTyeConfiguration<IdentityRole>
        - modify R.RepositoryContext.OnModelCreating with
            modelBuilder.ApplyConfiguration(new RoleConfiguration());
        - apply role migrations
            PM> Add-Migration AddedRolesToDb
            PM> Update-Database
      
      extract auth logic into separate layer 
        - create SC.IAuthentificationService.cs 
        - add SE.IAuthentificationService.RegisterUser(userFor...Dto user..)
        - impl S.AuthentificationService : SC.IAuthentificationService.RegisterUser()..
      
      to provide auth service to our contollers through ServiceManager (provider)
        - modify S.ServiceManager with IAuthentificationService...
      
      creating/register users flow
        - add CEP.Controllers.AuthentificationController.cs
        - add Sh.DTO.UserForRegistrationDTO.cs (record)
        - add CE.MappingProfile.cs mapping rule for Sh.UserForRegistration...
        
        - create action CEP.Controllers.AuthentificationController.RegisterUser
            returned 201 which means user created and added to role.
        (- increase rate limit from 3 to 30 for testing) 

    J     W     T (json web token (header (alg, typ), payload(user claimss), signature) in base64 ITS NOT ENCRYPTED ! )
  A U T H E N T I C A T I O N
        Configure Jwt authentification:
          - add Ms.AspCoreNet.Authentications.JwtBearer
          - add JwtSettings in application.json
        for making signature need secret key encode(header,payload, secretkey) etc
          - create system environment variable in windows (not local) 
               cmd(under admin) >> setx SECRET CodeMazeSecretKey /M (/M - system variable not local)
               restart VS or computer
          - modify SE with ConfigureJWT()
                (we need namespaces MS.Asp.Auth.JwtBearer (Identity), MS.IdentityModel.Tokens, System.Text)
          - modify Program.cs with builder.Services.ConfugreJwt(builder.Configuration);  
        protecting endpoints:
          - add [Authorize] attribute in CEP.Controllers.CompanyController.GetCompanies
                (action or controller its applied to requires authorization
          - test protection works 
                (without jwt token we get 401 UnAuhorized error)
        
        Implement jwt authorization
           - add Sh.DTO.UserForAuthenticationDTO.cs
           - modify SC.IAuthenticationService.cs with ValidateUser and CreateToken methods
           - modify S.AuthenticationService by adding private User? _user variable
                and implement mentioned methods
           - add appsettings.json jwtsettings exprires
           - add CEP.Controllers.AuthenticationController.Authenticate()

        Role-based authentication
            access to actions for authenticated users depending on his ROLES
            add [Authorize(Roles = "Manager")] under action
            - create administrator role user
       
        Expires time. After this time due to time difference between servers, which is embedded inside the token, token will be invalid.
        (this can be overridden with the ClockSkew property in the TokenValidationParameters object)
 */
#endregion
#region R E F R E S H   J W T
/*
    Refresh token is credential
    If exist requirement not to always log when token expires
    Refresh token expires time more long then main jwt
    After authentication success server return both access token and refresh token
    Server can identify the app sends requests
    Server sends expiration time response

    IN CLIENT SIDE WE INSPECT EXPRIRE TIME OF ACCESS TOKEN
    IF TIME EXPIRED CALL TokenController.RefreshTOken

    Implement Refresh Token Generating
        - modify E.Models.User.cs with RefreshToken and RefreshExpiration... props
        - update AspNetUsers table in db by migration
            Add-Migration AdditionalUserFieldsForRefreshToken
        its nessesary to inspect futher migration file by migration before Update-Database
        due to possible losses data
        - inspect RepositoryContextModelSnapshot file (after Add-Migration this file can changes, and we can get warning (An operation as scaffolded that..)
        - - find AspNetRoles and revert the Ids both Roles to the previos values
        - execute Update-Database

        - add Sh.DTO.TokenDTO
        - modify SC.IAuthenticationService with Task<TokenDTO> CreateToken
        - implement SC.IAuthenticationService in S.AuthenticationService with
                GenerateRefreshToken() GetPrincipalFromExpiredToken() updated CreateToken(bool pop..)
        - modify CEP.Controllers.AutheticateController.Authenticate() with tokenDTO
    
    Implement Refresh Token Update
        - add CEP.Controllers.TokenController
        - modify SC.IAuthenticationService contract with RefreshToken()
        - impl RefreshToken in S.AuthenticationService    
        - add E.Exceptions.RefreshTokenBadRequest.cs
        - add CEP.Controllers.TokenController.Refresh
*/
#endregion
#region BINDING CONFIGURATION AND OPTIONS PATTERN
/*
    Параметры конфигурации из application.json мы обычно получаем чз
    интерфейс IConfiguration.GetSection(string subSectionName)..
    Ввод строковых литералов грозит ошибками и NullException'ами, которые трудно обрабатывать
    Поэтому имена разделов из appsettings.json надо привязать к строго-типизированным типам!

BINDING CONFIGURATION
    - в проекты NUGET:Microsoft.Extensions.Configuration.Binder
    - add E.ConfigurationModels.JwtConfiguration.cs
    - modify SE.ConfigureJwt with JwtConfiguration.cs
    - modify S.AuthenticationService with JwtConfiguration 
            m: GetPrincipalFromExpired() GenerateTokenOptions()

OPTIONS PATTERN (best safe and cheap)
    Более гибкое управление. Исп-ся DI:IOptions<T> нужную часть конфига, а не весь
    Имеется горячее обновление конфига:
        IOptionsSnapshot<T> или IOptionsMonitor<T>
    и механизмы валидации значений конфига по любым локальным правилам приложения чз DataAnnotations
    - register and configure mentioned up JwtConfiguration.cs in SE
        add SE.AddJwtConfiguration()
    - call in Program: builder.Services.AddJwtConfiguration(builder.Configuration)
        here we can use IOptions by DI
    - modify S.ServiceManager ctor with IOptions<JwtConfiguration>
        replace IConfiguration, configuration.Bind calls to IOptions<JwtCOnfiguration> calls, 
    - modify S.AuthenticationService with IOptions<JwtCOnfigutation>

   не надо трогать SE.ConfigureJwt т.к. IOptions используется после регистрации сервисов, а не во время
   вызвать RequiredService не получиться. Хотя способ есть. Через BuildServiceProvider, содержащий все сервисы из IServiceCollection
   на момент вызова. Но если так сделать получим копию синглетонов всех сервисов в приложении (расход памяти)

ГОРЯЧАЯ ПЕРЕЗАГРУЗКА
   нужно заменить вызовы IOptions<T> на IOptionsSnashot<T>, либо на IOptionsMonitor<T>
         заменить configuration.Value на configuration.CurrentValue (для IOptionsMonitor)

     IOptionSnaphot                                 IOptionMonitor                                 IOptions
SCOPED (нельзя интегр в SINGLETON)              SINGLETON (можно итегр в люб сервис)        SINGLETON (можно интег в люб сервис)      
    Config hot reload                               Config hot reload                       No config hot reload
  Values reload by request                 Values are cached and reload immidiatly   Values once in registration, same all app lifetime
доступ к разделу конфига по имени           доступ к разделу конфига по имени            Best for keep whole config, NO named options   

ДОСТУП К РАЗДЕЛУ ПО ИМЕНИ
    - add JwtApi2Setting into application.json (instead creating new JwtConfiguration every time it one)
    - in SE modify services.Configure<JwtConfiguration>("JwtSettings", configuration.GetSection("JwtSettings"))
                or services.Configure<JwtConfiguration>("JwtSettingsAPI2", configuration.GetSection("JwtSettingsAPI2"))..
    if we need concrete config version: (look in S.AuthenticationService)
        - instead _jwtConfiguration = configuration.CurrentValue or Value
                _jwtConfiguration = configuration.Get("JwtSettings...")
 */
#endregion
#region S W A G G E R (OPEN API)
/*
    Языко-независимое средство документирования REST API серивисов. Также исвестен как OpenAPI
    Спецификация swagger (Swagger Specification) основная часть. По умолчанию генерируется файл swagger.json в проекте,
        содержащий специфику API и как получить доступ по HTTP

    Установка. Пакет Swashbuckle (генерируе Swagger Specification, Swagger UI...)
        В нужный проект установить:
            Swachbuckle.AspNetCore.[Swagger, SwaggerGen, SwaggerUI]
        Swagger    - its object model and middleware SwaggerDocument objects as json
        SwaggerGen - gen which builds SwaggerDocument directly from routes, controllers, methods.. 
        SwaggerUI  - builds rich, colorful interface for describing or API (down list, fonts, markup..)

    БАЗОВАЯ НАСТРОЙКА
    - install in main project (CE) PM> Install-Package Swashbuckle.AspNetCore
    - add SE.ConfigureSwagger().. (configure Swagger Middleware)
    - Program.cs > 
            builder.Services.ConfigureSwagger() (register swagger)
            app.UseSwagger() app.UseSwaggerUI() .... (middleware in pipeline)
    - slightly modify CompanyControllers(V2) with [ApiExplorerSettings(GroupName="v1...
            мы обозначили группы контроллеров (см. .swagger/v1 OpenApiInfo in SE, остальные добавятся ко всем т.к. не версионированы

    ИСПОЛЬЗОВАНИЕ
        browser> https://localhost:5001/swagger/v1/swagger.json - our api v1 json (change v2) 
        browser> https://localhost:5001/swagger/index.html - Swagger Document in HTML. Schemas div - explain DTO objects

    ДОБАВЛЕНИЕ АВТОРИЗАЦИИ (swagger security definintion)
        - modify SE.ConfigureSwagger with s.AddSecurityDefinition and s.AddSecurityRequirement
            after app reload we'll see locks against [Authorize] actions and controllers

    РАСШИРИТЬ ЗАГЛАВНОЕ ОПИСАНИЕ API
        - modify SE.ConfigureSwagger with s.SwaggerDoc adding Contact, Licence, etc.

    РАЗРЕШИТЬ XML ОПИСАНИЕ (учет описания из ///<summa... - C# комментариев
        - suppress 1591 warning for CEP project (action methods doesn't have /// - commets)
            modify CEP.csproj with <ProjectGroup Condition...NoWarn (2 of them)
        - modify SE.ConfigureSwagger() with xmlFile, xmlPath....
        - add /// commets in any method (CE.CompanyController.[GetCompanies() | CreateCompany(..]) with DataAnnotation ([ProducesResponseType(201)] if needed
 */
#endregion
#region D E P L O Y M E N T   IIS
/*
    Развертывание и разработка - это ПАРАЛЛЕЛЬНЫЕ ПРОЦЕССЫ, а не задача заключительной стадии
    Его нужно выполнять сразу в ПРОИЗВОДСТВЕННОЙ СРЕДЕ, чтобы понимать поведение разрабатываемой системы.
    
    ВЕРСИЯ УСТАНОВЛЕННОГО ASP NET CORE модуля
        - C:\Program Files\IIS\Asp.Net Core Module\V2
        - aspnetcore.dll -> Properties -> Details -> [File version | Product version]
    ПРОВЕРИТЬ СРЕДУ ИСПОЛНЕНИЯ
        - cmd> dotnet --list-runtimes
    БАЗОВАЯ НАСТРОЙКА
        - create "Publish" folder on local machine
        - main project (CE) -> click Publish -> local folder -> будет создан профиль публикации "C:\Employees\CompanyEmployees\CompanyEmployees\Properties\PublishProfiles\FolderProfile.pubxml"
        - скачать .NET Core Windows Server Hosting Bundle на ту систему, где установлен .NET Core Runtime
                Bundle содержит .NET Core Library и ASP.NET Core Module. Эта штука создаст реверсивный прокси между IIS и Kestrel,
                что критически важно для разработки
        - после установки проверьте, что PATH не сбился на путь к SDK
            cmd> dotnet --info (No SDK..detected чтобы не было)
            Если пропадет добавить в PATH С\Program Files\dotnet до С\Program Files(x86)\dotnet
        - активировать IIS
                Win-R -> control panel -> Программы -> включение и отключение компонентов Windows -> IIS
        - добавить в Windows host file (C\Windows\System32\drivers\etc) строчку
                127.0.0.1 www.companyemployees.codemaze
            (https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-aspnetcore-6.0.0-windows-hosting-bundle-installer) на офиц сайте может быть не доступен
             или https://dotnetcli.azureedge.net/dotnet/aspnetcore/Runtime/6.0.21/dotnet-hosting-6.0.21-win.exe
        - перезапустить IIS под админом
            - cmd> net stop was /y (остановить службу активации Windows и службу веб-публикации Windows)
              cmd> net start w3svc (запустить службу веб-публикаций Windows)

        - IIS (Win-R -> inetmgr)
            - добавить сайт 
                (ввести site name, physic path, host name)
            - базовые настройки пула (версия среды clr (Без управляемого кода))

                ASP Core ядро запускается в отдельном изолированном процессе и управляет своей средой выполнения. 
                Не зависит от загрузки CLR запущенной винды, на кот работает наш IIS. Отдельный CLR нужен чтобы изолированно разместить приложение api
                No managed code - необязательно, но рекомендовано.
            
            - конфигурация файла среды (сайт -> редактор конфигурации)
                (скопируем в IIS данные из веб-конфига API
                - section: system.webServer -> aspNetCore
                - from: application host config .. local path...
              Добавить данные и ключи
                - environmentVariable: add SECRET from win env variables (cmd>set)
    
             
 */
#endregion