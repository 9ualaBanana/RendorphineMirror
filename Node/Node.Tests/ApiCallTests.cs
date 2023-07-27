using System.Net;

namespace Node.Tests;

[TestFixture]
public class ApiCallTests
{
    [Test]
    public async Task Test()
    {
        static async ValueTask<OperationResult<JToken>> request(HttpStatusCode status, JObject? response)
        {
            return await Api.ResponseJsonToOpResult(
                new HttpResponseMessage(status),
                null,
                response,
                null,
                false,
                default
            );
        }


        #region m+

        await request(HttpStatusCode.OK, new JObject() { ["ok"] = 0, ["errorcode"] = 2, ["errormessage"] = "err" })
            .With(req => req.Success.Should().BeFalse())
            .With(req => req.HttpData.ThrowIfValueNull()
                .With(http => http.IsSuccessStatusCode.Should().BeTrue())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.OK))
                .With(http => http.ErrorCode.Should().Be(2))
            );


        await request(HttpStatusCode.OK, new JObject() { ["ok"] = 1 })
            .With(req => req.Success.Should().BeTrue())
            .With(req => req.HttpData.ThrowIfValueNull()
                .With(http => http.IsSuccessStatusCode.Should().BeTrue())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.OK))
                .With(http => http.ErrorCode.Should().BeNull())
            );


        await request(HttpStatusCode.BadRequest, null)
            .With(req => req.Success.Should().BeFalse())
            .With(req => req.HttpData.ThrowIfValueNull()
                .With(http => http.IsSuccessStatusCode.Should().BeFalse())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.BadRequest))
                .With(http => http.ErrorCode.Should().BeNull())
            );

        #endregion

        #region qwerty


        await request(HttpStatusCode.OK, new JObject() { ["errorcode"] = 2, ["errormessage"] = "err" })
            .With(req => req.Success.Should().BeFalse())
            .With(req => req.HttpData.ThrowIfValueNull()
                .With(http => http.IsSuccessStatusCode.Should().BeTrue())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.OK))
                .With(http => http.ErrorCode.Should().Be(2))
            );


        await request(HttpStatusCode.OK, new JObject() { })
            .With(req => req.Success.Should().BeTrue())
            .With(req => req.HttpData.ThrowIfValueNull()
                .With(http => http.IsSuccessStatusCode.Should().BeTrue())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.OK))
                .With(http => http.ErrorCode.Should().BeNull())
            );


        await request(HttpStatusCode.BadRequest, new JObject() { ["errorcode"] = 2, ["errormessage"] = "err" })
            .With(req => req.Success.Should().BeFalse())
            .With(req => req.HttpData.ThrowIfValueNull()
                .With(http => http.IsSuccessStatusCode.Should().BeFalse())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.BadRequest))
                .With(http => http.ErrorCode.Should().Be(2))
            );


        await request(HttpStatusCode.BadRequest, null)
            .With(req => req.Success.Should().BeFalse())
            .With(req => req.HttpData.ThrowIfValueNull()
                .With(http => http.IsSuccessStatusCode.Should().BeFalse())
                .With(http => http.StatusCode.Should().Be(HttpStatusCode.BadRequest))
                .With(http => http.ErrorCode.Should().BeNull())
            );

        #endregion
    }
}
