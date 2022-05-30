using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Buffers;

namespace ReepoBot.Controllers.Binders;

public class NewtonsoftJsonBinder : IModelBinder
{
    readonly BodyModelBinder _bodyModelBinder;

    public NewtonsoftJsonBinder(
        IHttpRequestStreamReaderFactory readerFactory,
        ILoggerFactory loggerFactory,
        IOptions<MvcOptions> options,
        IOptions<MvcNewtonsoftJsonOptions> jsonOptions,
        ArrayPool<char> charPool,
        ObjectPoolProvider objectPoolProvider
        )
    {
        var inputFormatters = options.Value.InputFormatters.ToList();
        var jsonFormatterIndex = inputFormatters.FindIndex(formatter => formatter is SystemTextJsonInputFormatter);
        inputFormatters[jsonFormatterIndex] = new NewtonsoftJsonInputFormatter(
            loggerFactory.CreateLogger<NewtonsoftJsonInputFormatter>(),
            jsonOptions.Value.SerializerSettings,
            charPool,
            objectPoolProvider,
            options.Value,
            jsonOptions.Value);

        _bodyModelBinder = new BodyModelBinder(inputFormatters, readerFactory, loggerFactory, options.Value);
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        return _bodyModelBinder.BindModelAsync(bindingContext);
    }
}
