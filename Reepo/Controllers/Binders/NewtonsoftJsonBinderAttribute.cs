using Microsoft.AspNetCore.Mvc;

namespace ReepoBot.Controllers.Binders;

public class NewtonsoftJsonBinderAttribute : ModelBinderAttribute
{
    public NewtonsoftJsonBinderAttribute() : base(typeof(NewtonsoftJsonBinder))
    {
        BindingSource = Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body;
    }
}
