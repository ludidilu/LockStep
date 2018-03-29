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

    public class Bullet : Unit
    {
        public int parent;
        public int life;
    }

    public static class Core
    {
        public static Dictionary<int, Unit> unitDic = new Dictionary<int, Unit>();

        public static Dictionary<int, Bullet> bulletDic = new Dictionary<int, Bullet>();

        private static Random random = new Random();

        private static List<Unit> loginList = new List<Unit>();

        private static List<Unit> moveList = new List<Unit>();

        private static List<Bullet> shootList = new List<Bullet>();

        private static byte[] refreshData;

        private static int bulletId;

        private static int GetBulletId()
        {
            bulletId++;

            return bulletId;
        }

        public static void Init()
        {
            bulletId = 0;
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

                        bw.Write(bulletDic.Count);

                        IEnumerator<KeyValuePair<int, Bullet>> enumerator2 = bulletDic.GetEnumerator();

                        while (enumerator2.MoveNext())
                        {
                            bw.Write(enumerator2.Current.Key);

                            Bullet bullet = enumerator2.Current.Value;

                            bw.Write(bullet.parent);

                            bw.Write(bullet.life);

                            bw.Write(bullet.posX);

                            bw.Write(bullet.posY);

                            bw.Write(bullet.mouseX);

                            bw.Write(bullet.mouseY);
                        }

                        refreshData = ms.ToArray();
                    }
                }
            }

            return refreshData;
        }

        public static void ServerGetCommand(int _id, BinaryReader _br)
        {
            Unit unit = unitDic[_id];

            byte type = _br.ReadByte();

            int mouseX = _br.ReadInt32();

            int mouseY = _br.ReadInt32();

            switch (type)
            {
                case 0:

                    if (mouseX > Constant.MAX_MOUSE_DISTANCE)
                    {
                        mouseX = Constant.MAX_MOUSE_DISTANCE;
                    }
                    else if (mouseX < -Constant.MAX_MOUSE_DISTANCE)
                    {
                        mouseX = -Constant.MAX_MOUSE_DISTANCE;
                    }

                    if (mouseY > Constant.MAX_MOUSE_DISTANCE)
                    {
                        mouseY = Constant.MAX_MOUSE_DISTANCE;
                    }
                    else if (mouseY < -Constant.MAX_MOUSE_DISTANCE)
                    {
                        mouseY = -Constant.MAX_MOUSE_DISTANCE;
                    }

                    if (unit.mouseX != mouseX || unit.mouseY != mouseY)
                    {
                        unit.mouseX = mouseX;

                        unit.mouseY = mouseY;

                        moveList.Add(unit);
                    }

                    break;

                case 1:

                    if (unit.mouseX == 0 && unit.mouseY == 0)
                    {
                        Bullet bullet = new Bullet();

                        bullet.id = GetBulletId();

                        bullet.parent = _id;

                        bullet.life = Constant.BULLET_LIFE_TICK;

                        bullet.posX = unit.posX;

                        bullet.posY = unit.posY;

                        bullet.mouseX = mouseX;

                        bullet.mouseY = mouseY;

                        shootList.Add(bullet);

                        bulletDic.Add(bullet.id, bullet);
                    }

                    break;
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

            _bw.Write(moveList.Count);

            for (int i = 0; i < moveList.Count; i++)
            {
                Unit unit = moveList[i];

                _bw.Write(unit.id);

                _bw.Write(unit.mouseX);

                _bw.Write(unit.mouseY);
            }

            moveList.Clear();

            _bw.Write(shootList.Count);

            for (int i = 0; i < shootList.Count; i++)
            {
                Bullet bullet = shootList[i];

                _bw.Write(bullet.id);

                _bw.Write(bullet.parent);

                _bw.Write(bullet.mouseX);

                _bw.Write(bullet.mouseY);
            }

            shootList.Clear();
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

                    double deltaX = Math.Cos(angle) * dis;

                    unit.posX += deltaX;

                    if (unit.posX + Constant.RADIUS > Constant.WIDTH)
                    {
                        unit.posX = Constant.WIDTH - Constant.RADIUS;
                    }
                    else if (unit.posX - Constant.RADIUS < 0)
                    {
                        unit.posX = Constant.RADIUS;
                    }

                    double deltaY = Math.Sin(angle) * dis;

                    unit.posY += deltaY;

                    if (unit.posY + Constant.RADIUS > Constant.HEIGHT)
                    {
                        unit.posY = Constant.HEIGHT - Constant.RADIUS;
                    }
                    else if (unit.posY - Constant.RADIUS < 0)
                    {
                        unit.posY = Constant.RADIUS;
                    }

                    unit.Fix();
                }
            }

            List<int> delList = null;

            IEnumerator<Bullet> enumerator2 = bulletDic.Values.GetEnumerator();

            while (enumerator2.MoveNext())
            {
                List<int> hitList = null;

                bool delete = false;

                Bullet bullet = enumerator2.Current;

                double angle = Math.Atan2(bullet.mouseY, bullet.mouseX);

                double deltaX = Math.Cos(angle) * Constant.BULLET_SPEED;

                bullet.posX += deltaX;

                if (bullet.posX + Constant.RADIUS > Constant.WIDTH || bullet.posX - Constant.RADIUS < 0)
                {
                    delete = true;
                }

                double deltaY = Math.Sin(angle) * Constant.BULLET_SPEED;

                bullet.posY += deltaY;

                if (bullet.posY + Constant.RADIUS > Constant.HEIGHT || bullet.posY - Constant.RADIUS < 0)
                {
                    delete = true;
                }

                bullet.life--;

                if (bullet.life < 0)
                {
                    delete = true;
                }

                enumerator = unitDic.Values.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    Unit unit = enumerator.Current;

                    if (unit.id == bullet.parent)
                    {
                        continue;
                    }

                    double x = unit.posX - bullet.posX;

                    double y = unit.posY - bullet.posY;

                    if (x * x + y * y < (Constant.RADIUS + Constant.BULLET_RADIUS) * (Constant.RADIUS + Constant.BULLET_RADIUS))
                    {
                        if (hitList == null)
                        {
                            hitList = new List<int>();
                        }

                        hitList.Add(unit.id);
                    }
                }

                if (hitList != null)
                {
                    for (int i = 0; i < hitList.Count; i++)
                    {
                        unitDic.Remove(hitList[i]);
                    }
                }

                if (delete)
                {
                    if (delList == null)
                    {
                        delList = new List<int>();
                    }

                    delList.Add(bullet.id);
                }
                else
                {
                    bullet.Fix();
                }
            }

            if (delList != null)
            {
                for (int i = 0; i < delList.Count; i++)
                {
                    bulletDic.Remove(delList[i]);
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

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                int id = _br.ReadInt32();

                int parent = _br.ReadInt32();

                int life = _br.ReadInt32();

                double posX = _br.ReadDouble();

                double posY = _br.ReadDouble();

                int mouseX = _br.ReadInt32();

                int mouseY = _br.ReadInt32();

                Bullet bullet = new Bullet();

                bullet.id = id;

                bullet.parent = parent;

                bullet.life = life;

                bullet.posX = posX;

                bullet.posY = posY;

                bullet.mouseX = mouseX;

                bullet.mouseY = mouseY;

                bulletDic.Add(bullet.id, bullet);
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

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                int id = _br.ReadInt32();

                int parent = _br.ReadInt32();

                int mouseX = _br.ReadInt32();

                int mouseY = _br.ReadInt32();

                Unit unit = unitDic[parent];

                Bullet bullet = new Bullet();

                bullet.id = id;

                bullet.parent = parent;

                bullet.life = Constant.BULLET_LIFE_TICK;

                bullet.posX = unit.posX;

                bullet.posY = unit.posY;

                bullet.mouseX = mouseX;

                bullet.mouseY = mouseY;

                bulletDic.Add(bullet.id, bullet);
            }
        }
    }
}
