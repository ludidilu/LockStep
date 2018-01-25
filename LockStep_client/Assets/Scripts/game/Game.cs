using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Connection;
using LockStep_lib;
using tuple;
using superTween;
using System.Diagnostics;

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
    private GameObject quad;

    [SerializeField]
    private TextMesh tm;

    private const float tweenTime = 0.5f;

    private Client client;

    private int id;

    private Dictionary<int, KeyValuePair<GameObject, GameObject>> dic = new Dictionary<int, KeyValuePair<GameObject, GameObject>>();

    private Unit myUnit;

    private GameObject myGo;

    private int tweenID = -1;

    private List<Tuple<GameObject, Vector2, Vector2>> tweenList = new List<Tuple<GameObject, Vector2, Vector2>>();

    private Stopwatch watch = new Stopwatch();

    void Awake()
    {
        Connection.Log.Init(UnityEngine.Debug.Log);

        Core.Init();

        ConfigDictionary.Instance.LoadLocalConfig(Path.Combine(Application.streamingAssetsPath, "local.xml"));

        client = new Client();

        client.Init(ConfigDictionary.Instance.ip, ConfigDictionary.Instance.port, ConfigDictionary.Instance.uid, GetData, Disconnect);

        client.Connect(ConnectSuccess, ConnectFail);

        quad.transform.localScale = new Vector3((float)Constant.WIDTH, (float)Constant.HEIGHT, 1);

        quad.transform.position = new Vector3(0.5f * (float)Constant.WIDTH, 0.5f * (float)Constant.HEIGHT, quad.transform.position.z);

        downSr.gameObject.SetActive(false);

        moveSr.gameObject.SetActive(false);

        mainCamera.gameObject.SetActive(false);

        watch.Start();
    }

    private void ConnectSuccess(BinaryReader _br)
    {
        id = _br.ReadInt32();

        Core.ClientGetRefreshData(_br);
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
        }

        tweenList.Clear();

        IEnumerator<Unit> enumerator = Core.unitDic.Values.GetEnumerator();

        while (enumerator.MoveNext())
        {
            Unit unit = enumerator.Current;

            KeyValuePair<GameObject, GameObject> pair;

            if (!dic.TryGetValue(unit.id, out pair))
            {
                GameObject real = Instantiate(unitSource);

                GameObject fake = Instantiate(unitSource);

                fake.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.3f);

                dic.Add(unit.id, new KeyValuePair<GameObject, GameObject>(real, fake));

                if (unit.id == id)
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



                Vector2 tv = new Vector2((float)unit.posX, (float)unit.posY);

                Vector2 v;

                float dist = Vector2.Distance(tv, real.transform.position);

                if (dist < Constant.MAX_SPEED)
                {
                    v = tv;
                }
                else
                {
                    v = Vector2.Lerp(real.transform.position, tv, (float)Constant.MAX_SPEED / dist);
                }

                real.transform.position = new Vector3(v.x, v.y, 0);

                if (unit.mouseX != 0 || unit.mouseY != 0)
                {
                    float angle = Mathf.Atan2(unit.mouseY, unit.mouseX);

                    float dis = Mathf.Sqrt(unit.mouseX * unit.mouseX + unit.mouseY * unit.mouseY) / Constant.MAX_MOUSE_DISTANCE * (float)Constant.MAX_SPEED * tweenTime * 1000 / Constant.TICK_TIME;

                    float deltaX = Mathf.Cos(angle) * dis;

                    float resultX = (float)unit.posX + deltaX;

                    float deltaY = Mathf.Sin(angle) * dis;

                    float resultY = (float)unit.posY + deltaY;

                    Tuple<GameObject, Vector2, Vector2> t = new Tuple<GameObject, Vector2, Vector2>(real, real.transform.position, new Vector2(resultX, resultY));

                    tweenList.Add(t);

                    tweenID = SuperTween.Instance.To(0, 1, tweenTime, TweenTo, TweenOver);
                }
                else
                {

                }
            }
        }

        if (!mainCamera.gameObject.activeSelf)
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
            Tuple<GameObject, Vector2, Vector2> t = tweenList[i];

            Vector2 v = Vector2.Lerp(t.second, t.third, _v);

            if (v.x > Constant.WIDTH)
            {
                v.x = (float)Constant.WIDTH;
            }
            else if (v.x < 0)
            {
                v.x = 0;
            }

            if (v.y > Constant.WIDTH)
            {
                v.y = (float)Constant.WIDTH;
            }
            else if (v.y < 0)
            {
                v.y = 0;
            }

            t.first.transform.position = new Vector3(v.x, v.y, t.first.transform.position.z);
        }
    }

    private void TweenOver()
    {
        tweenID = -1;
    }

    private void Disconnect()
    {

    }

    private Vector2 downPos;

    private float deltaTime;

    // Update is called once per frame
    void Update()
    {
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

                Vector2 v = uiCamera.ScreenToWorldPoint(downPos);

                downSr.gameObject.SetActive(true);

                moveSr.gameObject.SetActive(true);

                downSr.transform.position = new Vector3(v.x, v.y, downSr.transform.position.z);

                moveSr.transform.position = new Vector3(v.x, v.y, moveSr.transform.position.z);
            }
            else if (Input.GetMouseButton(0))
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

                            bw.Write(mouseX);

                            bw.Write(mouseY);

                            client.Send(ms);
                        }
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                downSr.gameObject.SetActive(false);

                moveSr.gameObject.SetActive(false);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write((byte)0);

                        bw.Write(0);

                        bw.Write(0);

                        client.Send(ms);
                    }
                }
            }
        }
    }

    private int m_pid;

    private int GetPid()
    {
        m_pid++;

        return m_pid;
    }
}
