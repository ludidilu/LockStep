using System;
using System.Collections.Generic;
using System.IO;

namespace LockStep_lib
{
    public class Unit
    {
        public int id;

        public double posX;
        public double posY;

        public int mouseX;
        public int mouseY;

        public void Fix()
        {
            posX = double.Parse(posX.ToString("f4"));
            posY = double.Parse(posY.ToString("f4"));
        }
    }

    public static class Core
    {
        public static Dictionary<int, Unit> unitDic = new Dictionary<int, Unit>();

        private static Random random = new Random();

        private static List<Unit> loginList = new List<Unit>();

        private static List<Unit> actionList = new List<Unit>();

        private static byte[] refreshData;

        public static void Init()
        {
        }

        public static byte[] ServerLogin(int _id)
        {
            byte[] bytes = ServerRefreshData();

            if (!unitDic.ContainsKey(_id))
            {
                Unit unit = new Unit();

                unit.id = _id;

                unit.posX = random.NextDouble() * Constant.WIDTH;

                unit.posY = random.NextDouble() * Constant.HEIGHT;

                unitDic.Add(_id, unit);

                loginList.Add(unit);
            }

            return bytes;
        }

        public static byte[] ServerRefreshData()
        {
            if (refreshData == null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write(unitDic.Count);

                        IEnumerator<KeyValuePair<int, Unit>> enumerator = unitDic.GetEnumerator();

                        while (enumerator.MoveNext())
                        {
                            bw.Write(enumerator.Current.Key);

                            Unit unit = enumerator.Current.Value;

                            bw.Write(unit.posX);

                            bw.Write(unit.posY);

                            bw.Write(unit.mouseX);

                            bw.Write(unit.mouseY);
                        }

                        refreshData = ms.ToArray();
                    }
                }
            }

            return refreshData;
        }

        public static void ServerGetCommand(int _id, int _mouseX, int _mouseY)
        {
            Log.Write("ServerGetCommand:" + _mouseX + "   " + _mouseY);

            Unit unit = unitDic[_id];

            if (_mouseX > Constant.MAX_MOUSE_DISTANCE)
            {
                _mouseX = Constant.MAX_MOUSE_DISTANCE;
            }
            else if (_mouseX < -Constant.MAX_MOUSE_DISTANCE)
            {
                _mouseX = -Constant.MAX_MOUSE_DISTANCE;
            }

            if (_mouseY > Constant.MAX_MOUSE_DISTANCE)
            {
                _mouseY = Constant.MAX_MOUSE_DISTANCE;
            }
            else if (_mouseY < -Constant.MAX_MOUSE_DISTANCE)
            {
                _mouseY = -Constant.MAX_MOUSE_DISTANCE;
            }

            if (unit.mouseX != _mouseX || unit.mouseY != _mouseY)
            {
                unit.mouseX = _mouseX;

                unit.mouseY = _mouseY;

                actionList.Add(unit);
            }
        }

        public static void ServerRefreshCommand(BinaryWriter _bw)
        {
            refreshData = null;

            _bw.Write(loginList.Count);

            for (int i = 0; i < loginList.Count; i++)
            {
                Unit unit = loginList[i];

                _bw.Write(unit.id);

                _bw.Write(unit.posX);

                _bw.Write(unit.posY);
            }

            loginList.Clear();

            _bw.Write(actionList.Count);

            for (int i = 0; i < actionList.Count; i++)
            {
                Unit unit = actionList[i];

                _bw.Write(unit.id);

                _bw.Write(unit.mouseX);

                _bw.Write(unit.mouseY);
            }

            actionList.Clear();
        }

        public static void Update()
        {
            IEnumerator<Unit> enumerator = unitDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Unit unit = enumerator.Current;

                if (unit.mouseX != 0 || unit.mouseY != 0)
                {
                    double angle = Math.Atan2(unit.mouseY, unit.mouseX);


                    double dis = Math.Sqrt(unit.mouseX * unit.mouseX + unit.mouseY * unit.mouseY) / Constant.MAX_MOUSE_DISTANCE * Constant.MAX_SPEED;

                    Log.Write("mouseX:" + unit.mouseX + "   mouseY:" + unit.mouseY + "  angle:" + angle + "    dis:" + dis);


                    double deltaX = Math.Cos(angle) * dis;

                    unit.posX += deltaX;

                    if (unit.posX > Constant.WIDTH)
                    {
                        unit.posX = Constant.WIDTH;
                    }
                    else if (unit.posX < 0)
                    {
                        unit.posX = 0;
                    }

                    double deltaY = Math.Sin(angle) * dis;

                    unit.posY += deltaY;

                    if (unit.posY > Constant.HEIGHT)
                    {
                        unit.posY = Constant.HEIGHT;
                    }
                    else if (unit.posY < 0)
                    {
                        unit.posY = 0;
                    }

                    Log.Write("deltaX:" + deltaX + "    deltaY:" + deltaY + "     x:" + unit.posX + "    y:" + unit.posY);

                    unit.Fix();

                    Log.Write("after     x:" + unit.posX + "    y:" + unit.posY);
                }
            }
        }

        public static void ClientGetRefreshData(BinaryReader _br)
        {
            unitDic.Clear();

            int num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                int id = _br.ReadInt32();

                double posX = _br.ReadDouble();

                double posY = _br.ReadDouble();

                int mouseX = _br.ReadInt32();

                int mouseY = _br.ReadInt32();

                Unit unit = new Unit();

                unit.id = id;

                unit.posX = posX;

                unit.posY = posY;

                unit.mouseX = mouseX;

                unit.mouseY = mouseY;

                unitDic.Add(unit.id, unit);
            }
        }

        public static void ClientGetRefreshCommand(BinaryReader _br)
        {
            int num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                int id = _br.ReadInt32();

                double posX = _br.ReadDouble();

                double posY = _br.ReadDouble();

                Unit unit = new Unit();

                unit.id = id;

                unit.posX = posX;

                unit.posY = posY;

                unitDic.Add(unit.id, unit);
            }

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                int id = _br.ReadInt32();

                int mouseX = _br.ReadInt32();

                int mouseY = _br.ReadInt32();

                Unit unit = unitDic[id];

                unit.mouseX = mouseX;

                unit.mouseY = mouseY;
            }
        }
    }
}
