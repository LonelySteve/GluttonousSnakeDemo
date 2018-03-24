using System;
using System.Drawing;

namespace GluttonousSnakeDemo
{
    class Program
    {
        public const int minLen = 5;
        public const int maxLen = 8;
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("这是经典游戏贪吃蛇的C#控制台Demo版本 By JLoeve\n按任意键开始游戏~~");
            Console.WriteLine("按AWSD或方向键控制由 {0} 组成的贪吃蛇\n注意不要让它撞到自己或者由 {1} 组成的围墙", GluttonousSnakeGame.SnakeChar, GluttonousSnakeGame.WallChar);
            Console.ReadKey(true);
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            var game = new GluttonousSnakeGame(new Size(30, 25), 5, 15);
            game.Moved += (o, e) =>
            {
                if (e.Status == GluttonousSnakeGame.MovedStatus.Normal)
                    Console.WriteLine("当前用时：{0}  分数：{1}   剩余目标数：{2}", e.UsedTime, e.Score * 100, e.RemainTarVal);
                else if (e.Status == GluttonousSnakeGame.MovedStatus.Success)
                {
                    Console.WriteLine("恭喜通关！总分数：{0} 总用时：{1}", e.Score * 100, e.UsedTime);
                    game.Stop();
                }
                else if (e.Status == GluttonousSnakeGame.MovedStatus.Fail)
                {
                    Console.WriteLine("游戏失败！总分数：{0} 总用时：{1}", e.Score * 100, e.UsedTime);
                    game.Stop();
                }
            };
            game.Start();
            Console.ReadLine();
        }
    }
}
