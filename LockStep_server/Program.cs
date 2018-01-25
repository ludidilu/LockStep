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

            LockStep_lib.Log.Init(Console.WriteLine);

            Core.Init();

            Server<PlayerUnit> server = new Server<PlayerUnit>();

            if (args.Length == 2)
            {
                int minLagTime = int.Parse(args[0]);

                int maxLagTime = int.Parse(args[1]);

                server.OpenLagTest(minLagTime, maxLagTime);
            }

            server.OpenLagTest(100, 200);

            server.Start("0.0.0.0", 1999, 100, 12000);

            Stopwatch watch = new Stopwatch();

            watch.Start();

            while (true)
            {
                long t0 = watch.ElapsedMilliseconds;

                server.Update();

                BattleManager.Instance.Update();

                long t1 = watch.ElapsedMilliseconds;

                int deltaTime = (int)(t1 - t0);

                int time = Constant.TICK_TIME - deltaTime;

                if (time > 0)
                {
                    Thread.Sleep(time);
                }
            }
        }
    }
}
