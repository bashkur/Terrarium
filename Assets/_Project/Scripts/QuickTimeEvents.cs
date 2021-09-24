using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickTimeEvents
{
    public bool activated;
    public GameObject player;
    public ZombieScript target;
    public Canvas can;
    public typeOfQTE type;

    public QuickTimeEvents(GameObject _player, ZombieScript _target, Canvas _can) { player = _player; target = _target; can = _can; }

    virtual public void Start() { }
    virtual public void Update() { }
}

public enum typeOfQTE
{
    SpamButton,
    PreciseClick
};

public class SpamButtonEvent: QuickTimeEvents
{
    public KeyCode keyCode;
    public TextMeshPro keyTextMesh;
    public GameObject parent;

    public GameObject quickTimeBar;
    //public Image BackgroundBar;
    public Image HumanSide;
    public Image ZombieSide;

    public float threshold = 0.1f;

    public AnimationCurve zombieForce;
    public float fillThreshold = 0.95f;
    private float time;
    private bool down;

    private float timeElapse;
    private int numPresses;
    private float rate;

    public SpamButtonEvent(GameObject _player, ZombieScript _target, Canvas _can) : base (_player, _target, _can)
    {
        type = typeOfQTE.SpamButton;
        keyCode = KeyCode.A +  (int)UnityEngine.Random.Range(0.0f, 25.0f);

        parent = new GameObject("QTE");
        parent.transform.parent = can.transform;

        parent.transform.localEulerAngles = Vector3.zero;
        parent.transform.localPosition = Vector3.zero;
        parent.transform.localScale = new Vector3(1, 1, 1);

        keyTextMesh = parent.AddComponent<TextMeshPro>();
        keyTextMesh.text = "" + keyCode.ToString();

        keyTextMesh.autoSizeTextContainer = true;
        keyTextMesh.fontSize = 748;
        keyTextMesh.alignment = TextAlignmentOptions.Center;
        keyTextMesh.color = new Color32(255, 0, 0, 255);

        keyTextMesh.outlineColor = new Color32(255, 255, 255, 255);
        keyTextMesh.outlineWidth = 0.2f;


        quickTimeBar = target.quickTimeBar;
        //GameObject instantiatedGameObj = GameObject.Instantiate(quickTimeBar, new Vector3(0, 0, 0), Quaternion.identity);
        quickTimeBar.SetActive(true);

        zombieForce = target.zombieForce;

        //RectTransform rekt = instantiatedGameObj.GetComponent<RectTransform>();
        //rekt.localScale = new Vector3(1, 1, 1);
        //rekt.eulerAngles = Vector3.zero;
        //rekt.position = new Vector3(instantiatedGameObj.transform.position.x, 0, 0);

        //quickTimeBar.transform.parent = can.transform;

        GameObject temp = quickTimeBar.transform.GetChild(0).gameObject;
        HumanSide = temp.GetComponent<Image>();
        temp = temp.transform.GetChild(0).gameObject;
        ZombieSide = temp.GetComponent<Image>();

        HumanSide.fillAmount = 0.5f;
        ZombieSide.fillAmount = 0.5f;

        rate = 1;

        Start();
    }

    public override void Start()
    {
        time = 0.0f;
        down = false;
        Debug.Log( keyCode);
    }

    public override void Update()
    {
        if (Input.GetKeyDown(keyCode) && !down)
        {
            timeElapse = time;
            down = true;
        }
        
        if (Input.GetKeyUp(keyCode))
        {
            rate = time - timeElapse;
            numPresses++;
            HumanSide.fillAmount += 0.01f;
            down = false;
        }

        if (HumanSide.fillAmount >= fillThreshold)
        {
            Debug.Log("player won");
            target.onComplete(true);
            quickTimeBar.SetActive(false);
            return;
        }
        else if (HumanSide.fillAmount < 1 - fillThreshold)
        {
            Debug.Log("zombie won");
            target.onComplete(false);
            quickTimeBar.SetActive(false);
            return;
        }
        //((rate < 1.0f)? rate : 1)
        float delta = zombieForce.Evaluate(time) * Time.deltaTime * rate;

        if (ZombieSide.fillAmount < threshold)
        {
            delta = delta * 2.0f;
        }

        HumanSide.fillAmount -= delta;
        ZombieSide.fillAmount = 1 - HumanSide.fillAmount;
        time += Time.deltaTime;
    }
}
