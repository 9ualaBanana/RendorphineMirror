using System.Net.Http.Headers;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeUI.Pages.MainWindowTabs;

public class RegistryTab : Panel
{
    public RegistryTab() => Reload().Consume();

    async Task Reload()
    {
        Children.Clear();

        var softlist = (await Apis.GetSoftwareAsync()).ThrowIfError();
        Children.Add(new StackPanel()
        {
            Children =
            {
                new MPButton()
                {
                    Text = "+ add soft",
                    Margin = new Thickness(0, 0, 0, bottom: 20),
                    OnClickSelf = addSoft,
                },
                NamedList.Create("Software Registry", softlist, x => softToControl(x.Key, x.Value)),
            },
        });


        static Task setTextTimed(MPButton button, string text, int duration) => button.TemporarySetText(text, duration);
        async void addSoft(MPButton button)
        {
            var softname = "NewSoftTodo";
            var soft = new SoftwareDefinition("New Soft Todo", ImmutableDictionary<string, SoftwareVersionDefinition>.Empty, null, ImmutableArray<string>.Empty);

            var send = await LocalApi.Post(Settings.RegistryUrl, $"addsoft?name={HttpUtility.UrlEncode(softname)}",
                new StringContent(JsonConvert.SerializeObject(soft)) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

            if (!send) await setTextTimed(button, "err " + send.AsString(), 2000);
            else await Reload();
        }
        IControl softToControl(string softname, SoftwareDefinition soft)
        {
            // return TaskCreationWindow.Settings.Create(new("_aed_", JObject.FromObject(soft)), FieldDescriber.Create(typeof(SoftwareDefinition)));

            var softnametb = new TextBox() { Text = soft.VisualName };
            var addnewbtn = new MPButton()
            {
                Text = "+ add version",
                OnClick = async () =>
                {
                    var vername = "1.0.0-todo";
                    var ver = new SoftwareVersionDefinition("<installscript>");

                    var send = await LocalApi.Post(Settings.RegistryUrl, $"addver?name={HttpUtility.UrlEncode(softname)}&version={HttpUtility.UrlEncode(vername)}",
                        new StringContent(JsonConvert.SerializeObject(ver)) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                    await Reload();
                },
            };

            var content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new MPButton() { Text = "update software", OnClickSelf = updateSoft, },
                    softnametb,
                    addnewbtn,
                    NamedList.Create(softname, soft.Versions,
                        x => new Expander()
                        {
                            Header = x.Key,
                            Margin = new Thickness(left: 20, 0, 0, 0),
                            Content = verToControl(x.Key, x.Value),
                        }
                    ),
                },
            };

            return new Expander()
            {
                Header = softname,
                Content = content,
            };


            async void updateSoft(MPButton button)
            {
                var json = new JObject() { ["VisualName"] = softnametb.Text, };

                var send = await LocalApi.Post(Settings.RegistryUrl, $"editsoft?name={HttpUtility.UrlEncode(softname)}",
                    new StringContent(json.ToString()) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                await setTextTimed(button, send ? "send!!!!" : ("err " + send.AsString()), 2000);
            }
            IControl verToControl(string vername, SoftwareVersionDefinition version)
            {
                var delbtn = new MPButton()
                {
                    Text = "!!! DELETE VERSION !!!",
                    Margin = new Thickness(0, 0, 0, bottom: 20),
                    OnClickSelf = deleteVersion,
                };

                var versiontb = new TextBox() { Text = vername };
                var installtb = new TextBox()
                {
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    Text = version.InstallScript,
                };

                var updatebtn = new MPButton() { Text = "send", OnClickSelf = updateVersion, };

                return new StackPanel()
                {
                    Margin = new Thickness(20, 0, 0, 0),
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        delbtn,
                        versiontb,
                        installtb,
                        updatebtn,
                    },
                };


                async void updateVersion(MPButton updatebtn)
                {
                    var json = new JObject()
                    {
                        ["InstallScript"] = installtb.Text,
                    };

                    var send = await LocalApi.Post(Settings.RegistryUrl, $"editver?name={HttpUtility.UrlEncode(softname)}&version={HttpUtility.UrlEncode(vername)}&newversion={HttpUtility.UrlEncode(versiontb.Text)}",
                        new StringContent(json.ToString()) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                    await setTextTimed(updatebtn, send ? "send!!!!" : ("err " + send.AsString()), 2000);
                }
                async void deleteVersion(MPButton delbtn)
                {
                    var send = await LocalApi.Send(Settings.RegistryUrl, $"delver?name={HttpUtility.UrlEncode(softname)}&version={HttpUtility.UrlEncode(vername)}");
                    if (!send) await setTextTimed(delbtn, "err " + send.AsString(), 2000);
                    else await Reload();
                }
            }
        }
    }
}
