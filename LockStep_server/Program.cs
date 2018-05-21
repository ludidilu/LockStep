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

            BattleManager.Instance = new BattleManager();

            ServerAsync<PlayerUnit> server = new ServerAsync<PlayerUnit>();

            //ServerUdp<PlayerUnit> server = new ServerUdp<PlayerUnit>();

            if (args.Length == 2)
            {
                int minLagTime = int.Parse(args[0]);

                int maxLagTime = int.Parse(args[1]);

                server.OpenLagTest(minLagTime, maxLagTime);
            }

            //server.OpenLagTest(100, 100);

            server.Start("0.0.0.0", 1999, 100, 12000);

            //server.Start("0,0,0,0", 1999, 12000);

            Stopwatch watch = new Stopwatch();

            watch.Start();

            while (true)
            {
                long t0 = watch.ElapsedMilliseconds;

                BattleManager.Instance.Process(BattleManager.Instance.Update);

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
