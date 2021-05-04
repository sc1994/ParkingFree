using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShellProgressBar;

namespace ParkingFree
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var bearer = File
                .ReadAllLines("Bearer")
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                ?.Trim();
            if (string.IsNullOrWhiteSpace(bearer))
            {
                Error("没有获取到有效的Bearer配置");
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + bearer);

            var errorCount = 0;
            while (true)
            {
                var id = GetId();

                var res1 = await client.GetAsync("https://3rd.merculet.cn/lec/k8sprod/activity/api/v1/rp/user/startInteraction?currentRpId=" + id);
                System.Console.WriteLine($"进入详情页: {id}, 等待页面倒计时");
                var str1 = await res1.Content.ReadAsStringAsync();
                if (JsonConvert.DeserializeObject<JObject>(str1)["code"].ToString() != "200")
                {
                    System.Console.WriteLine(str1);
                    if (errorCount++ > 15)
                    {
                        Error($"连续失败次数达到{errorCount}次, 系统将停止运行.");
                    }
                    if (JsonConvert.DeserializeObject<JObject>(str1)["code"].ToString() == "401")
                    {
                        Error("Bearer 失效 , 需要重新获取");
                    }
                    continue;
                }

                try
                {
                    var awaitTime = new Random().Next(8000, 10000); // 6.0s ~ 7.0s
                    var options = new ProgressBarOptions
                    {
                        ProgressCharacter = '▇',
                        ProgressBarOnBottom = true
                    };
                    var time = DateTime.Now;
                    using (var pbar = new ProgressBar(awaitTime, "Initial message", options))
                    {
                        var lag = 0;
                        while (lag <= awaitTime)
                        {
                            lag = int.Parse((DateTime.Now - time).TotalMilliseconds.ToString("0"));
                            pbar.Tick(lag);
                            pbar.Tick($"模拟广告等待 {awaitTime} 毫秒...");
                            await Task.Delay(50);
                        }
                        pbar.Tick($"等待广告结束");
                    }
                }
                catch
                {
                    Console.ResetColor();
                }

                System.Console.WriteLine();
                System.Console.WriteLine("-------查询看广告的结果-------");
                var res2 = await client.GetAsync("https://3rd.merculet.cn/lec/k8sprod/activity/api/v1/rp/user/getBindingStatus?rpParentId=" + id);
                var str2 = await res2.Content.ReadAsStringAsync();
                if (JsonConvert.DeserializeObject<JObject>(str2)["code"].ToString() != "200")
                    System.Console.WriteLine(str2);
                else
                    System.Console.WriteLine(200);

                var res3 = await client.GetAsync("https://3rd.merculet.cn/lec/k8sprod/activity/api/v1/rp/user/getRpDetail?rpParentId=" + id);
                var str3 = await res3.Content.ReadAsStringAsync();
                if (JsonConvert.DeserializeObject<JObject>(str3)["code"].ToString() != "200")
                    System.Console.WriteLine(str3);
                else
                    System.Console.WriteLine(200);

                System.Console.WriteLine("-------尝试领取奖励-------");
                var res4 = await client.GetAsync("https://3rd.merculet.cn/lec/k8sprod/activity/api/v1/rp/user/tryReward?puzzleNum=0&rpParentId=" + id);
                var str4 = await res4.Content.ReadAsStringAsync();
                System.Console.WriteLine($"奖励金额: [{GetAmount(str4)}]");
                if (JsonConvert.DeserializeObject<JObject>(str4)["code"].ToString() != "200")
                {
                    System.Console.WriteLine(str4);
                    if (errorCount++ > 15)
                    {
                        Error($"连续失败次数达到{errorCount}次, 系统将停止运行.");
                    }
                    continue;
                }

                errorCount = 0;
                System.Console.WriteLine();
                System.Console.WriteLine();
                System.Console.WriteLine("--------------进入下一轮--------------");
                System.Console.WriteLine();
                System.Console.WriteLine();
            }
        }

        static string GetAmount(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<JObject>(json)["body"]["lebi"].ToString();
            }
            catch { }
            return "0";
        }

        static void Error(string msg)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(msg);
            Console.ResetColor();
            Console.WriteLine("按任意键结束...");
            Console.ReadLine();
            throw new Exception("程序异常中断");
        }

        static string[] Ids = new[]
        {
            "440705593179635718",
            "440705593179635721",
            "440705593179635719",
            "440705593179635720",
            "440705593179635722"
        };

        static string GetId()
        {
            return Ids[new Random().Next(0, Ids.Length)];
        }
    }
}
