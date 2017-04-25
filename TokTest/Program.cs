using System;
using OpenTokSDK;
namespace TokTest
{
    class Program
    {
        static void Main(string[] args)
        {
            OpenTok t = new OpenTok(45770902, "34ebc68fe8aeda675079e4abf0dcb9db2a836d7f");
            var n = t.CreateSession();
            Console.WriteLine(n.ApiKey + " " + n.ApiSecret + " " + n.GenerateToken());
        }
    }
}
