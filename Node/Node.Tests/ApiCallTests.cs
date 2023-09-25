using System.Net;

namespace Node.Tests;

[TestFixture]
public class ApiCallTests
{
    [Test]
    public async Task Test()
    {
        var builder = Init.CreateContainer(new Init.InitConfig("renderfin"), typeof(ApiCallTests).Assembly);

        builder.RegisterType<HttpClient>()
            .SingleInstance();

        builder.RegisterType<Api>()
            .SingleInstance();

        builder.RegisterType<ElevenLabsApi>()
            .SingleInstance()
            .WithParameter("apiKey", "123");

        using var container = builder.Build();
        var api = container.Resolve<Api>();

        async Task<OperationResult<JToken>> requestapi(ApiBase api, HttpStatusCode status, JObject? response)
        {
            return await api.ResponseJsonToOpResult(
                new HttpResponseMessage(status),
                response,
                "Testing",
                default
            );
        }

        async Task<OperationResult<JToken>> request(HttpStatusCode status, JObject? response) => await requestapi(api, status, response);


        #region m+

        await request(HttpStatusCode.OK, new JObject() { ["ok"] = 0, ["errorcode"] = 2, ["errormessage"] = "err" })
            .With(req => req.Success.Should().BeFalse())
            .With(req => (req.Error as HttpError).ThrowIfNull()
                .With(http => http.IsSuccessStatusCode.Should().BeTrue())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.OK))
                .With(http => http.ErrorCode.Should().Be(2))
                .With(http => http.Message.Should().Be("err"))
            );


        await request(HttpStatusCode.OK, new JObject() { ["ok"] = 1 })
            .With(req => req.Success.Should().BeTrue())
            .With(req => req.Error.Should().BeNull());


        await request(HttpStatusCode.BadRequest, null)
            .With(req => req.Success.Should().BeFalse())
            .With(req => (req.Error as HttpError).ThrowIfNull()
                .With(http => http.IsSuccessStatusCode.Should().BeFalse())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.BadRequest))
                .With(http => http.ErrorCode.Should().BeNull())
                .With(http => http.Message.Should().BeNull())
            );

        #endregion

        #region qwerty

        await request(HttpStatusCode.OK, new JObject() { ["errorcode"] = 2, ["errormessage"] = "err" })
            .With(req => req.Success.Should().BeFalse())
            .With(req => (req.Error as HttpError).ThrowIfNull()
                .With(http => http.IsSuccessStatusCode.Should().BeTrue())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.OK))
                .With(http => http.ErrorCode.Should().Be(2))
                .With(http => http.Message.Should().Be("err"))
            );


        await request(HttpStatusCode.OK, new JObject() { })
            .With(req => req.Success.Should().BeTrue())
            .With(req => req.Error.Should().BeNull());


        await request(HttpStatusCode.BadRequest, new JObject() { ["errorcode"] = 2, ["errormessage"] = "err" })
            .With(req => req.Success.Should().BeFalse())
            .With(req => (req.Error as HttpError).ThrowIfNull()
                .With(http => http.IsSuccessStatusCode.Should().BeFalse())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.BadRequest))
                .With(http => http.ErrorCode.Should().Be(2))
                .With(http => http.Message.Should().Be("err"))
            );


        await request(HttpStatusCode.BadRequest, null)
            .With(req => req.Success.Should().BeFalse())
            .With(req => (req.Error as HttpError).ThrowIfNull()
                .With(http => http.IsSuccessStatusCode.Should().BeFalse())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.BadRequest))
                .With(http => http.ErrorCode.Should().BeNull())
                .With(http => http.Message.Should().BeNull())
            );

        #endregion

        #region elevenlabs
        var eapi = container.Resolve<ElevenLabsApi>();
        async Task<OperationResult<JToken>> requesteleven(HttpStatusCode status, JObject? response) => await requestapi(eapi, status, response);

        await requesteleven(HttpStatusCode.OK, new JObject() { ["detail"] = new JObject() { ["message"] = "err" } })
            .With(req => req.Success.Should().BeTrue())
            .With(req => req.Error.Should().BeNull());


        await requesteleven(HttpStatusCode.OK, new JObject() { })
            .With(req => req.Success.Should().BeTrue())
            .With(req => req.Error.Should().BeNull());


        await requesteleven(HttpStatusCode.BadRequest, new JObject() { ["detail"] = new JObject() { ["message"] = "err" } })
            .With(req => req.Success.Should().BeFalse())
            .With(req => (req.Error as HttpErrorBase).ThrowIfNull()
                .With(http => http.IsSuccessStatusCode.Should().BeFalse())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.BadRequest))
                .With(http => http.Message.Should().Be("err"))
            );


        await requesteleven(HttpStatusCode.BadRequest, null)
            .With(req => req.Success.Should().BeFalse())
            .With(req => (req.Error as HttpErrorBase).ThrowIfNull()
                .With(http => http.IsSuccessStatusCode.Should().BeFalse())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.BadRequest))
                .With(http => http.Message.Should().BeNull())
            );

        #endregion
    }
}
