
#nullable disable

using Microsoft.Win32;
using Spectre.Console;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

const string REMOTE_IP = "10.10.244.11";

int[] ports = [
    802, 804
];



var infoRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "njupt_webautologin");
Directory.CreateDirectory(infoRoot);

var usrNamePath = Path.Combine(infoRoot, "userName.txt");
var pwdPath = Path.Combine(infoRoot, "userPwd.txt");
var networkPath = Path.Combine(infoRoot, "network.txt");
var v4ipRegex = V46IpRegex();

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

AnsiConsole.MarkupLine("配置文件位置：" + infoRoot);
AnsiConsole.MarkupLine("[green]你可以删除上述文件以清除配置！[/]");

for (int i = 0; i < 10; i++)
{
    AnsiConsole.MarkupLine("[red]注意!: 配置文件中保存了你的智慧校园账户和密码，请不要将上述目录中的任何文件分享给任何人！！！！！[/]");
}

var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
};
var client = new HttpClient(handler);

var ua = client.DefaultRequestHeaders.UserAgent;
ua.Clear();
ua.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/152.0.0.0 Safari/537.36 Edg/152.0.0.0");

AnsiConsole.MarkupLine("检查端口...");

int curPort = 0;

for (int i = 0; i < 10; i++)
{

    foreach (var v in ports)
    {
        try
        {
            var sc = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await client.GetAsync($"https://{REMOTE_IP}:{v}", sc.Token);
            curPort = v;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"无法连接端口{v}: [red]{ex.Message}[/]");
        }
    }

    if (curPort != 0)
    {
        break;
    }

    await Task.Delay(TimeSpan.FromSeconds(1));
}

if (curPort == 0)
{
    AnsiConsole.MarkupLine("[red]无法连接到任何端口！[/]");
    Environment.Exit(-1);
}

AnsiConsole.MarkupLine($"当前可用端口: [green]{curPort}[/]");

async Task<string> GetStringRawAsync(string uri, List<string> secret = null)
{
    var uriDis = uri;

    if (secret != null)
    {
        foreach (var v in secret)
        {
            uriDis = uriDis.Replace(v, "***", StringComparison.OrdinalIgnoreCase)
                            .Replace(Uri.EscapeDataString(v), "***", StringComparison.OrdinalIgnoreCase);
        }
    }

    AnsiConsole.MarkupLine($"[grey]正在尝试访问：{uriDis}[/]");
    return await client.GetStringAsync(uri);
}

async Task<JsonObject> GetStringAsync(string path, Dictionary<string, string> args = null, List<string> secret = null)
{
    args ??= [];
    StringBuilder sb = new();
    foreach((var name, var val) in args)
    {
        sb.Append(name);
        sb.Append('=');
        sb.Append(Uri.EscapeDataString(val));
        sb.Append('&');
    }
    var uri = $"https://{REMOTE_IP}:{curPort}/{path}?{sb}";

    var resp = await GetStringRawAsync(uri, secret);
    var first = resp.IndexOf('{');
    var jsonStr = resp[first..];
    jsonStr = jsonStr[..(jsonStr.LastIndexOf('}') + 1)];
    var json = (JsonObject) JsonNode.Parse(jsonStr);

    Debug.Assert(json != null);

    return json;
}

async Task CheckLogin()
{
    AnsiConsole.MarkupLine("检查登录状态...");
    var status = await GetStringAsync("eportal/portal/online_list");
    if ((int)status["result"] == 1)
    {
        AnsiConsole.MarkupLine("[green]已登录！[/]");
        Environment.Exit(0);
    }
}

AnsiConsole.MarkupLine("正在获取信息中...");

var config = await GetStringAsync("eportal/portal/page/loadConfig");
var configData = config["data"];

if ((int)config["code"] != 1 ||
    configData["login_method"]?.ToString() != "1")
{
    AnsiConsole.MarkupLine("[red]很遗憾，这个程序好像过时了。qwq[/]");
    Environment.Exit(-1);
}

Debug.Assert((int)config["code"] == 1);

AnsiConsole.MarkupLine("正在获取WLAN IP中...");



var page_index = configData["page_index"].ToString();
var program_index = configData["program_index"].ToString();
var rcn = configData["rcn"]?.ToString();

string wlan_ip;
List<(string name, string id)> networks;

if (rcn != null)
{
    AnsiConsole.MarkupLine("接口1");
    var ipJson = await GetStringAsync("eportal/portal/index/getClientIp");
    wlan_ip = ipJson["wlan_user_ip"].ToString();

    networks = [
    ("校园网（收费）", ""),
    ("中国电信", "@njxy"),
    ("中国移动", "@njyd")
    ];
}
else
{
    AnsiConsole.MarkupLine("接口2");
    var htm = await GetStringRawAsync($"https://{REMOTE_IP}");
    wlan_ip = v4ipRegex.Match(htm).Groups["ip"].Value;

    networks = [
    ("校园网（收费）", ""),
    ("中国电信", "@njxy"),
    ("中国移动", "@cmcc")
    ];
}

Debug.Assert(!string.IsNullOrWhiteSpace(wlan_ip));

RE_TRY:

await CheckLogin();

if (File.Exists(usrNamePath) &&
    File.Exists(pwdPath) &&
    File.Exists(networkPath))
{
    //尝试登录
    var usr = File.ReadAllText(usrNamePath);
    var pwd = File.ReadAllText(pwdPath);
    var nw = File.ReadAllText(networkPath);

    usr = $",0,{usr}{nw}";

    Dictionary<string, string> loginReq;

    if(rcn == null)
    {
        loginReq = new()
        {
            ["callback"] = "dr1003",
            ["login_method"] = "1",
            ["user_account"] = usr,
            ["user_password"] = pwd,
            ["wlan_user_ip"] = wlan_ip,
            ["wlan_user_ipv6"] = "",
            ["wlan_user_mac"] = "000000000000",
            ["wlan_ac_ip"] = "",
            ["wlan_ac_name"] = "",
            ["jsVersion"] = "4.1.3",
            ["terminal_type"] = "1",
            ["lang"] = "zh-cn",
            ["v"] = Random.Shared.Next().ToString(),
        };
    }
    else
    {
        loginReq = new()
        {
            ["callback"] = "dr1003",
            ["login_method"] = "1",
            ["is_base64encode"] = "0",
            ["user_account"] = usr,
            ["user_password"] = pwd,
            ["wlan_user_ip"] = wlan_ip,
            ["wlan_user_ipv6"] = "",
            ["wlan_user_mac"] = "000000000000",
            ["wlan_vlan_id"] = "0",
            ["wlan_ac_ip"] = "",
            ["wlan_ac_name"] = "",
            ["authex_enable"] = "",
            ["jsVersion"] = "4.5",
            ["terminal_type"] = "1",
            ["lang"] = "zh-cn",
            ["user_agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/152.0.0.0 Safari/537.36 Edg/152.0.0.0",
            ["enable_r3"] = "0",
            ["mac_type"] = "0",
            ["rcn"] = rcn,
            ["operate"] = "portal_login",
            ["business_type"] = "1",
            ["program_index"] = program_index,
            ["page_index"] = page_index,
            ["v"] = Random.Shared.Next().ToString(),
            ["lang"] = "zh"
        };
    }

    AnsiConsole.MarkupLine("正在尝试登录中...");
    var result = await GetStringAsync("eportal/portal/login", loginReq, [usr, pwd]);
    if ((int)result["result"] == 1)
    {
        AnsiConsole.MarkupLine("[green]登录成功！[/]");
        Environment.Exit(0);
    }

    await CheckLogin();
}

AnsiConsole.MarkupLine("[red]登录失败！请重试。[/]");
AnsiConsole.MarkupLine("[red]如果多次失败，可能是该程序已过时！！！[/]");

RE_CHOICE_NETWORK:
var selection = new SelectionPrompt<string>();
foreach(var v in networks)
{
    selection.AddChoice(v.name);
}
selection.Title("请选择校园网提供商：");
selection.Mode(SelectionMode.Independent);
var cur_network = await selection.ShowAsync(AnsiConsole.Console, default);

if(!(await AnsiConsole.ConfirmAsync($"确定选择{cur_network}吗？", false)))
{
    goto RE_CHOICE_NETWORK;
}

var cur_network_id = networks.First(x => x.name == cur_network).id;
File.WriteAllText(networkPath, cur_network_id);

RE_INPUT_NAME:
var usrNameStr = await AnsiConsole.PromptAsync(
    new TextPrompt<string>("请输入你的学号（例如: B12345678 ）（留空以使用原学号）：")
        .AllowEmpty()
    );

if(!string.IsNullOrWhiteSpace(usrNameStr))
{
    if (!usrNameStr.StartsWith('B'))
    {
        AnsiConsole.MarkupLine("[red]无效的学号！[/]");
        goto RE_INPUT_NAME;
    }
    File.WriteAllText(usrNamePath, usrNameStr);
}

var pwdStr = await AnsiConsole.PromptAsync(
    new TextPrompt<string>("请输入智慧校园密码（留空以使用原密码）:")
        .Secret()
        .AllowEmpty()
    );


if (!string.IsNullOrWhiteSpace(pwdStr))
{
    File.WriteAllText(pwdPath, pwdStr);
}


goto RE_TRY;



partial class Program
{
    [GeneratedRegex(
        @"v46ip\s*=\s*['""](?<ip>[0-9]{1,3}(?:\.[0-9]{1,3}){3})['""]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex V46IpRegex();

}
