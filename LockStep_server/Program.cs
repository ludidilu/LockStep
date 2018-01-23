using System;
using Connection;
using System.Diagnostics;
using System.Threading;
using LockStep_lib;

namespace LockStep_server
{
    class Program
    {
        static void Main(string[] args)
        {
            Connection.Log.Init(Console.WriteLine);

            Server<PlayerUnit> server = new Server<PlayerUnit>();

            server.Start("0.0.0.0", 1999, 100, 12000);

            Stopwatch watch = new Stopwatch();

            while (true)
            {
                watch.Reset();

                server.Update();

                BattleManager.Instance.Update();

                watch.Stop();

                int time = Constant.TICK_TIME - (int)watch.ElapsedMilliseconds;

                Thread.Sleep(time);
            }
        }
    }
}
