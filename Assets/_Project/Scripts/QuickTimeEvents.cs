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

public class PreciseClickEvent : QuickTimeEvents
{
    public Vector2 spawnLocation;
    public Vector4 bounds;
    public float radius;

    public float timeLeft;
    private float startingTime;

    public PreciseClickEvent(GameObject _player, ZombieScript _target, Canvas _can) : base(_player, _target, _can)
    {

    }

    public override void Start()
    {
        base.Update();
        startingTime = timeLeft;
    }

    public override void Update()
    {
        base.Update();
        timeLeft -= Time.deltaTime;
    }
   
}

public class SpamButtonEvent: QuickTimeEvents
{
    public KeyCode keyCode;
    public TextMeshPro keyTextMesh;
    public GameObject parent;

    public GameObject quickTimeBar;
    //public Image BackgroundBar;
    public Image HumanSide;
    public Image ZombieSide;
    public Image MiddleBar;

    public float threshold = 0.1f;
    public float coeffecient = 0.25f;

    public AnimationCurve zombieForce;
    public float fillThreshold = 0.85f;
    private float time;
    private bool down;

    private float timeElapse;
    private int numPresses;
    private float rate;
    private float maxCurveVal;

    private float maxHeight;

    public SpamButtonEvent(GameObject _player, ZombieScript _target, Canvas _can) : base (_player, _target, _can)
    {
        type = typeOfQTE.SpamButton;
        keyCode = KeyCode.A +  (int)UnityEngine.Random.Range(0.0f, 25.0f);

        quickTimeBar = target.quickTimeBar;

        parent = new GameObject("QTE");
        parent.transform.parent = quickTimeBar.transform;

        parent.transform.localEulerAngles = Vector3.zero;
        //parent.transform.localPosition = Vector3.zero;
        parent.transform.localPosition = new Vector3(0, 227, 0);
        parent.transform.localScale = new Vector3(1, 1, 1);

        keyTextMesh = parent.GetComponent<TextMeshPro>();
        if (keyTextMesh == null)
        {
            keyTextMesh = parent.AddComponent<TextMeshPro>();
        }
        else
        {
            keyTextMesh.enabled = true;
        }
        keyTextMesh.text = "" + keyCode.ToString();

        keyTextMesh.autoSizeTextContainer = true;
        keyTextMesh.fontSize = 748;
        keyTextMesh.alignment = TextAlignmentOptions.Center;
        keyTextMesh.color = new Color32(255, 0, 0, 255);

        keyTextMesh.outlineColor = new Color32(255, 255, 255, 255);
        keyTextMesh.outlineWidth = 0.2f;


        
        //GameObject instantiatedGameObj = GameObject.Instantiate(quickTimeBar, new Vector3(0, 0, 0), Quaternion.identity);
        quickTimeBar.SetActive(true);

        zombieForce = target.zombieForce;

        //RectTransform rekt = instantiatedGameObj.GetComponent<RectTransform>();
        //rekt.localScale = new Vector3(1, 1, 1);
        //rekt.eulerAngles = Vector3.zero;
        //rekt.position = new Vector3(instantiatedGameObj.transform.position.x, 0, 0);

        //quickTimeBar.transform.parent = can.transform;

        maxHeight = quickTimeBar.transform.GetComponent<RectTransform>().rect.height;

        GameObject temp = quickTimeBar.transform.GetChild(0).gameObject;
        HumanSide = temp.GetComponent<Image>();
        temp = temp.transform.GetChild(0).gameObject;
        ZombieSide = temp.GetComponent<Image>();
        temp = temp.transform.GetChild(0).gameObject;
        MiddleBar = temp.GetComponent<Image>();

        HumanSide.fillAmount = 0.5f;
        ZombieSide.fillAmount = 0.5f;

        rate = 0;

        maxCurveVal = zombieForce[zombieForce.length - 1].value;

        Start();
    }

    public override void Start()
    {
        base.Start();
        time = 0.0f;
        down = false;
        Debug.Log( keyCode);
    }

    public override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(keyCode) && !down)
        {
            timeElapse = time;
            down = true;
        }
        
        if (Input.GetKeyUp(keyCode))
        {
            rate = time - timeElapse;
            numPresses++;
            HumanSide.fillAmount += 0.01f * ((rate < 0.5f) ? 1.5f : 1) * ((rate < 0.05f) ? 2 : 1);
            down = false;
        }

        if (HumanSide.fillAmount >= fillThreshold)
        {
            Debug.Log("player won");
            target.onComplete(true);
            quickTimeBar.SetActive(false);
            keyTextMesh.enabled = false;
            return;
        }
        else if (HumanSide.fillAmount < 1 - fillThreshold)
        {
            Debug.Log("zombie won");
            target.onComplete(false);
            quickTimeBar.SetActive(false);
            keyTextMesh.enabled = false;
            return;
        }
        
        if(rate!= 0 && !down)
        {
            rate = time - timeElapse;
        }
        //* ((rate < 0.05f) ? 0.05f : 1)
        float delta = zombieForce.Evaluate(time / 4)/ maxCurveVal * Time.deltaTime * coeffecient * ((rate != 0)? 1 : 0) ;
        HumanSide.fillAmount -= delta;
        ZombieSide.fillAmount = 1 - HumanSide.fillAmount;

        MiddleBar.gameObject.transform.localPosition = new Vector3(0, maxHeight * HumanSide.fillAmount, 0);

        time += Time.deltaTime;
    }
}
