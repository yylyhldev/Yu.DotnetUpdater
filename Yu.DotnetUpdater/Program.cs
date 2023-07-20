﻿// See https://aka.ms/new-console-template for more information
//using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Yu.DotnetUpdater;

string processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
_ = new Mutex(true, processName, out bool FirstOpen);
if (FirstOpen == false)
{
    Console.WriteLine($"{processName} 已开过了");
    Environment.Exit(0);
}
Console.Title = "服务更新工具";
Util.WriteGreen("待更新的项目各自配置文件需提前手动更新好，如appsettings.json");
Util.Info("将待更新的项目各自打包(projectName.zip)放置本程序根目录下");
Util.Info(string.Empty);

#region 开始更新
Util.Configuration = new ConfigurationBuilder()
    .Add(new Microsoft.Extensions.Configuration.Json.JsonConfigurationSource { Path = "appsettings.json", ReloadOnChange = true })
    //.Add(new Microsoft.Extensions.Configuration.Json.JsonConfigurationSource { Path = "updatesettings.json", ReloadOnChange = true })
    .Build();
var stopwatch = new System.Diagnostics.Stopwatch();
var updateConf = Util.Configuration.GetSection("Services").Get<UpdateServiceConf[]>();
if (updateConf == null || !updateConf.Any())
{
    Util.WriteYellow(">无待更新服务：appsetting.json-Services");
    Close(stopwatch, 1);
    return;
}
for (var i = 0; i < updateConf.Length; i++)
{
    Util.Info($">{(i < 10 ? " " : "") + i}：{updateConf[i].UpdatePack}");
}
Util.Info($">请输入待更新项目索引，多个以',/，'分隔......");
var inputVal = Console.ReadLine();
if (string.IsNullOrWhiteSpace(inputVal))
{
    Close(stopwatch, 1);
    return;
}
inputVal = inputVal.Replace("，", ",");
var vals = inputVal.Split(',').Where(d => !string.IsNullOrWhiteSpace(d)).Distinct()
    .Select(d => int.Parse(d))
    .Where(d => d < updateConf.Length && d >= 0).ToArray();
if (!vals.Any())
{
    Util.Info($">请正确输入待更新项目索引，多个以',/，'分隔......");
    inputVal = Console.ReadLine();
    return;
}
Util.Info($">------------更新项目------------");
foreach (var i in vals)
{
    Util.Info($">{updateConf[i].UpdatePack}");
}
Util.Info($">------------按[y/Y]确认------------");
var confim = Console.ReadLine();
if (string.Compare(confim, "y", true) != 0)
{
    Util.WriteYellow(">取消更新");
    Close(stopwatch, 1);
    return;
}

Util.Info($">更新开始[{DateTime.Now:HH:mm:ss.fff}]...\n");
stopwatch.Restart();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    DeployWindows.StartForWindow(vals, updateConf);
    Close(stopwatch, 10);
}
else
{
    Util.WriteYellow("不支持当前系统");
    Close(stopwatch, 5);
}
#endregion

static void Close(System.Diagnostics.Stopwatch stopwatch, int sec)
{
    stopwatch.Stop();
    Util.WriteGreen($"\n>更新结束[{DateTime.Now:HH:mm:ss.fff}]，总耗时：{stopwatch.ElapsedMilliseconds}ms");
    if (Util.WarnOrError) Util.WriteYellow($">>>有错误或警告需关注");
    Util.Info($">{sec}秒后自动退出");
    Thread.Sleep(sec * 1000);
    Environment.Exit(0);
}