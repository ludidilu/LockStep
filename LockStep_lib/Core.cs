using System;
using System.Collections.Generic;
using System.IO;

namespace LockStep_lib
{
    public class Unit
    {
        public double posX;
        public double posY;

        public int mouseX;
        public int mouseY;

        public void Fix()
        {
            posX = double.Parse(posX.ToString("f4"));
            posY = double.Parse(posX.ToString("f4"));
        }
    }

    public static class Core
    {
        public static Dictionary<int, Unit> unitDic = new Dictionary<int, Unit>();

        private static Random random = new Random();

        private static double max_mouse_distance;

        public static void Init()
        {
            max_mouse_distance = Math.Sqrt(2) * Constant.MAX_MOUSE_DISTANCE;
        }

        public static MemoryStream Login(int _id)
        {
            if (!unitDic.ContainsKey(_id))
            {
                Unit unit = new Unit();

                unit.posX = random.NextDouble() * Constant.WIDTH;

                unit.posY = random.NextDouble() * Constant.HEIGHT;

                unit.mouseX = unit.mouseY = 0;

                unitDic.Add(_id, unit);
            }

            return Refresh();
        }

        public static MemoryStream Refresh()
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

                    return ms;
                }
            }
        }

        public static void GetCommand(int _id, int _mouseX, int _mouseY)
        {
            Unit unit = unitDic[_id];

            if (_mouseX > Constant.MAX_MOUSE_DISTANCE)
            {
                _mouseX = Constant.MAX_MOUSE_DISTANCE;
            }
            else if (_mouseX < Constant.MAX_MOUSE_DISTANCE)
            {
                _mouseX = -Constant.MAX_MOUSE_DISTANCE;
            }

            if (_mouseY > Constant.MAX_MOUSE_DISTANCE)
            {
                _mouseY = Constant.MAX_MOUSE_DISTANCE;
            }
            else if (_mouseY < Constant.MAX_MOUSE_DISTANCE)
            {
                _mouseY = -Constant.MAX_MOUSE_DISTANCE;
            }

            unit.mouseX = _mouseX;

            unit.mouseY = _mouseY;
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

                    double dis = Math.Sqrt(unit.mouseX * unit.mouseX + unit.mouseY * unit.mouseY) / max_mouse_distance * Constant.MAX_SPEED;

                    unit.posX += Math.Cos(angle) * dis;

                    if (unit.posX > Constant.WIDTH)
                    {
                        unit.posX = Constant.WIDTH;
                    }
                    else if (unit.posX < 0)
                    {
                        unit.posX = 0;
                    }

                    unit.posY += Math.Sin(angle) * dis;

                    if (unit.posY > Constant.HEIGHT)
                    {
                        unit.posY = Constant.HEIGHT;
                    }
                    else if (unit.posY < 0)
                    {
                        unit.posY = 0;
                    }

                    unit.Fix();
                }
            }
        }
    }
}
