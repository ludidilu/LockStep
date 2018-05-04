using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Connection;
using LockStep_lib;
using tuple;
using superTween;
using System.Diagnostics;
using wwwManager;

public class Game : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer downSr;

    [SerializeField]
    private SpriteRenderer moveSr;

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private Camera uiCamera;

    [SerializeField]
    private GameObject unitSource;

    [SerializeField]
    private GameObject bulletSource;

    [SerializeField]
    private GameObject quad;

    [SerializeField]
    private TextMesh tm;

    [SerializeField]
    private float clickTime;

    private const float tweenTime = 0.5f;

    private Client client;

    //private ClientUdp client;

    private Dictionary<int, KeyValuePair<GameObject, GameObject>> dic = new Dictionary<int, KeyValuePair<GameObject, GameObject>>();

    private Dictionary<int, GameObject> bulletDic = new Dictionary<int, GameObject>();

    private Unit myUnit;

    private GameObject myGo;

    private int tweenID = -1;

    private List<Tuple<GameObject, Vector2, Vector2, bool>> tweenList = new List<Tuple<GameObject, Vector2, Vector2, bool>>();

    private Stopwatch watch = new Stopwatch();

    void Awake()
    {
        Connection.Log.Init(UnityEngine.Debug.Log);

        Core.Init();

        WWWManager.Instance.Load("local.xml", GetConfig);
    }

    private void GetConfig(WWW _www)
    {
        ConfigDictionary.Instance.SetData(_www.text);

        client = new Client();

        client.Init(ConfigDictionary.Instance.ip, ConfigDictionary.Instance.port, GetData, Disconnect);

        client.Connect(ConnectOver);

        //client = new ClientUdp();

        //client.Init(ConfigDictionary.Instance.ip, ConfigDictionary.Instance.port, ConfigDictionary.Instance.localPort, GetData);

        //ConnectOver(true);

        quad.transform.localScale = new Vector3((float)Constant.WIDTH, (float)Constant.HEIGHT, 1);

        quad.transform.position = new Vector3(0.5f * (float)Constant.WIDTH, 0.5f * (float)Constant.HEIGHT, quad.transform.position.z);

        downSr.gameObject.SetActive(false);

        moveSr.gameObject.SetActive(false);

        mainCamera.gameObject.SetActive(false);

        watch.Start();
    }

    private void ConnectOver(bool _b)
    {
        if (_b)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(ConfigDictionary.Instance.uid);

                    client.Send(ms, Core.ClientGetRefreshData);
                }
            }
        }
    }

    private void OnDestroy()
    {
        client.Close();
    }

    private void ConnectFail()
    {

    }

    private void GetData(BinaryReader _br)
    {
        byte type = _br.ReadByte();

        if (type == 0)
        {
            GetBattleData(_br);
        }
        else if (type == 1)
        {
            GetPingData(_br);
        }
    }

    private void GetPingData(BinaryReader _br)
    {
        long t = _br.ReadInt64();

        t = watch.ElapsedMilliseconds - t;

        tm.text = t.ToString();
    }

    private void GetBattleData(BinaryReader _br)
    {
        Core.ClientGetRefreshCommand(_br);

        Core.Update();

        if (tweenID != -1)
        {
            SuperTween.Instance.Remove(tweenID);

            tweenID = -1;

            tweenList.Clear();
        }

        IEnumerator<Unit> enumerator = Core.unitDic.Values.GetEnumerator();

        while (enumerator.MoveNext())
        {
            Unit unit = enumerator.Current;

            KeyValuePair<GameObject, GameObject> pair;

            if (!dic.TryGetValue(unit.id, out pair))
            {
                GameObject real = Instantiate(unitSource);

                real.transform.localScale = new Vector3((float)Constant.RADIUS * 2, (float)Constant.RADIUS * 2, 1);

                GameObject fake = Instantiate(unitSource);

                fake.transform.localScale = new Vector3((float)Constant.RADIUS * 2, (float)Constant.RADIUS * 2, 1);

                fake.SetActive(false);

                fake.GetComponent<Renderer>().material.SetFloat("_Alpha", 0.3f);

                dic.Add(unit.id, new KeyValuePair<GameObject, GameObject>(real, fake));

                if (unit.id == ConfigDictionary.Instance.uid)
                {
                    myUnit = unit;

                    myGo = real;
                }

                real.transform.position = new Vector3((float)unit.posX, (float)unit.posY, 0);

                fake.transform.position = new Vector3((float)unit.posX, (float)unit.posY, 100);
            }
            else
            {
                GameObject real = pair.Key;

                GameObject fake = pair.Value;

                fake.transform.position = new Vector3((float)unit.posX, (float)unit.posY, 100);

                if (unit.mouseX != 0 || unit.mouseY != 0)
                {
                    float angle = Mathf.Atan2(unit.mouseY, unit.mouseX);

                    float dis = Mathf.Sqrt(unit.mouseX * unit.mouseX + unit.mouseY * unit.mouseY) / Constant.MAX_MOUSE_DISTANCE * (float)Constant.MAX_SPEED * tweenTime * 1000 / Constant.TICK_TIME;

                    float deltaX = Mathf.Cos(angle) * dis;

                    float resultX = (float)unit.posX + deltaX;

                    float deltaY = Mathf.Sin(angle) * dis;

                    float resultY = (float)unit.posY + deltaY;



                    Tuple<GameObject, Vector2, Vector2, bool> t = new Tuple<GameObject, Vector2, Vector2, bool>(real, real.transform.position, new Vector2(resultX, resultY), true);

                    tweenList.Add(t);
                }
                else
                {
                    if (real.transform.position.x != (float)unit.posX || real.transform.position.y != (float)unit.posY)
                    {
                        Tuple<GameObject, Vector2, Vector2, bool> t = new Tuple<GameObject, Vector2, Vector2, bool>(real, real.transform.position, new Vector2((float)unit.posX, (float)unit.posY), true);

                        tweenList.Add(t);
                    }
                }
            }
        }

        List<int> delList = null;

        IEnumerator<KeyValuePair<int, KeyValuePair<GameObject, GameObject>>> enumerator4 = dic.GetEnumerator();

        while (enumerator4.MoveNext())
        {
            if (!Core.unitDic.ContainsKey(enumerator4.Current.Key))
            {
                if (delList == null)
                {
                    delList = new List<int>();
                }

                delList = new List<int>();

                delList.Add(enumerator4.Current.Key);
            }
        }

        if (delList != null)
        {
            for (int i = 0; i < delList.Count; i++)
            {
                int id = delList[i];

                KeyValuePair<GameObject, GameObject> pair = dic[id];

                dic.Remove(id);

                Destroy(pair.Key);

                Destroy(pair.Value);
            }
        }


        IEnumerator<Bullet> enumerator2 = Core.bulletDic.Values.GetEnumerator();

        while (enumerator2.MoveNext())
        {
            Bullet bullet = enumerator2.Current;

            GameObject go;

            if (!bulletDic.TryGetValue(bullet.id, out go))
            {
                go = Instantiate(bulletSource);

                go.transform.localScale = new Vector3((float)Constant.BULLET_RADIUS * 2, (float)Constant.BULLET_RADIUS * 2, 1);

                bulletDic.Add(bullet.id, go);

                go.transform.position = new Vector3((float)bullet.posX, (float)bullet.posY, 0);
            }

            float angle = Mathf.Atan2(bullet.mouseY, bullet.mouseX);

            float dis = (float)Constant.BULLET_SPEED * tweenTime * 1000 / Constant.TICK_TIME;

            float deltaX = Mathf.Cos(angle) * dis;

            float resultX = (float)bullet.posX + deltaX;

            float deltaY = Mathf.Sin(angle) * dis;

            float resultY = (float)bullet.posY + deltaY;

            Tuple<GameObject, Vector2, Vector2, bool> t = new Tuple<GameObject, Vector2, Vector2, bool>(go, go.transform.position, new Vector2(resultX, resultY), false);

            tweenList.Add(t);
        }

        delList = null;

        IEnumerator<KeyValuePair<int, GameObject>> enumerator3 = bulletDic.GetEnumerator();

        while (enumerator3.MoveNext())
        {
            if (!Core.bulletDic.ContainsKey(enumerator3.Current.Key))
            {
                if (delList == null)
                {
                    delList = new List<int>();
                }

                delList.Add(enumerator3.Current.Key);
            }
        }

        if (delList != null)
        {
            for (int i = 0; i < delList.Count; i++)
            {
                int id = delList[i];

                GameObject go = bulletDic[id];

                bulletDic.Remove(id);

                Destroy(go);
            }
        }

        if (tweenList.Count > 0)
        {
            tweenID = SuperTween.Instance.To(0, 1, tweenTime, TweenTo, TweenOver);
        }

        if (mainCamera != null && !mainCamera.gameObject.activeSelf)
        {
            mainCamera.gameObject.SetActive(true);

            mainCamera.transform.SetParent(myGo.transform, false);

            mainCamera.transform.localPosition = new Vector3(0, 0, -1000);
        }
    }

    private void TweenTo(float _v)
    {
        for (int i = 0; i < tweenList.Count; i++)
        {
            Tuple<GameObject, Vector2, Vector2, bool> t = tweenList[i];

            Vector2 v = Vector2.Lerp(t.second, t.third, _v);

            double radius = t.fourth ? Constant.RADIUS : Constant.BULLET_RADIUS;

            if (v.x + radius > Constant.WIDTH)
            {
                v.x = (float)(Constant.WIDTH - radius);
            }
            else if (v.x - radius < 0)
            {
                v.x = (float)radius;
            }

            if (v.y + radius > Constant.WIDTH)
            {
                v.y = (float)(Constant.HEIGHT - radius);
            }
            else if (v.y - radius < 0)
            {
                v.y = (float)radius;
            }

            t.first.transform.position = new Vector3(v.x, v.y, t.first.transform.position.z);
        }
    }

    private void TweenOver()
    {
        tweenID = -1;

        tweenList.Clear();
    }

    private void Disconnect()
    {

    }

    private bool moveBegin = false;

    private float downTime;

    private Vector2 downPos;

    private float deltaTime;

    // Update is called once per frame
    void Update()
    {
        if (client == null)
        {
            return;
        }

        client.Update();

        if (myUnit != null)
        {
            deltaTime += Time.deltaTime;

            if (deltaTime > 0.5f)
            {
                deltaTime = 0;

                using (MemoryStream ms2 = new MemoryStream())
                {
                    using (BinaryWriter bw2 = new BinaryWriter(ms2))
                    {
                        bw2.Write((byte)1);

                        long t = watch.ElapsedMilliseconds;

                        bw2.Write(t);

                        client.Send(ms2);
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                downPos = Input.mousePosition;

                downTime = Time.time;
            }
            else if (Input.GetMouseButton(0))
            {
                if (moveBegin)
                {
                    Move();
                }
                else if (Time.time - downTime > clickTime)
                {
                    moveBegin = true;

                    MoveDown();
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (moveBegin)
                {
                    MoveUp();

                    moveBegin = false;
                }
                else
                {
                    Shoot();
                }
            }
        }
    }

    private void MoveDown()
    {
        Vector2 v = uiCamera.ScreenToWorldPoint(downPos);

        downSr.gameObject.SetActive(true);

        moveSr.gameObject.SetActive(true);

        downSr.transform.position = new Vector3(v.x, v.y, downSr.transform.position.z);

        moveSr.transform.position = new Vector3(v.x, v.y, moveSr.transform.position.z);

        Move();
    }

    private void Move()
    {
        Vector2 pos = Input.mousePosition;

        float dis = Vector2.Distance(downPos, pos);

        if (dis > Constant.MAX_MOUSE_DISTANCE)
        {
            pos = Vector2.Lerp(downPos, pos, Constant.MAX_MOUSE_DISTANCE / dis);
        }

        Vector2 v = uiCamera.ScreenToWorldPoint(pos);

        moveSr.transform.position = new Vector3(v.x, v.y, moveSr.transform.position.z);

        int mouseX = (int)Mathf.Round(pos.x) - (int)downPos.x;

        int mouseY = (int)Mathf.Round(pos.y) - (int)downPos.y;

        if (mouseX != myUnit.mouseX || mouseY != myUnit.mouseY)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((byte)0);

                    bw.Write((byte)0);

                    bw.Write(mouseX);

                    bw.Write(mouseY);

                    client.Send(ms);
                }
            }
        }
    }

    private void MoveUp()
    {
        downSr.gameObject.SetActive(false);

        moveSr.gameObject.SetActive(false);

        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((byte)0);

                bw.Write((byte)0);

                bw.Write(0);

                bw.Write(0);

                client.Send(ms);
            }
        }
    }

    private void Shoot()
    {
        int mouseX = (int)Input.mousePosition.x - Screen.width / 2;

        int mouseY = (int)Input.mousePosition.y - Screen.height / 2;

        if (mouseX == 0 && mouseY == 0)
        {
            return;
        }

        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((byte)0);

                bw.Write((byte)1);

                bw.Write(mouseX);

                bw.Write(mouseY);

                client.Send(ms);
            }
        }
    }
}
